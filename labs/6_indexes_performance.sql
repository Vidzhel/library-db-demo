-- =============================================
-- Lab 6: Indexes and Performance Optimization
-- =============================================
-- This lab covers essential performance optimization concepts including:
--   1. Query Execution Plans & Analysis
--   2. Index Design & Strategies
--   3. Index Maintenance
--   4. Performance Troubleshooting
--
-- Target Database: LibraryDb
-- SQL Server Version: 2019+
--
-- IMPORTANT: Most queries are READ-ONLY demonstrations.
-- Index creation examples are commented out for safety.
-- =============================================

USE LibraryDb;
GO

PRINT '========================================';
PRINT 'LAB 6: INDEXES AND PERFORMANCE OPTIMIZATION';
PRINT '========================================';
PRINT 'Database: ' + DB_NAME();
PRINT 'Server: ' + @@SERVERNAME;
PRINT 'Execution Time: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT '========================================';
GO

-- =============================================
-- SECTION 1: QUERY EXECUTION PLANS & ANALYSIS
-- =============================================
-- An execution plan is SQL Server's strategy for executing a query.
-- Understanding execution plans is CRITICAL for performance optimization.
--
-- Key Concepts:
--   - Execution Plan: The steps SQL Server takes to execute a query
--   - Estimated Plan: Plan created by query optimizer (before execution)
--   - Actual Plan: Plan with runtime statistics (after execution)
--   - Query Cost: Relative cost units assigned by optimizer
--   - Operators: Actions in plan (Scan, Seek, Join, Sort, etc.)
--
-- How to View Plans:
--   - SSMS: Ctrl+L (Estimated), Ctrl+M then execute (Actual)
--   - SET SHOWPLAN_TEXT ON (text format)
--   - SET STATISTICS PROFILE ON (execution statistics)
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 1: EXECUTION PLANS & ANALYSIS';
PRINT '========================================';
GO

-- =============================================
-- 1.1 ENABLE STATISTICS
-- =============================================
-- SET STATISTICS IO shows number of logical/physical reads
-- SET STATISTICS TIME shows CPU and elapsed time
--
-- Logical Reads: Pages read from buffer cache (memory)
-- Physical Reads: Pages read from disk (expensive!)
-- Read-Ahead Reads: Pages read in anticipation
--
-- Goal: Minimize logical reads (indicates efficiency)
-- =============================================

PRINT '';
PRINT '1.1 Statistics Demonstration:';
PRINT 'ℹ️  Watch the Messages tab for IO and TIME statistics';
GO

-- Turn on statistics
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

-- Simple query to demonstrate
PRINT 'Query 1: Simple SELECT without WHERE clause (Table Scan)';
GO

SELECT
    BookId,
    Title,
    ISBN,
    Publisher
FROM dbo.Books;
GO

PRINT '';
PRINT 'Interpreting Results (check Messages tab):';
PRINT '  - Table ''Books'' Scan count: Number of times table was scanned';
PRINT '  - Logical reads: Pages read from memory (8KB each)';
PRINT '  - Physical reads: Pages read from disk (should be 0 after first run)';
PRINT '  - CPU time: Milliseconds of CPU usage';
PRINT '  - Elapsed time: Total time including waits';
GO

-- Query with WHERE clause using indexed column
PRINT '';
PRINT 'Query 2: SELECT with WHERE on indexed column (Index Seek)';
GO

SELECT
    BookId,
    Title,
    ISBN,
    Publisher,
    PageCount
FROM dbo.Books
WHERE ISBN = '978-0-13-468599-1';
GO

PRINT '';
PRINT 'Compare the statistics:';
PRINT '  - Index Seek should have FAR fewer logical reads than Table Scan';
PRINT '  - This demonstrates the power of indexes!';
GO

-- =============================================
-- 1.2 TABLE SCAN vs INDEX SEEK vs INDEX SCAN
-- =============================================
-- Understanding these operations is fundamental to query optimization.
--
-- TABLE SCAN (Heap Scan):
--   - Reads every row in a heap (table without clustered index)
--   - Most expensive operation
--   - Acceptable only for very small tables
--
-- CLUSTERED INDEX SCAN:
--   - Reads every row in a table with clustered index
--   - Still reads all data, but in sorted order
--   - Better than Table Scan, but still expensive for large tables
--
-- INDEX SEEK:
--   - Uses index B-tree to jump directly to needed rows
--   - BEST performance (logarithmic lookup)
--   - Requires WHERE clause matching index key columns
--
-- INDEX SCAN:
--   - Reads entire nonclustered index
--   - Better than table scan if index is smaller
--   - Happens when WHERE clause doesn't match index leading column
-- =============================================

PRINT '';
PRINT '1.2 Table Scan vs Index Seek vs Index Scan:';
GO

-- Example 1: Table Scan (no useful index)
PRINT '';
PRINT 'Example 1: Query causing Table/Clustered Index Scan';
PRINT '(Searching on non-indexed column)';
GO

SELECT
    BookId,
    Title,
    Publisher,
    PageCount
FROM dbo.Books
WHERE Publisher = 'Random House';  -- Publisher is not indexed
GO

PRINT 'Result: Clustered Index Scan (must read all rows to find matches)';
PRINT 'Logical reads: High (all pages)';
GO

-- Example 2: Index Seek (perfect match)
PRINT '';
PRINT 'Example 2: Query using Index Seek';
PRINT '(Searching on indexed column with equality)';
GO

SELECT
    BookId,
    Title,
    ISBN
FROM dbo.Books
WHERE ISBN = '978-0-13-468599-1';  -- ISBN has unique index UQ_Books_ISBN
GO

PRINT 'Result: Index Seek (B-tree navigation to exact row)';
PRINT 'Logical reads: Very low (few pages)';
GO

-- Example 3: Index Seek with range
PRINT '';
PRINT 'Example 3: Index Seek with Range Query';
GO

SELECT
    LoanId,
    MemberId,
    BookId,
    BorrowedAt,
    DueDate
FROM dbo.Loans
WHERE BorrowedAt >= '2024-01-01'
    AND BorrowedAt < '2024-02-01';
GO

PRINT 'Result: Index Seek with range scan';
PRINT 'Seeks to start of range, then scans until end';
GO

-- Turn off statistics for cleaner output
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO

-- =============================================
-- 1.3 READING EXECUTION PLANS
-- =============================================
-- Execution plans are read RIGHT-TO-LEFT, TOP-TO-BOTTOM.
-- Each operator has a cost percentage (adds up to 100% for the query).
--
-- Key Operators to Know:
--   - Clustered Index Scan: Reading all rows (expensive)
--   - Index Seek: B-tree lookup (efficient)
--   - Key Lookup: Extra read to get columns not in index (expensive!)
--   - Nested Loops: Join method for small datasets
--   - Hash Match: Join method for large datasets
--   - Sort: Sorting operation (expensive, avoid if possible)
--   - Filter: Applying WHERE clause after reading data (inefficient)
--
-- Red Flags in Plans:
--   - Table Scans on large tables
--   - Key Lookups with high row counts
--   - Missing Index suggestions (green text in SSMS)
--   - Thick arrows (many rows flowing)
--   - Sorts and spools
--   - Warnings (yellow exclamation marks)
-- =============================================

PRINT '';
PRINT '1.3 Execution Plan Reading Guide:';
PRINT '';
PRINT '📊 TO VIEW EXECUTION PLANS IN SSMS:';
PRINT '   - Estimated Plan: Ctrl+L (before running query)';
PRINT '   - Actual Plan: Ctrl+M, then run query';
PRINT '';
PRINT '📖 READING EXECUTION PLANS:';
PRINT '   - Read RIGHT to LEFT, TOP to BOTTOM';
PRINT '   - Each box is an operator (action)';
PRINT '   - Arrow thickness = number of rows';
PRINT '   - Percentages = relative cost of each operator';
PRINT '';
PRINT '✅ GOOD SIGNS:';
PRINT '   - Index Seek operators';
PRINT '   - Thin arrows (few rows)';
PRINT '   - Low cost percentages spread across operators';
PRINT '   - No warnings';
PRINT '';
PRINT '❌ RED FLAGS:';
PRINT '   - Table Scan / Clustered Index Scan on large tables';
PRINT '   - Key Lookup with many rows';
PRINT '   - Thick arrows (millions of rows)';
PRINT '   - High-cost operators (>50%)';
PRINT '   - Yellow warning icons';
PRINT '   - Sort operators (ORDER BY without index)';
PRINT '   - Missing Index hint (green text)';
GO

-- Example query to analyze (enable actual plan with Ctrl+M)
PRINT '';
PRINT 'Example Query for Plan Analysis:';
PRINT 'ℹ️  Enable "Include Actual Execution Plan" (Ctrl+M) before running';
GO

SELECT
    b.Title,
    b.ISBN,
    a.FirstName,
    a.LastName,
    c.CategoryName
FROM dbo.Books b
INNER JOIN dbo.BookAuthors ba ON b.BookId = ba.BookId
INNER JOIN dbo.Authors a ON ba.AuthorId = a.AuthorId
INNER JOIN dbo.Categories c ON b.CategoryId = c.CategoryId
WHERE b.PublishedDate >= '2020-01-01'
ORDER BY b.Title;
GO

PRINT '';
PRINT 'Analyze the execution plan:';
PRINT '  1. Which operators are most expensive?';
PRINT '  2. Are there any Index Seeks or mostly Scans?';
PRINT '  3. Do you see any Key Lookups?';
PRINT '  4. Are there any Sort operators?';
PRINT '  5. Does SQL Server suggest any missing indexes?';
GO

-- =============================================
-- 1.4 ACTUAL vs ESTIMATED EXECUTION PLANS
-- =============================================
-- Estimated Plan:
--   - Created by query optimizer using statistics
--   - Shows what SQL Server THINKS will happen
--   - No actual execution (fast to generate)
--   - No runtime metrics
--
-- Actual Plan:
--   - Created after query execution
--   - Shows what ACTUALLY happened
--   - Includes actual row counts, execution time
--   - Use this for performance troubleshooting
--
-- When Estimates Differ from Actuals:
--   - Statistics are out of date (UPDATE STATISTICS)
--   - Parameter sniffing issues
--   - Complex predicates optimizer can't estimate
--   - Data skew (uneven distribution)
-- =============================================

PRINT '';
PRINT '1.4 Estimated vs Actual Execution Plans:';
PRINT '';
PRINT '📊 ESTIMATED PLAN (Ctrl+L):';
PRINT '   ✓ Fast to generate (no execution)';
PRINT '   ✓ Shows optimizer strategy';
PRINT '   ✗ No actual row counts or timings';
PRINT '   ✗ May not match actual performance';
PRINT '';
PRINT '📊 ACTUAL PLAN (Ctrl+M + Execute):';
PRINT '   ✓ Real execution metrics';
PRINT '   ✓ Actual vs estimated row counts';
PRINT '   ✓ True performance characteristics';
PRINT '   ✗ Requires query execution (may be slow)';
PRINT '';
PRINT '⚠️  WHEN ESTIMATES ARE WRONG:';
PRINT '   - Update statistics: UPDATE STATISTICS TableName;';
PRINT '   - Check statistics age: DBCC SHOW_STATISTICS';
PRINT '   - Look for parameter sniffing issues';
PRINT '   - Consider query rewrite or hints';
GO

-- =============================================
-- SECTION 2: INDEX DESIGN & STRATEGIES
-- =============================================
-- Indexes are database structures that improve query performance by
-- providing fast access paths to data. Think of them like a book index.
--
-- Index Types in SQL Server:
--   1. CLUSTERED INDEX:
--      - Determines physical order of data in table
--      - One per table (usually on Primary Key)
--      - Table data IS the leaf level of the index
--
--   2. NONCLUSTERED INDEX:
--      - Separate structure with pointers to data rows
--      - Multiple per table (up to 999 in SQL Server)
--      - Contains index keys + row locator
--
--   3. UNIQUE INDEX:
--      - Enforces uniqueness (no duplicate values)
--      - Can be clustered or nonclustered
--
--   4. FILTERED INDEX:
--      - Index on subset of rows (has WHERE clause)
--      - Smaller, more efficient for specific queries
--
--   5. COVERING INDEX:
--      - Nonclustered index with INCLUDE clause
--      - Contains all columns needed by query
--      - Eliminates key lookups
--
-- Index Structure:
--   - B-tree (balanced tree) with three levels typically:
--     - Root level (1 page)
--     - Intermediate level(s) (few pages)
--     - Leaf level (many pages)
--   - Logarithmic lookup time: log_n(rows)
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 2: INDEX DESIGN & STRATEGIES';
PRINT '========================================';
GO

-- =============================================
-- 2.1 ANALYZING EXISTING INDEXES
-- =============================================
-- Let's examine the indexes already in LibraryDb
-- (created in migration V002__add_indexes.sql)
-- =============================================

PRINT '';
PRINT '2.1 Current Indexes in LibraryDb:';
GO

SELECT
    OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey,
    i.fill_factor AS FillFactor,
    i.has_filter AS IsFiltered,
    i.filter_definition AS FilterDefinition,
    STUFF((
        SELECT ', ' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS KeyColumns,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.is_included_column = 1
        ORDER BY ic.index_column_id
        FOR XML PATH('')
    ), 1, 2, '') AS IncludedColumns
FROM sys.indexes i
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
    AND i.type > 0  -- Exclude heaps
ORDER BY TableName, i.index_id;
GO

PRINT '';
PRINT 'Index Design Observations:';
PRINT '  ✓ All tables have clustered indexes (PK)';
PRINT '  ✓ Foreign keys are indexed for join performance';
PRINT '  ✓ Several filtered indexes for specific scenarios';
PRINT '  ✓ INCLUDE clause used for covering queries';
PRINT '  ✓ FILLFACTOR tuned based on INSERT frequency';
GO

-- =============================================
-- 2.2 INDEX SELECTIVITY & CARDINALITY
-- =============================================
-- Selectivity: How well an index narrows down the result set
--   - High Selectivity: Few rows match (GOOD for indexes)
--   - Low Selectivity: Many rows match (BAD for indexes)
--
-- Cardinality: Number of distinct values in a column
--   - High Cardinality: Many unique values (GOOD for indexes)
--   - Low Cardinality: Few unique values (BAD for indexes)
--
-- Examples:
--   - High Selectivity: Email, ISBN, Social Security Number
--   - Low Selectivity: Gender (M/F), Boolean flags, Status codes
--
-- Rule of Thumb:
--   - Don't index columns where >20% of rows match a typical query
--   - Index columns with high cardinality (many distinct values)
-- =============================================

PRINT '';
PRINT '2.2 Index Selectivity Analysis:';
GO

-- Check cardinality of various columns in Books table
SELECT
    'Books' AS TableName,
    'ISBN' AS ColumnName,
    COUNT(DISTINCT ISBN) AS DistinctValues,
    COUNT(*) AS TotalRows,
    CAST(COUNT(DISTINCT ISBN) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS CardinalityPercent,
    CASE
        WHEN COUNT(DISTINCT ISBN) * 100.0 / COUNT(*) > 95 THEN 'Excellent for indexing'
        WHEN COUNT(DISTINCT ISBN) * 100.0 / COUNT(*) > 80 THEN 'Good for indexing'
        WHEN COUNT(DISTINCT ISBN) * 100.0 / COUNT(*) > 50 THEN 'Moderate for indexing'
        ELSE 'Poor for indexing (low cardinality)'
    END AS IndexSuitability
FROM dbo.Books
WHERE ISBN IS NOT NULL

UNION ALL

SELECT
    'Books' AS TableName,
    'CategoryId' AS ColumnName,
    COUNT(DISTINCT CategoryId) AS DistinctValues,
    COUNT(*) AS TotalRows,
    CAST(COUNT(DISTINCT CategoryId) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS CardinalityPercent,
    CASE
        WHEN COUNT(DISTINCT CategoryId) * 100.0 / COUNT(*) > 95 THEN 'Excellent for indexing'
        WHEN COUNT(DISTINCT CategoryId) * 100.0 / COUNT(*) > 80 THEN 'Good for indexing'
        WHEN COUNT(DISTINCT CategoryId) * 100.0 / COUNT(*) > 50 THEN 'Moderate for indexing'
        ELSE 'Poor for indexing (low cardinality)'
    END AS IndexSuitability
FROM dbo.Books

UNION ALL

SELECT
    'Books' AS TableName,
    'IsDeleted' AS ColumnName,
    COUNT(DISTINCT IsDeleted) AS DistinctValues,
    COUNT(*) AS TotalRows,
    CAST(COUNT(DISTINCT IsDeleted) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS CardinalityPercent,
    CASE
        WHEN COUNT(DISTINCT IsDeleted) * 100.0 / COUNT(*) > 95 THEN 'Excellent for indexing'
        WHEN COUNT(DISTINCT IsDeleted) * 100.0 / COUNT(*) > 80 THEN 'Good for indexing'
        WHEN COUNT(DISTINCT IsDeleted) * 100.0 / COUNT(*) > 50 THEN 'Moderate for indexing'
        ELSE 'Poor for indexing (low cardinality)'
    END AS IndexSuitability
FROM dbo.Books;
GO

PRINT '';
PRINT 'Cardinality Insights:';
PRINT '  - ISBN: High cardinality (unique) - EXCELLENT for indexing';
PRINT '  - CategoryId: Moderate cardinality - GOOD for indexing (foreign key)';
PRINT '  - IsDeleted: Very low cardinality (true/false) - Use FILTERED index!';
GO

-- =============================================
-- 2.3 COMPOSITE INDEXES & KEY ORDER
-- =============================================
-- Composite Index: Index on multiple columns
-- Key Order: The sequence of columns in the index definition
--
-- CRITICAL RULE: Column order matters!
--   - Index on (A, B, C) can satisfy queries on:
--     ✓ WHERE A = 1
--     ✓ WHERE A = 1 AND B = 2
--     ✓ WHERE A = 1 AND B = 2 AND C = 3
--     ✗ WHERE B = 2 (doesn't use index efficiently)
--     ✗ WHERE C = 3 (doesn't use index)
--
-- Best Practice for Column Order:
--   1. Equality columns first (WHERE col = value)
--   2. Range columns next (WHERE col > value)
--   3. Most selective columns first
--   4. Columns used in ORDER BY
-- =============================================

PRINT '';
PRINT '2.3 Composite Index Column Order:';
GO

-- Example: Loans table index on (Status, DueDate)
-- This index supports queries filtering on Status, or Status + DueDate
PRINT 'Existing Composite Index: IX_Loans_Status_DueDate';
PRINT 'Key Columns: Status, DueDate';
PRINT '';

-- Query 1: Uses index efficiently (Status is leading column)
PRINT 'Query 1: WHERE Status = ... (Uses index)';
SET STATISTICS IO ON;

SELECT LoanId, MemberId, BookId, Status, DueDate
FROM dbo.Loans
WHERE Status = 1;  -- Borrowed

SET STATISTICS IO OFF;
GO

-- Query 2: Uses index efficiently (Status + DueDate)
PRINT '';
PRINT 'Query 2: WHERE Status = ... AND DueDate > ... (Uses index efficiently)';
SET STATISTICS IO ON;

SELECT LoanId, MemberId, BookId, Status, DueDate
FROM dbo.Loans
WHERE Status = 1  -- Borrowed
    AND DueDate > GETDATE();

SET STATISTICS IO OFF;
GO

-- Query 3: Less efficient (DueDate only, Status not specified)
PRINT '';
PRINT 'Query 3: WHERE DueDate > ... (Index less efficient, may scan)';
SET STATISTICS IO ON;

SELECT LoanId, MemberId, BookId, Status, DueDate
FROM dbo.Loans
WHERE DueDate > GETDATE();  -- Can't seek directly to DueDate

SET STATISTICS IO OFF;
GO

PRINT '';
PRINT 'Key Takeaway:';
PRINT '  - Index (Status, DueDate) is optimized for queries filtering on Status';
PRINT '  - Column order determines which queries can use the index efficiently';
PRINT '  - Most selective columns should be first';
GO

-- =============================================
-- 2.4 COVERING INDEXES (INCLUDE Clause)
-- =============================================
-- Covering Index: An index that contains ALL columns needed by a query
--
-- Without Covering:
--   1. Index Seek (find matching rows in nonclustered index)
--   2. Key Lookup (fetch additional columns from clustered index)
--   - Key Lookup is EXPENSIVE (random I/O)
--
-- With Covering (INCLUDE clause):
--   1. Index Seek (all needed columns are in the index)
--   - No Key Lookup needed!
--
-- INCLUDE vs Key Columns:
--   - Key Columns: Used for searching, sorting, seeking
--   - INCLUDE Columns: Only stored in leaf pages, not in B-tree structure
--
-- Benefits of INCLUDE:
--   - Smaller B-tree (faster seeks)
--   - Covers queries without Key Lookup
--   - Less overhead than adding to key columns
-- =============================================

PRINT '';
PRINT '2.4 Covering Indexes (INCLUDE clause):';
GO

-- Example: Query that would benefit from covering index
PRINT 'Query needing Title and PageCount (not in index):';
SET STATISTICS IO ON;

SELECT
    BookId,
    CategoryId,
    Title,
    PageCount
FROM dbo.Books
WHERE CategoryId = 1;

SET STATISTICS IO OFF;
GO

PRINT '';
PRINT 'Execution Plan Analysis:';
PRINT '  - Index Seek on IX_Books_CategoryId (finds BookId)';
PRINT '  - Key Lookup to get Title and PageCount (expensive!)';
PRINT '';
PRINT 'Solution: The existing index ALREADY includes these columns:';
PRINT '  CREATE NONCLUSTERED INDEX IX_Books_CategoryId';
PRINT '  ON Books(CategoryId)';
PRINT '  INCLUDE (Title, ISBN, Publisher, PublishedDate, PageCount, AvailableCopies);';
PRINT '';
PRINT '✓ This is a covering index - no Key Lookup needed!';
GO

-- =============================================
-- 2.5 FILTERED INDEXES
-- =============================================
-- Filtered Index: Index on a subset of rows (has WHERE clause)
--
-- Benefits:
--   - Smaller index (less storage, faster maintenance)
--   - More efficient for specific queries
--   - Better statistics for subset of data
--   - Lower maintenance overhead
--
-- Use Cases:
--   - Sparse columns (many NULLs)
--   - Status-based queries (WHERE IsActive = 1)
--   - Date ranges (WHERE OrderDate > '2024-01-01')
--   - Soft deletes (WHERE IsDeleted = 0)
--
-- Example from V002:
--   CREATE INDEX IX_Books_IsDeleted_AvailableCopies
--   ON Books(IsDeleted, AvailableCopies)
--   WHERE IsDeleted = 0;  -- Only index active books
-- =============================================

PRINT '';
PRINT '2.5 Filtered Indexes:';
GO

-- Show filtered indexes in database
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.filter_definition AS FilterCondition,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS KeyColumns
FROM sys.indexes i
WHERE i.has_filter = 1
    AND OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
ORDER BY TableName, IndexName;
GO

PRINT '';
PRINT 'Example: Querying only active books';
SET STATISTICS IO ON;

SELECT
    BookId,
    Title,
    AvailableCopies
FROM dbo.Books
WHERE IsDeleted = 0  -- Matches filter condition!
    AND AvailableCopies > 0;

SET STATISTICS IO OFF;
GO

PRINT '';
PRINT 'Benefits of Filtered Index:';
PRINT '  ✓ Smaller index (excludes deleted books)';
PRINT '  ✓ Faster to search (fewer pages)';
PRINT '  ✓ Lower maintenance cost (deleted books don''t update index)';
PRINT '  ✓ Better statistics for active books';
PRINT '';
PRINT '⚠️  Query must match filter condition to use the index!';
GO

-- =============================================
-- 2.6 FILLFACTOR & PAGE SPLITS
-- =============================================
-- FILLFACTOR: Percentage of space filled on index leaf pages
--
-- Default: 0 (same as 100) - Fill pages completely
--
-- Why use FILLFACTOR < 100?
--   - Prevent page splits during INSERT/UPDATE
--   - Page Split: Expensive operation when page is full
--     1. Allocate new page
--     2. Move half of rows to new page
--     3. Update parent pages
--   - Causes fragmentation and performance degradation
--
-- FILLFACTOR Guidelines:
--   - 100: Read-only tables (no INSERTs/UPDATEs)
--   - 90-95: Tables with mostly SELECTs, occasional writes
--   - 80-85: Tables with balanced read/write
--   - 70-75: Tables with heavy INSERT/UPDATE activity
--   - 50-70: Tables with high INSERT in middle of key range
--
-- Trade-off:
--   - Lower FILLFACTOR = Less page splits BUT more disk space & more pages to scan
-- =============================================

PRINT '';
PRINT '2.6 FILLFACTOR and Page Splits:';
GO

-- Check FILLFACTOR settings in current indexes
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.fill_factor AS FillFactor,
    CASE
        WHEN i.fill_factor = 0 OR i.fill_factor = 100 THEN 'Read-heavy / Static data'
        WHEN i.fill_factor >= 90 THEN 'Mostly reads, few writes'
        WHEN i.fill_factor >= 80 THEN 'Balanced read/write'
        WHEN i.fill_factor >= 70 THEN 'High INSERT/UPDATE activity'
        ELSE 'Very high write activity'
    END AS WorkloadType
FROM sys.indexes i
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
    AND i.type > 0
ORDER BY i.fill_factor, TableName;
GO

PRINT '';
PRINT 'FILLFACTOR Best Practices:';
PRINT '  • 100%: Read-only tables (no page splits)';
PRINT '  • 90%: Books table (mostly reads, occasional new books)';
PRINT '  • 80%: Loans table (moderate INSERT/UPDATE frequency)';
PRINT '  • 70%: High-frequency INSERT tables';
PRINT '';
PRINT '⚠️  Setting FILLFACTOR only affects index at REBUILD time!';
PRINT '   Run: ALTER INDEX IX_IndexName ON TableName REBUILD WITH (FILLFACTOR = 85);';
GO

-- =============================================
-- 2.7 WHEN NOT TO INDEX
-- =============================================
-- Indexes are NOT free! They have overhead:
--   1. Storage: Each index takes disk space
--   2. Maintenance: INSERT/UPDATE/DELETE must update ALL indexes
--   3. Statistics: Must be kept up-to-date
--
-- DO NOT index when:
--   ❌ Table is very small (<1000 rows) - table scan is fast enough
--   ❌ Column has low cardinality (Gender, Boolean) - use filtered index instead
--   ❌ Column is frequently updated - index maintenance overhead too high
--   ❌ Query returns >20% of rows - table scan is more efficient
--   ❌ Table has very high INSERT rate - indexes slow down INSERTs
--   ❌ Index is never used (check sys.dm_db_index_usage_stats)
--
-- Signs of Too Many Indexes:
--   - Slow INSERT/UPDATE/DELETE operations
--   - High write latency
--   - Unused indexes (0 reads in usage stats)
--   - Excessive fragmentation
-- =============================================

PRINT '';
PRINT '2.7 When NOT to Index:';
PRINT '';
PRINT '❌ DON''T INDEX IF:';
PRINT '   • Table is very small (<1,000 rows)';
PRINT '   • Column has low cardinality (few distinct values)';
PRINT '   • Column is frequently updated (high maintenance cost)';
PRINT '   • Query returns >20% of table rows';
PRINT '   • Table has extremely high INSERT rate';
PRINT '   • Index is never used (check usage stats)';
PRINT '';
PRINT '✅ DO INDEX IF:';
PRINT '   • Column is frequently in WHERE, JOIN, ORDER BY';
PRINT '   • Column has high selectivity (filters to few rows)';
PRINT '   • Query performance is critical';
PRINT '   • Table is large (>10,000 rows)';
PRINT '   • Column is used in foreign key joins';
PRINT '';
PRINT '⚖️  BALANCE: Index benefits vs. maintenance overhead';
GO

-- Example: Checking index write overhead
PRINT '';
PRINT 'Example: Index Write Overhead Analysis';
GO

SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    ius.user_seeks + ius.user_scans + ius.user_lookups AS TotalReads,
    ius.user_updates AS TotalWrites,
    CASE
        WHEN ius.user_updates > (ius.user_seeks + ius.user_scans + ius.user_lookups) * 10
        THEN 'HIGH write overhead (consider dropping)'
        WHEN ius.user_seeks + ius.user_scans + ius.user_lookups = 0
        THEN 'UNUSED index (consider dropping)'
        ELSE 'Healthy read/write ratio'
    END AS Assessment
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats ius
    ON i.object_id = ius.object_id
    AND i.index_id = ius.index_id
    AND ius.database_id = DB_ID()
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
    AND i.type > 0
    AND i.is_primary_key = 0  -- Exclude PK (can't drop)
ORDER BY TotalReads DESC;
GO

-- =============================================
-- SECTION 3: INDEX MAINTENANCE
-- =============================================
-- Indexes degrade over time due to:
--   1. Fragmentation: Pages out of logical order (random I/O)
--   2. Page splits: Empty space in pages (wasted storage)
--   3. Outdated statistics: Optimizer makes poor choices
--
-- Maintenance Operations:
--   - REORGANIZE: Defragments leaf pages (online, low overhead)
--   - REBUILD: Recreates index from scratch (offline*, thorough)
--   - UPDATE STATISTICS: Refreshes data distribution info
--
-- Maintenance Thresholds:
--   - Fragmentation 5-30%: REORGANIZE
--   - Fragmentation >30%: REBUILD
--   - Statistics: Update weekly or after large data changes
--
-- *REBUILD can be ONLINE in Enterprise Edition
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 3: INDEX MAINTENANCE';
PRINT '========================================';
GO

-- =============================================
-- 3.1 FRAGMENTATION DETECTION
-- =============================================
-- Fragmentation Types:
--   - Logical Fragmentation: Pages out of order (causes scattered reads)
--   - Extent Fragmentation: Extents (8 pages) not contiguous
--
-- sys.dm_db_index_physical_stats:
--   - avg_fragmentation_in_percent: Logical fragmentation %
--   - avg_page_space_used_in_percent: How full pages are
--   - page_count: Total pages in index
--
-- Scan Modes:
--   - LIMITED: Fast, samples pages
--   - SAMPLED: Samples 1% of pages (good balance)
--   - DETAILED: Scans all pages (most accurate, slowest)
-- =============================================

PRINT '';
PRINT '3.1 Index Fragmentation Analysis:';
GO

SELECT
    OBJECT_SCHEMA_NAME(ips.object_id) AS SchemaName,
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    ips.index_depth AS IndexDepth,
    ips.page_count AS PageCount,
    CAST(ips.avg_fragmentation_in_percent AS DECIMAL(5,2)) AS FragmentationPercent,
    CAST(ips.avg_page_space_used_in_percent AS DECIMAL(5,2)) AS AvgPageFullnessPercent,
    ips.fragment_count AS FragmentCount,
    CASE
        WHEN ips.avg_fragmentation_in_percent < 5 THEN '✓ Good (No action needed)'
        WHEN ips.avg_fragmentation_in_percent < 30 THEN '⚠️ Moderate (REORGANIZE)'
        ELSE '❌ High (REBUILD)'
    END AS Recommendation,
    CASE
        WHEN ips.avg_fragmentation_in_percent < 5 THEN 'No maintenance needed'
        WHEN ips.avg_fragmentation_in_percent < 30 THEN
            'ALTER INDEX ' + i.name + ' ON ' + OBJECT_SCHEMA_NAME(ips.object_id) + '.' +
            OBJECT_NAME(ips.object_id) + ' REORGANIZE;'
        ELSE
            'ALTER INDEX ' + i.name + ' ON ' + OBJECT_SCHEMA_NAME(ips.object_id) + '.' +
            OBJECT_NAME(ips.object_id) + ' REBUILD WITH (ONLINE = OFF);'
    END AS MaintenanceCommand
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'SAMPLED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE OBJECTPROPERTY(ips.object_id, 'IsUserTable') = 1
    AND i.type > 0  -- Exclude heaps
    AND ips.page_count > 100  -- Only indexes with >100 pages (small indexes don't matter)
ORDER BY ips.avg_fragmentation_in_percent DESC;
GO

PRINT '';
PRINT 'Fragmentation Thresholds:';
PRINT '  ✓ 0-5%: Good - No action needed';
PRINT '  ⚠️ 5-30%: Moderate - Run REORGANIZE (online, low impact)';
PRINT '  ❌ >30%: High - Run REBUILD (offline, thorough)';
PRINT '';
PRINT 'ℹ️  Small indexes (<100 pages) excluded - fragmentation doesn''t matter';
GO

-- =============================================
-- 3.2 REORGANIZE vs REBUILD
-- =============================================
-- REORGANIZE:
--   ✓ Online operation (table remains available)
--   ✓ Low resource usage
--   ✓ Compacts leaf pages
--   ✓ Updates statistics automatically
--   ✗ Only partially defragments
--   ✗ Slower than REBUILD for high fragmentation
--   Use for: 5-30% fragmentation
--
-- REBUILD:
--   ✓ Completely rebuilds index (fresh B-tree)
--   ✓ Removes all fragmentation
--   ✓ Reclaims wasted space
--   ✓ Updates statistics automatically
--   ✗ Offline by default (table locked)
--   ✗ High resource usage (CPU, disk, memory)
--   ✗ Requires free space (2x index size)
--   Use for: >30% fragmentation
--
-- REBUILD ONLINE (Enterprise Edition only):
--   ✓ Table remains available during rebuild
--   ✗ Slower than offline rebuild
--   ✗ Not available for XML, spatial indexes
-- =============================================

PRINT '';
PRINT '3.2 REORGANIZE vs REBUILD Comparison:';
PRINT '';
PRINT '📊 REORGANIZE (5-30% fragmentation):';
PRINT '   ✓ Online (table remains available)';
PRINT '   ✓ Low resource usage';
PRINT '   ✓ Can run during business hours';
PRINT '   ✗ Only partially defragments';
PRINT '   Syntax: ALTER INDEX IX_Name ON TableName REORGANIZE;';
PRINT '';
PRINT '🔨 REBUILD (>30% fragmentation):';
PRINT '   ✓ Complete defragmentation';
PRINT '   ✓ Reclaims wasted space';
PRINT '   ✗ Offline (table locked) in Standard Edition';
PRINT '   ✗ High resource usage';
PRINT '   ✗ Requires maintenance window';
PRINT '   Syntax: ALTER INDEX IX_Name ON TableName REBUILD;';
PRINT '';
PRINT '🔧 REBUILD WITH FILLFACTOR:';
PRINT '   ALTER INDEX IX_Name ON TableName REBUILD WITH (FILLFACTOR = 85);';
PRINT '';
PRINT '💎 REBUILD ONLINE (Enterprise Edition):';
PRINT '   ALTER INDEX IX_Name ON TableName REBUILD WITH (ONLINE = ON);';
GO

-- Example maintenance commands (commented for safety)
PRINT '';
PRINT 'Example Maintenance Commands:';
PRINT '';
PRINT '-- Reorganize single index (online, safe during business hours)';
PRINT '-- ALTER INDEX IX_Books_CategoryId ON dbo.Books REORGANIZE;';
PRINT '';
PRINT '-- Rebuild single index (offline, use maintenance window)';
PRINT '-- ALTER INDEX IX_Books_CategoryId ON dbo.Books REBUILD;';
PRINT '';
PRINT '-- Rebuild with FILLFACTOR adjustment';
PRINT '-- ALTER INDEX IX_Loans_Status_DueDate ON dbo.Loans REBUILD WITH (FILLFACTOR = 85);';
PRINT '';
PRINT '-- Rebuild all indexes on a table';
PRINT '-- ALTER INDEX ALL ON dbo.Books REBUILD;';
PRINT '';
PRINT '-- Reorganize all indexes on a table';
PRINT '-- ALTER INDEX ALL ON dbo.Books REORGANIZE;';
GO

-- =============================================
-- 3.3 STATISTICS UPDATES
-- =============================================
-- Statistics: Histograms of data distribution in columns
--   - Used by Query Optimizer to estimate row counts
--   - Critical for good execution plans
--   - Automatically created on indexed columns
--   - Can be manually created on non-indexed columns
--
-- When Statistics Become Outdated:
--   - Large data modifications (20% of rows changed)
--   - Data skew (uneven distribution)
--   - After bulk loads
--
-- Update Methods:
--   1. Automatic (if AUTO_UPDATE_STATISTICS ON)
--   2. UPDATE STATISTICS TableName;
--   3. sp_updatestats (all tables in database)
--   4. ALTER INDEX REBUILD (updates stats automatically)
-- =============================================

PRINT '';
PRINT '3.3 Statistics Management:';
GO

-- View statistics for a table
PRINT 'Current Statistics on Books Table:';
GO

SELECT
    s.name AS StatisticName,
    s.auto_created AS IsAutoCreated,
    s.user_created AS IsUserCreated,
    s.has_filter AS HasFilter,
    s.filter_definition AS FilterDefinition,
    STATS_DATE(s.object_id, s.stats_id) AS LastUpdated,
    DATEDIFF(DAY, STATS_DATE(s.object_id, s.stats_id), GETDATE()) AS DaysSinceUpdate,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.stats_columns sc
        INNER JOIN sys.columns c ON sc.object_id = c.object_id AND sc.column_id = c.column_id
        WHERE sc.object_id = s.object_id AND sc.stats_id = s.stats_id
        ORDER BY sc.stats_column_id
        FOR XML PATH('')
    ), 1, 2, '') AS StatisticColumns
FROM sys.stats s
WHERE s.object_id = OBJECT_ID('dbo.Books')
ORDER BY s.name;
GO

PRINT '';
PRINT 'Statistics Best Practices:';
PRINT '  • Update after large data changes (>20% rows modified)';
PRINT '  • Update weekly for frequently queried tables';
PRINT '  • Update immediately after bulk loads';
PRINT '  • Enable AUTO_UPDATE_STATISTICS (default: ON)';
PRINT '';
PRINT 'Update Commands:';
PRINT '  -- Update statistics for specific table';
PRINT '  UPDATE STATISTICS dbo.Books;';
PRINT '';
PRINT '  -- Update statistics for specific index/statistic';
PRINT '  UPDATE STATISTICS dbo.Books IX_Books_CategoryId WITH FULLSCAN;';
PRINT '';
PRINT '  -- Update all statistics in database';
PRINT '  EXEC sp_updatestats;';
GO

-- =============================================
-- 3.4 UNUSED INDEX DETECTION
-- =============================================
-- Unused indexes waste resources:
--   - Storage space
--   - INSERT/UPDATE/DELETE overhead
--   - Backup time
--   - Maintenance window time
--
-- sys.dm_db_index_usage_stats tracks:
--   - user_seeks: Index seeks by user queries
--   - user_scans: Index scans by user queries
--   - user_lookups: Key lookups by user queries
--   - user_updates: Index updates (INSERT/UPDATE/DELETE)
--
-- Candidate for Removal:
--   - 0 reads AND many writes
--   - Created long ago but never used
--   - Duplicate indexes (same columns, different name)
--
-- CAUTION: Don't drop indexes used by:
--   - Primary keys
--   - Unique constraints
--   - Foreign keys (join performance)
--   - Scheduled reports (may run monthly)
-- =============================================

PRINT '';
PRINT '3.4 Unused Index Detection:';
GO

SELECT
    OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    ISNULL(ius.user_seeks, 0) AS UserSeeks,
    ISNULL(ius.user_scans, 0) AS UserScans,
    ISNULL(ius.user_lookups, 0) AS UserLookups,
    ISNULL(ius.user_seeks + ius.user_scans + ius.user_lookups, 0) AS TotalReads,
    ISNULL(ius.user_updates, 0) AS UserUpdates,
    CASE
        WHEN ius.user_seeks IS NULL THEN 'NEVER USED since last restart'
        WHEN ius.user_seeks + ius.user_scans + ius.user_lookups = 0 THEN 'NO READS (only writes)'
        WHEN ius.user_updates > (ius.user_seeks + ius.user_scans + ius.user_lookups) * 5
            THEN 'HIGH WRITE overhead (writes >> reads)'
        ELSE 'Actively used'
    END AS UsageAssessment,
    CASE
        WHEN i.is_primary_key = 1 THEN 'PRIMARY KEY - Keep'
        WHEN i.is_unique_constraint = 1 THEN 'UNIQUE CONSTRAINT - Keep'
        WHEN ius.user_seeks IS NULL THEN 'Consider dropping (never used)'
        WHEN ius.user_seeks + ius.user_scans + ius.user_lookups = 0 THEN 'Consider dropping (no reads)'
        ELSE 'Keep'
    END AS Recommendation
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats ius
    ON i.object_id = ius.object_id
    AND i.index_id = ius.index_id
    AND ius.database_id = DB_ID()
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
    AND i.type > 0  -- Exclude heaps
ORDER BY TotalReads ASC, UserUpdates DESC;
GO

PRINT '';
PRINT '⚠️  CAUTION: Don''t drop indexes that:';
PRINT '   • Support PRIMARY KEY or UNIQUE constraints';
PRINT '   • Support foreign keys (needed for joins)';
PRINT '   • Used by monthly/quarterly reports';
PRINT '   • Required for application functionality';
PRINT '';
PRINT 'ℹ️  Usage stats reset on SQL Server restart!';
PRINT '   Longer uptime = more reliable statistics';
GO

-- =============================================
-- 3.5 MISSING INDEX SUGGESTIONS
-- =============================================
-- SQL Server tracks queries that would benefit from indexes
-- in the Missing Index DMVs:
--   - sys.dm_db_missing_index_groups
--   - sys.dm_db_missing_index_group_stats
--   - sys.dm_db_missing_index_details
--
-- Metrics:
--   - avg_user_impact: % improvement estimate
--   - avg_total_user_cost: Query cost
--   - user_seeks: How many times query ran
--   - last_user_seek: When query last ran
--
-- CAUTION:
--   - Don't blindly create all missing indexes!
--   - Consider index overhead
--   - Look for patterns (similar indexes)
--   - Combine similar suggestions
-- =============================================

PRINT '';
PRINT '3.5 Missing Index Recommendations:';
GO

SELECT TOP 20
    OBJECT_NAME(mid.object_id, mid.database_id) AS TableName,
    migs.avg_user_impact AS AvgImprovementPercent,
    migs.avg_total_user_cost AS AvgQueryCost,
    migs.user_seeks AS TimesQueried,
    migs.last_user_seek AS LastQueryTime,
    (migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans)) / 100 AS TotalImpactScore,
    'CREATE NONCLUSTERED INDEX IX_' + OBJECT_NAME(mid.object_id, mid.database_id) + '_'
        + REPLACE(REPLACE(REPLACE(ISNULL(mid.equality_columns, ''), ', ', '_'), '[', ''), ']', '')
        + CASE WHEN mid.inequality_columns IS NOT NULL
               THEN '_' + REPLACE(REPLACE(REPLACE(mid.inequality_columns, ', ', '_'), '[', ''), ']', '')
               ELSE '' END
        + ' ON ' + mid.statement
        + ' (' + ISNULL(mid.equality_columns, '')
        + CASE WHEN mid.inequality_columns IS NOT NULL
               THEN CASE WHEN mid.equality_columns IS NOT NULL THEN ', ' ELSE '' END + mid.inequality_columns
               ELSE '' END
        + ')'
        + CASE WHEN mid.included_columns IS NOT NULL
               THEN ' INCLUDE (' + mid.included_columns + ')'
               ELSE '' END
        + ';' AS CreateIndexStatement
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE mid.database_id = DB_ID()
ORDER BY TotalImpactScore DESC;
GO

PRINT '';
PRINT 'Missing Index Interpretation:';
PRINT '  • AvgImprovementPercent: Estimated performance gain (0-100%)';
PRINT '  • TotalImpactScore: Overall impact (higher = more important)';
PRINT '  • TimesQueried: How often this query ran';
PRINT '';
PRINT '⚠️  Don''t create all suggested indexes!';
PRINT '   - Review for duplicates';
PRINT '   - Consider combining similar indexes';
PRINT '   - Balance read improvement vs write overhead';
PRINT '   - Test in development first';
GO

-- =============================================
-- SECTION 4: PERFORMANCE TROUBLESHOOTING
-- =============================================
-- When you have a performance problem, follow this workflow:
--   1. Identify slow queries (sys.dm_exec_query_stats)
--   2. Analyze execution plans (SET SHOWPLAN)
--   3. Check for missing indexes
--   4. Look for table scans, key lookups
--   5. Check statistics freshness
--   6. Analyze wait statistics
--   7. Test fixes in development
--   8. Measure before/after performance
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'SECTION 4: PERFORMANCE TROUBLESHOOTING';
PRINT '========================================';
GO

-- =============================================
-- 4.1 FINDING SLOW QUERIES
-- =============================================
-- sys.dm_exec_query_stats contains performance metrics for cached queries:
--   - execution_count: How many times executed
--   - total_elapsed_time: Total time spent
--   - total_logical_reads: Total pages read
--   - total_worker_time: Total CPU time
--
-- Key Metrics to Monitor:
--   - Avg Elapsed Time: Wall-clock time per execution
--   - Avg CPU Time: CPU milliseconds per execution
--   - Avg Logical Reads: Pages read per execution
--
-- Focus On:
--   - High average times (slow per execution)
--   - High execution counts (frequent queries)
--   - Highest total time (cumulative impact)
-- =============================================

PRINT '';
PRINT '4.1 Top 20 Slowest Queries by Average Elapsed Time:';
GO

SELECT TOP 20
    DB_NAME(qt.dbid) AS DatabaseName,
    OBJECT_NAME(qt.objectid, qt.dbid) AS ObjectName,
    qs.execution_count AS ExecutionCount,
    CAST(qs.total_elapsed_time / 1000000.0 AS DECIMAL(18, 2)) AS TotalElapsedSeconds,
    CAST(qs.total_elapsed_time / qs.execution_count / 1000.0 AS DECIMAL(18, 2)) AS AvgElapsedMS,
    CAST(qs.total_worker_time / qs.execution_count / 1000.0 AS DECIMAL(18, 2)) AS AvgCPU_MS,
    qs.total_logical_reads / qs.execution_count AS AvgLogicalReads,
    qs.total_physical_reads / qs.execution_count AS AvgPhysicalReads,
    CAST(qs.last_execution_time AS DATETIME) AS LastExecutionTime,
    SUBSTRING(qt.text, (qs.statement_start_offset / 2) + 1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset) / 2) + 1) AS QueryText
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.dbid = DB_ID()
ORDER BY qs.total_elapsed_time / qs.execution_count DESC;
GO

PRINT '';
PRINT 'Focus on queries with:';
PRINT '  • High AvgElapsedMS (slow per execution)';
PRINT '  • High AvgLogicalReads (reading many pages)';
PRINT '  • High ExecutionCount (frequent bottleneck)';
PRINT '  • Physical reads > 0 (disk reads, not cached)';
GO

-- =============================================
-- 4.2 TOP QUERIES BY TOTAL IMPACT
-- =============================================
-- Sometimes a moderately slow query executed millions of times
-- has more total impact than a very slow query executed once.
-- =============================================

PRINT '';
PRINT '4.2 Top 20 Queries by Total Elapsed Time (Cumulative Impact):';
GO

SELECT TOP 20
    DB_NAME(qt.dbid) AS DatabaseName,
    OBJECT_NAME(qt.objectid, qt.dbid) AS ObjectName,
    qs.execution_count AS ExecutionCount,
    CAST(qs.total_elapsed_time / 1000000.0 AS DECIMAL(18, 2)) AS TotalElapsedSeconds,
    CAST(qs.total_elapsed_time / qs.execution_count / 1000.0 AS DECIMAL(18, 2)) AS AvgElapsedMS,
    qs.total_logical_reads / qs.execution_count AS AvgLogicalReads,
    SUBSTRING(qt.text, (qs.statement_start_offset / 2) + 1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset) / 2) + 1) AS QueryText
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.dbid = DB_ID()
ORDER BY qs.total_elapsed_time DESC;
GO

-- =============================================
-- 4.3 QUERIES WITH HIGH LOGICAL READS
-- =============================================
-- Logical reads indicate I/O pressure
-- High logical reads = reading many pages
-- Often indicates missing indexes or table scans
-- =============================================

PRINT '';
PRINT '4.3 Top 20 Queries by Logical Reads (I/O Intensive):';
GO

SELECT TOP 20
    DB_NAME(qt.dbid) AS DatabaseName,
    qs.execution_count AS ExecutionCount,
    qs.total_logical_reads AS TotalLogicalReads,
    qs.total_logical_reads / qs.execution_count AS AvgLogicalReads,
    CAST(qs.total_elapsed_time / qs.execution_count / 1000.0 AS DECIMAL(18, 2)) AS AvgElapsedMS,
    CAST(qs.last_execution_time AS DATETIME) AS LastExecutionTime,
    SUBSTRING(qt.text, (qs.statement_start_offset / 2) + 1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset) / 2) + 1) AS QueryText
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.dbid = DB_ID()
    AND qs.execution_count > 1
ORDER BY qs.total_logical_reads / qs.execution_count DESC;
GO

-- =============================================
-- 4.4 PERFORMANCE TROUBLESHOOTING WORKFLOW
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'PERFORMANCE TROUBLESHOOTING WORKFLOW';
PRINT '========================================';
PRINT '';
PRINT '1️⃣ IDENTIFY THE PROBLEM:';
PRINT '   • Find slow queries (Section 4.1-4.3 above)';
PRINT '   • User reports (specific screens/features)';
PRINT '   • Monitoring alerts (high CPU, disk I/O)';
PRINT '';
PRINT '2️⃣ CAPTURE THE QUERY:';
PRINT '   • Copy query text from sys.dm_exec_query_stats';
PRINT '   • Use SQL Profiler or Extended Events';
PRINT '   • Check application logs';
PRINT '';
PRINT '3️⃣ ANALYZE EXECUTION PLAN:';
PRINT '   • Enable "Include Actual Execution Plan" (Ctrl+M)';
PRINT '   • Run the query';
PRINT '   • Look for red flags:';
PRINT '     - Table Scans';
PRINT '     - Key Lookups';
PRINT '     - Missing Index hints';
PRINT '     - Sort operators';
PRINT '     - High-cost operators';
PRINT '';
PRINT '4️⃣ CHECK STATISTICS:';
PRINT '   • SELECT STATS_DATE(object_id, stats_id)';
PRINT '   • UPDATE STATISTICS if outdated';
PRINT '';
PRINT '5️⃣ REVIEW INDEXES:';
PRINT '   • Check if WHERE columns are indexed';
PRINT '   • Look for missing indexes (Section 3.5)';
PRINT '   • Check for unused/duplicate indexes';
PRINT '   • Verify FILLFACTOR settings';
PRINT '';
PRINT '6️⃣ MEASURE BASELINE:';
PRINT '   • SET STATISTICS IO ON';
PRINT '   • SET STATISTICS TIME ON';
PRINT '   • Record logical reads and elapsed time';
PRINT '';
PRINT '7️⃣ IMPLEMENT FIX:';
PRINT '   • Create missing indexes';
PRINT '   • Add covering indexes (INCLUDE clause)';
PRINT '   • Update statistics';
PRINT '   • Rewrite query (better joins, WHERE clause)';
PRINT '   • Add filtered indexes';
PRINT '';
PRINT '8️⃣ TEST & COMPARE:';
PRINT '   • Run query with SET STATISTICS again';
PRINT '   • Compare logical reads (should decrease)';
PRINT '   • Compare elapsed time (should decrease)';
PRINT '   • Review new execution plan';
PRINT '';
PRINT '9️⃣ DEPLOY TO PRODUCTION:';
PRINT '   • Create index during maintenance window';
PRINT '   • Monitor performance after deployment';
PRINT '   • Keep index creation script for rollback';
PRINT '';
PRINT '🔟 DOCUMENT:';
PRINT '   • Before/after metrics';
PRINT '   • Index definitions created';
PRINT '   • Lessons learned';
GO

-- =============================================
-- 4.5 BEFORE/AFTER COMPARISON EXAMPLE
-- =============================================
PRINT '';
PRINT '4.5 Before/After Performance Testing:';
GO

-- Simulate a query that could benefit from optimization
PRINT 'Example: Query to find all loans for a specific member';
PRINT 'Let''s test with/without statistics to see the difference';
PRINT '';

-- Baseline (before optimization)
PRINT 'BASELINE (Before Optimization):';
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT
    l.LoanId,
    l.BorrowedAt,
    l.DueDate,
    l.Status,
    b.Title AS BookTitle,
    b.ISBN
FROM dbo.Loans l
INNER JOIN dbo.Books b ON l.BookId = b.BookId
WHERE l.MemberId = 1
ORDER BY l.BorrowedAt DESC;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO

PRINT '';
PRINT 'Review the statistics above:';
PRINT '  • How many logical reads?';
PRINT '  • CPU time?';
PRINT '  • Elapsed time?';
PRINT '';
PRINT 'Check execution plan:';
PRINT '  • Index Seek or Scan on Loans?';
PRINT '  • Key Lookups?';
PRINT '  • Join type?';
PRINT '';
PRINT 'Good news: IX_Loans_MemberId index already exists and should optimize this!';
GO

-- =============================================
-- LAB COMPLETION
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'LAB 6 COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT 'You have learned about:';
PRINT '  ✅ Query Execution Plans (reading, analyzing, interpreting)';
PRINT '  ✅ SET STATISTICS IO/TIME for performance measurement';
PRINT '  ✅ Index Seek vs Table Scan vs Index Scan';
PRINT '  ✅ Index types (clustered, nonclustered, unique, filtered, covering)';
PRINT '  ✅ Index design principles (selectivity, cardinality, column order)';
PRINT '  ✅ Covering indexes (INCLUDE clause)';
PRINT '  ✅ Filtered indexes for specific scenarios';
PRINT '  ✅ FILLFACTOR tuning to prevent page splits';
PRINT '  ✅ When NOT to index (trade-offs)';
PRINT '  ✅ Index fragmentation detection and thresholds';
PRINT '  ✅ REORGANIZE vs REBUILD maintenance';
PRINT '  ✅ Statistics updates and freshness';
PRINT '  ✅ Unused index detection';
PRINT '  ✅ Missing index recommendations';
PRINT '  ✅ Finding slow queries with DMVs';
PRINT '  ✅ Performance troubleshooting workflow';
PRINT '  ✅ Before/after performance testing';
PRINT '';
PRINT 'Key Takeaways:';
PRINT '  💡 Execution plans are your best friend for optimization';
PRINT '  💡 Indexes dramatically improve reads but slow down writes';
PRINT '  💡 Column order matters in composite indexes';
PRINT '  💡 Covering indexes eliminate expensive key lookups';
PRINT '  💡 Regular maintenance prevents performance degradation';
PRINT '  💡 Don''t create indexes blindly - measure impact!';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Analyze slow queries in your own databases';
PRINT '  2. Review execution plans for table scans and key lookups';
PRINT '  3. Implement missing indexes (test first!)';
PRINT '  4. Set up weekly index maintenance jobs';
PRINT '  5. Monitor index usage and fragmentation monthly';
PRINT '  6. Document your optimization wins (before/after)';
PRINT '';
PRINT 'Resources:';
PRINT '  • SQL Server Execution Plans by Grant Fritchey';
PRINT '  • sys.dm_db_index_* DMVs documentation';
PRINT '  • Brent Ozar''s sp_BlitzIndex (free tool)';
PRINT '  • SQL Server Performance Tuning course';
PRINT '';
PRINT 'Remember: Measure twice, optimize once!';
PRINT '========================================';
GO
