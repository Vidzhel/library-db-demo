-- Migration V004: Table-Valued Parameters (TVPs)
--
-- This migration creates:
-- 1. User-defined table type for bulk book operations
-- 2. Stored procedure that accepts the table type as a parameter
--
-- Table-Valued Parameters (TVPs) provide a way to pass multiple rows of data
-- to stored procedures without creating temporary tables. They offer:
-- - Better performance than individual INSERT statements
-- - Integration with stored procedure logic
-- - Type safety and compile-time checking
-- - Support for constraints and validation

-- =====================================================================
-- Step 1: Create User-Defined Table Type for Books
-- =====================================================================

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE is_table_type = 1 AND name = 'BookTableType')
BEGIN
    CREATE TYPE dbo.BookTableType AS TABLE
    (
        ISBN            NVARCHAR(20)    NOT NULL,
        Title           NVARCHAR(500)   NOT NULL,
        Subtitle        NVARCHAR(500)   NULL,
        Description     NVARCHAR(MAX)   NULL,
        Publisher       NVARCHAR(200)   NULL,
        PublishedDate   DATE            NULL,
        PageCount       INT             NULL,
        Language        NVARCHAR(50)    NULL,
        CategoryId      INT             NOT NULL,
        TotalCopies     INT             NOT NULL,
        AvailableCopies INT             NOT NULL,
        ShelfLocation   NVARCHAR(50)    NULL,

        -- Constraints on the table type
        INDEX IX_BookTableType_ISBN NONCLUSTERED (ISBN)
    );

    PRINT 'Created user-defined table type: BookTableType';
END
GO

-- =====================================================================
-- Step 2: Create Stored Procedure for Bulk Insert with TVP
-- =====================================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'BulkInsertBooks')
BEGIN
    DROP PROCEDURE dbo.BulkInsertBooks;
    PRINT 'Dropped existing procedure: BulkInsertBooks';
END
GO

CREATE PROCEDURE dbo.BulkInsertBooks
    @Books BookTableType READONLY,
    @InsertedCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Ensure transaction rolls back on any error

    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate input
        IF NOT EXISTS (SELECT 1 FROM @Books)
        BEGIN
            RAISERROR('No books provided for insertion', 16, 1);
            RETURN;
        END

        -- Check for duplicate ISBNs in input
        IF EXISTS (
            SELECT ISBN
            FROM @Books
            GROUP BY ISBN
            HAVING COUNT(*) > 1
        )
        BEGIN
            RAISERROR('Duplicate ISBNs found in input data', 16, 1);
            RETURN;
        END

        -- Check for existing ISBNs in database
        IF EXISTS (
            SELECT 1
            FROM Books b
            INNER JOIN @Books input ON b.ISBN = input.ISBN
            WHERE b.IsDeleted = 0
        )
        BEGIN
            -- Get the duplicate ISBNs for error message
            DECLARE @DuplicateISBNs NVARCHAR(MAX);
            SELECT @DuplicateISBNs = STRING_AGG(b.ISBN, ', ')
            FROM Books b
            INNER JOIN @Books input ON b.ISBN = input.ISBN
            WHERE b.IsDeleted = 0;

            SET @ErrorMessage = 'Books with the following ISBNs already exist: ' + @DuplicateISBNs;
            RAISERROR(@ErrorMessage, 16, 1);
            RETURN;
        END

        -- Validate all CategoryIds exist
        IF EXISTS (
            SELECT 1
            FROM @Books b
            LEFT JOIN Categories c ON b.CategoryId = c.Id
            WHERE c.Id IS NULL
        )
        BEGIN
            RAISERROR('One or more CategoryIds do not exist', 16, 1);
            RETURN;
        END

        -- Insert books with current UTC timestamp
        INSERT INTO Books (
            ISBN,
            Title,
            Subtitle,
            Description,
            Publisher,
            PublishedDate,
            PageCount,
            Language,
            CategoryId,
            TotalCopies,
            AvailableCopies,
            ShelfLocation,
            IsDeleted,
            CreatedAt,
            UpdatedAt
        )
        SELECT
            ISBN,
            Title,
            Subtitle,
            Description,
            Publisher,
            PublishedDate,
            PageCount,
            Language,
            CategoryId,
            TotalCopies,
            AvailableCopies,
            ShelfLocation,
            0 as IsDeleted,
            SYSUTCDATETIME() as CreatedAt,
            SYSUTCDATETIME() as UpdatedAt
        FROM @Books;

        -- Set output parameter
        SET @InsertedCount = @@ROWCOUNT;

        COMMIT TRANSACTION;

        -- Log success
        DECLARE @EndTime DATETIME2 = SYSUTCDATETIME();
        DECLARE @DurationMs INT = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

        PRINT 'Successfully inserted ' + CAST(@InsertedCount AS NVARCHAR(10)) +
              ' books in ' + CAST(@DurationMs AS NVARCHAR(10)) + ' ms';

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Capture error information
        SELECT
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Re-raise the error
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);

        SET @InsertedCount = 0;
    END CATCH
END
GO

-- =====================================================================
-- Step 3: Grant permissions (if needed)
-- =====================================================================

-- Grant EXECUTE permission to application user
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'library_app_user')
BEGIN
    GRANT EXECUTE ON dbo.BulkInsertBooks TO library_app_user;
    PRINT 'Granted EXECUTE permission on BulkInsertBooks to library_app_user';
END
GO

-- =====================================================================
-- Step 4: Create helper stored procedure to get inserted books
-- =====================================================================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'GetBooksByISBNs')
BEGIN
    DROP PROCEDURE dbo.GetBooksByISBNs;
END
GO

CREATE PROCEDURE dbo.GetBooksByISBNs
    @ISBNs NVARCHAR(MAX) -- Comma-separated list of ISBNs
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        ISBN,
        Title,
        Subtitle,
        Description,
        Publisher,
        PublishedDate,
        PageCount,
        Language,
        CategoryId,
        TotalCopies,
        AvailableCopies,
        ShelfLocation,
        IsDeleted,
        CreatedAt,
        UpdatedAt
    FROM Books
    WHERE ISBN IN (SELECT value FROM STRING_SPLIT(@ISBNs, ','))
        AND IsDeleted = 0
    ORDER BY ISBN;
END
GO

-- Grant permission
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'library_app_user')
BEGIN
    GRANT EXECUTE ON dbo.GetBooksByISBNs TO library_app_user;
END
GO

-- =====================================================================
-- Step 5: Example usage (commented out for migration)
-- =====================================================================

/*
-- Example: Declare and populate a table variable
DECLARE @TestBooks BookTableType;

INSERT INTO @TestBooks (ISBN, Title, Subtitle, Description, Publisher, PublishedDate,
                        PageCount, Language, CategoryId, TotalCopies, AvailableCopies, ShelfLocation)
VALUES
    ('978-0-123-45678-9', 'Test Book 1', NULL, 'Description 1', 'Publisher 1', '2024-01-01',
     300, 'English', 1, 5, 5, 'A-1-1'),
    ('978-0-123-45679-6', 'Test Book 2', 'Volume 2', 'Description 2', 'Publisher 2', '2024-02-01',
     350, 'English', 1, 3, 3, 'A-1-2');

-- Execute the stored procedure
DECLARE @Count INT;
EXEC dbo.BulkInsertBooks @Books = @TestBooks, @InsertedCount = @Count OUTPUT;

-- Display result
SELECT @Count as BooksInserted;

-- Verify insertion
SELECT * FROM Books WHERE ISBN LIKE '978-0-123-4567%';
*/

PRINT '';
PRINT '=============================================================================';
PRINT 'Migration V004 completed successfully';
PRINT 'Created:';
PRINT '  - User-defined table type: BookTableType';
PRINT '  - Stored procedure: BulkInsertBooks (with validation and error handling)';
PRINT '  - Stored procedure: GetBooksByISBNs (helper for retrieving books)';
PRINT '';
PRINT 'TVP provides a powerful way to pass multiple rows to stored procedures';
PRINT 'with better performance than individual INSERTs and support for business logic.';
PRINT '=============================================================================';
GO
