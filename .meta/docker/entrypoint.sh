#!/bin/bash

# Start the script to configure the database in the background
/opt/mssql-scripts/configure-db.sh &

# Start SQL Server in the foreground
/opt/mssql/bin/sqlservr
