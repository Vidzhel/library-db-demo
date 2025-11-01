-- =============================================
-- Migration: V006 - Add Books Audit Trigger
-- Description: Create audit trail for Books table changes
-- Author: DbDemo Project
-- Date: 2025-10-29
-- =============================================

PRINT 'Starting V006: Adding Books audit trigger...';
GO

-- =============================================
-- Create BooksAudit table
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BooksAudit' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT '  Creating BooksAudit table...';

    CREATE TABLE dbo.BooksAudit (
        AuditId         INT             IDENTITY(1,1) NOT NULL,
        BookId          INT             NOT NULL,
        Action          NVARCHAR(10)    NOT NULL,  -- 'INSERT', 'UPDATE', 'DELETE'
        OldISBN         NVARCHAR(13)    NULL,
        NewISBN         NVARCHAR(13)    NULL,
        OldTitle        NVARCHAR(200)   NULL,
        NewTitle        NVARCHAR(200)   NULL,
        OldAvailableCopies INT          NULL,
        NewAvailableCopies INT          NULL,
        OldTotalCopies  INT             NULL,
        NewTotalCopies  INT             NULL,
        ChangedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        ChangedBy       NVARCHAR(128)   NOT NULL DEFAULT SUSER_SNAME(),

        CONSTRAINT PK_BooksAudit PRIMARY KEY CLUSTERED (AuditId),
        CONSTRAINT FK_BooksAudit_Books FOREIGN KEY (BookId)
            REFERENCES dbo.Books(Id)
    );

    PRINT '  ✓ BooksAudit table created';
END
ELSE
BEGIN
    PRINT '  ⊙ BooksAudit table already exists';
END
GO

-- =============================================
-- Create index on BookId for efficient queries
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BooksAudit_BookId' AND object_id = OBJECT_ID('dbo.BooksAudit'))
BEGIN
    PRINT '  Creating index IX_BooksAudit_BookId...';

    CREATE NONCLUSTERED INDEX IX_BooksAudit_BookId
        ON dbo.BooksAudit(BookId, ChangedAt DESC)
        INCLUDE (Action, NewTitle, NewAvailableCopies);

    PRINT '  ✓ Index IX_BooksAudit_BookId created';
END
ELSE
BEGIN
    PRINT '  ⊙ Index IX_BooksAudit_BookId already exists';
END
GO

-- =============================================
-- Create audit trigger on Books table
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_Books_Audit' AND parent_id = OBJECT_ID('dbo.Books'))
BEGIN
    PRINT '  Creating trigger TR_Books_Audit...';

    EXEC('
    CREATE TRIGGER dbo.TR_Books_Audit
    ON dbo.Books
    AFTER INSERT, UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @Action NVARCHAR(10);

        -- Determine action type
        IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
            SET @Action = ''UPDATE'';
        ELSE IF EXISTS (SELECT 1 FROM inserted)
            SET @Action = ''INSERT'';
        ELSE IF EXISTS (SELECT 1 FROM deleted)
            SET @Action = ''DELETE'';

        -- Insert audit records
        INSERT INTO dbo.BooksAudit (
            BookId,
            Action,
            OldISBN,
            NewISBN,
            OldTitle,
            NewTitle,
            OldAvailableCopies,
            NewAvailableCopies,
            OldTotalCopies,
            NewTotalCopies,
            ChangedAt,
            ChangedBy
        )
        SELECT
            COALESCE(i.Id, d.Id) AS BookId,
            @Action AS Action,
            d.ISBN AS OldISBN,
            i.ISBN AS NewISBN,
            d.Title AS OldTitle,
            i.Title AS NewTitle,
            d.AvailableCopies AS OldAvailableCopies,
            i.AvailableCopies AS NewAvailableCopies,
            d.TotalCopies AS OldTotalCopies,
            i.TotalCopies AS NewTotalCopies,
            SYSUTCDATETIME() AS ChangedAt,
            SUSER_SNAME() AS ChangedBy
        FROM inserted i
        FULL OUTER JOIN deleted d ON i.Id = d.Id;
    END
    ');

    PRINT '  ✓ Trigger TR_Books_Audit created';
END
ELSE
BEGIN
    PRINT '  ⊙ Trigger TR_Books_Audit already exists';
END
GO

-- =============================================
-- Grant permissions
-- =============================================
PRINT '  Granting permissions to library_app_user...';

GRANT SELECT ON dbo.BooksAudit TO library_app_user;

PRINT '  ✓ Permissions granted';
GO

PRINT '✓ V006 migration completed successfully!';
GO
