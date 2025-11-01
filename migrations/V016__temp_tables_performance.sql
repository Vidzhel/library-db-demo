-- =============================================
-- Migration: V016 - Temporary Tables & Performance Comparison
-- Description: Creates stored procedures demonstrating #TempTable, @TableVariable, and CTE approaches
-- Author: DbDemo Project
-- Date: 2025-11-01
-- =============================================

-- This migration demonstrates:
-- 1. #TempTable - Temporary table with statistics and indexes
-- 2. @TableVariable - Table variable with limited optimization
-- 3. CTE (Common Table Expression) - Inline query optimization
-- 4. Performance characteristics of each approach
-- 5. When to use which approach

SET NOCOUNT ON;
GO

-- =============================================
-- Stored Procedure: sp_GetLibraryStatsWithTempTable
-- Purpose: Complex report using #TempTable (best for large datasets, multiple operations)
-- Returns: Comprehensive library statistics by category
-- =============================================

IF OBJECT_ID('dbo.sp_GetLibraryStatsWithTempTable', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetLibraryStatsWithTempTable;
END
GO

CREATE PROCEDURE dbo.sp_GetLibraryStatsWithTempTable
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: Create temporary table with explicit schema
    CREATE TABLE #CategoryStats
    (
        CategoryId INT NOT NULL,
        CategoryName NVARCHAR(100) NOT NULL,
        TotalBooks INT NOT NULL,
        TotalLoans INT NOT NULL,
        ActiveLoans INT NOT NULL,
        PRIMARY KEY (CategoryId)
    );

    -- Step 2: Populate temp table with book counts
    INSERT INTO #CategoryStats (CategoryId, CategoryName, TotalBooks, TotalLoans, ActiveLoans)
    SELECT
        C.Id,
        C.Name,
        COUNT(DISTINCT B.Id) AS TotalBooks,
        0 AS TotalLoans,  -- Will update in next step
        0 AS ActiveLoans
    FROM Categories C
    LEFT JOIN Books B ON C.Id = B.CategoryId AND B.IsDeleted = 0
    GROUP BY C.Id, C.Name;

    -- Step 3: Update with loan counts (demonstrates multiple operations on temp table)
    UPDATE cs
    SET
        TotalLoans = ISNULL(loan_counts.TotalLoans, 0),
        ActiveLoans = ISNULL(loan_counts.ActiveLoans, 0)
    FROM #CategoryStats cs
    LEFT JOIN (
        SELECT
            B.CategoryId,
            COUNT(*) AS TotalLoans,
            SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans
        FROM Loans L
        INNER JOIN Books B ON L.BookId = B.Id
        GROUP BY B.CategoryId
    ) AS loan_counts ON cs.CategoryId = loan_counts.CategoryId;

    -- Step 4: Calculate additional metrics and return results
    SELECT
        cs.CategoryId,
        cs.CategoryName,
        cs.TotalBooks,
        cs.TotalLoans,
        cs.ActiveLoans,
        CASE
            WHEN cs.TotalBooks > 0 THEN CAST(cs.TotalLoans AS DECIMAL(10,2)) / cs.TotalBooks
            ELSE 0
        END AS AverageLoansPerBook,
        -- Get most popular book in category
        (
            SELECT TOP 1 B.Title
            FROM Books B
            LEFT JOIN Loans L ON B.Id = L.BookId
            WHERE B.CategoryId = cs.CategoryId AND B.IsDeleted = 0
            GROUP BY B.Id, B.Title
            ORDER BY COUNT(L.Id) DESC, B.Title
        ) AS MostPopularBookTitle
    FROM #CategoryStats cs
    ORDER BY cs.TotalLoans DESC, cs.CategoryName;

    -- Cleanup: Drop temp table (automatic at end of session, but explicit is good practice)
    DROP TABLE #CategoryStats;
END;
GO

-- =============================================
-- Stored Procedure: sp_GetLibraryStatsWithTableVariable
-- Purpose: Same report using @TableVariable (good for small datasets, single operation)
-- Returns: Comprehensive library statistics by category
-- =============================================

IF OBJECT_ID('dbo.sp_GetLibraryStatsWithTableVariable', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetLibraryStatsWithTableVariable;
END
GO

CREATE PROCEDURE dbo.sp_GetLibraryStatsWithTableVariable
AS
BEGIN
    SET NOCOUNT ON;

    -- Declare table variable
    DECLARE @CategoryStats TABLE
    (
        CategoryId INT NOT NULL PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL,
        TotalBooks INT NOT NULL,
        TotalLoans INT NOT NULL,
        ActiveLoans INT NOT NULL
    );

    -- Populate table variable in single operation (best practice for table variables)
    INSERT INTO @CategoryStats (CategoryId, CategoryName, TotalBooks, TotalLoans, ActiveLoans)
    SELECT
        C.Id,
        C.Name,
        COUNT(DISTINCT B.Id) AS TotalBooks,
        COUNT(L.Id) AS TotalLoans,
        SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans
    FROM Categories C
    LEFT JOIN Books B ON C.Id = B.CategoryId AND B.IsDeleted = 0
    LEFT JOIN Loans L ON B.Id = L.BookId
    GROUP BY C.Id, C.Name;

    -- Return results with calculated metrics
    SELECT
        cs.CategoryId,
        cs.CategoryName,
        cs.TotalBooks,
        cs.TotalLoans,
        cs.ActiveLoans,
        CASE
            WHEN cs.TotalBooks > 0 THEN CAST(cs.TotalLoans AS DECIMAL(10,2)) / cs.TotalBooks
            ELSE 0
        END AS AverageLoansPerBook,
        -- Get most popular book in category
        (
            SELECT TOP 1 B.Title
            FROM Books B
            LEFT JOIN Loans L ON B.Id = L.BookId
            WHERE B.CategoryId = cs.CategoryId AND B.IsDeleted = 0
            GROUP BY B.Id, B.Title
            ORDER BY COUNT(L.Id) DESC, B.Title
        ) AS MostPopularBookTitle
    FROM @CategoryStats cs
    ORDER BY cs.TotalLoans DESC, cs.CategoryName;
END;
GO

-- =============================================
-- Stored Procedure: sp_GetLibraryStatsWithCTE
-- Purpose: Same report using CTE (best for single-use, inline calculations)
-- Returns: Comprehensive library statistics by category
-- =============================================

IF OBJECT_ID('dbo.sp_GetLibraryStatsWithCTE', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_GetLibraryStatsWithCTE;
END
GO

CREATE PROCEDURE dbo.sp_GetLibraryStatsWithCTE
AS
BEGIN
    SET NOCOUNT ON;

    -- Use CTE for inline calculation (no materialization)
    WITH CategoryStats AS
    (
        SELECT
            C.Id AS CategoryId,
            C.Name AS CategoryName,
            COUNT(DISTINCT B.Id) AS TotalBooks,
            COUNT(L.Id) AS TotalLoans,
            SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans
        FROM Categories C
        LEFT JOIN Books B ON C.Id = B.CategoryId AND B.IsDeleted = 0
        LEFT JOIN Loans L ON B.Id = L.BookId
        GROUP BY C.Id, C.Name
    )
    SELECT
        cs.CategoryId,
        cs.CategoryName,
        cs.TotalBooks,
        cs.TotalLoans,
        cs.ActiveLoans,
        CASE
            WHEN cs.TotalBooks > 0 THEN CAST(cs.TotalLoans AS DECIMAL(10,2)) / cs.TotalBooks
            ELSE 0
        END AS AverageLoansPerBook,
        -- Get most popular book in category
        (
            SELECT TOP 1 B.Title
            FROM Books B
            LEFT JOIN Loans L ON B.Id = L.BookId
            WHERE B.CategoryId = cs.CategoryId AND B.IsDeleted = 0
            GROUP BY B.Id, B.Title
            ORDER BY COUNT(L.Id) DESC, B.Title
        ) AS MostPopularBookTitle
    FROM CategoryStats cs
    ORDER BY cs.TotalLoans DESC, cs.CategoryName;
END;
GO

-- =============================================
-- Validation & Usage Examples
-- =============================================

-- Example 1: Execute with #TempTable (best for complex, multi-step operations)
-- EXEC dbo.sp_GetLibraryStatsWithTempTable;

-- Example 2: Execute with @TableVariable (good for small datasets)
-- EXEC dbo.sp_GetLibraryStatsWithTableVariable;

-- Example 3: Execute with CTE (best for simple, single-use queries)
-- EXEC dbo.sp_GetLibraryStatsWithCTE;

-- Example 4: Performance comparison (measure execution time)
-- DECLARE @StartTime DATETIME2, @EndTime DATETIME2;
--
-- SET @StartTime = SYSUTCDATETIME();
-- EXEC dbo.sp_GetLibraryStatsWithTempTable;
-- SET @EndTime = SYSUTCDATETIME();
-- SELECT 'TempTable' AS Method, DATEDIFF(MILLISECOND, @StartTime, @EndTime) AS ExecutionTimeMs;
--
-- SET @StartTime = SYSUTCDATETIME();
-- EXEC dbo.sp_GetLibraryStatsWithTableVariable;
-- SET @EndTime = SYSUTCDATETIME();
-- SELECT 'TableVariable' AS Method, DATEDIFF(MILLISECOND, @StartTime, @EndTime) AS ExecutionTimeMs;
--
-- SET @StartTime = SYSUTCDATETIME();
-- EXEC dbo.sp_GetLibraryStatsWithCTE;
-- SET @EndTime = SYSUTCDATETIME();
-- SELECT 'CTE' AS Method, DATEDIFF(MILLISECOND, @StartTime, @EndTime) AS ExecutionTimeMs;

PRINT 'Migration V016 completed successfully: Created temp table comparison stored procedures';
GO
