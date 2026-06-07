using DataFlowPlatform.API.DTOs.Connections;
using DataFlowPlatform.API.DTOs.Pipelines;
using DataFlowPlatform.Infrastructure.Persistence;
using DataFlowPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data.Common;
using System.Text.Json;
using EtlAuditLogEntity      = DataFlowPlatform.Domain.Entities.Pipeline.EtlAuditLog;
using PipelineDataEntity     = DataFlowPlatform.Domain.Entities.Pipeline.PipelineData;
using PipelineExecutionEntity = DataFlowPlatform.Domain.Entities.Pipeline.PipelineExecution;

namespace DataFlowPlatform.API.Controllers;

[ApiController]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly DataFlowPlatformDbContext    _context;
    private readonly IConfiguration              _config;
    private readonly BigQueryService             _bigQuery;
    private readonly ILogger<ConnectionsController> _logger;

    public ConnectionsController(
        DataFlowPlatformDbContext       context,
        IConfiguration                 config,
        BigQueryService                bigQuery,
        ILogger<ConnectionsController> logger)
    {
        _context  = context;
        _config   = config;
        _bigQuery = bigQuery;
        _logger   = logger;
    }

    // ── POST /connections/test ──────────────────────────────────────────────────
    [HttpPost("connections/test")]
    public async Task<IActionResult> Test([FromBody] ConnectionRequestDto dto)
    {
        try
        {
            await using var conn = OpenConnection(dto.Type, dto.Host, dto.Port, dto.Database, dto.User, dto.Password, dto.WinAuth);
            await conn.OpenAsync();
            var tables = await GetTablesAsync(conn, dto.Type);
            return Ok(new ConnectionTestResponseDto(true, "Conexión exitosa.", tables));
        }
        catch (Exception ex)
        {
            return Ok(new ConnectionTestResponseDto(false, CleanError(ex.Message), []));
        }
    }

    // ── POST /connections/preview ───────────────────────────────────────────────
    [HttpPost("connections/preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewRequestDto dto)
    {
        try
        {
            await using var conn = OpenConnection(dto.Type, dto.Host, dto.Port, dto.Database, dto.User, dto.Password, dto.WinAuth);
            await conn.OpenAsync();

            // Obtiene columnas con tipo desde INFORMATION_SCHEMA (evita SELECT * ciego)
            var columns = await GetColumnsInfoAsync(conn, dto.Type, dto.Database, dto.Table);
            if (columns.Count == 0)
                return BadRequest(new { message = $"Tabla '{dto.Table}' no encontrada o no tiene columnas." });

            // Extrae las primeras 10 filas (ignoramos la lista de nombres del reader)
            var (_, rows) = await ReadRowsAsync(conn, dto.Type, dto.Table, limit: 10);
            return Ok(new PreviewResponseDto(columns, rows));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al obtener vista previa: {CleanError(ex.Message)}" });
        }
    }

    // ── POST /connections/columns ───────────────────────────────────────────────
    // Devuelve solo las columnas de una tabla (sin cargar filas)
    [HttpPost("connections/columns")]
    public async Task<IActionResult> GetColumns([FromBody] PreviewRequestDto dto)
    {
        try
        {
            await using var conn = OpenConnection(dto.Type, dto.Host, dto.Port, dto.Database, dto.User, dto.Password, dto.WinAuth);
            await conn.OpenAsync();

            var tables = await GetTablesAsync(conn, dto.Type);
            if (!tables.Contains(dto.Table, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Tabla '{dto.Table}' no encontrada." });

            // LIMIT 0 / TOP 0 devuelve el schema sin datos
            var quoted = QuoteIdentifier(dto.Table, dto.Type);
            var sql = dto.Type.ToLower() == "mysql"
                ? $"SELECT * FROM {quoted} LIMIT 0"
                : $"SELECT TOP 0 * FROM {quoted}";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            var columns = new List<string>();
            await using var reader = await cmd.ExecuteReaderAsync();
            for (var i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            return Ok(columns);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = CleanError(ex.Message) });
        }
    }

    // ── GET /connections/internal/tables ───────────────────────────────────────
    [HttpGet("connections/internal/tables")]
    public async Task<IActionResult> GetInternalTables()
    {
        var connStr = _config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        var tables = await GetTablesAsync(conn, "sqlserver");
        return Ok(tables);
    }

    // ── GET /connections/internal/columns/{table} ───────────────────────────────
    [HttpGet("connections/internal/columns/{table}")]
    public async Task<IActionResult> GetInternalColumns(string table)
    {
        var connStr = _config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var tables = await GetTablesAsync(conn, "sqlserver");
        if (!tables.Contains(table, StringComparer.OrdinalIgnoreCase))
            return NotFound(new { message = $"Tabla '{table}' no encontrada." });

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t ORDER BY ORDINAL_POSITION";
        cmd.Parameters.AddWithValue("@t", table);

        var columns = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(0));

        return Ok(columns);
    }

    // ── POST /pipelines/{id}/run-from-db ───────────────────────────────────────
    // Lee filas de BD origen, serializa cada una como JSON y las guarda en pipeline_data
    [HttpPost("/pipelines/{pipelineId:int}/run-from-db")]
    public async Task<IActionResult> RunFromDb(int pipelineId, [FromBody] RunFromDbRequestDto dto)
    {
        var pipeline = await _context.Pipelines.FindAsync(pipelineId);
        if (pipeline is null)
            return NotFound(new { message = "Pipeline no encontrado." });

        if (pipeline.Status != "Active")
            return BadRequest(new { message = $"El pipeline está '{pipeline.Status}' y no puede ejecutarse." });

        var userId = HttpContext.Items.TryGetValue("userId", out var uid)
            ? (int)uid!
            : int.Parse(User.FindFirst("userId")?.Value ?? "0");

        var execution = new PipelineExecutionEntity
        {
            PipelineId  = pipelineId,
            Status      = "Running",
            TotalRows   = 0,
            SuccessRows = 0,
            ErrorRows   = 0,
            ExecutedBy  = userId,
            ExecutedAt  = DateTime.UtcNow
        };
        _context.PipelineExecutions.Add(execution);
        await _context.SaveChangesAsync();

        var startedAt = execution.ExecutedAt;
        int total = 0, success = 0;

        try
        {
            // 1. Conectar a la BD origen y extraer filas con filtros literales
            await using var srcConn = OpenConnection(
                dto.SrcType, dto.SrcHost, dto.SrcPort, dto.SrcDatabase, dto.SrcUser, dto.SrcPassword, dto.SrcWinAuth);
            await srcConn.OpenAsync();

            var srcTables = await GetTablesAsync(srcConn, dto.SrcType);
            if (!srcTables.Contains(dto.SrcTable, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Tabla origen '{dto.SrcTable}' no encontrada.");

            var (whereClause, whereParams) = BuildWhereClause(dto.Filters, dto.SrcType);
            var (_, rawSrcRows) = await ReadRowsAsync(
                srcConn, dto.SrcType, dto.SrcTable, limit: null, whereClause, whereParams);

            // 2. Filtros de tipo "column" — necesitan conexión al destino para comparar valores
            var columnFilters = dto.Filters
                .Where(f => f.ValueType.Equals("column", StringComparison.OrdinalIgnoreCase))
                .ToList();

            IReadOnlyList<Dictionary<string, object?>> srcRows;
            if (columnFilters.Count > 0 && !string.IsNullOrEmpty(dto.DstTable))
            {
                var connStrInternal = _config.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

                await using var dstConn = dto.DstIsInternal
                    ? (DbConnection)new SqlConnection(connStrInternal)
                    : OpenConnection(dto.DstType, dto.DstHost, dto.DstPort, dto.DstDatabase, dto.DstUser, dto.DstPassword, dto.DstWinAuth);
                await dstConn.OpenAsync();

                var dstType = dto.DstIsInternal ? "sqlserver" : dto.DstType;
                srcRows = await ApplyColumnFiltersAsync(rawSrcRows, columnFilters, dstConn, dstType, dto.DstTable);
            }
            else
            {
                srcRows = rawSrcRows;
            }

            total = srcRows.Count;
            _logger.LogInformation("RunFromDb pipeline {Id}: {N} filas leídas de {Tbl}", pipelineId, total, dto.SrcTable);

            // 3. Serializar cada fila completa como JSON → pipeline_data
            var errors   = 0;
            var batch    = new List<PipelineDataEntity>();
            var jsonRows = new List<string>();

            foreach (var srcRow in srcRows)
            {
                try
                {
                    var json = JsonSerializer.Serialize(srcRow);
                    batch.Add(new PipelineDataEntity
                    {
                        PipelineId = pipelineId,
                        Data       = json,
                        LoadedAt   = DateTime.UtcNow
                    });
                    jsonRows.Add(json);
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    if (errors <= 5)
                        _logger.LogWarning("RunFromDb fila error [{Type}]: {Msg}", ex.GetType().Name, ex.Message);
                }
            }

            if (batch.Count > 0)
                _context.PipelineData.AddRange(batch);

            _logger.LogInformation("RunFromDb pipeline {Id}: ok={S} err={E}", pipelineId, success, errors);

            // 4. Registrar resultado
            execution.Status      = "Completed";
            execution.TotalRows   = total;
            execution.SuccessRows = success;
            execution.ErrorRows   = errors;
            execution.FinishedAt  = DateTime.UtcNow;

            var duration = (int)(execution.FinishedAt.Value - startedAt).TotalMilliseconds;

            _context.EtlAuditLogs.Add(new EtlAuditLogEntity
            {
                PipelineId  = pipelineId,
                FileName    = $"{dto.SrcType}://{dto.SrcHost}/{dto.SrcDatabase}/{dto.SrcTable} → pipeline_data",
                TotalRows   = total,
                SuccessRows = success,
                ErrorRows   = errors,
                Metadata    = JsonSerializer.Serialize(new
                {
                    srcType     = dto.SrcType,
                    srcHost     = dto.SrcHost,
                    srcDatabase = dto.SrcDatabase,
                    srcTable    = dto.SrcTable,
                    executionId = execution.Id,
                    durationMs  = duration
                }),
                ExecutedBy = userId,
                ExecutedAt = startedAt
            });

            await _context.SaveChangesAsync();

            // 5. Envía a BigQuery — fallo no interrumpe el ETL
            if (jsonRows.Count > 0)
            {
                try
                {
                    await _bigQuery.EnsureTableExistsAsync();
                    await _bigQuery.InsertRowsAsync(pipelineId, pipeline.Name, jsonRows);
                    _logger.LogInformation("BigQuery: {N} filas enviadas del pipeline {Id}.", jsonRows.Count, pipelineId);
                }
                catch (Exception bqEx)
                {
                    _logger.LogError(bqEx, "BigQuery: error en pipeline {Id}: {Msg}", pipelineId, bqEx.Message);
                }
            }
        }
        catch (Exception ex)
        {
            execution.Status      = "Failed";
            execution.TotalRows   = total;
            execution.SuccessRows = success;
            execution.ErrorRows   = total - success;
            execution.FinishedAt  = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogError(ex, "RunFromDb pipeline {Id} falló: {Msg}", pipelineId, ex.Message);
            return StatusCode(500, new { message = $"Error en el proceso ETL: {CleanError(ex.Message)}" });
        }

        return Ok(new PipelineRunResponseDto
        {
            ExecutionId = execution.Id,
            Status      = execution.Status,
            TotalRows   = execution.TotalRows,
            SuccessRows = execution.SuccessRows,
            ErrorRows   = execution.ErrorRows,
            ExecutedAt  = execution.ExecutedAt,
            FinishedAt  = execution.FinishedAt ?? DateTime.UtcNow
        });
    }

    // ── Helpers privados ────────────────────────────────────────────────────────

    // Operadores permitidos — lista cerrada para prevenir inyección SQL
    private static readonly HashSet<string> AllowedOperators = new(StringComparer.OrdinalIgnoreCase)
        { "=", "!=", ">", "<", ">=", "<=", "LIKE", "IN", "NOT IN", "IS NULL", "IS NOT NULL" };

    // Construye el WHERE solo para filtros de tipo "literal" (los de tipo "column" se aplican en memoria)
    private static (string Clause, IReadOnlyList<(string Name, string Value)> Params)
        BuildWhereClause(IReadOnlyList<FilterConditionDto> filters, string dbType)
    {
        // Solo procesa los filtros de valor literal y los IS NULL / IS NOT NULL
        var literalFilters = filters
            .Where(f => f.ValueType.Equals("literal", StringComparison.OrdinalIgnoreCase) ||
                        f.Operator.Equals("IS NULL",     StringComparison.OrdinalIgnoreCase) ||
                        f.Operator.Equals("IS NOT NULL", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (literalFilters.Count == 0) return ("", []);

        var parts  = new List<string>();
        var parms  = new List<(string, string)>();
        var pIndex = 0;

        for (var i = 0; i < literalFilters.Count; i++)
        {
            var f = literalFilters[i];

            if (string.IsNullOrWhiteSpace(f.Column))
                throw new ArgumentException($"Un filtro tiene la columna vacía.");

            if (!AllowedOperators.Contains(f.Operator))
                throw new ArgumentException($"Operador no permitido: '{f.Operator}'.");

            if (i > 0 && !string.Equals(f.Connector, "AND", StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(f.Connector, "OR",  StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Conector inválido: use AND u OR.");

            var col    = QuoteIdentifier(f.Column, dbType);
            var prefix = i == 0 ? "" : $" {f.Connector.ToUpper()} ";

            if (f.Operator.Equals("IS NULL",     StringComparison.OrdinalIgnoreCase) ||
                f.Operator.Equals("IS NOT NULL", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add($"{prefix}{col} {f.Operator.ToUpper()}");
            }
            else
            {
                var pName = $"@fw{pIndex++}";
                parts.Add($"{prefix}{col} {f.Operator} {pName}");
                parms.Add((pName, f.Value));
            }
        }

        return ($"WHERE {string.Join("", parts)}", parms);
    }

    // Carga los valores distintos de una columna destino para filtros de tipo "column"
    private static async Task<HashSet<string>> GetDestColumnValuesAsync(
        DbConnection conn, string dbType, string table, string column)
    {
        var quotedTable = QuoteIdentifier(table, dbType);
        var quotedCol   = QuoteIdentifier(column, dbType);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT DISTINCT {quotedCol} FROM {quotedTable} WHERE {quotedCol} IS NOT NULL";

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            values.Add(reader.GetValue(0)?.ToString() ?? "");

        return values;
    }

    // Aplica en memoria los filtros de tipo "column" (comparación contra valores de la tabla destino)
    private static async Task<IReadOnlyList<Dictionary<string, object?>>> ApplyColumnFiltersAsync(
        IReadOnlyList<Dictionary<string, object?>> rows,
        IEnumerable<FilterConditionDto>            filters,
        DbConnection                               dstConn,
        string                                     dstType,
        string                                     dstTable)
    {
        var result = rows.ToList();

        foreach (var f in filters)
        {
            if (string.IsNullOrWhiteSpace(f.DestColumn)) continue;

            // Carga los valores distintos de la columna destino
            var destVals = await GetDestColumnValuesAsync(dstConn, dstType, dstTable, f.DestColumn);

            result = result.Where(row =>
            {
                var raw    = row.TryGetValue(f.Column, out var v) ? v?.ToString() ?? "" : "";
                var inDest = destVals.Contains(raw);

                return f.Operator.ToUpper() switch
                {
                    "="      or "IN"     => inDest,
                    "!="     or "NOT IN" => !inDest,
                    // Para >, <, >=, <=: compara numéricamente contra todos los valores destino
                    ">"  => destVals.Any(d => TryCompare(raw, d) > 0),
                    ">=" => destVals.Any(d => TryCompare(raw, d) >= 0),
                    "<"  => destVals.Any(d => TryCompare(raw, d) < 0),
                    "<=" => destVals.Any(d => TryCompare(raw, d) <= 0),
                    // LIKE no aplica bien contra un conjunto — no filtra
                    _    => true
                };
            }).ToList();
        }

        return result;
    }

    // Compara dos strings como números si es posible, sino como texto
    private static int TryCompare(string a, string b)
    {
        if (double.TryParse(a, out var da) && double.TryParse(b, out var db))
            return da.CompareTo(db);
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static DbConnection OpenConnection(
        string type, string host, int port, string database, string user, string password,
        bool winAuth = false)
    {
        var ltype = type.ToLower();

        // SQL Server con Windows Auth (flag explícito o usuario vacío)
        // Usa "." en lugar de "localhost" para evitar TCP/IP y preferir named pipes locales
        if (ltype == "sqlserver" && (winAuth || string.IsNullOrEmpty(user)))
        {
            var srv = host.Replace("localhost", ".", StringComparison.OrdinalIgnoreCase);
            return new SqlConnection(
                $"Server={srv};Database={database};" +
                "Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=10;");
        }

        return ltype switch
        {
            "mysql" => new MySqlConnection(
                $"Server={host};Port={port};Database={database};" +
                $"Uid={user};Pwd={password};Connect Timeout=10;SslMode=None;AllowZeroDateTime=True;"),
            "sqlserver" => new SqlConnection(
                $"Server={host},{port};Database={database};" +
                $"User Id={user};Password={password};TrustServerCertificate=True;Connect Timeout=10;"),
            _ => throw new ArgumentException($"Tipo de BD no soportado: '{type}'. Use 'mysql' o 'sqlserver'.")
        };
    }

    // Consulta INFORMATION_SCHEMA para obtener nombre + tipo de cada columna
    private static async Task<IReadOnlyList<ColumnInfoDto>> GetColumnsInfoAsync(
        DbConnection conn, string dbType, string database, string table)
    {
        // MySQL filtra por TABLE_SCHEMA (nombre del catálogo = la BD actual)
        // SQL Server filtra por TABLE_CATALOG (nombre de la base de datos)
        var sql = dbType.ToLower() == "mysql"
            ? "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
              "WHERE TABLE_SCHEMA = @database AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION"
            : "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
              "WHERE TABLE_CATALOG = @database AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var pDb = cmd.CreateParameter();
        pDb.ParameterName = "@database";
        pDb.Value = database;
        cmd.Parameters.Add(pDb);

        var pTbl = cmd.CreateParameter();
        pTbl.ParameterName = "@table";
        pTbl.Value = table;
        cmd.Parameters.Add(pTbl);

        var columns = new List<ColumnInfoDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(new ColumnInfoDto(reader.GetString(0), reader.GetString(1)));

        return columns;
    }

    private static async Task<IReadOnlyList<string>> GetTablesAsync(DbConnection conn, string dbType)
    {
        var sql = dbType.ToLower() == "mysql"
            ? "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME"
            : "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var tables = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));

        return tables;
    }

    private static async Task<(IReadOnlyList<string> Columns, IReadOnlyList<Dictionary<string, object?>> Rows)>
        ReadRowsAsync(DbConnection conn, string dbType, string table, int? limit,
                      string whereClause = "",
                      IReadOnlyList<(string Name, string Value)>? whereParams = null)
    {
        var quoted = QuoteIdentifier(table, dbType);
        var where  = string.IsNullOrEmpty(whereClause) ? "" : $" {whereClause}";

        var sql = (dbType.ToLower(), limit) switch
        {
            ("mysql", int n)     => $"SELECT * FROM {quoted}{where} LIMIT {n}",
            ("sqlserver", int n) => $"SELECT TOP {n} * FROM {quoted}{where}",
            _                    => $"SELECT * FROM {quoted}{where}"
        };

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        // Agrega los parámetros del WHERE si existen
        if (whereParams != null)
        {
            foreach (var (name, value) in whereParams)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value         = (object)value;
                cmd.Parameters.Add(p);
            }
        }

        var columns = new List<string>();
        var rows    = new List<Dictionary<string, object?>>();

        await using var reader = await cmd.ExecuteReaderAsync();
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
                row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        return (columns, rows);
    }

    // Delimita identificadores (tabla o columna) para prevenir inyección SQL
    private static string QuoteIdentifier(string name, string dbType) =>
        dbType.ToLower() == "mysql"
            ? $"`{name.Replace("`", "``")}`"
            : $"[{name.Replace("]", "]]")}]";

    private static string CleanError(string msg) =>
        msg.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
}
