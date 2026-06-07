# Falabella ETL — DataFlow Platform

Plataforma de integración de datos (ETL) desarrollada con .NET 10, Angular 18, Python FastAPI y Google BigQuery. Permite conectar bases de datos externas, transformar datos y cargar en BigQuery para análisis en tiempo real.

---

##  Tecnologías

| Capa | Tecnología |
|------|-----------|
| Backend API | .NET 10, Entity Framework Core, SQL Server |
| Frontend | Angular 18, Angular Material, Chart.js |
| Analytics | Python 3.14, FastAPI, pandas, BigQuery |
| Base de datos operacional | SQL Server 2022 Express |
| Data warehouse | Google BigQuery (GCP) |
| Autenticación | JWT Bearer Tokens |

---

## Requisitos previos

Instalar antes de comenzar:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org)
- [Python 3.11+](https://www.python.org/downloads)
- [SQL Server 2022 Express](https://www.microsoft.com/sql-server/sql-server-downloads)
- [Google Cloud SDK](https://cloud.google.com/sdk/docs/install)
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`

---

## ⚙️ Instalación

### 1. Clonar el repositorio

```bash
git clone https://github.com/Robin1-afk/Falabella-ETL.git
cd Falabella-ETL
```

### 2. Base de datos — SQL Server

Ejecutar el schema para crear todas las tablas:

```bash
sqlcmd -S .\SQLEXPRESS -E -i backend/database/schema.sql
```

### 3. Backend — .NET

```bash
cd backend
dotnet restore
dotnet run --project DataFlowPlatform.API
```

La API queda disponible en: `http://localhost:5228`

> **Credenciales por defecto:**
> - Email: `admin@dataflowplatform.com`
> - Password: `Admin123!`

### 4. Frontend — Angular

```bash
cd frontend
npm install
ng serve
```

El frontend queda disponible en: `http://localhost:4200`

### 5. Python — FastAPI Analytics

```bash
cd python

# Instalar dependencias
pip install -r requirements.txt

# Autenticarse con Google Cloud (solo la primera vez)
gcloud auth application-default login

# Iniciar el servidor
uvicorn main:app --port 8000 --reload
```

El microservicio queda disponible en: `http://localhost:8000`
Documentación automática: `http://localhost:8000/docs`

---

## Cómo usar

### Dashboard
Resumen general: total de pipelines, registros cargados, última ejecución y gráfica de cargas.

### Pipelines
Conecta una base de datos origen (MySQL o SQL Server), selecciona la tabla, configura filtros y mapeo de columnas, y ejecuta el pipeline. Los datos se cargan en SQL Server y se envían automáticamente a BigQuery.

### Analytics
Dashboard analítico con datos de BigQuery. Filtra por país, sede, categoría y rango de fechas. Incluye gráficas de barras, línea temporal y distribución por categoría. Calculado con pandas desde Python.

### Auditoría
Historial completo de todas las ejecuciones de pipelines: registros exitosos, errores, tiempo de ejecución y usuario.

---

## Estructura del proyecto
Falabella-ETL/
├── backend/                    # API REST en .NET 10
│   ├── DataFlowPlatform.API/   # Controllers, DTOs, Middleware
│   ├── DataFlowPlatform.Domain/        # Entidades del dominio
│   └── DataFlowPlatform.Infrastructure/ # EF Core, JWT, BigQuery, ETL
├── frontend/                   # Angular 18
│   └── src/app/
│       ├── core/               # Auth, interceptors, guards
│       ├── pages/              # Dashboard, Pipelines, Analytics, Audit
│       └── shared/             # Sidebar, Header, Layout
└── python/                     # Microservicio de analytics
├── main.py                 # FastAPI endpoints
├── analytics.py            # Cálculos con pandas
└── database.py             # Conexión BigQuery y SQL Server

---

## Variables de entorno

El archivo `python/.env` no se incluye en el repositorio. Crear uno con:

```env
BQ_PROJECT=tu-project-id-de-google-cloud
BQ_DATASET=dataflow_platform
DB_SERVER=localhost\SQLEXPRESS
DB_DATABASE=DataFlowPlatform
```

---

## Arquitectura
Angular (4200)
├── → .NET API (5228)      Auth, ETL, Pipelines, Auditoría
└── → Python FastAPI (8000) Analytics desde BigQuery
.NET API
├── → SQL Server            BD operacional
└── → BigQuery              Datos procesados por ETL
Python FastAPI
└── → BigQuery              Consultas analíticas con pandas

