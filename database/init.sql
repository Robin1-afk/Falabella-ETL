-- Esquema completo de DataFlowPlatform — se ejecuta automáticamente al iniciar el contenedor.
-- Todos los bloques usan IF NOT EXISTS para ser idempotentes en reinicios.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DataFlowPlatform')
    CREATE DATABASE DataFlowPlatform;
GO

USE DataFlowPlatform;
GO

-- ── roles ─────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'roles')
BEGIN
    CREATE TABLE roles (
        id        INT           NOT NULL IDENTITY(1,1),
        name      NVARCHAR(100) NOT NULL,
        is_active BIT           NOT NULL DEFAULT 1,
        CONSTRAINT PK_roles      PRIMARY KEY (id),
        CONSTRAINT UQ_roles_name UNIQUE      (name)
    );
END
GO

-- ── views ─────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'views')
BEGIN
    CREATE TABLE views (
        id          INT           NOT NULL IDENTITY(1,1),
        name        NVARCHAR(150) NOT NULL,
        description NVARCHAR(500)     NULL,
        is_active   BIT           NOT NULL DEFAULT 1,
        CONSTRAINT PK_views      PRIMARY KEY (id),
        CONSTRAINT UQ_views_name UNIQUE      (name)
    );
END
GO

-- ── users ─────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'users')
BEGIN
    CREATE TABLE users (
        id        INT           NOT NULL IDENTITY(1,1),
        name      NVARCHAR(150) NOT NULL,
        email     NVARCHAR(250) NOT NULL,
        password  NVARCHAR(500) NOT NULL,
        role_id   INT           NOT NULL,
        is_active BIT           NOT NULL DEFAULT 1,
        CONSTRAINT PK_users       PRIMARY KEY (id),
        CONSTRAINT UQ_users_email UNIQUE      (email),
        CONSTRAINT FK_users_role  FOREIGN KEY (role_id)
            REFERENCES roles (id) ON DELETE NO ACTION
    );
END
GO

-- ── user_sessions ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'user_sessions')
BEGIN
    CREATE TABLE user_sessions (
        id         INT            NOT NULL IDENTITY(1,1),
        user_id    INT            NOT NULL,
        token      NVARCHAR(1000) NOT NULL,
        -- Columna computada PERSISTED para indexar sin superar el límite de 1700 bytes de índice
        token_hash AS (CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', token), 2)) PERSISTED,
        is_active  BIT            NOT NULL DEFAULT 1,
        created_at DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_user_sessions      PRIMARY KEY (id),
        CONSTRAINT FK_user_sessions_user FOREIGN KEY (user_id)
            REFERENCES users (id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_user_sessions_token_hash ON user_sessions (token_hash);
END
GO

-- ── role_views ────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'role_views')
BEGIN
    CREATE TABLE role_views (
        id        INT NOT NULL IDENTITY(1,1),
        role_id   INT NOT NULL,
        view_id   INT NOT NULL,
        is_active BIT NOT NULL DEFAULT 1,
        CONSTRAINT PK_role_views       PRIMARY KEY (id),
        CONSTRAINT UQ_role_views       UNIQUE      (role_id, view_id),
        CONSTRAINT FK_role_views_role  FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE NO ACTION,
        CONSTRAINT FK_role_views_view  FOREIGN KEY (view_id) REFERENCES views (id) ON DELETE NO ACTION
    );
END
GO

-- ── pipelines ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'pipelines')
BEGIN
    CREATE TABLE pipelines (
        id          INT            NOT NULL IDENTITY(1,1),
        name        NVARCHAR(200)  NOT NULL,
        description NVARCHAR(1000)     NULL,
        source_type NVARCHAR(100)  NOT NULL,   -- CSV | Excel | API | Database
        schedule    NVARCHAR(100)      NULL,   -- expresión CRON; NULL = solo manual
        status      NVARCHAR(50)   NOT NULL DEFAULT 'Active',  -- Active | Inactive | Archived
        created_by  INT            NOT NULL,
        created_at  DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_pipelines    PRIMARY KEY (id),
        CONSTRAINT FK_pipelines_user FOREIGN KEY (created_by)
            REFERENCES users (id) ON DELETE NO ACTION
    );
END
GO

-- ── pipeline_executions ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'pipeline_executions')
BEGIN
    CREATE TABLE pipeline_executions (
        id           INT          NOT NULL IDENTITY(1,1),
        pipeline_id  INT          NOT NULL,
        status       NVARCHAR(50) NOT NULL,   -- Pending | Running | Completed | Failed
        total_rows   INT          NOT NULL DEFAULT 0,
        success_rows INT          NOT NULL DEFAULT 0,
        error_rows   INT          NOT NULL DEFAULT 0,
        executed_by  INT          NOT NULL,
        executed_at  DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
        finished_at  DATETIME2        NULL,   -- NULL mientras la ejecución está en curso
        CONSTRAINT PK_pipeline_executions PRIMARY KEY (id),
        CONSTRAINT FK_pe_pipeline FOREIGN KEY (pipeline_id)
            REFERENCES pipelines (id) ON DELETE NO ACTION,
        CONSTRAINT FK_pe_user     FOREIGN KEY (executed_by)
            REFERENCES users (id)     ON DELETE NO ACTION
    );
END
GO

-- ── etl_audit_logs ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'etl_audit_logs')
BEGIN
    CREATE TABLE etl_audit_logs (
        id           INT            NOT NULL IDENTITY(1,1),
        pipeline_id  INT            NOT NULL,
        file_name    NVARCHAR(500)  NOT NULL,
        total_rows   INT            NOT NULL DEFAULT 0,
        success_rows INT            NOT NULL DEFAULT 0,
        error_rows   INT            NOT NULL DEFAULT 0,
        metadata     NVARCHAR(MAX)      NULL,   -- JSON libre con detalles de errores por fila
        executed_by  INT            NOT NULL,
        executed_at  DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_etl_audit_logs PRIMARY KEY (id),
        CONSTRAINT FK_eal_pipeline   FOREIGN KEY (pipeline_id)
            REFERENCES pipelines (id) ON DELETE NO ACTION,
        CONSTRAINT FK_eal_user       FOREIGN KEY (executed_by)
            REFERENCES users (id)     ON DELETE NO ACTION
    );
END
GO

-- ── pipeline_data ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'pipeline_data')
BEGIN
    CREATE TABLE pipeline_data (
        id          BIGINT        NOT NULL IDENTITY(1,1),   -- BIGINT para volúmenes altos
        pipeline_id INT           NOT NULL,
        data        NVARCHAR(MAX) NOT NULL,                 -- fila CSV serializada como JSON
        loaded_at   DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_pipeline_data          PRIMARY KEY (id),
        CONSTRAINT FK_pipeline_data_pipeline FOREIGN KEY (pipeline_id)
            REFERENCES pipelines (id) ON DELETE NO ACTION
    );
END
GO

-- ── Datos semilla ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Administrador')
    INSERT INTO roles (name) VALUES ('Administrador');
IF NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Operador')
    INSERT INTO roles (name) VALUES ('Operador');
GO

IF NOT EXISTS (SELECT 1 FROM views WHERE name = 'Dashboard')
    INSERT INTO views (name, description) VALUES ('Dashboard',  'Panel principal de métricas');
IF NOT EXISTS (SELECT 1 FROM views WHERE name = 'Pipelines')
    INSERT INTO views (name, description) VALUES ('Pipelines',  'Gestión de pipelines ETL');
IF NOT EXISTS (SELECT 1 FROM views WHERE name = 'Auditoría')
    INSERT INTO views (name, description) VALUES ('Auditoría',  'Registro de auditoría ETL');
IF NOT EXISTS (SELECT 1 FROM views WHERE name = 'Usuarios')
    INSERT INTO views (name, description) VALUES ('Usuarios',   'Gestión de usuarios y roles');
IF NOT EXISTS (SELECT 1 FROM views WHERE name = 'Analytics')
    INSERT INTO views (name, description) VALUES ('Analytics',  'Análisis desde BigQuery');
GO

-- Administrador: acceso total a todas las vistas
INSERT INTO role_views (role_id, view_id)
SELECT r.id, v.id
FROM   roles r CROSS JOIN views v
WHERE  r.name = 'Administrador'
  AND  NOT EXISTS (
           SELECT 1 FROM role_views rv WHERE rv.role_id = r.id AND rv.view_id = v.id
       );
GO

-- Operador: acceso solo a Dashboard, Pipelines y Auditoría
INSERT INTO role_views (role_id, view_id)
SELECT r.id, v.id
FROM   roles r CROSS JOIN views v
WHERE  r.name = 'Operador'
  AND  v.name IN ('Dashboard', 'Pipelines', 'Auditoría')
  AND  NOT EXISTS (
           SELECT 1 FROM role_views rv WHERE rv.role_id = r.id AND rv.view_id = v.id
       );
GO

-- Usuario admin por defecto
-- Contraseña: password  (hash BCrypt costo 10 — cambiar antes de pasar a producción)
IF NOT EXISTS (SELECT 1 FROM users WHERE email = 'admin@dataflow.local')
    INSERT INTO users (name, email, password, role_id)
    SELECT 'Administrador',
           'admin@dataflow.local',
           '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi',
           id
    FROM   roles
    WHERE  name = 'Administrador';
GO
