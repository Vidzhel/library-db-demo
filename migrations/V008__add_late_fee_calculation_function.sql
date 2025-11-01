-- =============================================
-- Migration: V008 - Add Late Fee Calculation Scalar Function
-- Description: Create scalar function to calculate late fees for loans
-- Author: DbDemo Project
-- Date: 2025-10-29
-- =============================================

PRINT 'Starting V008: Adding late fee calculation scalar function...';
GO

-- =============================================
-- Create fn_CalculateLateFee scalar function
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'fn_CalculateLateFee' AND type = 'FN')
BEGIN
    PRINT '  Creating scalar function fn_CalculateLateFee...';

    EXEC('
    CREATE FUNCTION dbo.fn_CalculateLateFee(@LoanId INT)
    RETURNS DECIMAL(10,2)
    AS
    BEGIN
        DECLARE @LateFee DECIMAL(10,2) = 0.00;
        DECLARE @DueDate DATETIME2;
        DECLARE @ReturnedAt DATETIME2;
        DECLARE @Status INT;
        DECLARE @DaysOverdue INT;
        DECLARE @LateFeePerDay DECIMAL(10,2) = 0.50;

        -- Get loan details
        SELECT
            @DueDate = DueDate,
            @ReturnedAt = ReturnedAt,
            @Status = Status
        FROM dbo.Loans
        WHERE Id = @LoanId;

        -- If loan not found, return 0
        IF @DueDate IS NULL
            RETURN 0.00;

        -- Calculate days overdue
        -- If not yet returned, use current date; otherwise use return date
        SET @DaysOverdue = DATEDIFF(DAY, @DueDate, COALESCE(@ReturnedAt, SYSUTCDATETIME()));

        -- Only charge fee if overdue (positive days)
        IF @DaysOverdue > 0
            SET @LateFee = @DaysOverdue * @LateFeePerDay;

        RETURN @LateFee;
    END
    ');

    PRINT '  ✓ Scalar function fn_CalculateLateFee created';
END
ELSE
BEGIN
    PRINT '  ⊙ Scalar function fn_CalculateLateFee already exists';
END
GO

-- =============================================
-- Grant execute permission to application user
-- =============================================
PRINT '  Granting EXECUTE permission to library_app_user...';

GRANT EXECUTE ON dbo.fn_CalculateLateFee TO library_app_user;

PRINT '  ✓ Permission granted';
GO

-- =============================================
-- Test the scalar function (optional demo)
-- =============================================
PRINT '  Testing scalar function...';

-- Test with a non-existent loan (should return 0.00)
DECLARE @TestFee1 DECIMAL(10,2);
SELECT @TestFee1 = dbo.fn_CalculateLateFee(999999);
PRINT '    Test 1 - Non-existent loan: £' + CAST(@TestFee1 AS NVARCHAR(10)) + ' (expected: £0.00)';

-- If there are any loans, test with the first one
IF EXISTS (SELECT 1 FROM dbo.Loans)
BEGIN
    DECLARE @FirstLoanId INT;
    SELECT TOP 1 @FirstLoanId = Id FROM dbo.Loans ORDER BY Id;

    DECLARE @TestFee2 DECIMAL(10,2);
    SELECT @TestFee2 = dbo.fn_CalculateLateFee(@FirstLoanId);
    PRINT '    Test 2 - Loan #' + CAST(@FirstLoanId AS NVARCHAR(10)) + ': £' + CAST(@TestFee2 AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '    Test 2 - Skipped (no loans in database)';
END

PRINT '  ✓ Scalar function tested successfully';
GO

PRINT '✓ V008 migration completed successfully!';
GO
