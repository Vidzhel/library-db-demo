-- ============================================================================
-- Migration: V018 - JSON Support
-- Description: Adds JSON column to Books table for flexible metadata storage.
--              Demonstrates JSON_VALUE(), JSON_QUERY(), OPENJSON() and
--              other JSON functions available in SQL Server 2016+.
-- ============================================================================

PRINT 'Starting V018 migration - JSON Support...';
GO

-- ======================
-- 1. Add JSON Column to Books Table
-- ======================

-- Add Metadata column with CHECK constraint to ensure valid JSON
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Books')
    AND name = 'Metadata'
)
BEGIN
    ALTER TABLE dbo.Books
    ADD Metadata NVARCHAR(MAX) NULL;

    PRINT 'Added Metadata column to Books table.';
END
ELSE
BEGIN
    PRINT 'Metadata column already exists on Books table.';
END
GO

-- Add CHECK constraint to ensure valid JSON
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Books_Metadata_ValidJson'
)
BEGIN
    ALTER TABLE dbo.Books
    ADD CONSTRAINT CK_Books_Metadata_ValidJson
        CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1);

    PRINT 'Added CHECK constraint for valid JSON.';
END
ELSE
BEGIN
    PRINT 'JSON CHECK constraint already exists.';
END
GO

-- ======================
-- 2. Stored Procedure - Search Books by Metadata Value
-- ======================

IF OBJECT_ID('dbo.sp_GetBooksByMetadataValue', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBooksByMetadataValue;
GO

CREATE PROCEDURE dbo.sp_GetBooksByMetadataValue
    @JsonPath NVARCHAR(100),
    @SearchValue NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Use JSON_VALUE to extract and filter by JSON property
    -- Example: @JsonPath = '$.genre', @SearchValue = 'Science Fiction'
    SELECT
        b.Id,
        b.ISBN,
        b.Title,
        b.Subtitle,
        b.Publisher,
        b.PublishedDate,
        b.CategoryId,
        b.TotalCopies,
        b.AvailableCopies,
        b.Metadata,
        JSON_VALUE(b.Metadata, @JsonPath) AS ExtractedValue
    FROM dbo.Books b
    WHERE JSON_VALUE(b.Metadata, @JsonPath) = @SearchValue
        AND b.IsDeleted = 0
    ORDER BY b.Title;
END
GO

PRINT 'Created stored procedure sp_GetBooksByMetadataValue.';
GO

-- ======================
-- 3. View - Books with Parsed Metadata
-- ======================

IF OBJECT_ID('dbo.vw_BooksWithMetadata', 'V') IS NOT NULL
    DROP VIEW dbo.vw_BooksWithMetadata;
GO

CREATE VIEW dbo.vw_BooksWithMetadata
AS
    SELECT
        b.Id,
        b.ISBN,
        b.Title,
        b.Publisher,
        b.PublishedDate,
        b.Metadata,
        -- Extract specific JSON properties
        JSON_VALUE(b.Metadata, '$.genre') AS Genre,
        JSON_VALUE(b.Metadata, '$.series') AS Series,
        JSON_VALUE(b.Metadata, '$.seriesNumber') AS SeriesNumber,
        JSON_VALUE(b.Metadata, '$.originalLanguage') AS OriginalLanguage,
        JSON_VALUE(b.Metadata, '$.awards') AS Awards,
        -- Extract complex object as JSON string
        JSON_QUERY(b.Metadata, '$.tags') AS TagsJson,
        JSON_QUERY(b.Metadata, '$.customFields') AS CustomFieldsJson
    FROM dbo.Books b
    WHERE b.Metadata IS NOT NULL
        AND b.IsDeleted = 0;
GO

PRINT 'Created view vw_BooksWithMetadata.';
GO

-- ======================
-- 4. Table-Valued Function - Extract Tags from JSON Array
-- ======================

IF OBJECT_ID('dbo.fn_ExtractMetadataTags', 'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_ExtractMetadataTags;
GO

CREATE FUNCTION dbo.fn_ExtractMetadataTags(@BookId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT
        @BookId AS BookId,
        tags.[value] AS Tag
    FROM dbo.Books b
    CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
    WHERE b.Id = @BookId
        AND b.Metadata IS NOT NULL
);
GO

PRINT 'Created function fn_ExtractMetadataTags.';
GO

-- ======================
-- 5. Table-Valued Function - Get All Books by Tag
-- ======================

IF OBJECT_ID('dbo.fn_GetBooksByTag', 'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_GetBooksByTag;
GO

CREATE FUNCTION dbo.fn_GetBooksByTag(@Tag NVARCHAR(50))
RETURNS TABLE
AS
RETURN
(
    SELECT DISTINCT
        b.Id,
        b.ISBN,
        b.Title,
        b.Publisher,
        tags.[value] AS Tag
    FROM dbo.Books b
    CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
    WHERE tags.[value] = @Tag
        AND b.IsDeleted = 0
);
GO

PRINT 'Created function fn_GetBooksByTag.';
GO

-- ======================
-- 6. Seed Sample JSON Metadata
-- ======================

-- Update existing books with sample metadata
-- Only update books that don't already have metadata

PRINT 'Seeding sample JSON metadata...';

-- Science Fiction book
UPDATE dbo.Books
SET Metadata = JSON_MODIFY(
    JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(
                JSON_MODIFY(
                    JSON_MODIFY('{}', '$.genre', 'Science Fiction'),
                    '$.tags', JSON_QUERY('["sci-fi", "space", "adventure"]')
                ),
                '$.series', 'Foundation'
            ),
            '$.seriesNumber', 1
        ),
        '$.originalLanguage', 'English'
    ),
    '$.awards', 'Hugo Award'
)
WHERE Title = 'Foundation'
    AND Metadata IS NULL;

-- Fantasy book
UPDATE dbo.Books
SET Metadata = JSON_MODIFY(
    JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(
                JSON_MODIFY('{}', '$.genre', 'Fantasy'),
                '$.tags', JSON_QUERY('["fantasy", "magic", "epic"]')
            ),
            '$.series', 'The Lord of the Rings'
        ),
        '$.seriesNumber', 1
    ),
    '$.originalLanguage', 'English'
)
WHERE Title = 'The Fellowship of the Ring'
    AND Metadata IS NULL;

-- Non-fiction book
UPDATE dbo.Books
SET Metadata = JSON_MODIFY(
    JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY('{}', '$.genre', 'Non-Fiction'),
            '$.tags', JSON_QUERY('["computer science", "algorithms", "education"]')
        ),
        '$.originalLanguage', 'English'
    ),
    '$.awards', 'Turing Award Lectures'
)
WHERE Title LIKE '%Computer%'
    AND Metadata IS NULL;

-- Mystery book
UPDATE dbo.Books
SET Metadata = JSON_MODIFY(
    JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(
                JSON_MODIFY('{}', '$.genre', 'Mystery'),
                '$.tags', JSON_QUERY('["mystery", "detective", "crime"]')
            ),
            '$.series', 'Sherlock Holmes'
        ),
        '$.seriesNumber', 1
    ),
    '$.originalLanguage', 'English'
)
WHERE Title LIKE '%Sherlock%' OR Title LIKE '%Holmes%'
    AND Metadata IS NULL;

PRINT 'Sample JSON metadata seeded.';
GO

-- ======================
-- 7. Verification Tests
-- ======================

PRINT '';
PRINT 'Running verification tests...';
PRINT '';

-- Test 1: Show books with metadata
PRINT 'Test 1: Books with metadata (using view):';
SELECT TOP 5
    Id,
    Title,
    Genre,
    Series,
    TagsJson
FROM dbo.vw_BooksWithMetadata
ORDER BY Title;
GO

-- Test 2: Search by genre using stored procedure
PRINT '';
PRINT 'Test 2: Books in Science Fiction genre (using stored procedure):';
EXEC dbo.sp_GetBooksByMetadataValue
    @JsonPath = '$.genre',
    @SearchValue = 'Science Fiction';
GO

-- Test 3: Extract tags using OPENJSON function
PRINT '';
PRINT 'Test 3: Extract tags from a book (using function):';
DECLARE @BookId INT;
SELECT TOP 1 @BookId = Id FROM dbo.Books WHERE Metadata IS NOT NULL;

SELECT * FROM dbo.fn_ExtractMetadataTags(@BookId);
GO

-- Test 4: Find all books with a specific tag
PRINT '';
PRINT 'Test 4: Books tagged with "fantasy" (using function):';
SELECT * FROM dbo.fn_GetBooksByTag('fantasy');
GO

-- Test 5: Complex JSON query with OPENJSON
PRINT '';
PRINT 'Test 5: All books with their tags (using OPENJSON):';
SELECT TOP 5
    b.Id,
    b.Title,
    JSON_VALUE(b.Metadata, '$.genre') AS Genre,
    tags.[value] AS Tag
FROM dbo.Books b
CROSS APPLY OPENJSON(b.Metadata, '$.tags') AS tags
WHERE b.Metadata IS NOT NULL
    AND b.IsDeleted = 0
ORDER BY b.Title, tags.[value];
GO

-- Test 6: Update JSON - Add a new property
PRINT '';
PRINT 'Test 6: Demonstrating JSON_MODIFY to add/update properties:';
DECLARE @TestBookId INT;
SELECT TOP 1 @TestBookId = Id FROM dbo.Books WHERE Metadata IS NOT NULL;

-- Show before
PRINT 'Before:';
SELECT Id, Title, Metadata
FROM dbo.Books
WHERE Id = @TestBookId;

-- Add a rating property
UPDATE dbo.Books
SET Metadata = JSON_MODIFY(Metadata, '$.rating', 4.5)
WHERE Id = @TestBookId;

-- Show after
PRINT 'After adding rating:';
SELECT Id, Title, Metadata, JSON_VALUE(Metadata, '$.rating') AS Rating
FROM dbo.Books
WHERE Id = @TestBookId;
GO

PRINT '';
PRINT '================================================================';
PRINT 'V018 migration completed successfully!';
PRINT 'JSON support has been added to the Books table.';
PRINT '';
PRINT 'Key features:';
PRINT '  - Books.Metadata column with JSON validation';
PRINT '  - sp_GetBooksByMetadataValue - Search by JSON property';
PRINT '  - vw_BooksWithMetadata - View with parsed JSON fields';
PRINT '  - fn_ExtractMetadataTags - Extract tags array';
PRINT '  - fn_GetBooksByTag - Find books by tag';
PRINT '================================================================';
GO
