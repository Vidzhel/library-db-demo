-- =============================================
-- Docker Init Script: Create Application Database and User
-- =============================================
-- This is a SIMPLIFIED version for Docker initialization
-- It runs automatically when the container first starts
-- =============================================

USE master;
GO

-- Create database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LibraryDb')
BEGIN
    CREATE DATABASE LibraryDb;
    PRINT '✅ Database LibraryDb created.';
END
GO

USE LibraryDb;
GO

-- Set to SIMPLE recovery for development
ALTER DATABASE LibraryDb SET RECOVERY SIMPLE;
GO

USE master;
GO

-- Create login
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'library_app_user')
BEGIN
    CREATE LOGIN library_app_user
    WITH PASSWORD = N'LibraryApp@2024!',
        CHECK_POLICY = ON,
        CHECK_EXPIRATION = OFF,
        DEFAULT_DATABASE = LibraryDb;
    PRINT '✅ Login library_app_user created.';
END
GO

USE LibraryDb;
GO

-- Create user
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'library_app_user')
BEGIN
    CREATE USER library_app_user FOR LOGIN library_app_user;
    PRINT '✅ User library_app_user created.';
END
GO

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER library_app_user;
ALTER ROLE db_datawriter ADD MEMBER library_app_user;
ALTER ROLE db_ddladmin ADD MEMBER library_app_user;
GRANT EXECUTE TO library_app_user;
GRANT VIEW DEFINITION TO library_app_user;

PRINT '✅ Application user setup completed!';
PRINT 'Connection: Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;';
GO

-- =============================================
-- Create Migration History Table
-- =============================================
-- This table tracks all applied migrations
-- Used by MigrationRunner to prevent re-running migrations
-- and to detect tampering via checksum validation
-- =============================================

USE LibraryDb;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__MigrationsHistory]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[__MigrationsHistory]
    (
        [MigrationVersion] VARCHAR(10) NOT NULL,      -- e.g., "001", "002", "003"
        [FileName] NVARCHAR(255) NOT NULL,            -- e.g., "V001__initial_schema.sql"
        [Checksum] VARCHAR(64) NOT NULL,              -- SHA256 hash for tamper detection
        [AppliedAt] DATETIME2(7) NOT NULL,            -- When migration was applied
        [ExecutionTimeMs] INT NOT NULL,               -- How long it took to execute

        CONSTRAINT [PK_MigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationVersion] ASC)
    );

    PRINT '✅ Table [__MigrationsHistory] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Table [__MigrationsHistory] already exists.';
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Database Initialization Complete!';
PRINT '========================================';
PRINT 'Database: LibraryDb';
PRINT 'User: library_app_user';
PRINT 'Migration tracking: __MigrationsHistory table ready';
PRINT '========================================';
GO
