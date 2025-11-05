-- =============================================
-- Docker Init Script: Create Application Database and User
-- =============================================
-- This is a SIMPLIFIED version for Docker initialization
-- It runs automatically when the container first starts
-- =============================================
--
-- NOTE: SA User Configuration
-- =============================================
-- The SA (System Administrator) user already exists in SQL Server.
-- Its password is configured via the SA_PASSWORD environment variable
-- in docker-compose.yml, which reads from the .env file.
--
-- To change the SA password:
-- 1. Update SA_PASSWORD in the .env file
-- 2. Rebuild the Docker container: docker-compose up --build -d
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

-- =============================================
-- Create Application User
-- =============================================
-- This user is used by the application for normal operations.
-- Username is configured via $(APP_USER) variable (default: library_app_user)
-- Password is configured via $(APP_PASSWORD) variable (default: LibraryApp@2024)
-- Both are read from .env file
-- =============================================

-- Create login
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'$(APP_USER)')
BEGIN
    CREATE LOGIN [$(APP_USER)]
    WITH PASSWORD = N'$(APP_PASSWORD)',
        CHECK_POLICY = ON,
        CHECK_EXPIRATION = OFF,
        DEFAULT_DATABASE = LibraryDb;
    PRINT '✅ Login $(APP_USER) created.';
END
GO

USE LibraryDb;
GO

-- Create user
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'$(APP_USER)')
BEGIN
    CREATE USER [$(APP_USER)] FOR LOGIN [$(APP_USER)];
    PRINT '✅ User $(APP_USER) created.';
END
GO

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER [$(APP_USER)];
ALTER ROLE db_datawriter ADD MEMBER [$(APP_USER)];
ALTER ROLE db_ddladmin ADD MEMBER [$(APP_USER)];
GRANT EXECUTE TO [$(APP_USER)];
GRANT VIEW DEFINITION TO [$(APP_USER)];

PRINT '✅ Application user setup completed!';
PRINT 'Connection: Server=localhost,1453;Database=LibraryDb;User Id=$(APP_USER);Password=$(APP_PASSWORD);TrustServerCertificate=True;';
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
