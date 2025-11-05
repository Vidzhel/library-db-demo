#!/bin/bash
set -e  # Exit on error

# Wait for SQL Server to start up by ensuring that calling SQLCMD does not return an error code
# This checks that all databases are in an "online" state (state = 0)
# https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-databases-transact-sql

echo "========================================"
echo "Database Initialization Script"
echo "========================================"

# Set default database name if not provided
DB_NAME=${DB_NAME:-LibraryDb}

# Validate that SA_PASSWORD is set
if [ -z "$SA_PASSWORD" ]; then
    echo "ERROR: SA_PASSWORD environment variable is not set!"
    echo "Please check your .env file and docker-compose.yml"
    exit 1
fi

echo "✓ SA_PASSWORD is set"
echo "✓ Target database: $DB_NAME"
echo "Waiting for SQL Server to start..."

# Give SQL Server more time to begin startup (increased from 10 to 20 seconds)
sleep 20

DBSTATUS=1
ERRCODE=1
i=0

# Wait up to 200 seconds for SQL Server to be ready (increased from 120)
while [[ $DBSTATUS -ne 0 ]] && [[ $i -lt 200 ]] && [[ $ERRCODE -ne 0 ]]; do
	i=$((i+1))

	# Try to connect and check database status
	DBSTATUS=$(/opt/mssql-tools18/bin/sqlcmd -h -1 -t 1 -U sa -P "$SA_PASSWORD" -Q "SET NOCOUNT ON; SELECT SUM(state) FROM sys.databases" -C 2>&1)
	ERRCODE=$?

	# Show progress every 10 seconds
	if [[ $((i % 10)) -eq 0 ]]; then
		echo "Still waiting... ($i seconds elapsed)"
	fi

	# If we get a login error, show it
	if [[ $ERRCODE -ne 0 ]] && [[ $i -eq 30 ]]; then
		echo "WARNING: Connection attempt failed after 30 seconds"
		echo "This is normal - SQL Server might still be starting up"
	fi

	sleep 1
done

if [[ $DBSTATUS -ne 0 ]] || [[ $ERRCODE -ne 0 ]]; then
	echo ""
	echo "========================================"
	echo "ERROR: SQL Server failed to start!"
	echo "========================================"
	echo "Time elapsed: $i seconds"
	echo "Last status: $DBSTATUS"
	echo "Last error code: $ERRCODE"
	echo ""
	echo "Troubleshooting steps:"
	echo "1. Check SA_PASSWORD meets complexity requirements"
	echo "2. Check Docker container logs: docker logs dbdemo-sqlserver"
	echo "3. Increase MSSQL_MEMORY_LIMIT_MB if system is low on memory"
	echo "========================================"
	exit 1
fi

echo "✓ SQL Server started successfully! (took $i seconds)"

# Drop the database if it exists (for clean initialization)
echo ""
echo "Dropping existing $DB_NAME database if it exists..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "IF EXISTS (SELECT name FROM sys.databases WHERE name = '$DB_NAME') BEGIN ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DB_NAME]; END" -C

if [ $? -ne 0 ]; then
    echo "WARNING: Failed to drop database (it might not exist yet - this is normal)"
fi

# Create the database
echo "Creating $DB_NAME database..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "CREATE DATABASE [$DB_NAME]" -C

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to create $DB_NAME database!"
    exit 1
fi

echo "✓ $DB_NAME database created"

# Set default application credentials if not provided
APP_USER=${APP_USER:-library_app_user}
APP_PASSWORD=${APP_PASSWORD:-LibraryApp@2024}

# Validate APP_PASSWORD is set
if [ -z "$APP_PASSWORD" ]; then
    echo "ERROR: APP_PASSWORD is not set!"
    exit 1
fi

# Run the initial schema migration
echo ""
echo "Running initial schema migration..."
echo "Creating application user: $APP_USER"
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -d $DB_NAME -i /opt/mssql-scripts/init.sql -v DB_NAME="$DB_NAME" -v APP_USER="$APP_USER" -v APP_PASSWORD="$APP_PASSWORD" -C

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================"
    echo "✓ Database initialization completed!"
    echo "========================================"
    echo "Database: $DB_NAME"
    echo "User: $APP_USER"
    echo "Ready for migrations!"
    echo "========================================"
else
    echo ""
    echo "========================================"
    echo "ERROR: Database initialization failed!"
    echo "========================================"
    echo "Check the logs above for details"
    exit 1
fi
