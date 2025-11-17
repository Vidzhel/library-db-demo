-- =============================================
-- Lab 4: Database Management - Access Control, Backup & Recovery, Security
-- =============================================
-- This lab covers essential database management concepts including:
--   1. Access Control (Roles, Users, Permissions)
--   2. Backup and Recovery Strategies
--   3. Security Best Practices
--
-- Target Database: LibraryDb
-- SQL Server Version: 2019+
--
-- IMPORTANT: This script is designed for educational purposes.
-- Some commands require elevated privileges (sysadmin/securityadmin roles).
-- =============================================

USE LibraryDb;
GO

-- =============================================
-- SECTION 1: ACCESS CONTROL & PERMISSIONS
-- =============================================
-- Access control is the foundation of database security. It determines:
--   - WHO can access the database
--   - WHAT operations they can perform
--   - WHERE (which objects) they can access
--
-- Key Concepts:
--   - LOGIN: Server-level identity (authentication)
--   - USER: Database-level identity (mapped from login)
--   - ROLE: Group of permissions that can be assigned to users
--   - PERMISSION: Specific right to perform an action (SELECT, INSERT, UPDATE, DELETE, EXECUTE, etc.)
-- =============================================

PRINT '========================================';
PRINT 'SECTION 1: ACCESS CONTROL & PERMISSIONS';
PRINT '========================================';
GO

-- =============================================
-- 1.1 PERMISSION MATRIX
-- =============================================
-- Below is a visual representation of our permission model:
--
-- +------------------+--------+--------+--------+--------+---------+-------------+
-- | ROLE             | SELECT | INSERT | UPDATE | DELETE | EXECUTE | DESCRIPTION |
-- +------------------+--------+--------+--------+--------+---------+-------------+
-- | DataAnalyst      | ✓      | ✗      | ✗      | ✗      | ✓ (SP)  | Read-only access, can run reports |
-- | Developer        | ✓      | ✓      | ✓      | ✗      | ✓       | Full DML except DELETE |
-- | Manager          | ✓      | ✓      | ✓      | ✓      | ✓       | Full DML access |
-- | DBAdmin          | ✓      | ✓      | ✓      | ✓      | ✓       | Full control (DDL + DML) |
-- +------------------+--------+--------+--------+--------+---------+-------------+
--
-- Table-Specific Restrictions:
--   - DataAnalyst: Can only read from Books, Members, Loans, and Views
--   - Developer: Cannot delete any records (data preservation)
--   - Manager: Full access to operational tables, no schema changes
--   - DBAdmin: Can create/alter/drop objects (DDL operations)
-- =============================================

-- =============================================
-- 1.2 CREATE DATABASE ROLES
-- =============================================
-- Database roles are containers for permissions. Instead of granting
-- permissions to individual users, we grant them to roles and then
-- assign users to roles. This follows the principle of RBAC
-- (Role-Based Access Control).
--
-- Benefits:
--   - Easier to manage (change role permissions vs. individual user permissions)
--   - Consistent security model across similar users
--   - Simplified auditing and compliance
-- =============================================

-- Role 1: DataAnalyst - Read-only access for business intelligence
IF DATABASE_PRINCIPAL_ID('DataAnalystRole') IS NULL
BEGIN
    CREATE ROLE DataAnalystRole;
    PRINT '✅ Role [DataAnalystRole] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Role [DataAnalystRole] already exists.';
END
GO

-- Role 2: Developer - Full DML (except DELETE) for application development
IF DATABASE_PRINCIPAL_ID('DeveloperRole') IS NULL
BEGIN
    CREATE ROLE DeveloperRole;
    PRINT '✅ Role [DeveloperRole] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Role [DeveloperRole] already exists.';
END
GO

-- Role 3: Manager - Full DML access for business operations
IF DATABASE_PRINCIPAL_ID('ManagerRole') IS NULL
BEGIN
    CREATE ROLE ManagerRole;
    PRINT '✅ Role [ManagerRole] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Role [ManagerRole] already exists.';
END
GO

-- Role 4: DBAdmin - Full control for database administration
IF DATABASE_PRINCIPAL_ID('DBAdminRole') IS NULL
BEGIN
    CREATE ROLE DBAdminRole;
    PRINT '✅ Role [DBAdminRole] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Role [DBAdminRole] already exists.';
END
GO

-- =============================================
-- 1.3 GRANT PERMISSIONS TO ROLES
-- =============================================
-- The GRANT statement gives specific permissions to database principals
-- (users or roles). Permissions can be granted at different levels:
--   - Database level: GRANT CONNECT, CREATE TABLE, etc.
--   - Schema level: GRANT SELECT ON SCHEMA::dbo
--   - Object level: GRANT SELECT ON dbo.Books
--   - Column level: GRANT SELECT ON dbo.Members(FirstName, LastName)
--
-- Syntax: GRANT <permission> ON <object> TO <principal>
-- =============================================

PRINT '';
PRINT 'Granting permissions to roles...';
GO

-- DataAnalystRole Permissions:
-- This role needs to READ data but cannot modify anything
GRANT SELECT ON dbo.Books TO DataAnalystRole;
GRANT SELECT ON dbo.Authors TO DataAnalystRole;
GRANT SELECT ON dbo.BookAuthors TO DataAnalystRole;
GRANT SELECT ON dbo.Categories TO DataAnalystRole;
GRANT SELECT ON dbo.Members TO DataAnalystRole;
GRANT SELECT ON dbo.Loans TO DataAnalystRole;

-- Grant access to views (for reports)
IF OBJECT_ID('dbo.vw_BookReadingTrends', 'V') IS NOT NULL
    GRANT SELECT ON dbo.vw_BookReadingTrends TO DataAnalystRole;

IF OBJECT_ID('dbo.vw_CategoryHierarchy', 'V') IS NOT NULL
    GRANT SELECT ON dbo.vw_CategoryHierarchy TO DataAnalystRole;

-- Grant EXECUTE permission on reporting stored procedures only
IF OBJECT_ID('dbo.sp_GetMonthlyLoanStatistics', 'P') IS NOT NULL
    GRANT EXECUTE ON dbo.sp_GetMonthlyLoanStatistics TO DataAnalystRole;

IF OBJECT_ID('dbo.sp_GetPopularBooks', 'P') IS NOT NULL
    GRANT EXECUTE ON dbo.sp_GetPopularBooks TO DataAnalystRole;

PRINT '✅ DataAnalystRole permissions granted (SELECT on tables/views, EXECUTE on report SPs).';
GO

-- DeveloperRole Permissions:
-- This role can perform INSERT, UPDATE, SELECT but NOT DELETE
-- This ensures developers can test features without accidentally losing data
GRANT SELECT, INSERT, UPDATE ON dbo.Books TO DeveloperRole;
GRANT SELECT, INSERT, UPDATE ON dbo.Authors TO DeveloperRole;
GRANT SELECT, INSERT, UPDATE ON dbo.BookAuthors TO DeveloperRole;
GRANT SELECT, INSERT, UPDATE ON dbo.Categories TO DeveloperRole;
GRANT SELECT, INSERT, UPDATE ON dbo.Members TO DeveloperRole;
GRANT SELECT, INSERT, UPDATE ON dbo.Loans TO DeveloperRole;

-- Grant EXECUTE on all stored procedures (developers need to test application logic)
GRANT EXECUTE ON SCHEMA::dbo TO DeveloperRole;

PRINT '✅ DeveloperRole permissions granted (SELECT, INSERT, UPDATE on tables, EXECUTE on schema).';
GO

-- ManagerRole Permissions:
-- Full DML access (SELECT, INSERT, UPDATE, DELETE) for business operations
-- Managers may need to correct data, handle refunds, deletions, etc.
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Books TO ManagerRole;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Authors TO ManagerRole;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.BookAuthors TO ManagerRole;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Categories TO ManagerRole;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Members TO ManagerRole;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Loans TO ManagerRole;

-- Full execute permissions
GRANT EXECUTE ON SCHEMA::dbo TO ManagerRole;

PRINT '✅ ManagerRole permissions granted (SELECT, INSERT, UPDATE, DELETE, EXECUTE).';
GO

-- DBAdminRole Permissions:
-- Database administrators need full control including DDL operations
-- (CREATE, ALTER, DROP tables, indexes, procedures, etc.)
GRANT CONTROL ON DATABASE::LibraryDb TO DBAdminRole;

-- Alternatively, you could grant specific DDL permissions:
-- GRANT CREATE TABLE, CREATE PROCEDURE, CREATE VIEW, ALTER ANY SCHEMA TO DBAdminRole;

PRINT '✅ DBAdminRole permissions granted (CONTROL on database - full DDL/DML access).';
GO

-- =============================================
-- 1.4 CREATE LOGINS AND USERS
-- =============================================
-- LOGIN vs USER:
--   - LOGIN: Server-level principal (authentication) - "Can I connect to the server?"
--   - USER: Database-level principal (authorization) - "What can I do in this database?"
--
-- Process:
--   1. CREATE LOGIN at server level (master database)
--   2. CREATE USER in each database, mapped to the login
--   3. Add user to appropriate role(s)
--
-- NOTE: Creating logins requires ALTER ANY LOGIN permission (typically sysadmin role).
-- For educational purposes, these commands are shown but may fail without proper privileges.
-- =============================================

PRINT '';
PRINT 'Creating logins and users...';
PRINT 'ℹ️  Note: LOGIN creation requires elevated privileges. These may fail in restricted environments.';
GO

-- Switch to master to create logins
USE master;
GO

-- User 1: Alice (Data Analyst)
IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'alice_analyst')
BEGIN
    -- Password should follow strong password policies in production
    -- Example: Minimum 8 characters, uppercase, lowercase, numbers, special chars
    CREATE LOGIN alice_analyst WITH PASSWORD = 'Analyst@2024';
    PRINT '✅ Login [alice_analyst] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Login [alice_analyst] already exists.';
END
GO

-- User 2: Bob (Developer)
IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'bob_developer')
BEGIN
    CREATE LOGIN bob_developer WITH PASSWORD = 'Developer@2024';
    PRINT '✅ Login [bob_developer] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Login [bob_developer] already exists.';
END
GO

-- User 3: Carol (Manager)
IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'carol_manager')
BEGIN
    CREATE LOGIN carol_manager WITH PASSWORD = 'Manager@2024';
    PRINT '✅ Login [carol_manager] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Login [carol_manager] already exists.';
END
GO

-- User 4: David (Database Administrator)
IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'david_dbadmin')
BEGIN
    CREATE LOGIN david_dbadmin WITH PASSWORD = 'DBAdmin@2024';
    PRINT '✅ Login [david_dbadmin] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Login [david_dbadmin] already exists.';
END
GO

-- Switch back to LibraryDb to create users
USE LibraryDb;
GO

-- Create database users and assign to roles
IF DATABASE_PRINCIPAL_ID('alice_analyst') IS NULL
BEGIN
    CREATE USER alice_analyst FOR LOGIN alice_analyst;
    ALTER ROLE DataAnalystRole ADD MEMBER alice_analyst;
    PRINT '✅ User [alice_analyst] created and added to DataAnalystRole.';
END
ELSE
BEGIN
    PRINT 'ℹ️  User [alice_analyst] already exists.';
END
GO

IF DATABASE_PRINCIPAL_ID('bob_developer') IS NULL
BEGIN
    CREATE USER bob_developer FOR LOGIN bob_developer;
    ALTER ROLE DeveloperRole ADD MEMBER bob_developer;
    PRINT '✅ User [bob_developer] created and added to DeveloperRole.';
END
ELSE
BEGIN
    PRINT 'ℹ️  User [bob_developer] already exists.';
END
GO

IF DATABASE_PRINCIPAL_ID('carol_manager') IS NULL
BEGIN
    CREATE USER carol_manager FOR LOGIN carol_manager;
    ALTER ROLE ManagerRole ADD MEMBER carol_manager;
    PRINT '✅ User [carol_manager] created and added to ManagerRole.';
END
ELSE
BEGIN
    PRINT 'ℹ️  User [carol_manager] already exists.';
END
GO

IF DATABASE_PRINCIPAL_ID('david_dbadmin') IS NULL
BEGIN
    CREATE USER david_dbadmin FOR LOGIN david_dbadmin;
    ALTER ROLE DBAdminRole ADD MEMBER david_dbadmin;
    PRINT '✅ User [david_dbadmin] created and added to DBAdminRole.';
END
ELSE
BEGIN
    PRINT 'ℹ️  User [david_dbadmin] already exists.';
END
GO

-- =============================================
-- 1.5 REVOKE PERMISSIONS (Example)
-- =============================================
-- REVOKE removes previously granted permissions. This is useful when:
--   - Security requirements change
--   - A role no longer needs access to certain objects
--   - Implementing principle of least privilege
--
-- Syntax: REVOKE <permission> ON <object> FROM <principal>
--
-- Example: If we decide DataAnalysts should NOT see member email addresses
-- (for privacy/GDPR compliance), we can revoke column-level access:
-- =============================================

PRINT '';
PRINT 'Example: Revoking sensitive column access from DataAnalystRole...';
GO

-- First, revoke the entire SELECT permission
REVOKE SELECT ON dbo.Members FROM DataAnalystRole;

-- Then grant SELECT on specific columns only (excluding Email and PhoneNumber)
GRANT SELECT ON dbo.Members(
    MemberId,
    MembershipNumber,
    FirstName,
    LastName,
    DateOfBirth,
    MembershipExpiresAt,
    MaxBooksAllowed,
    OutstandingFees,
    CreatedAt,
    UpdatedAt
) TO DataAnalystRole;

PRINT '✅ DataAnalystRole can now only see non-sensitive columns in Members table.';
PRINT '   (Email and PhoneNumber are hidden for privacy compliance)';
GO

-- =============================================
-- 1.6 ROW-LEVEL SECURITY (RLS) - ADVANCED
-- =============================================
-- Row-Level Security (RLS) allows you to control access to rows in a table
-- based on the characteristics of the user executing a query.
--
-- Use Cases:
--   - Multi-tenant applications (users see only their data)
--   - Regional restrictions (users see only their region's data)
--   - Hierarchical access (managers see their department's data)
--
-- Components:
--   1. Security Predicate Function: Returns TABLE with filter logic
--   2. Security Policy: Binds the function to a table
-- =============================================

PRINT '';
PRINT 'Setting up Row-Level Security example...';
GO

-- Example: Analysts can only see loans from the current year
-- Create a security predicate function
IF OBJECT_ID('dbo.fn_SecurityPredicateLoans', 'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_SecurityPredicateLoans;
GO

CREATE FUNCTION dbo.fn_SecurityPredicateLoans(@BorrowedAt DATETIME2)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS fn_SecurityPredicateLoans_Result
    WHERE
        -- DBAdmins and Managers can see all records
        IS_MEMBER('DBAdminRole') = 1
        OR IS_MEMBER('ManagerRole') = 1
        -- Analysts can only see current year loans
        OR (IS_MEMBER('DataAnalystRole') = 1 AND YEAR(@BorrowedAt) = YEAR(GETDATE()))
        -- Developers can see last 2 years
        OR (IS_MEMBER('DeveloperRole') = 1 AND @BorrowedAt >= DATEADD(YEAR, -2, GETDATE()));
GO

PRINT '✅ Security predicate function created: fn_SecurityPredicateLoans';
GO

-- Create security policy
IF EXISTS (SELECT * FROM sys.security_policies WHERE name = 'LoansSecurityPolicy')
    DROP SECURITY POLICY dbo.LoansSecurityPolicy;
GO

CREATE SECURITY POLICY dbo.LoansSecurityPolicy
ADD FILTER PREDICATE dbo.fn_SecurityPredicateLoans(BorrowedAt)
ON dbo.Loans
WITH (STATE = ON);
GO

PRINT '✅ Row-Level Security policy applied to Loans table.';
PRINT '   - DBAdmins & Managers: See all records';
PRINT '   - DataAnalysts: See only current year';
PRINT '   - Developers: See last 2 years';
GO

-- =============================================
-- 1.7 TESTING PERMISSIONS
-- =============================================
-- To test permissions, you would need to:
--   1. Connect as the specific user (e.g., EXECUTE AS USER = 'alice_analyst')
--   2. Run queries to verify access
--   3. Revert back to original context
--
-- Example test queries (uncomment to test):
-- =============================================

PRINT '';
PRINT 'Permission testing examples (uncommented to execute):';
PRINT '-- EXECUTE AS USER = ''alice_analyst'';';
PRINT '-- SELECT * FROM dbo.Books; -- Should succeed';
PRINT '-- INSERT INTO dbo.Books (...) VALUES (...); -- Should fail';
PRINT '-- REVERT;';
GO

/*
-- Uncomment to test as alice_analyst (DataAnalyst)
EXECUTE AS USER = 'alice_analyst';
PRINT 'Testing as alice_analyst (DataAnalyst)...';

-- This should succeed (SELECT permission granted)
SELECT TOP 5 Title, ISBN FROM dbo.Books;

-- This should fail (INSERT permission not granted)
-- INSERT INTO dbo.Books (Title, ISBN, ...) VALUES ('Test', 'XXX', ...);

REVERT;
GO
*/

/*
-- Uncomment to test as bob_developer (Developer)
EXECUTE AS USER = 'bob_developer';
PRINT 'Testing as bob_developer (Developer)...';

-- These should succeed (SELECT, INSERT, UPDATE granted)
SELECT TOP 5 Title FROM dbo.Books;
-- UPDATE dbo.Books SET Title = 'Updated Title' WHERE BookId = 999;

-- This should fail (DELETE permission not granted)
-- DELETE FROM dbo.Books WHERE BookId = 999;

REVERT;
GO
*/

-- =============================================
-- SECTION 2: BACKUP & RECOVERY
-- =============================================
-- Database backups are critical for:
--   - Disaster recovery (hardware failure, corruption)
--   - Point-in-time recovery (restore to before error occurred)
--   - Compliance and auditing requirements
--   - Migration and testing (restore to dev/test environments)
--
-- SQL Server Backup Types:
--   1. Full Backup: Complete copy of database (base for all other backups)
--   2. Differential Backup: Changes since last FULL backup
--   3. Transaction Log Backup: Changes since last LOG backup (allows point-in-time recovery)
--
-- Recovery Models:
--   - SIMPLE: Automatic log truncation, no point-in-time recovery
--   - FULL: Complete logging, point-in-time recovery supported
--   - BULK_LOGGED: Minimal logging for bulk operations, limited point-in-time recovery
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 2: BACKUP & RECOVERY';
PRINT '========================================';
GO

-- =============================================
-- 2.1 RECOVERY MODELS
-- =============================================
-- The recovery model determines how transactions are logged and how
-- you can recover your database.
--
-- SIMPLE:
--   - Pros: Minimal log space usage, automatic management
--   - Cons: Can only recover to last full/differential backup
--   - Use case: Development databases, data warehouses with nightly ETL
--
-- FULL:
--   - Pros: Point-in-time recovery, supports log shipping and mirroring
--   - Cons: Requires regular log backups, uses more storage
--   - Use case: Production databases with critical data
--
-- BULK_LOGGED:
--   - Pros: Better performance for bulk operations (minimal logging)
--   - Cons: Limited point-in-time recovery during bulk operations
--   - Use case: Large data imports/exports
-- =============================================

-- Check current recovery model
DECLARE @RecoveryModel NVARCHAR(60);
SELECT @RecoveryModel = recovery_model_desc
FROM sys.databases
WHERE name = 'LibraryDb';

PRINT 'Current recovery model for LibraryDb: ' + @RecoveryModel;
GO

-- Set recovery model to FULL (recommended for production)
-- NOTE: Requires ALTER DATABASE permission
PRINT 'Setting recovery model to FULL...';
ALTER DATABASE LibraryDb SET RECOVERY FULL;
PRINT '✅ Recovery model set to FULL.';
PRINT '   This enables point-in-time recovery with transaction log backups.';
GO

-- =============================================
-- 2.2 FULL DATABASE BACKUP
-- =============================================
-- A full backup captures the entire database at a specific point in time.
-- This is the foundation for all recovery scenarios.
--
-- Syntax:
--   BACKUP DATABASE <database_name>
--   TO DISK = '<file_path>'
--   WITH <options>;
--
-- Common Options:
--   - COMPRESSION: Reduces backup file size (requires Enterprise/Standard edition)
--   - CHECKSUM: Validates backup integrity
--   - STATS: Shows progress percentage
--   - INIT: Overwrites existing backup file
--   - FORMAT: Creates new media set
-- =============================================

PRINT '';
PRINT 'Performing FULL database backup...';
PRINT 'ℹ️  Note: Backup location must be accessible by SQL Server service account.';
PRINT '   Default path: C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup\';
GO

-- Create backup
DECLARE @BackupPath NVARCHAR(500);
DECLARE @BackupFile NVARCHAR(500);
DECLARE @BackupDate NVARCHAR(50);

-- Format: LibraryDb_FULL_YYYYMMDD_HHMMSS.bak
SET @BackupDate = CONVERT(NVARCHAR(50), GETDATE(), 112) + '_' +
                  REPLACE(CONVERT(NVARCHAR(50), GETDATE(), 108), ':', '');
SET @BackupFile = 'LibraryDb_FULL_' + @BackupDate + '.bak';

-- Change this path to your backup location
SET @BackupPath = 'C:\Backup\' + @BackupFile;

PRINT 'Backup file: ' + @BackupPath;

-- Uncomment to execute backup (requires permissions and valid path)
/*
BACKUP DATABASE LibraryDb
TO DISK = @BackupPath
WITH
    COMPRESSION,        -- Compress backup (reduces size by ~60-70%)
    CHECKSUM,           -- Verify backup integrity
    STATS = 10,         -- Show progress every 10%
    INIT,               -- Overwrite if file exists
    NAME = 'LibraryDb Full Backup',
    DESCRIPTION = 'Full backup of LibraryDb for disaster recovery';

PRINT '✅ Full backup completed successfully.';
PRINT '   Location: ' + @BackupPath;
*/

PRINT 'ℹ️  Backup command prepared (uncomment to execute).';
GO

-- =============================================
-- 2.3 DIFFERENTIAL BACKUP
-- =============================================
-- A differential backup captures only the data that has changed since
-- the last FULL backup. This reduces backup time and storage.
--
-- Restore Process:
--   1. Restore the last FULL backup (WITH NORECOVERY)
--   2. Restore the latest DIFFERENTIAL backup (WITH RECOVERY)
--
-- Benefits:
--   - Faster than full backups
--   - Smaller file size
--   - Faster recovery than using transaction logs
-- =============================================

PRINT '';
PRINT 'Performing DIFFERENTIAL backup...';
GO

DECLARE @DiffBackupFile NVARCHAR(500);
DECLARE @DiffBackupPath NVARCHAR(500);
DECLARE @DiffBackupDate NVARCHAR(50);

SET @DiffBackupDate = CONVERT(NVARCHAR(50), GETDATE(), 112) + '_' +
                      REPLACE(CONVERT(NVARCHAR(50), GETDATE(), 108), ':', '');
SET @DiffBackupFile = 'LibraryDb_DIFF_' + @DiffBackupDate + '.bak';
SET @DiffBackupPath = 'C:\Backup\' + @DiffBackupFile;

PRINT 'Differential backup file: ' + @DiffBackupPath;

/*
BACKUP DATABASE LibraryDb
TO DISK = @DiffBackupPath
WITH
    DIFFERENTIAL,       -- Specify differential backup
    COMPRESSION,
    CHECKSUM,
    STATS = 10,
    INIT,
    NAME = 'LibraryDb Differential Backup',
    DESCRIPTION = 'Differential backup since last full backup';

PRINT '✅ Differential backup completed successfully.';
*/

PRINT 'ℹ️  Differential backup command prepared (uncomment to execute).';
GO

-- =============================================
-- 2.4 TRANSACTION LOG BACKUP
-- =============================================
-- Transaction log backups capture all transactions since the last log backup.
-- Required for:
--   - Point-in-time recovery
--   - Minimizing data loss (can backup every 15 minutes)
--   - Managing log file size
--
-- IMPORTANT: Only available in FULL or BULK_LOGGED recovery model.
--
-- Backup Strategy Example:
--   - Full backup: Sunday 2:00 AM
--   - Differential backup: Daily 2:00 AM
--   - Log backup: Every 15 minutes
-- =============================================

PRINT '';
PRINT 'Performing TRANSACTION LOG backup...';
GO

DECLARE @LogBackupFile NVARCHAR(500);
DECLARE @LogBackupPath NVARCHAR(500);
DECLARE @LogBackupDate NVARCHAR(50);

SET @LogBackupDate = CONVERT(NVARCHAR(50), GETDATE(), 112) + '_' +
                     REPLACE(CONVERT(NVARCHAR(50), GETDATE(), 108), ':', '');
SET @LogBackupFile = 'LibraryDb_LOG_' + @LogBackupDate + '.trn';
SET @LogBackupPath = 'C:\Backup\' + @LogBackupFile;

PRINT 'Transaction log backup file: ' + @LogBackupPath;

/*
BACKUP LOG LibraryDb
TO DISK = @LogBackupPath
WITH
    COMPRESSION,
    CHECKSUM,
    STATS = 10,
    INIT,
    NAME = 'LibraryDb Transaction Log Backup',
    DESCRIPTION = 'Transaction log backup for point-in-time recovery';

PRINT '✅ Transaction log backup completed successfully.';
*/

PRINT 'ℹ️  Log backup command prepared (uncomment to execute).';
GO

-- =============================================
-- 2.5 RESTORE DATABASE (Example)
-- =============================================
-- Restoring a database involves:
--   1. Put database in restoring state (WITH NORECOVERY)
--   2. Apply full backup
--   3. Apply differential backup (if available) - WITH NORECOVERY
--   4. Apply log backups in sequence - last one WITH RECOVERY
--   5. Database becomes available
--
-- Point-in-Time Recovery:
--   RESTORE LOG LibraryDb FROM DISK = '...'
--   WITH STOPAT = '2024-01-15 14:30:00', RECOVERY;
--
-- NOTE: Restore operations require database to be offline or not exist.
-- =============================================

PRINT '';
PRINT 'Example RESTORE commands (for reference):';
GO

/*
-- Step 1: Restore Full Backup (WITH NORECOVERY to apply more backups)
RESTORE DATABASE LibraryDb
FROM DISK = 'C:\Backup\LibraryDb_FULL_20241117_120000.bak'
WITH
    NORECOVERY,
    REPLACE,        -- Overwrite existing database
    STATS = 10;

-- Step 2: Restore Differential Backup (WITH NORECOVERY if applying logs)
RESTORE DATABASE LibraryDb
FROM DISK = 'C:\Backup\LibraryDb_DIFF_20241117_180000.bak'
WITH
    NORECOVERY,
    STATS = 10;

-- Step 3: Restore Transaction Log Backups (in sequence)
RESTORE LOG LibraryDb
FROM DISK = 'C:\Backup\LibraryDb_LOG_20241117_183000.trn'
WITH
    NORECOVERY,
    STATS = 10;

RESTORE LOG LibraryDb
FROM DISK = 'C:\Backup\LibraryDb_LOG_20241117_184500.trn'
WITH
    NORECOVERY,
    STATS = 10;

-- Step 4: Final log restore with RECOVERY (brings database online)
RESTORE LOG LibraryDb
FROM DISK = 'C:\Backup\LibraryDb_LOG_20241117_190000.trn'
WITH
    RECOVERY,       -- Brings database online
    STATS = 10;

PRINT '✅ Database restored successfully.';

-- Point-in-Time Recovery Example:
-- Restore to specific date/time (e.g., before data was accidentally deleted)
RESTORE LOG LibraryDb
FROM DISK = 'C:\Backup\LibraryDb_LOG_20241117_190000.trn'
WITH
    STOPAT = '2024-11-17 18:45:00',  -- Stop at this exact time
    RECOVERY,
    STATS = 10;
*/

PRINT 'ℹ️  Restore examples provided as comments (for disaster recovery reference).';
GO

-- =============================================
-- 2.6 VERIFY BACKUP
-- =============================================
-- Always verify backups to ensure they can be restored successfully.
-- RESTORE VERIFYONLY checks backup integrity without actually restoring.
-- =============================================

PRINT '';
PRINT 'Verifying backup integrity...';
GO

/*
RESTORE VERIFYONLY
FROM DISK = 'C:\Backup\LibraryDb_FULL_20241117_120000.bak'
WITH CHECKSUM;

PRINT '✅ Backup verification completed successfully.';
PRINT '   Backup file is valid and can be restored.';
*/

PRINT 'ℹ️  Use RESTORE VERIFYONLY to check backup integrity (uncomment to execute).';
GO

-- =============================================
-- 2.7 BACKUP BEST PRACTICES
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'BACKUP BEST PRACTICES:';
PRINT '========================================';
PRINT '1. Follow 3-2-1 rule:';
PRINT '   - 3 copies of data (original + 2 backups)';
PRINT '   - 2 different media types (disk + tape/cloud)';
PRINT '   - 1 copy offsite (for disaster recovery)';
PRINT '';
PRINT '2. Test restores regularly (monthly minimum)';
PRINT '   - Verify backups actually work';
PRINT '   - Time the restore process';
PRINT '   - Document restore procedures';
PRINT '';
PRINT '3. Automate backups using SQL Server Agent or scheduled tasks';
PRINT '   - Full: Weekly (Sundays)';
PRINT '   - Differential: Daily';
PRINT '   - Log: Every 15-30 minutes (production)';
PRINT '';
PRINT '4. Monitor backup status and retention:';
PRINT '   - Check for failed backups daily';
PRINT '   - Maintain 30 days of backups minimum';
PRINT '   - Archive monthly backups for compliance';
PRINT '';
PRINT '5. Document your backup/restore plan:';
PRINT '   - RTO (Recovery Time Objective)';
PRINT '   - RPO (Recovery Point Objective)';
PRINT '   - Contact information for emergencies';
PRINT '========================================';
GO

-- =============================================
-- SECTION 3: SECURITY BEST PRACTICES
-- =============================================
-- Database security is multi-layered and includes:
--   - SQL Injection prevention
--   - Encryption (at rest and in transit)
--   - Auditing and monitoring
--   - Principle of least privilege
--   - Regular security updates
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 3: SECURITY BEST PRACTICES';
PRINT '========================================';
GO

-- =============================================
-- 3.1 SQL INJECTION PREVENTION
-- =============================================
-- SQL Injection is one of the most dangerous vulnerabilities (OWASP Top 10).
-- It occurs when untrusted user input is concatenated directly into SQL queries.
--
-- VULNERABLE CODE EXAMPLE (DO NOT USE):
--   string query = "SELECT * FROM Users WHERE Username = '" + userInput + "'";
--   // If userInput = "admin' OR '1'='1", query becomes:
--   // SELECT * FROM Users WHERE Username = 'admin' OR '1'='1'
--   // This returns ALL users, bypassing authentication!
--
-- SECURE CODE EXAMPLE (USE THIS):
--   Always use parameterized queries or stored procedures with parameters.
-- =============================================

PRINT '';
PRINT 'SQL Injection Prevention Examples:';
GO

-- Create a stored procedure that safely handles user input
IF OBJECT_ID('dbo.sp_GetBookByISBN', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBookByISBN;
GO

CREATE PROCEDURE dbo.sp_GetBookByISBN
    @ISBN NVARCHAR(20)  -- Parameter (not concatenated into query)
AS
BEGIN
    SET NOCOUNT ON;

    -- This is SAFE because @ISBN is a parameter, not concatenated string
    SELECT
        BookId,
        Title,
        ISBN,
        Publisher,
        PublishedDate,
        PageCount
    FROM dbo.Books
    WHERE ISBN = @ISBN;  -- Parameterized query prevents injection
END
GO

PRINT '✅ Secure stored procedure created: sp_GetBookByISBN';
PRINT '   Always use parameters, never concatenate user input into queries!';
GO

-- Demonstrate the difference:
PRINT '';
PRINT '-- VULNERABLE (Dynamic SQL without parameters):';
PRINT '-- DECLARE @UserInput NVARCHAR(50) = ''ABC123'' OR ''1''=''1'';';
PRINT '-- DECLARE @SQL NVARCHAR(MAX) = ''SELECT * FROM Books WHERE ISBN = '''''' + @UserInput + '''''''';';
PRINT '-- EXEC sp_executesql @SQL;  -- DANGER: SQL Injection possible!';
PRINT '';
PRINT '-- SECURE (Parameterized query):';
PRINT '-- EXEC sp_GetBookByISBN @ISBN = ''ABC123'';  -- SAFE: Parameter is sanitized';
GO

-- =============================================
-- 3.2 ENCRYPTION
-- =============================================
-- Encryption protects sensitive data from unauthorized access.
--
-- Types of Encryption in SQL Server:
--   1. Transparent Data Encryption (TDE): Encrypts entire database at rest
--   2. Column-Level Encryption: Encrypts specific columns
--   3. Always Encrypted: Client-side encryption (data never decrypted on server)
--   4. Connection Encryption: TLS/SSL for data in transit
--
-- Example: Column-level encryption for sensitive member data
-- =============================================

PRINT '';
PRINT 'Column-Level Encryption Example:';
GO

-- Add encrypted column for credit card numbers (example)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Members') AND name = 'CreditCardEncrypted')
BEGIN
    ALTER TABLE dbo.Members
    ADD CreditCardEncrypted VARBINARY(256);

    PRINT '✅ Encrypted column added to Members table.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Encrypted column already exists.';
END
GO

-- Example: Encrypt and decrypt data using symmetric key
/*
-- Create master key (one per database)
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'StrongP@ssw0rd123!';

-- Create certificate
CREATE CERTIFICATE MemberDataCert
WITH SUBJECT = 'Certificate for Member PII Encryption';

-- Create symmetric key
CREATE SYMMETRIC KEY MemberDataKey
WITH ALGORITHM = AES_256
ENCRYPTION BY CERTIFICATE MemberDataCert;

-- Encrypt data (INSERT/UPDATE)
OPEN SYMMETRIC KEY MemberDataKey
DECRYPTION BY CERTIFICATE MemberDataCert;

UPDATE dbo.Members
SET CreditCardEncrypted = ENCRYPTBYKEY(KEY_GUID('MemberDataKey'), '4111-1111-1111-1111')
WHERE MemberId = 1;

CLOSE SYMMETRIC KEY MemberDataKey;

-- Decrypt data (SELECT)
OPEN SYMMETRIC KEY MemberDataKey
DECRYPTION BY CERTIFICATE MemberDataCert;

SELECT
    MemberId,
    FirstName,
    LastName,
    CONVERT(NVARCHAR(50), DECRYPTBYKEY(CreditCardEncrypted)) AS CreditCardNumber
FROM dbo.Members
WHERE MemberId = 1;

CLOSE SYMMETRIC KEY MemberDataKey;
*/

PRINT 'ℹ️  Encryption setup commands provided as reference (uncomment to use).';
PRINT '   In production, use Azure Key Vault or HSM for key management.';
GO

-- Simple example using passphrase (for demonstration only)
PRINT '';
PRINT 'Simple passphrase encryption (for demonstration):';
GO

-- Encrypt sensitive data
DECLARE @SensitiveData NVARCHAR(100) = 'Member SSN: 123-45-6789';
DECLARE @Passphrase NVARCHAR(128) = 'MySecretP@ssphrase2024';
DECLARE @EncryptedData VARBINARY(256);

SET @EncryptedData = ENCRYPTBYPASSPHRASE(@Passphrase, @SensitiveData);

PRINT 'Original: ' + @SensitiveData;
PRINT 'Encrypted: ' + CAST(@EncryptedData AS NVARCHAR(MAX));

-- Decrypt data
DECLARE @DecryptedData NVARCHAR(100);
SET @DecryptedData = CAST(DECRYPTBYPASSPHRASE(@Passphrase, @EncryptedData) AS NVARCHAR(100));

PRINT 'Decrypted: ' + @DecryptedData;
PRINT '✅ Encryption/Decryption successful.';
GO

-- =============================================
-- 3.3 AUDITING
-- =============================================
-- Auditing tracks and logs database activities for:
--   - Compliance (SOX, HIPAA, GDPR, PCI-DSS)
--   - Security monitoring (detect unauthorized access)
--   - Forensics (investigate incidents)
--
-- SQL Server Audit Components:
--   1. Server Audit: Defines where audit logs are stored
--   2. Server Audit Specification: What server-level events to audit
--   3. Database Audit Specification: What database-level events to audit
-- =============================================

PRINT '';
PRINT 'Auditing Configuration Example:';
PRINT 'ℹ️  Requires ALTER ANY SERVER AUDIT permission (sysadmin role).';
GO

/*
-- Create server audit (logs to file)
USE master;
GO

CREATE SERVER AUDIT LibraryDb_Audit
TO FILE
(
    FILEPATH = 'C:\AuditLogs\',
    MAXSIZE = 10 MB,
    MAX_ROLLOVER_FILES = 20,
    RESERVE_DISK_SPACE = OFF
)
WITH (ON_FAILURE = CONTINUE);  -- Continue if logging fails

-- Enable audit
ALTER SERVER AUDIT LibraryDb_Audit
WITH (STATE = ON);

-- Create database audit specification
USE LibraryDb;
GO

CREATE DATABASE AUDIT SPECIFICATION LibraryDb_DatabaseAudit
FOR SERVER AUDIT LibraryDb_Audit
ADD (SELECT, INSERT, UPDATE, DELETE ON dbo.Members BY public),
ADD (SELECT, INSERT, UPDATE, DELETE ON dbo.Loans BY public),
ADD (EXECUTE ON dbo.sp_GetBookByISBN BY public)
WITH (STATE = ON);

PRINT '✅ Auditing enabled for sensitive tables and procedures.';
PRINT '   Logs location: C:\AuditLogs\';

-- View audit logs
SELECT
    event_time,
    action_id,
    succeeded,
    server_principal_name,
    database_principal_name,
    object_name,
    statement
FROM sys.fn_get_audit_file('C:\AuditLogs\*.sqlaudit', DEFAULT, DEFAULT)
ORDER BY event_time DESC;
*/

PRINT 'ℹ️  Auditing configuration provided as reference (uncomment to enable).';
GO

-- =============================================
-- 3.4 SECURITY CHECKLIST
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'DATABASE SECURITY CHECKLIST:';
PRINT '========================================';
PRINT '';
PRINT '✓ AUTHENTICATION & ACCESS CONTROL:';
PRINT '  □ Use Windows Authentication when possible (more secure than SQL Auth)';
PRINT '  □ Enforce strong password policies (ALTER LOGIN ... CHECK_POLICY = ON)';
PRINT '  □ Disable ''sa'' account or rename it';
PRINT '  □ Remove unused logins and users regularly';
PRINT '  □ Implement least privilege (grant minimum permissions needed)';
PRINT '  □ Use roles instead of granting permissions directly to users';
PRINT '';
PRINT '✓ SQL INJECTION PREVENTION:';
PRINT '  □ Always use parameterized queries';
PRINT '  □ Use stored procedures with parameters';
PRINT '  □ Validate and sanitize all user input';
PRINT '  □ Never concatenate user input into dynamic SQL';
PRINT '  □ Use ORM frameworks (Entity Framework) that prevent injection';
PRINT '';
PRINT '✓ ENCRYPTION:';
PRINT '  □ Enable Transparent Data Encryption (TDE) for databases at rest';
PRINT '  □ Encrypt backups (BACKUP DATABASE ... WITH ENCRYPTION)';
PRINT '  □ Use SSL/TLS for connections (ENCRYPT=TRUE in connection string)';
PRINT '  □ Encrypt sensitive columns (SSN, credit cards, passwords)';
PRINT '  □ Store encryption keys securely (Azure Key Vault, HSM)';
PRINT '';
PRINT '✓ AUDITING & MONITORING:';
PRINT '  □ Enable SQL Server Audit for sensitive tables';
PRINT '  □ Log failed login attempts (Server Audit Specification)';
PRINT '  □ Monitor for suspicious activity (unusual query patterns)';
PRINT '  □ Review audit logs regularly';
PRINT '  □ Set up alerts for security events';
PRINT '';
PRINT '✓ NETWORK SECURITY:';
PRINT '  □ Change default SQL Server port (1433)';
PRINT '  □ Use firewall rules to restrict IP addresses';
PRINT '  □ Disable SQL Server Browser service if not needed';
PRINT '  □ Use VPN for remote access';
PRINT '';
PRINT '✓ UPDATES & PATCHES:';
PRINT '  □ Apply SQL Server security updates regularly';
PRINT '  □ Keep SQL Server up to date with Cumulative Updates';
PRINT '  □ Test patches in dev/staging before production';
PRINT '';
PRINT '✓ DATA PROTECTION:';
PRINT '  □ Implement row-level security where appropriate';
PRINT '  □ Mask sensitive data in non-production environments';
PRINT '  □ Use Dynamic Data Masking for PII';
PRINT '  □ Implement soft deletes (IsDeleted flag) instead of hard deletes';
PRINT '';
PRINT '✓ BACKUP & RECOVERY:';
PRINT '  □ Follow 3-2-1 backup rule';
PRINT '  □ Encrypt backups';
PRINT '  □ Test restores regularly';
PRINT '  □ Store backups offsite';
PRINT '========================================';
GO

-- =============================================
-- 3.5 QUERY SYSTEM SECURITY METADATA
-- =============================================
-- These queries help you audit your security configuration
-- =============================================

PRINT '';
PRINT 'Security Audit Queries:';
GO

PRINT '';
PRINT '-- List all database users and their roles:';
/*
SELECT
    dp.name AS UserName,
    dp.type_desc AS UserType,
    r.name AS RoleName
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON dp.principal_id = drm.member_principal_id
LEFT JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
WHERE dp.type IN ('S', 'U', 'G')  -- SQL user, Windows user, Windows group
ORDER BY dp.name;
*/

PRINT '';
PRINT '-- List all permissions granted to a specific user:';
/*
SELECT
    USER_NAME(grantee_principal_id) AS Grantee,
    OBJECT_SCHEMA_NAME(major_id) + '.' + OBJECT_NAME(major_id) AS Object,
    permission_name,
    state_desc
FROM sys.database_permissions
WHERE USER_NAME(grantee_principal_id) = 'alice_analyst'
ORDER BY Object, permission_name;
*/

PRINT '';
PRINT '-- List all logins and their password policy settings:';
/*
USE master;
SELECT
    name AS LoginName,
    type_desc AS LoginType,
    is_disabled,
    is_policy_checked AS PasswordPolicyEnforced,
    is_expiration_checked AS PasswordExpirationEnforced,
    create_date,
    modify_date
FROM sys.sql_logins
ORDER BY name;
*/

PRINT 'ℹ️  Security audit queries provided as comments (uncomment to execute).';
GO

-- =============================================
-- LAB COMPLETION
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'LAB 4 COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT 'You have learned about:';
PRINT '  ✅ Access Control (Roles, Users, Permissions)';
PRINT '  ✅ Row-Level Security (RLS)';
PRINT '  ✅ Backup Strategies (Full, Differential, Log)';
PRINT '  ✅ Restore Procedures and Point-in-Time Recovery';
PRINT '  ✅ SQL Injection Prevention';
PRINT '  ✅ Data Encryption (Column-level, TDE)';
PRINT '  ✅ Auditing and Monitoring';
PRINT '  ✅ Security Best Practices';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Uncomment and test permission scenarios (EXECUTE AS USER)';
PRINT '  2. Practice backup/restore operations in a test environment';
PRINT '  3. Review security checklist and implement in your projects';
PRINT '  4. Set up automated backups using SQL Server Agent';
PRINT '  5. Enable auditing for compliance requirements';
PRINT '';
PRINT 'Remember: Security is not a one-time task, it''s an ongoing process!';
PRINT '========================================';
GO
