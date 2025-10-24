-- =============================================
-- Migration V000: Create Application Database and User
-- =============================================
-- This is Migration ZERO - it sets up security before any schema migrations
--
-- IMPORTANT: This migration must be run with SA credentials because it:
-- 1. Creates a new database
-- 2. Creates a new login (server-level)
-- 3. Creates a user in that database
-- 4. Grants permissions
--
-- After this migration, all subsequent migrations can use the app user!
-- =============================================

USE master;
GO

-- =============================================
-- Step 1: Create Database
-- =============================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LibraryDb')
BEGIN
    CREATE DATABASE LibraryDb;
    PRINT '✅ Database [LibraryDb] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Database [LibraryDb] already exists.';
END
GO

-- Set database options for development
USE LibraryDb;
GO

-- SIMPLE recovery model (reduces transaction log growth in development)
-- In production, you'd use FULL for point-in-time recovery
IF (SELECT recovery_model_desc FROM sys.databases WHERE name = 'LibraryDb') != 'SIMPLE'
BEGIN
    ALTER DATABASE LibraryDb SET RECOVERY SIMPLE;
    PRINT '✅ Recovery model set to SIMPLE (development mode).';
END
GO

USE master;
GO

-- =============================================
-- Step 2: Create Server Login
-- =============================================
-- A LOGIN is server-level authentication (SQL Server instance)
-- This allows the user to connect to the SQL Server
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'library_app_user')
BEGIN
    -- ⚠️ SECURITY NOTE: In production, use a strong password from environment/secrets!
    -- This password is for demonstration purposes only
    CREATE LOGIN library_app_user
    WITH PASSWORD = N'LibraryApp@2024!',
        CHECK_POLICY = ON,          -- Enforce password policy
        CHECK_EXPIRATION = OFF,     -- Don't expire password (dev only!)
        DEFAULT_DATABASE = LibraryDb;

    PRINT '✅ Server login [library_app_user] created successfully.';
    PRINT '   Default database: LibraryDb';
END
ELSE
BEGIN
    PRINT 'ℹ️  Server login [library_app_user] already exists.';
END
GO

-- =============================================
-- Step 3: Create Database User
-- =============================================
-- A USER is database-level authorization (specific database)
-- This maps the server login to permissions within LibraryDb
USE LibraryDb;
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'library_app_user')
BEGIN
    CREATE USER library_app_user FOR LOGIN library_app_user;
    PRINT '✅ Database user [library_app_user] created in LibraryDb.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Database user [library_app_user] already exists in LibraryDb.';
END
GO

-- =============================================
-- Step 4: Grant Permissions (Principle of Least Privilege)
-- =============================================

-- db_datareader: Can SELECT from all user tables and views
-- This is needed for reading data (SELECT queries)
IF IS_ROLEMEMBER('db_datareader', 'library_app_user') = 0
BEGIN
    ALTER ROLE db_datareader ADD MEMBER library_app_user;
    PRINT '✅ Granted db_datareader role (can read all tables).';
END

-- db_datawriter: Can INSERT, UPDATE, DELETE in all user tables
-- This is needed for write operations
IF IS_ROLEMEMBER('db_datawriter', 'library_app_user') = 0
BEGIN
    ALTER ROLE db_datawriter ADD MEMBER library_app_user;
    PRINT '✅ Granted db_datawriter role (can write to all tables).';
END

-- db_ddladmin: Can run DDL commands (CREATE, ALTER, DROP)
-- This is needed for running migrations that create/modify schema
IF IS_ROLEMEMBER('db_ddladmin', 'library_app_user') = 0
BEGIN
    ALTER ROLE db_ddladmin ADD MEMBER library_app_user;
    PRINT '✅ Granted db_ddladmin role (can create/modify schema).';
END

-- EXECUTE: Can execute stored procedures and functions
-- Grant at database level for all procedures/functions
IF NOT EXISTS (
    SELECT 1 FROM sys.database_permissions
    WHERE grantee_principal_id = USER_ID('library_app_user')
    AND permission_name = 'EXECUTE'
    AND state = 'G'
)
BEGIN
    GRANT EXECUTE TO library_app_user;
    PRINT '✅ Granted EXECUTE permission (can run procedures/functions).';
END

-- VIEW DEFINITION: Can see object definitions (useful for debugging)
IF NOT EXISTS (
    SELECT 1 FROM sys.database_permissions
    WHERE grantee_principal_id = USER_ID('library_app_user')
    AND permission_name = 'VIEW DEFINITION'
    AND state = 'G'
)
BEGIN
    GRANT VIEW DEFINITION TO library_app_user;
    PRINT '✅ Granted VIEW DEFINITION permission (can view object definitions).';
END

-- =============================================
-- Step 5: Summary
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ Application User Setup Completed!';
PRINT '========================================';
PRINT '';
PRINT '📋 Summary:';
PRINT '   Database: LibraryDb';
PRINT '   Login: library_app_user';
PRINT '   Permissions:';
PRINT '     ✅ Read all tables (db_datareader)';
PRINT '     ✅ Write to all tables (db_datawriter)';
PRINT '     ✅ Create/modify schema (db_ddladmin)';
PRINT '     ✅ Execute procedures/functions';
PRINT '     ✅ View object definitions';
PRINT '';
PRINT '🔒 Security Notes:';
PRINT '   ❌ Cannot drop the database';
PRINT '   ❌ Cannot modify server settings';
PRINT '   ❌ Cannot access other databases';
PRINT '   ❌ Cannot create/modify other logins';
PRINT '';
PRINT '📝 Connection String for Application:';
PRINT 'Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;';
PRINT '';
PRINT '⚠️  IMPORTANT: Change password in production!';
PRINT '========================================';
GO
