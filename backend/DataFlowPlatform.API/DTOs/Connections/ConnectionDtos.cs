namespace DataFlowPlatform.API.DTOs.Connections;

// Datos de conexión a BD externa
public record ConnectionRequestDto(
    string Type,      // "mysql" | "sqlserver"
    string Host,
    int    Port,
    string Database,
    string User,
    string Password,
    bool   WinAuth = false  // SQL Server: usa Trusted_Connection en lugar de usuario/contraseña
);

// Respuesta del test de conexión
public record ConnectionTestResponseDto(
    bool                  Success,
    string                Message,
    IReadOnlyList<string> Tables
);

// Preview: conexión + tabla
public record PreviewRequestDto(
    string Type, string Host, int Port, string Database, string User, string Password,
    string Table,
    bool   WinAuth = false
);

// Metadato de una columna: nombre + tipo SQL
public record ColumnInfoDto(string Name, string Type);

// Resultado del preview: columnas con tipo + primeras 10 filas
public record PreviewResponseDto(
    IReadOnlyList<ColumnInfoDto>               Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows
);

// Un mapeo columna-origen → columna-destino
public record ColumnMappingDto(string Source, string Destination);

// Una condición de filtro para el WHERE de la query origen
public record FilterConditionDto(
    string Column,     // columna de la tabla origen
    string Operator,   // =, !=, >, <, >=, <=, LIKE, IN, NOT IN, IS NULL, IS NOT NULL
    string ValueType,  // "literal" = valor escrito | "column" = columna de la tabla destino
    string Value,      // valor literal (ValueType = "literal")
    string DestColumn, // columna destino a comparar (ValueType = "column")
    string Connector   // AND | OR — ignorado en el primer filtro
);

// Payload para ejecutar ETL: extrae de BD origen, filtra, e inserta en BD destino
public record RunFromDbRequestDto(
    // Fuente
    string SrcType, string SrcHost, int SrcPort, string SrcDatabase,
    string SrcUser, string SrcPassword, string SrcTable,
    bool   SrcWinAuth,    // SQL Server origen: Trusted_Connection
    // Filtros de origen (opcional, vacío = sin WHERE)
    IReadOnlyList<FilterConditionDto> Filters,
    // Destino (DstIsInternal = true usa el connection string de appsettings)
    bool   DstIsInternal,
    string DstType, string DstHost, int DstPort, string DstDatabase,
    string DstUser, string DstPassword, string DstTable,
    bool   DstWinAuth,    // SQL Server destino: Trusted_Connection
    // Mapeo columna-origen → columna-destino
    IReadOnlyList<ColumnMappingDto> ColumnMappings
);
