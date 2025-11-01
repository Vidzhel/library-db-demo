-- =============================================
-- Migration: V009 - Fix BooksAudit ISBN Column Sizes
-- Description: Increase ISBN column sizes to match Books table (NVARCHAR(20))
-- Author: DbDemo Project
-- Date: 2025-10-29
-- =============================================

PRINT 'Starting V009: Fixing BooksAudit ISBN column sizes...';
GO

-- =============================================
-- Alter OldISBN and NewISBN column sizes
-- =============================================
PRINT '  Altering OldISBN column size to NVARCHAR(20)...';
ALTER TABLE dbo.BooksAudit
ALTER COLUMN OldISBN NVARCHAR(20) NULL;
PRINT '  ✓ OldISBN column altered';

PRINT '  Altering NewISBN column size to NVARCHAR(20)...';
ALTER TABLE dbo.BooksAudit
ALTER COLUMN NewISBN NVARCHAR(20) NULL;
PRINT '  ✓ NewISBN column altered';
GO

PRINT '✓ V009 migration completed successfully!';
GO
