-- =============================================
-- Migration: V010 - Drop Foreign Key from BooksAudit
-- Description: Remove FK constraint - audit tables should preserve records even if source is deleted
-- Author: DbDemo Project
-- Date: 2025-10-29
-- =============================================

PRINT 'Starting V010: Dropping FK constraint from BooksAudit...';
GO

-- =============================================
-- Drop FK constraint
-- =============================================
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BooksAudit_Books')
BEGIN
    PRINT '  Dropping FK_BooksAudit_Books constraint...';
    ALTER TABLE dbo.BooksAudit
    DROP CONSTRAINT FK_BooksAudit_Books;
    PRINT '  ✓ FK constraint dropped';
END
ELSE
BEGIN
    PRINT '  ⊙ FK constraint does not exist';
END
GO

PRINT '✓ V010 migration completed successfully!';
PRINT '';
PRINT 'Note: Audit tables should NOT have FK constraints to source tables.';
PRINT 'This allows audit records to persist even after source records are deleted.';
GO
