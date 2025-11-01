-- ============================================================================
-- Migration: V017 - Advanced Aggregations (GROUPING SETS, ROLLUP, CUBE)
-- Description: Demonstrates advanced GROUP BY extensions for multi-dimensional
--              reporting and analytics. Creates stored procedures and views that
--              use GROUPING SETS, ROLLUP, CUBE, and GROUPING() function.
-- ============================================================================

-- ======================
-- 1. GROUPING SETS Demo
-- ======================
-- GROUPING SETS allows multiple grouping levels in a single query
-- More efficient than UNION ALL of separate GROUP BY queries

IF OBJECT_ID('dbo.sp_GetLibraryStatsGroupingSets', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLibraryStatsGroupingSets;
GO

CREATE PROCEDURE dbo.sp_GetLibraryStatsGroupingSets
AS
BEGIN
    SET NOCOUNT ON;

    -- Multi-dimensional analysis of library loans
    -- Returns aggregations at three levels:
    -- 1. By Category only
    -- 2. By Year/Month only
    -- 3. By Category + Year/Month combination
    -- Plus grand total (all NULLs)

    SELECT
        C.Name AS CategoryName,
        YEAR(L.BorrowedAt) AS LoanYear,
        MONTH(L.BorrowedAt) AS LoanMonth,
        COUNT(L.Id) AS TotalLoans,
        COUNT(DISTINCT L.MemberId) AS UniqueMembers,
        COUNT(DISTINCT B.Id) AS UniqueBooksLoaned,
        -- GROUPING() returns 1 if column is aggregated (NULL), 0 otherwise
        GROUPING(C.Name) AS IsCategoryAggregated,
        GROUPING(YEAR(L.BorrowedAt)) AS IsYearAggregated,
        GROUPING(MONTH(L.BorrowedAt)) AS IsMonthAggregated
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    INNER JOIN Categories C ON B.CategoryId = C.Id
    GROUP BY GROUPING SETS (
        (C.Name),                                    -- Category level
        (YEAR(L.BorrowedAt), MONTH(L.BorrowedAt)),      -- Time level
        (C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt)), -- Detail level
        ()                                           -- Grand total
    )
    ORDER BY
        GROUPING(C.Name),
        C.Name,
        GROUPING(YEAR(L.BorrowedAt)),
        YEAR(L.BorrowedAt),
        MONTH(L.BorrowedAt);
END;
GO

-- ======================
-- 2. ROLLUP Demo
-- ======================
-- ROLLUP creates hierarchical subtotals and grand total
-- Aggregates from right to left: (A, B, C) -> (A, B) -> (A) -> ()

IF OBJECT_ID('dbo.sp_GetLibraryStatsRollup', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLibraryStatsRollup;
GO

CREATE PROCEDURE dbo.sp_GetLibraryStatsRollup
AS
BEGIN
    SET NOCOUNT ON;

    -- Hierarchical aggregation: Category -> Year -> Month
    -- Returns:
    -- 1. Category + Year + Month (detail)
    -- 2. Category + Year (year subtotals)
    -- 3. Category (category subtotals)
    -- 4. Grand total

    SELECT
        C.Name AS CategoryName,
        YEAR(L.BorrowedAt) AS LoanYear,
        MONTH(L.BorrowedAt) AS LoanMonth,
        COUNT(L.Id) AS TotalLoans,
        AVG(DATEDIFF(DAY, L.BorrowedAt, COALESCE(L.ReturnedAt, GETDATE()))) AS AvgLoanDurationDays,
        -- GROUPING_ID combines GROUPING() values into single integer
        -- Useful for identifying aggregation level
        GROUPING_ID(C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt)) AS GroupingLevel,
        CASE GROUPING_ID(C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt))
            WHEN 0 THEN 'Detail (Category-Year-Month)'
            WHEN 1 THEN 'Subtotal (Category-Year)'
            WHEN 3 THEN 'Subtotal (Category)'
            WHEN 7 THEN 'Grand Total'
            ELSE 'Unknown'
        END AS AggregationLevel
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    INNER JOIN Categories C ON B.CategoryId = C.Id
    GROUP BY ROLLUP (C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt))
    ORDER BY
        GROUPING(C.Name),
        C.Name,
        GROUPING(YEAR(L.BorrowedAt)),
        YEAR(L.BorrowedAt),
        GROUPING(MONTH(L.BorrowedAt)),
        MONTH(L.BorrowedAt);
END;
GO

-- ======================
-- 3. CUBE Demo
-- ======================
-- CUBE creates all possible grouping combinations (2^n combinations)
-- For 3 columns: 8 combinations (including grand total)

IF OBJECT_ID('dbo.sp_GetLibraryStatsCube', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLibraryStatsCube;
GO

CREATE PROCEDURE dbo.sp_GetLibraryStatsCube
AS
BEGIN
    SET NOCOUNT ON;

    -- All possible combinations of Category, Year, Status
    -- 2^3 = 8 grouping combinations

    SELECT
        C.Name AS CategoryName,
        YEAR(L.BorrowedAt) AS LoanYear,
        CASE L.Status
            WHEN 0 THEN 'Active'
            WHEN 1 THEN 'Returned'
            WHEN 2 THEN 'Overdue'
            ELSE 'Unknown'
        END AS LoanStatus,
        COUNT(L.Id) AS TotalLoans,
        -- Individual GROUPING indicators
        GROUPING(C.Name) AS IsCategoryAggregated,
        GROUPING(YEAR(L.BorrowedAt)) AS IsYearAggregated,
        GROUPING(L.Status) AS IsStatusAggregated,
        -- Combined grouping ID (0-7)
        GROUPING_ID(C.Name, YEAR(L.BorrowedAt), L.Status) AS GroupingId
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    INNER JOIN Categories C ON B.CategoryId = C.Id
    GROUP BY CUBE (C.Name, YEAR(L.BorrowedAt), L.Status)
    ORDER BY
        GROUPING_ID(C.Name, YEAR(L.BorrowedAt), L.Status),
        C.Name,
        YEAR(L.BorrowedAt),
        L.Status;
END;
GO

-- ======================
-- 4. Dashboard View
-- ======================
-- Comprehensive library dashboard using GROUPING SETS
-- Provides multiple aggregation levels in single view

IF OBJECT_ID('dbo.vw_LibraryDashboard', 'V') IS NOT NULL
    DROP VIEW dbo.vw_LibraryDashboard;
GO

CREATE VIEW dbo.vw_LibraryDashboard
AS
    SELECT
        C.Name AS CategoryName,
        YEAR(L.BorrowedAt) AS LoanYear,
        MONTH(L.BorrowedAt) AS LoanMonth,
        CASE L.Status
            WHEN 0 THEN 'Active'
            WHEN 1 THEN 'Returned'
            WHEN 2 THEN 'Overdue'
            ELSE 'Unknown'
        END AS LoanStatus,
        COUNT(L.Id) AS TotalLoans,
        COUNT(DISTINCT L.MemberId) AS UniqueMembers,
        COUNT(DISTINCT B.Id) AS UniqueBooks,
        AVG(DATEDIFF(DAY, L.BorrowedAt, COALESCE(L.ReturnedAt, GETDATE()))) AS AvgDurationDays,
        -- Grouping indicators for each dimension
        GROUPING(C.Name) AS IsCategoryGrouped,
        GROUPING(YEAR(L.BorrowedAt)) AS IsYearGrouped,
        GROUPING(MONTH(L.BorrowedAt)) AS IsMonthGrouped,
        GROUPING(L.Status) AS IsStatusGrouped,
        -- Binary representation: Category(8) + Year(4) + Month(2) + Status(1)
        GROUPING_ID(C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt), L.Status) AS GroupingId,
        -- Human-readable aggregation level
        CASE
            WHEN GROUPING_ID(C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt), L.Status) = 0
                THEN 'Detail'
            WHEN GROUPING_ID(C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt), L.Status) = 15
                THEN 'Grand Total'
            ELSE 'Subtotal'
        END AS AggregationType
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    INNER JOIN Categories C ON B.CategoryId = C.Id
    GROUP BY GROUPING SETS (
        -- Detail level
        (C.Name, YEAR(L.BorrowedAt), MONTH(L.BorrowedAt), L.Status),
        -- Category aggregations
        (C.Name),
        (C.Name, L.Status),
        (C.Name, YEAR(L.BorrowedAt)),
        -- Time aggregations
        (YEAR(L.BorrowedAt), MONTH(L.BorrowedAt)),
        (YEAR(L.BorrowedAt)),
        -- Status aggregations
        (L.Status),
        -- Grand total
        ()
    );
GO

-- ======================
-- 5. Summary Statistics
-- ======================
-- Stored procedure for simplified dashboard stats
-- Returns pre-computed aggregation levels

IF OBJECT_ID('dbo.sp_GetDashboardSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDashboardSummary;
GO

CREATE PROCEDURE dbo.sp_GetDashboardSummary
AS
BEGIN
    SET NOCOUNT ON;

    -- Simplified dashboard with key metrics at different aggregation levels
    SELECT
        C.Name AS CategoryName,
        YEAR(L.BorrowedAt) AS LoanYear,
        COUNT(L.Id) AS TotalLoans,
        COUNT(DISTINCT L.MemberId) AS UniqueMembers,
        COUNT(DISTINCT B.Id) AS UniqueBooks,
        SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans,
        SUM(CASE WHEN L.Status = 1 THEN 1 ELSE 0 END) AS ReturnedLoans,
        SUM(CASE WHEN L.Status = 2 THEN 1 ELSE 0 END) AS OverdueLoans,
        AVG(DATEDIFF(DAY, L.BorrowedAt, COALESCE(L.ReturnedAt, GETDATE()))) AS AvgLoanDurationDays,
        -- Grouping indicators
        GROUPING(C.Name) AS IsCategoryGrouped,
        GROUPING(YEAR(L.BorrowedAt)) AS IsYearGrouped
    FROM Loans L
    INNER JOIN Books B ON L.BookId = B.Id
    INNER JOIN Categories C ON B.CategoryId = C.Id
    GROUP BY ROLLUP (C.Name, YEAR(L.BorrowedAt))
    ORDER BY
        GROUPING(C.Name),
        C.Name,
        GROUPING(YEAR(L.BorrowedAt)),
        YEAR(L.BorrowedAt);
END;
GO

-- ======================
-- Verification Queries
-- ======================
-- Test that stored procedures execute successfully

PRINT 'Testing sp_GetLibraryStatsGroupingSets...';
EXEC dbo.sp_GetLibraryStatsGroupingSets;

PRINT 'Testing sp_GetLibraryStatsRollup...';
EXEC dbo.sp_GetLibraryStatsRollup;

PRINT 'Testing sp_GetLibraryStatsCube...';
EXEC dbo.sp_GetLibraryStatsCube;

PRINT 'Testing sp_GetDashboardSummary...';
EXEC dbo.sp_GetDashboardSummary;

PRINT 'Testing vw_LibraryDashboard...';
SELECT TOP 10 * FROM dbo.vw_LibraryDashboard ORDER BY GroupingId, CategoryName;

PRINT 'V017 migration completed successfully!';
GO
