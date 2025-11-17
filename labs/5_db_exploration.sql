-- =============================================
-- Lab 5: Database Exploration - Schema Discovery Using SELECT
-- =============================================
-- This lab teaches you how to explore and understand database schemas
-- using system catalog views and dynamic management views (DMVs).
--
-- Topics Covered:
--   1. Database Objects Overview
--   2. Physical File Locations
--   3. Tables Exploration (structure, row counts, activity)
--   4. Views Exploration
--   5. Stored Procedures Exploration
--   6. Functions Exploration
--   7. Triggers Exploration
--   8. Check Constraints
--   9. Data Model Analysis
--
-- Target Database: LibraryDb
-- SQL Server Version: 2019+
--
-- IMPORTANT: These queries are READ-ONLY and safe to execute.
-- No data or schema modifications will be made.
-- =============================================

USE LibraryDb;
GO

PRINT '========================================';
PRINT 'LAB 5: DATABASE SCHEMA EXPLORATION';
PRINT '========================================';
PRINT 'Database: ' + DB_NAME();
PRINT 'Server: ' + @@SERVERNAME;
PRINT 'Execution Time: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT '========================================';
GO

-- =============================================
-- SECTION 1: DATABASE OBJECTS OVERVIEW
-- =============================================
-- SQL Server stores metadata about all database objects in system catalog views.
-- The most important view is sys.objects, which contains a row for each
-- user-defined object (tables, views, procedures, functions, etc.).
--
-- Common Object Types:
--   U  = User table (BASE TABLE)
--   V  = View
--   P  = Stored Procedure (SQL)
--   FN = Scalar function
--   IF = Inline table-valued function
--   TF = Table-valued function
--   TR = Trigger (DML)
--   C  = Check constraint
--   D  = Default constraint
--   F  = Foreign key constraint
--   PK = Primary key constraint
--   UQ = Unique constraint
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 1: DATABASE OBJECTS OVERVIEW';
PRINT '========================================';
GO

-- List ALL user-defined objects in the database
PRINT '1.1 All Database Objects:';
GO

SELECT
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS ObjectName,
    type AS TypeCode,
    type_desc AS ObjectType,
    create_date AS CreatedDate,
    modify_date AS ModifiedDate
FROM sys.objects
WHERE is_ms_shipped = 0  -- Exclude system objects
ORDER BY type_desc, name;
GO

-- Filter by specific object type (User Tables)
PRINT '';
PRINT '1.2 User Tables Only (type = ''U''):';
GO

SELECT
    name AS TableName,
    type_desc AS ObjectType,
    create_date AS CreatedDate
FROM sys.objects
WHERE type = 'U'  -- User table
ORDER BY name;
GO

-- Try different type filters to explore other objects
PRINT '';
PRINT 'ℹ️  Try these other type filters:';
PRINT '   type = ''V''  -- Views';
PRINT '   type = ''P''  -- Stored Procedures';
PRINT '   type = ''FN'' -- Scalar Functions';
PRINT '   type = ''TR'' -- Triggers';
GO

-- Object count by type
PRINT '';
PRINT '1.3 Object Count Summary by Type:';
GO

SELECT
    type_desc AS ObjectType,
    COUNT(*) AS ObjectCount
FROM sys.objects
WHERE is_ms_shipped = 0
GROUP BY type_desc
ORDER BY ObjectCount DESC, type_desc;
GO

-- =============================================
-- SECTION 2: DATABASE FILE LOCATIONS
-- =============================================
-- SQL Server databases consist of at least two files:
--   1. Primary Data File (.mdf) - Contains actual data and objects
--   2. Transaction Log File (.ldf) - Contains transaction log for recovery
--
-- Additional files:
--   - Secondary Data Files (.ndf) - For distributing data across drives
--   - FileStream data files - For FILESTREAM data
--
-- Understanding file locations is critical for:
--   - Backup/restore operations
--   - Performance tuning (place files on different drives)
--   - Disk space management
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 2: DATABASE FILE LOCATIONS';
PRINT '========================================';
GO

-- Method 1: Using stored procedure (classic approach)
PRINT '2.1 Database Files (using sp_helpfile):';
GO

EXEC sp_helpfile;
GO

-- Method 2: Using system catalog view (more detailed)
PRINT '';
PRINT '2.2 Database Files (using sys.database_files):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    file_id AS FileID,
    type_desc AS FileType,
    name AS LogicalName,
    LEFT(physical_name, 1) AS DriveLetter,
    physical_name AS PhysicalPath,
    RIGHT(physical_name, 3) AS FileExtension,
    size * 8 / 1024 AS SizeMB,  -- Size in pages (8KB each)
    CASE
        WHEN max_size = -1 THEN 'Unlimited'
        WHEN max_size = 0 THEN 'No Growth'
        ELSE CAST(max_size * 8 / 1024 AS VARCHAR(20)) + ' MB'
    END AS MaxSize,
    CASE
        WHEN is_percent_growth = 1 THEN CAST(growth AS VARCHAR(10)) + '%'
        ELSE CAST(growth * 8 / 1024 AS VARCHAR(10)) + ' MB'
    END AS GrowthIncrement,
    is_read_only AS IsReadOnly
FROM sys.database_files
ORDER BY file_id;
GO

-- File space usage analysis
PRINT '';
PRINT '2.3 File Space Usage:';
GO

SELECT
    DB_NAME() AS DatabaseName,
    name AS FileName,
    type_desc AS FileType,
    size * 8.0 / 1024 AS AllocatedSizeMB,
    FILEPROPERTY(name, 'SpaceUsed') * 8.0 / 1024 AS UsedSpaceMB,
    (size - FILEPROPERTY(name, 'SpaceUsed')) * 8.0 / 1024 AS FreeSpaceMB,
    CAST((FILEPROPERTY(name, 'SpaceUsed') * 100.0 / size) AS DECIMAL(5,2)) AS PercentUsed
FROM sys.database_files
ORDER BY type_desc;
GO

-- =============================================
-- SECTION 3: TABLES EXPLORATION
-- =============================================
-- Tables are the primary data storage objects in a database.
-- Understanding table structure, size, and usage patterns is essential
-- for database administration and optimization.
--
-- We'll explore tables using three different approaches:
--   1. INFORMATION_SCHEMA.TABLES (ANSI standard, portable)
--   2. sys.objects (SQL Server specific, more details)
--   3. sys.tables (SQL Server specific, table-focused)
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 3: TABLES EXPLORATION';
PRINT '========================================';
GO

-- Method 1: Using stored procedure
PRINT '3.1 Tables List (using sp_tables):';
PRINT 'ℹ️  Note: sp_tables returns both tables AND views';
GO

EXEC sp_tables;
GO

-- Method 2: Using INFORMATION_SCHEMA (ANSI Standard)
PRINT '';
PRINT '3.2 Tables List (using INFORMATION_SCHEMA):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    TABLE_CATALOG AS DatabaseName,
    TABLE_SCHEMA AS SchemaName,
    TABLE_NAME AS TableName,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;
GO

-- Method 3: Using sys.objects
PRINT '';
PRINT '3.3 Tables List (using sys.objects):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS TableName,
    type_desc AS ObjectType,
    create_date AS CreatedDate,
    modify_date AS LastModifiedDate
FROM sys.objects
WHERE type = 'U'  -- User table
ORDER BY name;
GO

-- Method 4: Using sys.tables (most detailed for tables)
PRINT '';
PRINT '3.4 Tables List (using sys.tables):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS TableName,
    create_date AS CreatedDate,
    modify_date AS LastModifiedDate,
    is_tracked_by_cdc AS IsTrackedByCDC,  -- Change Data Capture
    is_memory_optimized AS IsMemoryOptimized,
    temporal_type_desc AS TemporalType
FROM sys.tables
ORDER BY name;
GO

-- =============================================
-- 3.5 TABLE ROW COUNTS
-- =============================================
-- Knowing how many rows each table contains helps with:
--   - Performance analysis (large tables need more optimization)
--   - Capacity planning
--   - Data growth monitoring
--
-- There are two approaches:
--   A) Fast but approximate: sys.partitions (uses metadata)
--   B) Slow but accurate: COUNT(*) (scans entire table)
-- =============================================

PRINT '';
PRINT '3.5 Table Row Counts (Fast - from metadata):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(t.object_id) AS SchemaName,
    t.name AS TableName,
    SUM(p.rows) AS RowCount,
    SUM(p.rows) / 1000000.0 AS MillionRows,
    CASE
        WHEN SUM(p.rows) < 1000 THEN 'Small'
        WHEN SUM(p.rows) < 100000 THEN 'Medium'
        WHEN SUM(p.rows) < 1000000 THEN 'Large'
        ELSE 'Very Large'
    END AS TableSize
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE p.index_id IN (0, 1)  -- Heap or Clustered Index
GROUP BY t.object_id, t.name
ORDER BY RowCount DESC;
GO

-- Generate accurate row count queries for all tables
PRINT '';
PRINT '3.6 Script Generator - Accurate Row Counts for All Tables:';
PRINT 'ℹ️  Copy the result and execute in a new query window for exact counts';
GO

SELECT
    'SELECT ''' + DB_NAME() + '.' + SCHEMA_NAME(schema_id) + '.' + name +
    ''' AS TableFullName, COUNT(*) AS RowCount FROM ' +
    QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';'
    AS QueryToExecute
FROM sys.tables
ORDER BY name;
GO

-- Example: Execute for a specific table
PRINT '';
PRINT '3.7 Exact Row Count Example (Books table):';
GO

SELECT
    DB_NAME() + '.dbo.Books' AS TableFullName,
    COUNT(*) AS RowCount
FROM dbo.Books;
GO

-- =============================================
-- 3.8 TABLE ACTIVITY ANALYSIS
-- =============================================
-- Understanding which tables are actively read from or written to
-- helps identify:
--   - Hot tables (need optimization, better indexing)
--   - Cold tables (candidates for archival)
--   - Read-heavy vs Write-heavy tables (different optimization strategies)
--
-- Data Source: sys.dm_db_index_usage_stats (DMV)
--   - Cleared on SQL Server restart
--   - Longer uptime = more reliable statistics
--   - Tracks: seeks, scans, lookups (reads), updates (writes)
-- =============================================

PRINT '';
PRINT '3.8 Table Activity Analysis (Reads and Writes):';
PRINT 'ℹ️  Statistics reset on SQL Server restart - longer uptime = better data';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(ddius.object_id) AS SchemaName,
    OBJECT_NAME(ddius.object_id) AS TableName,
    SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups) AS TotalReads,
    SUM(ddius.user_updates) AS TotalWrites,
    SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups + ddius.user_updates) AS TotalOperations,
    CASE
        WHEN SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups + ddius.user_updates) = 0 THEN 0
        ELSE CAST(SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups) * 100.0 /
             SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups + ddius.user_updates) AS DECIMAL(5,2))
    END AS ReadPercentage,
    (
        SELECT DATEDIFF(DAY, create_date, GETDATE())
        FROM sys.databases
        WHERE name = 'tempdb'
    ) AS StatisticsAgeDays,
    (
        SELECT DATEDIFF(SECOND, create_date, GETDATE())
        FROM sys.databases
        WHERE name = 'tempdb'
    ) AS StatisticsAgeSeconds
FROM sys.dm_db_index_usage_stats ddius
INNER JOIN sys.indexes i ON ddius.object_id = i.object_id AND i.index_id = ddius.index_id
WHERE OBJECTPROPERTY(ddius.object_id, 'IsUserTable') = 1
    AND ddius.database_id = DB_ID()
GROUP BY ddius.object_id
ORDER BY TotalOperations DESC;
GO

-- Table activity classification
PRINT '';
PRINT '3.9 Table Activity Classification:';
GO

SELECT
    OBJECT_NAME(ddius.object_id) AS TableName,
    SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups) AS Reads,
    SUM(ddius.user_updates) AS Writes,
    CASE
        WHEN SUM(ddius.user_updates) = 0 THEN 'Read-Only'
        WHEN SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups) = 0 THEN 'Write-Only'
        WHEN SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups) >
             SUM(ddius.user_updates) * 10 THEN 'Read-Heavy (10:1)'
        WHEN SUM(ddius.user_updates) >
             SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups) * 10 THEN 'Write-Heavy (1:10)'
        ELSE 'Balanced Read/Write'
    END AS ActivityPattern
FROM sys.dm_db_index_usage_stats ddius
WHERE OBJECTPROPERTY(ddius.object_id, 'IsUserTable') = 1
    AND ddius.database_id = DB_ID()
GROUP BY ddius.object_id
ORDER BY ActivityPattern, TableName;
GO

-- =============================================
-- 3.10 CROSS-DATABASE TABLE ACTIVITY (ADVANCED)
-- =============================================
-- This cursor-based approach collects table activity statistics
-- across ALL databases on the server (excluding system databases).
--
-- Cursors are generally avoided due to performance concerns, but they
-- are acceptable when:
--   1. Iterating across databases (no set-based alternative)
--   2. Running administrative/reporting tasks (not in OLTP path)
--   3. Processing small result sets
--
-- WARNING: This query can take time on servers with many databases.
-- =============================================

PRINT '';
PRINT '3.10 Cross-Database Table Activity Analysis:';
PRINT 'ℹ️  This may take a while on servers with many databases...';
GO

-- Create cursor to iterate through databases
DECLARE @DBName NVARCHAR(128);
DECLARE @SQL NVARCHAR(MAX);

DECLARE DBCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name
    FROM sys.databases
    WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb', 'distribution')
        AND state_desc = 'ONLINE'
        AND DATABASEPROPERTYEX(name, 'Updateability') = 'READ_WRITE'
    ORDER BY name;

-- Create temporary table to store results
IF OBJECT_ID('tempdb..#TableActivityAllDatabases') IS NOT NULL
    DROP TABLE #TableActivityAllDatabases;

CREATE TABLE #TableActivityAllDatabases
(
    ServerName NVARCHAR(128),
    DatabaseName NVARCHAR(128),
    SchemaName NVARCHAR(128),
    TableName NVARCHAR(128),
    TotalReads BIGINT,
    TotalWrites BIGINT,
    TotalOperations BIGINT,
    StatisticsAgeDays DECIMAL(18, 2),
    StatisticsAgeSeconds INT
);

-- Iterate through databases and collect statistics
OPEN DBCursor;
FETCH NEXT FROM DBCursor INTO @DBName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = N'
    USE ' + QUOTENAME(@DBName) + N';
    INSERT INTO #TableActivityAllDatabases
    SELECT
        @@SERVERNAME AS ServerName,
        DB_NAME() AS DatabaseName,
        OBJECT_SCHEMA_NAME(ddius.object_id) AS SchemaName,
        OBJECT_NAME(ddius.object_id) AS TableName,
        SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups) AS TotalReads,
        SUM(ddius.user_updates) AS TotalWrites,
        SUM(ddius.user_seeks + ddius.user_scans + ddius.user_lookups + ddius.user_updates) AS TotalOperations,
        (SELECT DATEDIFF(SECOND, create_date, GETDATE()) / 86400.0
         FROM sys.databases WHERE name = ''tempdb'') AS StatisticsAgeDays,
        (SELECT DATEDIFF(SECOND, create_date, GETDATE())
         FROM sys.databases WHERE name = ''tempdb'') AS StatisticsAgeSeconds
    FROM sys.dm_db_index_usage_stats ddius
    INNER JOIN sys.indexes i ON ddius.object_id = i.object_id AND i.index_id = ddius.index_id
    WHERE OBJECTPROPERTY(ddius.object_id, ''IsUserTable'') = 1
        AND ddius.database_id = DB_ID()
    GROUP BY ddius.object_id;';

    BEGIN TRY
        EXEC sp_executesql @SQL;
    END TRY
    BEGIN CATCH
        PRINT 'Error collecting stats from database: ' + @DBName;
    END CATCH

    FETCH NEXT FROM DBCursor INTO @DBName;
END

CLOSE DBCursor;
DEALLOCATE DBCursor;

-- Display aggregated results
SELECT
    ServerName,
    DatabaseName,
    SchemaName,
    TableName,
    TotalReads,
    TotalWrites,
    TotalOperations,
    CAST(StatisticsAgeDays AS DECIMAL(10, 2)) AS StatisticsAgeDays
FROM #TableActivityAllDatabases
ORDER BY DatabaseName, TotalOperations DESC;

-- Cleanup
DROP TABLE #TableActivityAllDatabases;
GO

-- =============================================
-- SECTION 4: VIEWS EXPLORATION
-- =============================================
-- Views are saved queries that act as virtual tables.
-- They provide:
--   - Abstraction layer (hide complexity from users)
--   - Security (show only specific columns/rows)
--   - Simplified querying (pre-join related tables)
--
-- View Characteristics:
--   - Read-only (usually) - updatable only if simple (1 base table, no aggregates)
--   - No data storage (query executed each time)
--   - Can be indexed (indexed views/materialized views)
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 4: VIEWS EXPLORATION';
PRINT '========================================';
GO

-- Method 1: Using sys.objects
PRINT '4.1 Views List (using sys.objects):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS ViewName,
    type_desc AS ObjectType,
    create_date AS CreatedDate,
    modify_date AS LastModifiedDate
FROM sys.objects
WHERE type = 'V'  -- View
ORDER BY name;
GO

-- Method 2: Using sys.views
PRINT '';
PRINT '4.2 Views List (using sys.views):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS ViewName,
    create_date AS CreatedDate,
    modify_date AS LastModifiedDate,
    is_replicated AS IsReplicated,
    has_opaque_metadata AS HasOpaqueMetadata,
    is_date_correlation_view AS IsDateCorrelationView
FROM sys.views
ORDER BY name;
GO

-- Method 3: Using INFORMATION_SCHEMA (ANSI Standard)
PRINT '';
PRINT '4.3 Views List (using INFORMATION_SCHEMA):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    TABLE_CATALOG AS DatabaseName,
    TABLE_SCHEMA AS SchemaName,
    TABLE_NAME AS ViewName,
    TABLE_TYPE,
    CHECK_OPTION,
    IS_UPDATABLE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'VIEW'
ORDER BY TABLE_SCHEMA, TABLE_NAME;
GO

-- View definitions (includes CREATE VIEW code)
PRINT '';
PRINT '4.4 View Definitions with Source Code:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName,
    o.name AS ViewName,
    o.type_desc AS ObjectType,
    o.create_date AS CreatedDate,
    sm.definition AS ViewSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type = 'V'  -- View
ORDER BY o.name;
GO

-- =============================================
-- SECTION 5: STORED PROCEDURES EXPLORATION
-- =============================================
-- Stored Procedures (SPs) are precompiled SQL code stored in the database.
--
-- Benefits:
--   - Performance (compiled once, execution plan cached)
--   - Security (grant EXECUTE permission, hide underlying tables)
--   - Maintainability (centralized business logic)
--   - Network efficiency (one call vs. multiple queries)
--
-- Best Practices:
--   - Use parameters to prevent SQL injection
--   - SET NOCOUNT ON to reduce network traffic
--   - Include error handling (TRY/CATCH)
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 5: STORED PROCEDURES EXPLORATION';
PRINT '========================================';
GO

-- List all stored procedures
PRINT '5.1 Stored Procedures List:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS ProcedureName,
    type_desc AS ObjectType,
    create_date AS CreatedDate,
    modify_date AS LastModifiedDate
FROM sys.objects
WHERE type = 'P'  -- Stored Procedure
    AND is_ms_shipped = 0  -- Exclude system procedures
ORDER BY name;
GO

-- Stored procedures with source code
PRINT '';
PRINT '5.2 Stored Procedures with Source Code:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName,
    o.name AS ProcedureName,
    o.type_desc AS ObjectType,
    o.create_date AS CreatedDate,
    o.modify_date AS LastModifiedDate,
    sm.definition AS ProcedureSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type = 'P'  -- Stored Procedure
    AND is_ms_shipped = 0
ORDER BY o.name;
GO

-- Find procedures that perform INSERT operations
PRINT '';
PRINT '5.3 Procedures that perform INSERT operations:';
GO

SELECT
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName,
    o.name AS ProcedureName,
    o.create_date AS CreatedDate,
    sm.definition AS ProcedureSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type = 'P'
    AND is_ms_shipped = 0
    AND sm.definition LIKE '%INSERT%'  -- Case insensitive
ORDER BY o.name;
GO

-- Find procedures that perform UPDATE operations
PRINT '';
PRINT '5.4 Procedures that perform UPDATE operations:';
GO

SELECT
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName,
    o.name AS ProcedureName,
    sm.definition AS ProcedureSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type = 'P'
    AND is_ms_shipped = 0
    AND sm.definition LIKE '%UPDATE%'
ORDER BY o.name;
GO

-- Find procedures that perform DELETE operations
PRINT '';
PRINT '5.5 Procedures that perform DELETE operations:';
GO

SELECT
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName,
    o.name AS ProcedureName,
    sm.definition AS ProcedureSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type = 'P'
    AND is_ms_shipped = 0
    AND sm.definition LIKE '%DELETE%'
ORDER BY o.name;
GO

-- Find procedures that reference a specific table
PRINT '';
PRINT '5.6 Procedures that reference specific table (example: Books):';
GO

SELECT
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName,
    o.name AS ProcedureName,
    sm.definition AS ProcedureSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type = 'P'
    AND is_ms_shipped = 0
    AND (sm.definition LIKE '%Books%' OR sm.definition LIKE '%books%')
ORDER BY o.name;
GO

-- Stored procedure parameters
PRINT '';
PRINT '5.7 Stored Procedure Parameters:';
GO

SELECT
    OBJECT_SCHEMA_NAME(p.object_id) AS SchemaName,
    OBJECT_NAME(p.object_id) AS ProcedureName,
    p.parameter_id AS ParameterOrder,
    p.name AS ParameterName,
    TYPE_NAME(p.user_type_id) AS DataType,
    p.max_length AS MaxLength,
    p.is_output AS IsOutputParameter,
    p.has_default_value AS HasDefaultValue
FROM sys.parameters p
INNER JOIN sys.objects o ON p.object_id = o.object_id
WHERE o.type = 'P' AND is_ms_shipped = 0
ORDER BY OBJECT_NAME(p.object_id), p.parameter_id;
GO

-- =============================================
-- SECTION 6: FUNCTIONS EXPLORATION
-- =============================================
-- SQL Server supports three types of user-defined functions:
--
-- 1. Scalar Functions (FN):
--    - Returns a single value (int, varchar, etc.)
--    - Can be used in SELECT, WHERE clauses
--    - Example: dbo.CalculateLateFee(@DaysLate)
--
-- 2. Inline Table-Valued Functions (IF):
--    - Returns a table (single SELECT statement)
--    - Performs like a view (query optimizer can optimize)
--    - Example: dbo.GetBooksByCategory(@CategoryId)
--
-- 3. Multi-Statement Table-Valued Functions (TF):
--    - Returns a table (multiple statements, variables, logic)
--    - Less efficient than inline TVFs
--    - Example: dbo.GetMemberLoanHistory(@MemberId)
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 6: FUNCTIONS EXPLORATION';
PRINT '========================================';
GO

-- List all functions (all types)
PRINT '6.1 All Functions List:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS FunctionName,
    type AS TypeCode,
    type_desc AS FunctionType,
    create_date AS CreatedDate,
    modify_date AS LastModifiedDate
FROM sys.objects
WHERE type IN ('FN', 'IF', 'TF')  -- Scalar, Inline TVF, Multi-statement TVF
ORDER BY type_desc, name;
GO

-- Scalar functions only
PRINT '';
PRINT '6.2 Scalar Functions (type = ''FN''):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS FunctionName,
    type_desc AS FunctionType,
    create_date AS CreatedDate
FROM sys.objects
WHERE type = 'FN'  -- Scalar function
ORDER BY name;
GO

-- Table-valued functions (inline and multi-statement)
PRINT '';
PRINT '6.3 Table-Valued Functions (inline and multi-statement):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    name AS FunctionName,
    type_desc AS FunctionType,
    CASE type
        WHEN 'IF' THEN 'Inline (Better Performance)'
        WHEN 'TF' THEN 'Multi-Statement (More Flexible)'
    END AS PerformanceNote,
    create_date AS CreatedDate
FROM sys.objects
WHERE type IN ('IF', 'TF')
ORDER BY type, name;
GO

-- Functions with source code
PRINT '';
PRINT '6.4 Functions with Source Code:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName,
    o.name AS FunctionName,
    o.type_desc AS FunctionType,
    o.create_date AS CreatedDate,
    sm.definition AS FunctionSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type IN ('FN', 'IF', 'TF')
ORDER BY o.type, o.name;
GO

-- Function parameters and return types
PRINT '';
PRINT '6.5 Function Parameters:';
GO

SELECT
    OBJECT_SCHEMA_NAME(p.object_id) AS SchemaName,
    OBJECT_NAME(p.object_id) AS FunctionName,
    p.parameter_id AS ParameterOrder,
    CASE
        WHEN p.parameter_id = 0 THEN 'RETURN VALUE'
        ELSE p.name
    END AS ParameterName,
    TYPE_NAME(p.user_type_id) AS DataType,
    p.max_length AS MaxLength,
    p.is_output AS IsOutputParameter
FROM sys.parameters p
INNER JOIN sys.objects o ON p.object_id = o.object_id
WHERE o.type IN ('FN', 'IF', 'TF')
ORDER BY OBJECT_NAME(p.object_id), p.parameter_id;
GO

-- =============================================
-- SECTION 7: TRIGGERS EXPLORATION
-- =============================================
-- Triggers are special stored procedures that automatically execute
-- in response to certain events on a table.
--
-- DML Triggers (Data Manipulation Language):
--   - AFTER INSERT, UPDATE, DELETE
--   - INSTEAD OF INSERT, UPDATE, DELETE
--
-- Uses:
--   - Enforce complex business rules
--   - Maintain audit trails
--   - Synchronize related tables
--   - Prevent invalid modifications
--
-- Special Tables in Triggers:
--   - INSERTED: Contains new rows (INSERT, UPDATE)
--   - DELETED: Contains old rows (UPDATE, DELETE)
--
-- WARNING: Overuse of triggers can hurt performance and make
-- debugging difficult. Use sparingly and document well.
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 7: TRIGGERS EXPLORATION';
PRINT '========================================';
GO

-- List all triggers with their parent tables
PRINT '7.1 Triggers List with Parent Tables:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(parent.object_id) AS SchemaName,
    parent.name AS TableName,
    o.name AS TriggerName,
    o.type_desc AS ObjectType,
    o.create_date AS CreatedDate,
    o.modify_date AS LastModifiedDate
FROM sys.objects o
INNER JOIN sys.objects parent ON o.parent_object_id = parent.object_id
WHERE o.type = 'TR'  -- Trigger
ORDER BY parent.name, o.name;
GO

-- Method 2: Using sys.triggers
PRINT '';
PRINT '7.2 Triggers List (using sys.triggers):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(parent_id) AS SchemaName,
    OBJECT_NAME(parent_id) AS TableName,
    name AS TriggerName,
    create_date AS CreatedDate,
    modify_date AS LastModifiedDate,
    is_disabled AS IsDisabled,
    is_instead_of_trigger AS IsInsteadOfTrigger
FROM sys.triggers
WHERE parent_class = 1  -- Object trigger (not database trigger)
ORDER BY OBJECT_NAME(parent_id), name;
GO

-- Triggers with source code and detailed info
PRINT '';
PRINT '7.3 Triggers with Source Code:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(parent_object_id) AS SchemaName,
    OBJECT_NAME(parent_object_id) AS TableName,
    o.name AS TriggerName,
    o.type_desc AS ObjectType,
    o.create_date AS CreatedDate,
    sm.definition AS TriggerSourceCode
FROM sys.objects o
INNER JOIN sys.sql_modules sm ON o.object_id = sm.object_id
WHERE o.type = 'TR'  -- Trigger
ORDER BY OBJECT_NAME(parent_object_id), o.name;
GO

-- Trigger event details (what events fire the trigger)
PRINT '';
PRINT '7.4 Trigger Events (INSERT/UPDATE/DELETE):';
GO

SELECT
    OBJECT_SCHEMA_NAME(t.parent_id) AS SchemaName,
    OBJECT_NAME(t.parent_id) AS TableName,
    t.name AS TriggerName,
    te.type_desc AS TriggerEvent,
    t.is_instead_of_trigger AS IsInsteadOf,
    CASE
        WHEN t.is_instead_of_trigger = 0 THEN 'AFTER'
        ELSE 'INSTEAD OF'
    END AS TriggerTiming,
    t.is_disabled AS IsDisabled
FROM sys.triggers t
INNER JOIN sys.trigger_events te ON t.object_id = te.object_id
WHERE t.parent_class = 1
ORDER BY OBJECT_NAME(t.parent_id), t.name, te.type_desc;
GO

-- =============================================
-- SECTION 8: CHECK CONSTRAINTS
-- =============================================
-- CHECK constraints enforce domain integrity by limiting the values
-- that can be inserted into a column.
--
-- Common Uses:
--   - Ensure values are positive (e.g., Price > 0)
--   - Validate ranges (e.g., Age BETWEEN 18 AND 100)
--   - Enforce relationships between columns (e.g., StartDate < EndDate)
--   - Pattern matching (e.g., Email LIKE '%@%.%')
--
-- Benefits:
--   - Data quality enforcement at database level
--   - Better than application-only validation
--   - Self-documenting (constraints are visible in schema)
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 8: CHECK CONSTRAINTS';
PRINT '========================================';
GO

-- List all check constraints
PRINT '8.1 Check Constraints List:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(parent.object_id) AS SchemaName,
    parent.name AS TableName,
    o.name AS ConstraintName,
    o.type_desc AS ObjectType,
    o.create_date AS CreatedDate
FROM sys.objects o
INNER JOIN sys.objects parent ON o.parent_object_id = parent.object_id
WHERE o.type = 'C'  -- Check constraint
ORDER BY parent.name, o.name;
GO

-- Check constraints with definitions
PRINT '';
PRINT '8.2 Check Constraint Definitions:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(cc.parent_object_id) AS SchemaName,
    OBJECT_NAME(cc.parent_object_id) AS TableName,
    cc.name AS ConstraintName,
    cc.type AS TypeCode,
    cc.type_desc AS ObjectType,
    cc.create_date AS CreatedDate,
    cc.is_disabled AS IsDisabled,
    cc.is_not_trusted AS IsNotTrusted,  -- Data might violate constraint
    OBJECT_DEFINITION(cc.object_id) AS ConstraintDefinition
FROM sys.check_constraints cc
ORDER BY OBJECT_NAME(cc.parent_object_id), cc.name;
GO

-- Check constraints by column
PRINT '';
PRINT '8.3 Check Constraints by Column:';
GO

SELECT
    OBJECT_SCHEMA_NAME(cc.parent_object_id) AS SchemaName,
    OBJECT_NAME(cc.parent_object_id) AS TableName,
    COL_NAME(cc.parent_object_id, cc.parent_column_id) AS ColumnName,
    cc.parent_column_id AS ColumnNumber,
    cc.name AS ConstraintName,
    OBJECT_DEFINITION(cc.object_id) AS ConstraintDefinition,
    cc.is_disabled AS IsDisabled
FROM sys.check_constraints cc
ORDER BY OBJECT_NAME(cc.parent_object_id), cc.parent_column_id;
GO

-- =============================================
-- SECTION 9: DATA MODEL ANALYSIS
-- =============================================
-- Understanding your data model (tables, columns, data types) is
-- essential for:
--   - Database design and normalization
--   - Performance tuning
--   - Data migration and integration
--   - Documentation and knowledge transfer
--
-- This section provides queries to export comprehensive metadata
-- that can be analyzed in Excel or other tools.
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 9: DATA MODEL ANALYSIS';
PRINT '========================================';
GO

-- Complete column inventory (great for Excel export)
PRINT '9.1 Complete Column Inventory:';
PRINT 'ℹ️  Copy results to Excel for filtering and analysis';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    isc.TABLE_SCHEMA AS SchemaName,
    isc.TABLE_NAME AS TableName,
    ist.TABLE_TYPE,
    isc.ORDINAL_POSITION AS ColumnOrder,
    isc.COLUMN_NAME AS ColumnName,
    isc.DATA_TYPE AS DataType,
    isc.NUMERIC_PRECISION AS NumericPrecision,
    isc.NUMERIC_SCALE AS NumericScale,
    CASE
        WHEN isc.CHARACTER_MAXIMUM_LENGTH = -1 THEN 'MAX'
        ELSE CAST(isc.CHARACTER_MAXIMUM_LENGTH AS VARCHAR(20))
    END AS CharacterMaxLength,
    isc.IS_NULLABLE AS IsNullable,
    isc.COLUMN_DEFAULT AS DefaultValue,
    CASE
        WHEN pk.COLUMN_NAME IS NOT NULL THEN 'YES'
        ELSE 'NO'
    END AS IsPrimaryKey,
    CASE
        WHEN fk.COLUMN_NAME IS NOT NULL THEN 'YES'
        ELSE 'NO'
    END AS IsForeignKey
FROM INFORMATION_SCHEMA.COLUMNS isc
INNER JOIN INFORMATION_SCHEMA.TABLES ist
    ON isc.TABLE_NAME = ist.TABLE_NAME
    AND isc.TABLE_SCHEMA = ist.TABLE_SCHEMA
LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    ON tc.TABLE_NAME = isc.TABLE_NAME
    AND tc.TABLE_SCHEMA = isc.TABLE_SCHEMA
    AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk
    ON pk.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
    AND pk.COLUMN_NAME = isc.COLUMN_NAME
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk
    ON fk.TABLE_NAME = isc.TABLE_NAME
    AND fk.COLUMN_NAME = isc.COLUMN_NAME
    AND EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc2
        WHERE tc2.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
        AND tc2.CONSTRAINT_TYPE = 'FOREIGN KEY'
    )
WHERE ist.TABLE_TYPE = 'BASE TABLE'
ORDER BY isc.TABLE_SCHEMA, isc.TABLE_NAME, isc.ORDINAL_POSITION;
GO

-- Column name analysis (find duplicate names with different data types)
PRINT '';
PRINT '9.2 Column Name Analysis - Potential Data Type Inconsistencies:';
PRINT 'ℹ️  Look for same column names with different data types across tables';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    isc.COLUMN_NAME AS ColumnName,
    isc.DATA_TYPE AS DataType,
    isc.NUMERIC_PRECISION AS NumericPrecision,
    isc.NUMERIC_SCALE AS NumericScale,
    CASE
        WHEN isc.CHARACTER_MAXIMUM_LENGTH = -1 THEN 'MAX'
        ELSE CAST(isc.CHARACTER_MAXIMUM_LENGTH AS VARCHAR(20))
    END AS CharacterMaxLength,
    COUNT(*) AS UsageCount,
    STRING_AGG(isc.TABLE_NAME, ', ') AS TablesUsingThisColumn
FROM INFORMATION_SCHEMA.COLUMNS isc
INNER JOIN INFORMATION_SCHEMA.TABLES ist
    ON isc.TABLE_NAME = ist.TABLE_NAME
WHERE ist.TABLE_TYPE = 'BASE TABLE'
GROUP BY
    isc.COLUMN_NAME,
    isc.DATA_TYPE,
    isc.NUMERIC_PRECISION,
    isc.NUMERIC_SCALE,
    isc.CHARACTER_MAXIMUM_LENGTH
HAVING COUNT(*) > 1  -- Only show columns used in multiple tables
ORDER BY UsageCount DESC, isc.COLUMN_NAME;
GO

-- Data type usage statistics
PRINT '';
PRINT '9.3 Data Type Usage Statistics:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    isc.DATA_TYPE AS DataType,
    isc.NUMERIC_PRECISION AS NumericPrecision,
    isc.NUMERIC_SCALE AS NumericScale,
    CASE
        WHEN isc.CHARACTER_MAXIMUM_LENGTH = -1 THEN 'MAX'
        ELSE CAST(isc.CHARACTER_MAXIMUM_LENGTH AS VARCHAR(20))
    END AS CharacterMaxLength,
    COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS isc
INNER JOIN INFORMATION_SCHEMA.TABLES ist
    ON isc.TABLE_NAME = ist.TABLE_NAME
WHERE ist.TABLE_TYPE = 'BASE TABLE'
GROUP BY
    isc.DATA_TYPE,
    isc.NUMERIC_PRECISION,
    isc.NUMERIC_SCALE,
    isc.CHARACTER_MAXIMUM_LENGTH
ORDER BY ColumnCount DESC, isc.DATA_TYPE;
GO

-- Large Object (LOB) data types - IMPORTANT for index rebuilds
PRINT '';
PRINT '9.4 Large Object (LOB) Columns:';
PRINT 'ℹ️  CRITICAL: Tables with LOB columns cannot rebuild indexes ONLINE in Standard Edition';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    isc.TABLE_SCHEMA AS SchemaName,
    isc.TABLE_NAME AS TableName,
    isc.ORDINAL_POSITION AS ColumnOrder,
    isc.COLUMN_NAME AS ColumnName,
    isc.DATA_TYPE AS LOBDataType,
    CASE
        WHEN isc.CHARACTER_MAXIMUM_LENGTH = -1 THEN 'MAX'
        ELSE CAST(isc.CHARACTER_MAXIMUM_LENGTH AS VARCHAR(20))
    END AS MaxLength,
    CASE isc.DATA_TYPE
        WHEN 'text' THEN 'DEPRECATED - Use VARCHAR(MAX)'
        WHEN 'ntext' THEN 'DEPRECATED - Use NVARCHAR(MAX)'
        WHEN 'image' THEN 'DEPRECATED - Use VARBINARY(MAX)'
        WHEN 'xml' THEN 'LOB type'
        ELSE 'MAX type'
    END AS Notes
FROM INFORMATION_SCHEMA.COLUMNS isc
INNER JOIN INFORMATION_SCHEMA.TABLES ist
    ON isc.TABLE_NAME = ist.TABLE_NAME
WHERE ist.TABLE_TYPE = 'BASE TABLE'
    AND (
        isc.DATA_TYPE IN ('text', 'ntext', 'image', 'xml')
        OR (
            isc.DATA_TYPE IN ('varchar', 'nvarchar', 'varbinary')
            AND isc.CHARACTER_MAXIMUM_LENGTH = -1  -- MAX columns
        )
    )
ORDER BY isc.TABLE_NAME, isc.ORDINAL_POSITION;
GO

-- Computed columns
PRINT '';
PRINT '9.5 Computed Columns:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(cc.object_id) AS SchemaName,
    OBJECT_NAME(cc.object_id) AS TableName,
    cc.column_id AS ColumnOrder,
    cc.name AS ComputedColumnName,
    cc.definition AS ComputationFormula,
    cc.is_persisted AS IsPersisted,  -- Stored physically vs. calculated on-the-fly
    TYPE_NAME(cc.user_type_id) AS DataType,
    cc.is_nullable AS IsNullable
FROM sys.computed_columns cc
ORDER BY OBJECT_NAME(cc.object_id), cc.column_id;
GO

-- Alternative method for computed columns
PRINT '';
PRINT '9.6 Computed Columns (alternative method):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(t.object_id) AS SchemaName,
    t.name AS TableName,
    c.column_id AS ColumnOrder,
    c.name AS ComputedColumnName,
    TYPE_NAME(c.user_type_id) AS DataType,
    cc.definition AS ComputationFormula,
    cc.is_persisted AS IsPersisted
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.computed_columns cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
WHERE c.is_computed = 1
ORDER BY t.name, c.column_id;
GO

-- Identity columns (auto-incrementing)
PRINT '';
PRINT '9.7 Identity Columns (Auto-Increment):';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(ic.object_id) AS SchemaName,
    OBJECT_NAME(ic.object_id) AS TableName,
    ic.name AS ColumnName,
    TYPE_NAME(ic.user_type_id) AS DataType,
    ic.seed_value AS SeedValue,
    ic.increment_value AS IncrementValue,
    ic.last_value AS LastValue,
    CASE
        WHEN ic.last_value IS NULL THEN 'No rows inserted yet'
        ELSE CAST(ic.last_value AS VARCHAR(50))
    END AS CurrentIdentityValue
FROM sys.identity_columns ic
ORDER BY OBJECT_NAME(ic.object_id);
GO

-- =============================================
-- SECTION 10: ADVANCED SCHEMA QUERIES
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 10: ADVANCED ANALYSIS';
PRINT '========================================';
GO

-- Foreign key relationships
PRINT '10.1 Foreign Key Relationships:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS SchemaName,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ChildColumn,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ParentColumn,
    fk.name AS ForeignKeyName,
    fk.delete_referential_action_desc AS OnDeleteAction,
    fk.update_referential_action_desc AS OnUpdateAction,
    fk.is_disabled AS IsDisabled
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
ORDER BY ChildTable, ParentTable, fkc.constraint_column_id;
GO

-- Primary keys
PRINT '';
PRINT '10.2 Primary Keys:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS PrimaryKeyName,
    i.type_desc AS IndexType,
    STRING_AGG(COL_NAME(ic.object_id, ic.column_id), ', ')
        WITHIN GROUP (ORDER BY ic.key_ordinal) AS PrimaryKeyColumns
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.is_primary_key = 1
GROUP BY i.object_id, i.name, i.type_desc
ORDER BY OBJECT_NAME(i.object_id);
GO

-- Indexes summary
PRINT '';
PRINT '10.3 Indexes Summary:';
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey,
    i.fill_factor AS FillFactor,
    COUNT(ic.column_id) AS ColumnCount
FROM sys.indexes i
LEFT JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
GROUP BY i.object_id, i.index_id, i.name, i.type_desc, i.is_unique, i.is_primary_key, i.fill_factor
ORDER BY OBJECT_NAME(i.object_id), i.index_id;
GO

-- =============================================
-- LAB COMPLETION
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'LAB 5 COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT 'You have learned how to explore:';
PRINT '  ✅ Database objects (tables, views, procedures, functions)';
PRINT '  ✅ Physical file locations and space usage';
PRINT '  ✅ Table row counts and activity patterns';
PRINT '  ✅ View definitions and source code';
PRINT '  ✅ Stored procedure operations and dependencies';
PRINT '  ✅ Function types and implementations';
PRINT '  ✅ Trigger events and timing';
PRINT '  ✅ Check constraints and business rules';
PRINT '  ✅ Complete data model (columns, types, relationships)';
PRINT '  ✅ Computed columns and identity columns';
PRINT '  ✅ Foreign key relationships and constraints';
PRINT '';
PRINT 'Key Takeaways:';
PRINT '  📊 INFORMATION_SCHEMA: ANSI standard, portable across databases';
PRINT '  🔧 sys catalog views: SQL Server specific, more detailed';
PRINT '  💡 DMVs (dm_*): Dynamic statistics, reset on restart';
PRINT '  📋 Export results to Excel for analysis and documentation';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Run these queries against your own databases';
PRINT '  2. Export key results to Excel for team documentation';
PRINT '  3. Create a database diagram from foreign key relationships';
PRINT '  4. Identify optimization opportunities (large tables, missing indexes)';
PRINT '  5. Document your schema for new team members';
PRINT '';
PRINT 'Pro Tip: Bookmark queries you use frequently!';
PRINT '========================================';
GO
