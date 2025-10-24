#!/bin/bash
# =============================================
# Docker Initialization Script
# Create Application Database and User
# =============================================
# This script runs when SQL Server container first starts
# Copy this file to: scripts/init/01-create-app-user.sh
# =============================================

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
sleep 15s

echo "Creating application database and user..."

# Run the SQL script
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -d master -i /docker-entrypoint-initdb.d/01-create-app-user.sql

if [ $? -eq 0 ]; then
    echo "✅ Application user created successfully!"
else
    echo "❌ Failed to create application user"
    exit 1
fi
