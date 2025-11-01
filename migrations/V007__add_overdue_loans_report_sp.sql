-- =============================================
-- Migration: V007 - Add Overdue Loans Report Stored Procedure
-- Description: Create stored procedure to generate overdue loans report
-- Author: DbDemo Project
-- Date: 2025-10-29
-- =============================================

PRINT 'Starting V007: Adding Overdue Loans Report stored procedure...';
GO

-- =============================================
-- Create sp_GetOverdueLoans stored procedure
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetOverdueLoans' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT '  Creating stored procedure sp_GetOverdueLoans...';

    EXEC('
    CREATE PROCEDURE dbo.sp_GetOverdueLoans
        @AsOfDate DATETIME2 = NULL,
        @MinDaysOverdue INT = 0,
        @TotalCount INT OUTPUT
    AS
    BEGIN
        SET NOCOUNT ON;

        -- Default @AsOfDate to current UTC time if not provided
        IF @AsOfDate IS NULL
            SET @AsOfDate = SYSUTCDATETIME();

        -- Select overdue loans with calculated fields
        SELECT
            l.Id AS LoanId,
            m.Id AS MemberId,
            m.FirstName + '' '' + m.LastName AS MemberName,
            m.Email AS MemberEmail,
            m.PhoneNumber AS MemberPhone,
            b.Id AS BookId,
            b.ISBN,
            b.Title AS BookTitle,
            b.Publisher,
            l.BorrowedAt,
            l.DueDate,
            DATEDIFF(DAY, l.DueDate, @AsOfDate) AS DaysOverdue,
            (DATEDIFF(DAY, l.DueDate, @AsOfDate) * 0.50) AS CalculatedLateFee,
            l.Status,
            l.Notes
        FROM dbo.Loans l
        INNER JOIN dbo.Members m ON l.MemberId = m.Id
        INNER JOIN dbo.Books b ON l.BookId = b.Id
        WHERE
            -- Either explicitly marked as Overdue (Status = 2)
            -- OR still active (Status = 0) but past due date
            (l.Status = 2 OR (l.Status = 0 AND l.DueDate < @AsOfDate))
            -- Filter by minimum days overdue
            AND DATEDIFF(DAY, l.DueDate, @AsOfDate) >= @MinDaysOverdue
        ORDER BY
            DATEDIFF(DAY, l.DueDate, @AsOfDate) DESC,  -- Most overdue first
            l.DueDate ASC;

        -- Set output parameter with total count
        SELECT @TotalCount = COUNT(*)
        FROM dbo.Loans l
        WHERE
            (l.Status = 2 OR (l.Status = 0 AND l.DueDate < @AsOfDate))
            AND DATEDIFF(DAY, l.DueDate, @AsOfDate) >= @MinDaysOverdue;

        RETURN 0;
    END
    ');

    PRINT '  ✓ Stored procedure sp_GetOverdueLoans created';
END
ELSE
BEGIN
    PRINT '  ⊙ Stored procedure sp_GetOverdueLoans already exists';
END
GO

-- =============================================
-- Grant execute permission to application user
-- =============================================
PRINT '  Granting EXECUTE permission to library_app_user...';

GRANT EXECUTE ON dbo.sp_GetOverdueLoans TO library_app_user;

PRINT '  ✓ Permission granted';
GO

-- =============================================
-- Test the stored procedure (optional demo)
-- =============================================
PRINT '  Testing stored procedure...';

DECLARE @Count INT;
EXEC dbo.sp_GetOverdueLoans
    @AsOfDate = NULL,           -- Use current time
    @MinDaysOverdue = 0,        -- Include all overdue loans
    @TotalCount = @Count OUTPUT;

PRINT '  ✓ Stored procedure executed successfully';
PRINT '    Total overdue loans found: ' + CAST(@Count AS NVARCHAR(10));
GO

PRINT '✓ V007 migration completed successfully!';
GO
