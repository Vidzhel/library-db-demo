-- =============================================
-- Migration: V011 - Member Statistics Table-Valued Function
-- Description: Creates an inline table-valued function to retrieve comprehensive statistics for a member
-- Author: DbDemo Project
-- Date: 2025-11-01
-- =============================================

-- This migration demonstrates:
-- 1. Inline table-valued functions (iTVF) for better performance than multi-statement TVFs
-- 2. Complex aggregation logic encapsulated in reusable function
-- 3. Handling NULL values in aggregations (ISNULL, COALESCE)
-- 4. Calculating derived metrics (average loan duration)

SET NOCOUNT ON;
GO

-- Drop function if exists (for idempotency)
IF OBJECT_ID('dbo.fn_GetMemberStatistics', 'IF') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.fn_GetMemberStatistics;
END
GO

-- =============================================
-- Function: fn_GetMemberStatistics
-- Purpose: Returns comprehensive statistics for a specific member
-- Parameters:
--   @MemberId INT - The ID of the member to get statistics for
-- Returns: Table with one row containing all statistics
-- =============================================
CREATE FUNCTION dbo.fn_GetMemberStatistics
(
    @MemberId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        @MemberId AS MemberId,

        -- Total number of books ever loaned (including returned)
        COUNT(L.Id) AS TotalBooksLoaned,

        -- Number of currently active loans (not yet returned)
        SUM(CASE WHEN L.Status = 0 THEN 1 ELSE 0 END) AS ActiveLoans,

        -- Number of currently overdue loans
        SUM(CASE WHEN L.Status = 2 THEN 1 ELSE 0 END) AS OverdueLoans,

        -- Number of loans returned late
        SUM(CASE WHEN L.Status = 3 THEN 1 ELSE 0 END) AS ReturnedLateCount,

        -- Total late fees accumulated (paid or unpaid)
        ISNULL(SUM(L.LateFee), 0.00) AS TotalLateFees,

        -- Total unpaid late fees
        ISNULL(SUM(CASE WHEN L.IsFeePaid = 0 THEN L.LateFee ELSE 0 END), 0.00) AS UnpaidLateFees,

        -- Average loan duration in days (for returned books only)
        -- NULL if no books have been returned yet
        AVG(
            CASE
                WHEN L.ReturnedAt IS NOT NULL
                THEN DATEDIFF(DAY, L.BorrowedAt, L.ReturnedAt)
                ELSE NULL
            END
        ) AS AvgLoanDurationDays,

        -- Last borrow date (most recent loan)
        MAX(L.BorrowedAt) AS LastBorrowDate,

        -- Total number of renewals across all loans
        ISNULL(SUM(L.RenewalCount), 0) AS TotalRenewals,

        -- Number of lost or damaged books
        SUM(CASE WHEN L.Status IN (4, 5) THEN 1 ELSE 0 END) AS LostOrDamagedCount

    FROM Members M
    LEFT JOIN Loans L ON M.Id = L.MemberId
    WHERE M.Id = @MemberId
    GROUP BY M.Id
);
GO

-- =============================================
-- Validation: Test the function with a sample query
-- =============================================
-- You can test this function after migration with:
-- SELECT * FROM dbo.fn_GetMemberStatistics(1);
-- =============================================

PRINT 'Migration V011 completed successfully: Created fn_GetMemberStatistics function';
GO
