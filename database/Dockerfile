FROM mcr.microsoft.com/mssql/server:2022-latest
USER root

# Copiar el script de arranque y el esquema inicial
COPY entrypoint.sh /entrypoint.sh
COPY init.sql      /init.sql
RUN chmod +x /entrypoint.sh

USER mssql
ENTRYPOINT ["/entrypoint.sh"]
CMD []
