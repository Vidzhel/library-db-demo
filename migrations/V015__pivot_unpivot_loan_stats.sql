-- =============================================
-- Migration: V015 - PIVOT/UNPIVOT for Monthly Loan Statistics
-- Description: Creates views and functions demonstrating PIVOT and UNPIVOT operations
-- Author: DbDemo Project
-- Date: 2025-11-01
-- =============================================

-- This migration demonstrates:
-- 1. PIVOT operator to transform rows into columns (categories become columns)
-- 2. UNPIVOT operator to normalize pivoted data back to rows
-- 3. Dynamic PIVOT for unknown column sets
-- 4. Aggregation with PIVOT
-- 5. Practical cross-tab reporting

SET NOCOUNT ON;
GO

-- =============================================
-- View: vw_MonthlyLoansByCategory
-- Purpose: PIVOT view showing loan counts per category as columns, months as rows
-- Returns: Year, Month, YearMonth, and one column per category name with loan counts
-- =============================================

IF OBJECT_ID('dbo.vw_MonthlyLoansByCategory', 'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_MonthlyLoansByCategory;
END
GO

CREATE VIEW dbo.vw_MonthlyLoansByCategory
AS
SELECT
    [Year],
    [Month],
    YearMonth,
    [Fiction],
    [Non-Fiction],
    [Science],
    [History],
    [Technology],
    [Biography],
    [Children],
    TotalLoans
FROM
(
    -- Source query: Get loans with category names and date parts
    SELECT
        YEAR(L.BorrowedAt) AS [Year],
        MONTH(L.BorrowedAt) AS [Month],
        FORMAT(L.BorrowedAt, 'yyyy-MM') AS YearMonth,
        C.Name AS CategoryName,
        L.Id AS LoanId
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    INNER JOIN Categories C ON B.CategoryId = C.Id
) AS SourceData
PIVOT
(
    -- Aggregate function: COUNT distinct loan IDs
    COUNT(LoanId)
    -- Columns to pivot: Category names become columns
    FOR CategoryName IN (
        [Fiction],
        [Non-Fiction],
        [Science],
        [History],
        [Technology],
        [Biography],
        [Children]
    )
) AS PivotTable
CROSS APPLY
(
    -- Calculate total across all categories for each row
    SELECT
        ISNULL([Fiction], 0) +
        ISNULL([Non-Fiction], 0) +
        ISNULL([Science], 0) +
        ISNULL([History], 0) +
        ISNULL([Technology], 0) +
        ISNULL([Biography], 0) +
        ISNULL([Children], 0) AS TotalLoans
) AS Totals;
GO

-- =============================================
-- View: vw_UnpivotedLoanStats
-- Purpose: Demonstrates UNPIVOT by normalizing pivoted data back to rows
-- Returns: YearMonth, CategoryName, LoanCount (one row per month-category combination)
-- =============================================

IF OBJECT_ID('dbo.vw_UnpivotedLoanStats', 'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_UnpivotedLoanStats;
END
GO

CREATE VIEW dbo.vw_UnpivotedLoanStats
AS
SELECT
    YearMonth,
    CategoryName,
    LoanCount
FROM
(
    -- Start with pivoted data (columns for each category)
    SELECT
        YearMonth,
        [Fiction],
        [Non-Fiction],
        [Science],
        [History],
        [Technology],
        [Biography],
        [Children]
    FROM dbo.vw_MonthlyLoansByCategory
) AS PivotedData
UNPIVOT
(
    -- Value column: The loan counts
    LoanCount
    -- Source columns: Category name columns to unpivot
    FOR CategoryName IN (
        [Fiction],
        [Non-Fiction],
        [Science],
        [History],
        [Technology],
        [Biography],
        [Children]
    )
) AS UnpivotTable
WHERE LoanCount > 0  -- Filter out zero-count categories
;
GO

-- =============================================
-- Function: fn_GetDynamicPivotQuery
-- Purpose: Generates dynamic PIVOT query SQL for any set of categories
-- Note: For educational purposes - shows how to handle dynamic column sets
-- Returns: NVARCHAR(MAX) containing the dynamic SQL query
-- =============================================

IF OBJECT_ID('dbo.fn_GetDynamicPivotQuery', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.fn_GetDynamicPivotQuery;
END
GO

CREATE FUNCTION dbo.fn_GetDynamicPivotQuery()
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @columns NVARCHAR(MAX);
    DECLARE @query NVARCHAR(MAX);

    -- Build comma-separated list of category names with brackets
    SELECT @columns = STRING_AGG(QUOTENAME(Name), ', ')
    FROM (SELECT DISTINCT Name FROM Categories) AS CategoryNames;

    -- Build the dynamic PIVOT query
    SET @query = '
    SELECT
        [Year],
        [Month],
        YearMonth,
        ' + @columns + ',
        TotalLoans
    FROM
    (
        SELECT
            YEAR(L.BorrowedAt) AS [Year],
            MONTH(L.BorrowedAt) AS [Month],
            FORMAT(L.BorrowedAt, ''yyyy-MM'') AS YearMonth,
            C.Name AS CategoryName,
            L.Id AS LoanId
        FROM Loans L
        INNER JOIN Books B ON L.BookId = B.Id
        INNER JOIN Categories C ON B.CategoryId = C.Id
    ) AS SourceData
    PIVOT
    (
        COUNT(LoanId)
        FOR CategoryName IN (' + @columns + ')
    ) AS PivotTable
    CROSS APPLY
    (
        SELECT ' + @columns + ' AS TotalLoans
    ) AS Totals';

    RETURN @query;
END;
GO

-- =============================================
-- Validation & Usage Examples
-- =============================================

-- Example 1: Query the static PIVOT view
-- SELECT * FROM dbo.vw_MonthlyLoansByCategory
-- ORDER BY [Year], [Month];

-- Example 2: Query specific categories and months
-- SELECT YearMonth, Fiction, Technology, [Non-Fiction]
-- FROM dbo.vw_MonthlyLoansByCategory
-- WHERE [Year] = 2024 AND Fiction > 0
-- ORDER BY [Month];

-- Example 3: Query the UNPIVOT view
-- SELECT *
-- FROM dbo.vw_UnpivotedLoanStats
-- WHERE YearMonth = '2024-01'
-- ORDER BY CategoryName;

-- Example 4: Get dynamic PIVOT query (requires dynamic SQL execution)
-- DECLARE @sql NVARCHAR(MAX);
-- SET @sql = dbo.fn_GetDynamicPivotQuery();
-- EXEC sp_executesql @sql;

-- Example 5: Verify PIVOT and UNPIVOT are reversible
-- Compare original loan counts with unpivoted results:
-- SELECT
--     FORMAT(L.BorrowedAt, 'yyyy-MM') AS YearMonth,
--     C.Name AS CategoryName,
--     COUNT(*) AS LoanCount
-- FROM Loans L
-- INNER JOIN Books B ON L.BookId = B.Id
-- INNER JOIN Categories C ON B.CategoryId = C.Id
-- GROUP BY FORMAT(L.BorrowedAt, 'yyyy-MM'), C.Name
-- ORDER BY YearMonth, CategoryName;

PRINT 'Migration V015 completed successfully: Created PIVOT/UNPIVOT views and functions';
GO
