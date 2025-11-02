-- ============================================================================
-- Migration: V019 - Computed Columns
-- Description: Demonstrates computed columns (both persisted and non-persisted)
--              with indexes. Shows how to derive values from existing columns
--              without storing redundant data or using application logic.
-- ============================================================================

PRINT 'Starting V019 migration - Computed Columns...';
GO

-- ======================
-- 1. Add Computed Column to Authors - FullName (PERSISTED)
-- ======================

-- Add FullName as persisted computed column
-- PERSISTED means the value is physically stored and can be indexed
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Authors')
    AND name = 'FullName'
)
BEGIN
    ALTER TABLE dbo.Authors
    ADD FullName AS (FirstName + ' ' + LastName) PERSISTED;

    PRINT 'Added FullName computed column to Authors table (PERSISTED)';
END
ELSE
BEGIN
    PRINT 'FullName column already exists on Authors table';
END
GO

-- Create index on persisted computed column for fast lookups
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Authors_FullName'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Authors_FullName
    ON dbo.Authors(FullName);

    PRINT 'Created index IX_Authors_FullName';
END
ELSE
BEGIN
    PRINT 'Index IX_Authors_FullName already exists';
END
GO

-- ======================
-- 2. Add Computed Column to Books - YearPublished (NON-PERSISTED)
-- ======================

-- Add YearPublished as non-persisted computed column
-- Non-persisted columns are calculated on-the-fly when queried
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Books')
    AND name = 'YearPublished'
)
BEGIN
    ALTER TABLE dbo.Books
    ADD YearPublished AS YEAR(PublishedDate);

    PRINT 'Added YearPublished computed column to Books table (NON-PERSISTED)';
END
ELSE
BEGIN
    PRINT 'YearPublished column already exists on Books table';
END
GO

-- ======================
-- 3. Add Computed Column to Members - Age (NON-PERSISTED)
-- ======================

-- Add Age as non-persisted computed column
-- Age changes over time, so we don't want to persist it
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Members')
    AND name = 'Age'
)
BEGIN
    ALTER TABLE dbo.Members
    ADD Age AS DATEDIFF(YEAR, DateOfBirth, GETDATE());

    PRINT 'Added Age computed column to Members table (NON-PERSISTED)';
END
ELSE
BEGIN
    PRINT 'Age column already exists on Members table';
END
GO

-- ======================
-- 4. Add Computed Column to Loans - DaysOverdue (NON-PERSISTED)
-- ======================

-- Add DaysOverdue as non-persisted computed column
-- Calculates days overdue for unreturned loans
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Loans')
    AND name = 'DaysOverdue'
)
BEGIN
    ALTER TABLE dbo.Loans
    ADD DaysOverdue AS (
        CASE
            WHEN ReturnedAt IS NULL AND GETDATE() > DueDate
            THEN DATEDIFF(DAY, DueDate, GETDATE())
            ELSE 0
        END
    );

    PRINT 'Added DaysOverdue computed column to Loans table (NON-PERSISTED)';
END
ELSE
BEGIN
    PRINT 'DaysOverdue column already exists on Loans table';
END
GO

-- ======================
-- 5. Add Persisted Computed Column to Books - PublishedDecade
-- ======================

-- Add PublishedDecade as persisted computed column
-- Groups books by decade for reporting
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Books')
    AND name = 'PublishedDecade'
)
BEGIN
    ALTER TABLE dbo.Books
    ADD PublishedDecade AS (
        CASE
            WHEN PublishedDate IS NULL THEN NULL
            ELSE (YEAR(PublishedDate) / 10) * 10
        END
    ) PERSISTED;

    PRINT 'Added PublishedDecade computed column to Books table (PERSISTED)';
END
ELSE
BEGIN
    PRINT 'PublishedDecade column already exists on Books table';
END
GO

-- Create index on persisted computed column
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Books_PublishedDecade'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Books_PublishedDecade
    ON dbo.Books(PublishedDecade);

    PRINT 'Created index IX_Books_PublishedDecade';
END
ELSE
BEGIN
    PRINT 'Index IX_Books_PublishedDecade already exists';
END
GO

-- ======================
-- 6. View - Demonstrating Computed Columns Usage
-- ======================

IF OBJECT_ID('dbo.vw_ComputedColumnsDemo', 'V') IS NOT NULL
    DROP VIEW dbo.vw_ComputedColumnsDemo;
GO

CREATE VIEW dbo.vw_ComputedColumnsDemo
AS
    SELECT
        b.Id AS BookId,
        b.Title,
        b.PublishedDate,
        b.YearPublished,        -- Computed: YEAR(PublishedDate)
        b.PublishedDecade,      -- Computed: Decade grouping
        a.FirstName,
        a.LastName,
        a.FullName,             -- Computed: FirstName + ' ' + LastName
        m.FirstName AS MemberFirstName,
        m.LastName AS MemberLastName,
        m.DateOfBirth,
        m.Age,                  -- Computed: Age in years
        l.BorrowedAt,
        l.DueDate,
        l.ReturnedAt,
        l.DaysOverdue,          -- Computed: Days past due date
        l.Status
    FROM dbo.Books b
    LEFT JOIN dbo.BookAuthors ba ON b.Id = ba.BookId
    LEFT JOIN dbo.Authors a ON ba.AuthorId = a.Id
    LEFT JOIN dbo.Loans l ON b.Id = l.BookId
    LEFT JOIN dbo.Members m ON l.MemberId = m.Id
    WHERE b.IsDeleted = 0;
GO

PRINT 'Created view vw_ComputedColumnsDemo';
GO

-- ======================
-- 7. Verification Tests
-- ======================

PRINT '';
PRINT 'Running verification tests...';
PRINT '';

-- Test 1: Verify Authors.FullName
PRINT 'Test 1: Authors with FullName computed column:';
SELECT TOP 5
    FirstName,
    LastName,
    FullName
FROM dbo.Authors
ORDER BY FullName;
GO

-- Test 2: Verify Books.YearPublished and PublishedDecade
PRINT '';
PRINT 'Test 2: Books with Year and Decade computed columns:';
SELECT TOP 5
    Title,
    PublishedDate,
    YearPublished,
    PublishedDecade
FROM dbo.Books
WHERE PublishedDate IS NOT NULL
ORDER BY PublishedDate DESC;
GO

-- Test 3: Verify Members.Age
PRINT '';
PRINT 'Test 3: Members with Age computed column:';
SELECT TOP 5
    FirstName + ' ' + LastName AS FullName,
    DateOfBirth,
    Age
FROM dbo.Members
ORDER BY Age DESC;
GO

-- Test 4: Verify Loans.DaysOverdue
PRINT '';
PRINT 'Test 4: Loans with DaysOverdue computed column:';
SELECT TOP 5
    l.Id,
    b.Title,
    l.DueDate,
    l.ReturnedAt,
    l.DaysOverdue,
    l.Status
FROM dbo.Loans l
INNER JOIN dbo.Books b ON l.BookId = b.Id
WHERE l.ReturnedAt IS NULL
ORDER BY l.DueDate;
GO

-- Test 5: Demonstrate computed column in WHERE clause
PRINT '';
PRINT 'Test 5: Find overdue loans using computed column:';
SELECT
    l.Id,
    b.Title,
    m.FirstName + ' ' + m.LastName AS MemberName,
    l.DueDate,
    l.DaysOverdue
FROM dbo.Loans l
INNER JOIN dbo.Books b ON l.BookId = b.Id
INNER JOIN dbo.Members m ON l.MemberId = m.Id
WHERE l.DaysOverdue > 0
ORDER BY l.DaysOverdue DESC;
GO

-- Test 6: Group by persisted computed column
PRINT '';
PRINT 'Test 6: Books per decade using PublishedDecade:';
SELECT
    PublishedDecade AS Decade,
    COUNT(*) AS BookCount
FROM dbo.Books
WHERE PublishedDecade IS NOT NULL
GROUP BY PublishedDecade
ORDER BY PublishedDecade DESC;
GO

-- Test 7: Search using indexed computed column
PRINT '';
PRINT 'Test 7: Search authors by FullName (using index):';
SELECT
    FullName,
    Email,
    Nationality
FROM dbo.Authors
WHERE FullName LIKE '%Smith%'
ORDER BY FullName;
GO

PRINT '';
PRINT '================================================================';
PRINT 'V019 migration completed successfully!';
PRINT 'Computed columns have been added to:';
PRINT '  - Authors.FullName (PERSISTED, indexed)';
PRINT '  - Books.YearPublished (non-persisted)';
PRINT '  - Books.PublishedDecade (PERSISTED, indexed)';
PRINT '  - Members.Age (non-persisted)';
PRINT '  - Loans.DaysOverdue (non-persisted)';
PRINT '';
PRINT 'Key Benefits:';
PRINT '  ✓ No application logic needed for derived values';
PRINT '  ✓ Values always consistent with source data';
PRINT '  ✓ Persisted columns can be indexed for fast queries';
PRINT '  ✓ Non-persisted columns save storage space';
PRINT '================================================================';
GO
