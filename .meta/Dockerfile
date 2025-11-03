FROM mcr.microsoft.com/mssql/server:2022-latest

# Switch to root to set up scripts
USER root

# Create a config directory
RUN mkdir -p /opt/mssql-scripts

# Copy configuration scripts
COPY .meta/docker/entrypoint.sh /opt/mssql-scripts/
COPY .meta/docker/configure-db.sh /opt/mssql-scripts/
COPY .meta/docker/init.sql /opt/mssql-scripts/

# Grant permissions for scripts to be executable
RUN chmod +x /opt/mssql-scripts/entrypoint.sh && \
    chmod +x /opt/mssql-scripts/configure-db.sh && \
    chown -R mssql:mssql /opt/mssql-scripts

# Switch back to mssql user
USER mssql

ENTRYPOINT ["/opt/mssql-scripts/entrypoint.sh"]
