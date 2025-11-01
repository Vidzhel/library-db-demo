-- =============================================
-- Migration: V013 - Fix vw_TopBooksOverall to include all columns
-- Description: Adds missing columns (Subtitle, CategoryId, RowNumber) to vw_TopBooksOverall
-- Author: DbDemo Project
-- Date: 2025-11-01
-- =============================================

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.vw_TopBooksOverall', 'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_TopBooksOverall;
END
GO

CREATE VIEW dbo.vw_TopBooksOverall
AS
SELECT TOP 100 PERCENT
    BookId,
    ISBN,
    Title,
    Subtitle,
    CategoryId,
    CategoryName,
    TotalLoans,
    RowNumber,
    Rank,
    DenseRank,
    GlobalRowNumber
FROM dbo.vw_PopularBooks
ORDER BY GlobalRowNumber;
GO

PRINT 'Migration V013 completed successfully: Fixed vw_TopBooksOverall to include all columns';
GO
