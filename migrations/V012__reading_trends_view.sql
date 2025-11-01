-- =============================================
-- Migration: V012 - Window Functions for Reading Trends
-- Description: Creates views demonstrating window functions for analytics and reporting
-- Author: DbDemo Project
-- Date: 2025-11-01
-- =============================================

-- This migration demonstrates:
-- 1. ROW_NUMBER() - Unique sequential ranking
-- 2. RANK() - Ranking with gaps when ties exist
-- 3. DENSE_RANK() - Ranking without gaps
-- 4. LAG() / LEAD() - Access previous/next row values for trend analysis
-- 5. OVER clause with PARTITION BY and ORDER BY

SET NOCOUNT ON;
GO

-- =============================================
-- View 1: Popular Books by Category
-- Demonstrates ROW_NUMBER(), RANK(), DENSE_RANK()
-- =============================================

IF OBJECT_ID('dbo.vw_PopularBooks', 'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_PopularBooks;
END
GO

CREATE VIEW dbo.vw_PopularBooks
AS
SELECT
    B.Id AS BookId,
    B.ISBN,
    B.Title,
    B.Subtitle,
    C.Id AS CategoryId,
    C.Name AS CategoryName,
    COUNT(L.Id) AS TotalLoans,

    -- ROW_NUMBER: Unique sequential number, no ties
    -- Within each category, order books by loan count (descending)
    ROW_NUMBER() OVER (
        PARTITION BY C.Id
        ORDER BY COUNT(L.Id) DESC, B.Title ASC
    ) AS RowNumber,

    -- RANK: Same rank for ties, but gaps in sequence
    -- Example: 1, 2, 2, 4 (two books tied for 2nd, next is 4th)
    RANK() OVER (
        PARTITION BY C.Id
        ORDER BY COUNT(L.Id) DESC
    ) AS Rank,

    -- DENSE_RANK: Same rank for ties, NO gaps
    -- Example: 1, 2, 2, 3 (two books tied for 2nd, next is 3rd)
    DENSE_RANK() OVER (
        PARTITION BY C.Id
        ORDER BY COUNT(L.Id) DESC
    ) AS DenseRank,

    -- Global ranking across all categories
    ROW_NUMBER() OVER (
        ORDER BY COUNT(L.Id) DESC, B.Title ASC
    ) AS GlobalRowNumber

FROM Books B
INNER JOIN Categories C ON B.CategoryId = C.Id
LEFT JOIN Loans L ON B.Id = L.BookId
WHERE B.IsDeleted = 0
GROUP BY B.Id, B.ISBN, B.Title, B.Subtitle, C.Id, C.Name;
GO

-- =============================================
-- View 2: Monthly Loan Trends
-- Demonstrates LAG() and LEAD() for time-series analysis
-- =============================================

IF OBJECT_ID('dbo.vw_MonthlyLoanTrends', 'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_MonthlyLoanTrends;
END
GO

CREATE VIEW dbo.vw_MonthlyLoanTrends
AS
WITH MonthlyLoans AS (
    SELECT
        C.Id AS CategoryId,
        C.Name AS CategoryName,
        YEAR(L.BorrowedAt) AS Year,
        MONTH(L.BorrowedAt) AS Month,
        FORMAT(L.BorrowedAt, 'yyyy-MM') AS YearMonth,
        COUNT(L.Id) AS LoanCount
    FROM Categories C
    LEFT JOIN Books B ON C.Id = B.CategoryId
    LEFT JOIN Loans L ON B.Id = L.BookId
    WHERE B.IsDeleted = 0 OR B.IsDeleted IS NULL
    GROUP BY C.Id, C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt), FORMAT(L.BorrowedAt, 'yyyy-MM')
)
SELECT
    CategoryId,
    CategoryName,
    Year,
    Month,
    YearMonth,
    LoanCount,

    -- LAG: Get loan count from previous month (for same category)
    LAG(LoanCount, 1) OVER (
        PARTITION BY CategoryId
        ORDER BY Year, Month
    ) AS PrevMonthLoans,

    -- LEAD: Get loan count from next month (for same category)
    LEAD(LoanCount, 1) OVER (
        PARTITION BY CategoryId
        ORDER BY Year, Month
    ) AS NextMonthLoans,

    -- Calculate month-over-month growth
    -- Returns NULL for first month (no previous data)
    CASE
        WHEN LAG(LoanCount, 1) OVER (
            PARTITION BY CategoryId
            ORDER BY Year, Month
        ) IS NULL THEN NULL
        WHEN LAG(LoanCount, 1) OVER (
            PARTITION BY CategoryId
            ORDER BY Year, Month
        ) = 0 THEN NULL
        ELSE
            CAST(
                (LoanCount - LAG(LoanCount, 1) OVER (
                    PARTITION BY CategoryId
                    ORDER BY Year, Month
                )) * 100.0 / LAG(LoanCount, 1) OVER (
                    PARTITION BY CategoryId
                    ORDER BY Year, Month
                )
                AS DECIMAL(10,2)
            )
    END AS GrowthPercentage,

    -- Moving average: current month + previous 2 months
    CAST(
        AVG(CAST(LoanCount AS DECIMAL(10,2))) OVER (
            PARTITION BY CategoryId
            ORDER BY Year, Month
            ROWS BETWEEN 2 PRECEDING AND CURRENT ROW
        )
        AS DECIMAL(10,2)
    ) AS ThreeMonthMovingAvg

FROM MonthlyLoans
WHERE YearMonth IS NOT NULL;  -- Exclude NULL months (categories with no loans)
GO

-- =============================================
-- View 3: Top Books Overall (Global Ranking)
-- Simplified view for querying top N books across all categories
-- =============================================

IF OBJECT_ID('dbo.vw_TopBooksOverall', 'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_TopBooksOverall;
END
GO

CREATE VIEW dbo.vw_TopBooksOverall
AS
SELECT TOP 100 PERCENT  -- TOP 100 PERCENT is a workaround to allow ORDER BY in view
    BookId,
    ISBN,
    Title,
    CategoryName,
    TotalLoans,
    GlobalRowNumber,
    Rank,
    DenseRank
FROM dbo.vw_PopularBooks
ORDER BY GlobalRowNumber;
GO

-- =============================================
-- Validation: Test the views
-- =============================================

-- You can test these views after migration with:

-- 1. Get top 5 books per category
-- SELECT * FROM dbo.vw_PopularBooks
-- WHERE RowNumber <= 5
-- ORDER BY CategoryName, RowNumber;

-- 2. Get overall top 10 books
-- SELECT TOP 10 * FROM dbo.vw_TopBooksOverall;

-- 3. Get loan trends for a specific category
-- SELECT * FROM dbo.vw_MonthlyLoanTrends
-- WHERE CategoryId = 1
-- ORDER BY Year, Month;

-- 4. Find categories with growing trends (positive growth)
-- SELECT DISTINCT CategoryName
-- FROM dbo.vw_MonthlyLoanTrends
-- WHERE GrowthPercentage > 10
-- ORDER BY CategoryName;

PRINT 'Migration V012 completed successfully: Created vw_PopularBooks, vw_MonthlyLoanTrends, and vw_TopBooksOverall views';
GO
