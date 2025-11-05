#!/bin/bash

# Wait for SQL Server to start up by ensuring that calling SQLCMD does not return an error code
# This checks that all databases are in an "online" state (state = 0)
# https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-databases-transact-sql

echo "Waiting for SQL Server to start..."

# Give SQL Server some time to begin startup
sleep 10

DBSTATUS=1
ERRCODE=1
i=0

# Wait up to 120 seconds for SQL Server to be ready
while [[ $DBSTATUS -ne 0 ]] && [[ $i -lt 120 ]] && [[ $ERRCODE -ne 0 ]]; do
	i=$((i+1))
	DBSTATUS=$(/opt/mssql-tools18/bin/sqlcmd -h -1 -t 1 -U sa -P $SA_PASSWORD -Q "SET NOCOUNT ON; SELECT SUM(state) FROM sys.databases" -C 2>/dev/null)
	ERRCODE=$?
	sleep 1
done

if [[ $DBSTATUS -ne 0 ]] || [[ $ERRCODE -ne 0 ]]; then
	echo "ERROR: SQL Server took more than 120 seconds to start up or one or more databases are not in an ONLINE state"
	exit 1
fi

echo "SQL Server started successfully!"

# Drop the LibraryDb database if it exists (for clean initialization)
echo "Dropping existing LibraryDb database if it exists..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "IF EXISTS (SELECT name FROM sys.databases WHERE name = 'LibraryDb') BEGIN ALTER DATABASE LibraryDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE LibraryDb; END" -C

# Create the LibraryDb database
echo "Creating LibraryDb database..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "CREATE DATABASE LibraryDb" -C

# Set default application credentials if not provided
APP_USER=${APP_USER:-library_app_user}
APP_PASSWORD=${APP_PASSWORD:-LibraryApp@2024}

# Run the initial schema migration
echo "Running initial schema migration..."
echo "Creating application user: $APP_USER"
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -d LibraryDb -i /opt/mssql-scripts/init.sql -v APP_USER="$APP_USER" -v APP_PASSWORD="$APP_PASSWORD" -C

if [ $? -eq 0 ]; then
    echo "Database initialization completed successfully!"
else
    echo "ERROR: Database initialization failed!"
    exit 1
fi
