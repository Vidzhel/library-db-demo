-- =============================================
-- Migration V002: Add Performance Indexes
-- =============================================
-- Creates indexes to optimize common query patterns
--
-- Index types:
--   1. Unique indexes - Enforce uniqueness and speed lookups
--   2. Foreign key indexes - Optimize JOIN operations
--   3. Search indexes - Common WHERE clause columns
--   4. Covering indexes - Include frequently accessed columns
--
-- Note: Clustered indexes already exist via PRIMARY KEY constraints
-- =============================================

USE LibraryDb;
GO

-- Required for filtered indexes
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================
-- UNIQUE INDEXES (Business Keys & Constraints)
-- =============================================

-- Books: ISBN must be unique (business rule)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Books]') AND name = N'UQ_Books_ISBN')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Books_ISBN]
        ON [dbo].[Books] ([ISBN] ASC)
        WITH (FILLFACTOR = 90);  -- Leave 10% space for INSERTs

    PRINT '✅ Unique index [UQ_Books_ISBN] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Unique index [UQ_Books_ISBN] already exists.';
END
GO

-- Members: MembershipNumber must be unique (business rule)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Members]') AND name = N'UQ_Members_MembershipNumber')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Members_MembershipNumber]
        ON [dbo].[Members] ([MembershipNumber] ASC)
        WITH (FILLFACTOR = 90);

    PRINT '✅ Unique index [UQ_Members_MembershipNumber] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Unique index [UQ_Members_MembershipNumber] already exists.';
END
GO

-- Members: Email should be unique (prevents duplicate accounts)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Members]') AND name = N'UQ_Members_Email')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Members_Email]
        ON [dbo].[Members] ([Email] ASC)
        WITH (FILLFACTOR = 90);

    PRINT '✅ Unique index [UQ_Members_Email] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Unique index [UQ_Members_Email] already exists.';
END
GO

-- =============================================
-- FOREIGN KEY INDEXES (JOIN Performance)
-- =============================================
-- These significantly speed up JOIN operations and CASCADE operations

-- Books.CategoryId (for Books JOIN Categories)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Books]') AND name = N'IX_Books_CategoryId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Books_CategoryId]
        ON [dbo].[Books] ([CategoryId] ASC)
        INCLUDE ([Title], [ISBN], [AvailableCopies])  -- Covering for common queries
        WITH (FILLFACTOR = 80);

    PRINT '✅ Index [IX_Books_CategoryId] created (with covering).';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Books_CategoryId] already exists.';
END
GO

-- Categories.ParentCategoryId (for hierarchical queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = N'IX_Categories_ParentCategoryId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Categories_ParentCategoryId]
        ON [dbo].[Categories] ([ParentCategoryId] ASC)
        WHERE [ParentCategoryId] IS NOT NULL  -- Filtered index (only sub-categories)
        WITH (FILLFACTOR = 80);

    PRINT '✅ Filtered index [IX_Categories_ParentCategoryId] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Categories_ParentCategoryId] already exists.';
END
GO

-- Loans.MemberId (for Member → Loans queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Loans]') AND name = N'IX_Loans_MemberId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Loans_MemberId]
        ON [dbo].[Loans] ([MemberId] ASC)
        INCLUDE ([BookId], [BorrowedAt], [DueDate], [Status])  -- Covering
        WITH (FILLFACTOR = 70);  -- More space for frequent INSERTs

    PRINT '✅ Index [IX_Loans_MemberId] created (with covering).';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Loans_MemberId] already exists.';
END
GO

-- Loans.BookId (for Book → Loans queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Loans]') AND name = N'IX_Loans_BookId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Loans_BookId]
        ON [dbo].[Loans] ([BookId] ASC)
        INCLUDE ([MemberId], [BorrowedAt], [ReturnedAt], [Status])
        WITH (FILLFACTOR = 70);

    PRINT '✅ Index [IX_Loans_BookId] created (with covering).';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Loans_BookId] already exists.';
END
GO

-- BookAuthors.AuthorId (for Author → Books queries)
-- Note: BookId already covered by clustered PK
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[BookAuthors]') AND name = N'IX_BookAuthors_AuthorId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_BookAuthors_AuthorId]
        ON [dbo].[BookAuthors] ([AuthorId] ASC)
        INCLUDE ([BookId], [AuthorOrder], [Role])
        WITH (FILLFACTOR = 80);

    PRINT '✅ Index [IX_BookAuthors_AuthorId] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_BookAuthors_AuthorId] already exists.';
END
GO

-- =============================================
-- SEARCH & FILTER INDEXES
-- =============================================
-- Optimize common WHERE clause patterns

-- Books: Search by Title (for book lookup)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Books]') AND name = N'IX_Books_Title')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Books_Title]
        ON [dbo].[Books] ([Title] ASC)
        WHERE [IsDeleted] = 0  -- Filtered index (exclude deleted books)
        WITH (FILLFACTOR = 80);

    PRINT '✅ Filtered index [IX_Books_Title] created (active books only).';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Books_Title] already exists.';
END
GO

-- Books: Filter by IsDeleted (for active books queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Books]') AND name = N'IX_Books_IsDeleted_AvailableCopies')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Books_IsDeleted_AvailableCopies]
        ON [dbo].[Books] ([IsDeleted] ASC, [AvailableCopies] ASC)
        INCLUDE ([Id], [Title], [ISBN])
        WHERE [IsDeleted] = 0 AND [AvailableCopies] > 0  -- Only available books
        WITH (FILLFACTOR = 80);

    PRINT '✅ Filtered index [IX_Books_IsDeleted_AvailableCopies] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Books_IsDeleted_AvailableCopies] already exists.';
END
GO

-- Authors: Search by LastName, FirstName (for author lookup)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Authors]') AND name = N'IX_Authors_Name')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Authors_Name]
        ON [dbo].[Authors] ([LastName] ASC, [FirstName] ASC)
        WITH (FILLFACTOR = 90);

    PRINT '✅ Index [IX_Authors_Name] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Authors_Name] already exists.';
END
GO

-- Members: Filter by IsActive (for active members)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Members]') AND name = N'IX_Members_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Members_IsActive]
        ON [dbo].[Members] ([IsActive] ASC, [MembershipExpiresAt] ASC)
        INCLUDE ([MembershipNumber], [FirstName], [LastName])
        WHERE [IsActive] = 1  -- Filtered index for active members only
        WITH (FILLFACTOR = 80);

    PRINT '✅ Filtered index [IX_Members_IsActive] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Members_IsActive] already exists.';
END
GO

-- =============================================
-- LOAN STATUS & OVERDUE TRACKING INDEXES
-- =============================================
-- Critical for library operations

-- Loans: Active loans (most frequent query)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Loans]') AND name = N'IX_Loans_Status_DueDate')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Loans_Status_DueDate]
        ON [dbo].[Loans] ([Status] ASC, [DueDate] ASC)
        INCLUDE ([Id], [MemberId], [BookId], [BorrowedAt], [ReturnedAt])
        WHERE [ReturnedAt] IS NULL  -- Only active loans (not returned)
        WITH (FILLFACTOR = 70);

    PRINT '✅ Filtered index [IX_Loans_Status_DueDate] created (active loans).';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Loans_Status_DueDate] already exists.';
END
GO

-- Loans: Overdue loans (for daily overdue report)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Loans]') AND name = N'IX_Loans_Overdue')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Loans_Overdue]
        ON [dbo].[Loans] ([DueDate] ASC)
        INCLUDE ([Id], [MemberId], [BookId], [BorrowedAt])
        WHERE [ReturnedAt] IS NULL AND [Status] IN (0, 2)  -- Active or Overdue status
        WITH (FILLFACTOR = 70);

    PRINT '✅ Filtered index [IX_Loans_Overdue] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Loans_Overdue] already exists.';
END
GO

-- Loans: Unpaid late fees
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Loans]') AND name = N'IX_Loans_UnpaidFees')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Loans_UnpaidFees]
        ON [dbo].[Loans] ([IsFeePaid] ASC, [LateFee] ASC)
        INCLUDE ([MemberId], [ReturnedAt])
        WHERE [IsFeePaid] = 0 AND [LateFee] IS NOT NULL AND [LateFee] > 0
        WITH (FILLFACTOR = 80);

    PRINT '✅ Filtered index [IX_Loans_UnpaidFees] created.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index [IX_Loans_UnpaidFees] already exists.';
END
GO

-- =============================================
-- Summary
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ Index Migration Completed!';
PRINT '========================================';
PRINT '';
PRINT '📊 Index Categories Created:';
PRINT '   ✅ Unique Indexes: 3 (ISBN, MembershipNumber, Email)';
PRINT '   ✅ Foreign Key Indexes: 5 (optimized JOINs)';
PRINT '   ✅ Search Indexes: 3 (Title, Name, Active)';
PRINT '   ✅ Loan Tracking Indexes: 4 (Status, Overdue, Fees)';
PRINT '   ✅ Filtered Indexes: 7 (for specific scenarios)';
PRINT '';
PRINT '🚀 Performance Optimizations:';
PRINT '   ✅ Fast book searches by ISBN or Title';
PRINT '   ✅ Fast member lookups by MembershipNumber or Email';
PRINT '   ✅ Optimized JOIN operations (all FKs indexed)';
PRINT '   ✅ Quick overdue loan detection';
PRINT '   ✅ Efficient category hierarchy traversal';
PRINT '';
PRINT '📝 Index Design Patterns Used:';
PRINT '   ✅ Filtered indexes (WHERE clause in index)';
PRINT '   ✅ Covering indexes (INCLUDE clause)';
PRINT '   ✅ Appropriate FILLFACTOR (70-90% based on INSERT frequency)';
PRINT '';
PRINT '⏭️  Next: Run V003__seed_data.sql to populate sample data';
PRINT '========================================';
GO
