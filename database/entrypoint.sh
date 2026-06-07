#!/bin/bash
# Iniciar SQL Server en segundo plano y ejecutar init.sql al estar listo

/opt/mssql/bin/sqlservr &
SQL_PID=$!

echo "Esperando a que SQL Server acepte conexiones..."
for i in $(seq 1 40); do
    /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SA_PASSWORD" \
        -Q "SELECT 1" -b -C \
        > /dev/null 2>&1 && break
    echo "  intento $i/40 — reintentando en 3 s..."
    sleep 3
done

echo "Ejecutando init.sql..."
/opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" \
    -d master -i /init.sql -C
echo "Inicialización completada."

# Mantener el proceso SQL Server en primer plano
wait $SQL_PID
