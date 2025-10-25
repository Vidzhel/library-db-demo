-- =============================================
-- Migration V003: Seed Sample Data
-- =============================================
-- Populates tables with realistic library data for testing
--
-- Data included:
--   - 12 Categories (hierarchical structure)
--   - 10 Authors (diverse backgrounds)
--   - 25 Books (classic and modern titles with real ISBNs)
--   - BookAuthor relationships
--   - 8 Members (various statuses)
--   - 12 Loans (active, returned, overdue)
--
-- All operations are idempotent (safe to re-run)
-- Uses MERGE with HOLDLOCK to prevent duplicates
-- =============================================

USE LibraryDb;
GO

SET NOCOUNT ON;  -- Suppress row count messages for cleaner output
SET QUOTED_IDENTIFIER ON;  -- Required for tables with filtered indexes
GO

-- =============================================
-- Seed Categories (with hierarchy)
-- =============================================
PRINT 'Seeding Categories...';

SET IDENTITY_INSERT [dbo].[Categories] ON;

-- Top-level categories
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 1)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (1, N'Fiction', N'Imaginative and narrative works', NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 2)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (2, N'Non-Fiction', N'Factual and informative works', NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 3)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (3, N'Science', N'Scientific and technical subjects', NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 4)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (4, N'History', N'Historical accounts and analysis', NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 5)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (5, N'Technology', N'Computer science and technology', NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

-- Sub-categories
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 6)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (6, N'Science Fiction', N'Speculative and futuristic fiction', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 7)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (7, N'Mystery', N'Detective and crime fiction', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 8)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (8, N'Fantasy', N'Magical and fantastical fiction', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 9)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (9, N'Biography', N'Life stories of notable individuals', 2, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 10)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (10, N'Self-Help', N'Personal development and improvement', 2, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 11)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (11, N'Programming', N'Software development and coding', 5, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Id] = 12)
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [CreatedAt], [UpdatedAt])
    VALUES (12, N'World War II', N'WWII history and accounts', 4, SYSUTCDATETIME(), SYSUTCDATETIME());

SET IDENTITY_INSERT [dbo].[Categories] OFF;

PRINT '✅ Categories seeded: 12 categories (5 top-level, 7 sub-categories)';
GO

-- =============================================
-- Seed Authors
-- =============================================
PRINT 'Seeding Authors...';

SET IDENTITY_INSERT [dbo].[Authors] ON;

MERGE INTO [dbo].[Authors] AS target
USING (VALUES
    (1, N'Robert', N'Martin', N'Robert C. Martin, known as "Uncle Bob", is a software engineer and author.',
     '1952-12-05', N'American', N'unclebob@cleancoder.com'),
    (2, N'Martin', N'Fowler', N'British software developer, author and international speaker on software development.',
     '1963-12-18', N'British', N'fowler@martinfowler.com'),
    (3, N'Eric', N'Evans', N'Software designer and writer, known for Domain-Driven Design.',
     '1960-01-01', N'American', NULL),
    (4, N'Donald', N'Knuth', N'Computer scientist and mathematician, author of The Art of Computer Programming.',
     '1938-01-10', N'American', NULL),
    (5, N'Andrew', N'Hunt', N'Co-author of The Pragmatic Programmer and founding member of the Agile Alliance.',
     '1964-06-15', N'American', NULL),
    (6, N'David', N'Thomas', N'Software developer and author, co-authored The Pragmatic Programmer.',
     '1956-03-20', N'British', NULL),
    (7, N'Frederick', N'Brooks', N'Software engineer and computer architect, known for The Mythical Man-Month.',
     '1931-04-19', N'American', NULL),
    (8, N'Kent', N'Beck', N'Software engineer and creator of Extreme Programming and Test-Driven Development.',
     '1961-03-31', N'American', NULL),
    (9, N'Steve', N'McConnell', N'Author of Code Complete, software engineer and consultant.',
     '1962-07-01', N'American', NULL),
    (10, N'Joshua', N'Bloch', N'Software engineer, formerly at Google and Sun Microsystems, author of Effective Java.',
     '1961-08-28', N'American', NULL)
) AS source ([Id], [FirstName], [LastName], [Biography], [DateOfBirth], [Nationality], [Email])
ON target.[Id] = source.[Id]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [FirstName], [LastName], [Biography], [DateOfBirth], [Nationality], [Email], [CreatedAt], [UpdatedAt])
    VALUES (source.[Id], source.[FirstName], source.[LastName], source.[Biography],
            source.[DateOfBirth], source.[Nationality], source.[Email],
            SYSUTCDATETIME(), SYSUTCDATETIME());

SET IDENTITY_INSERT [dbo].[Authors] OFF;

PRINT '✅ Authors seeded: 10 authors';
GO

-- =============================================
-- Seed Books
-- =============================================
PRINT 'Seeding Books...';

SET IDENTITY_INSERT [dbo].[Books] ON;

MERGE INTO [dbo].[Books] AS target
USING (VALUES
    -- Programming & Technology Books
    (1, N'978-0-13-468599-1', N'Clean Code', N'A Handbook of Agile Software Craftsmanship',
     N'Even bad code can function. But if code isn''t clean, it can bring a development organization to its knees.',
     N'Prentice Hall', '2008-08-01', 464, N'English', 11, 5, 5),
    (2, N'978-0-13-235088-4', N'Clean Architecture', N'A Craftsman''s Guide to Software Structure and Design',
     N'Building upon the success of best-sellers Clean Code and The Clean Coder...',
     N'Prentice Hall', '2017-09-10', 432, N'English', 11, 3, 3),
    (3, N'978-0-13-475759-9', N'Refactoring', N'Improving the Design of Existing Code',
     N'Refactoring is about improving the design of existing code. It is the process of changing a software system...',
     N'Addison-Wesley', '2018-11-20', 448, N'English', 11, 4, 3),
    (4, N'978-0-32-112521-7', N'Domain-Driven Design', N'Tackling Complexity in the Heart of Software',
     N'The software development community widely acknowledges that domain modeling is central to software design.',
     N'Addison-Wesley', '2003-08-20', 560, N'English', 11, 2, 2),
    (5, N'978-0-20-161622-4', N'The Pragmatic Programmer', N'Your Journey to Mastery',
     N'Written as a series of self-contained sections and filled with entertaining anecdotes...',
     N'Addison-Wesley', '2019-09-13', 352, N'English', 11, 6, 5),
    (6, N'978-0-73-561993-0', N'Code Complete', N'A Practical Handbook of Software Construction',
     N'Widely considered one of the best practical guides to programming.',
     N'Microsoft Press', '2004-06-09', 960, N'English', 11, 4, 4),
    (7, N'978-0-20-183595-3', N'The Mythical Man-Month', N'Essays on Software Engineering',
     N'Few books on software project management have been as influential and timeless.',
     N'Addison-Wesley', '1995-08-02', 336, N'English', 5, 3, 3),
    (8, N'978-0-13-476904-2', N'Test-Driven Development', N'By Example',
     N'Quite simply, test-driven development is meant to eliminate fear in application development.',
     N'Addison-Wesley', '2002-11-08', 240, N'English', 11, 3, 2),
    (9, N'978-0-13-468599-0', N'Effective Java', N'Programming Language Guide',
     N'The Definitive Guide to Java Platform Best Practices.',
     N'Addison-Wesley', '2017-12-27', 416, N'English', 11, 5, 4),
    (10, N'978-0-20-163361-0', N'The Art of Computer Programming, Vol. 1', N'Fundamental Algorithms',
     N'The bible of all fundamental algorithms and the work that taught many of today''s software developers...',
     N'Addison-Wesley', '1997-07-04', 672, N'English', 3, 2, 2),

    -- Fiction Books
    (11, N'978-0-54-792822-7', N'1984', NULL,
     N'A dystopian social science fiction novel and cautionary tale.',
     N'Signet Classic', '1949-06-08', 328, N'English', 6, 4, 3),
    (12, N'978-0-06-112008-4', N'To Kill a Mockingbird', NULL,
     N'The unforgettable novel of a childhood in a sleepy Southern town.',
     N'Harper Perennial', '1960-07-11', 324, N'English', 1, 5, 5),
    (13, N'978-0-54-479338-6', N'The Hobbit', NULL,
     N'A great modern classic and the prelude to The Lord of the Rings.',
     N'Houghton Mifflin', '1937-09-21', 310, N'English', 8, 6, 5),
    (14, N'978-0-39-615773-6', N'The Great Gatsby', NULL,
     N'The story of the mysteriously wealthy Jay Gatsby and his love for Daisy Buchanan.',
     N'Scribner', '1925-04-10', 180, N'English', 1, 4, 4),
    (15, N'978-0-14-303943-3', N'Brave New World', NULL,
     N'A dystopian novel set in a futuristic World State.',
     N'Harper Perennial', '1932-01-01', 268, N'English', 6, 3, 2),

    -- Non-Fiction
    (16, N'978-0-67-102303-4', N'Sapiens', N'A Brief History of Humankind',
     N'From a renowned historian comes a groundbreaking narrative of humanity''s creation.',
     N'Harper', '2015-02-10', 443, N'English', 4, 4, 3),
    (17, N'978-0-74-324316-3', N'Educated', N'A Memoir',
     N'A remarkable memoir of a young woman who escapes her survivalist family.',
     N'Random House', '2018-02-20', 334, N'English', 9, 3, 2),
    (18, N'978-1-50-117778-1', N'Atomic Habits', N'An Easy & Proven Way to Build Good Habits',
     N'Transform your life with tiny changes in behaviour.',
     N'Avery', '2018-10-16', 320, N'English', 10, 5, 4),
    (19, N'978-0-30-746363-6', N'Thinking, Fast and Slow', NULL,
     N'A lifetime of running the most important experiment in the history of psychology.',
     N'Farrar, Straus and Giroux', '2011-10-25', 499, N'English', 2, 2, 1),
    (20, N'978-0-38-550945-0', N'A Brief History of Time', NULL,
     N'From the Big Bang to Black Holes.',
     N'Bantam', '1988-04-01', 256, N'English', 3, 3, 3),

    -- Mystery
    (21, N'978-0-06-293082-7', N'The Da Vinci Code', NULL,
     N'An ingenious code hidden in the works of Leonardo da Vinci.',
     N'Doubleday', '2003-03-18', 689, N'English', 7, 4, 4),
    (22, N'978-0-31-615942-9', N'Gone Girl', NULL,
     N'On a warm summer morning in North Carthage, Missouri, it is Nick and Amy Dunne''s fifth wedding anniversary.',
     N'Crown', '2012-06-05', 415, N'English', 7, 3, 3),
    (23, N'978-0-06-207348-5', N'The Girl on the Train', NULL,
     N'Rachel takes the same commuter train every morning and night.',
     N'Riverhead Books', '2015-01-13', 336, N'English', 7, 4, 3),

    -- World War II History
    (24, N'978-0-68-481470-7', N'The Diary of a Young Girl', NULL,
     N'Discovered in the attic in which she spent the last years of her life.',
     N'Bantam', '1947-06-25', 283, N'English', 12, 3, 3),
    (25, N'978-0-80-419630-3', N'Band of Brothers', N'E Company, 506th Regiment, 101st Airborne',
     N'The true story of Easy Company and their incredible World War II journey.',
     N'Simon & Schuster', '1992-01-01', 333, N'English', 12, 2, 2)
) AS source ([Id], [ISBN], [Title], [Subtitle], [Description], [Publisher], [PublishedDate],
              [PageCount], [Language], [CategoryId], [TotalCopies], [AvailableCopies])
ON target.[Id] = source.[Id]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [ISBN], [Title], [Subtitle], [Description], [Publisher], [PublishedDate],
            [PageCount], [Language], [CategoryId], [TotalCopies], [AvailableCopies],
            [IsDeleted], [CreatedAt], [UpdatedAt])
    VALUES (source.[Id], source.[ISBN], source.[Title], source.[Subtitle], source.[Description],
            source.[Publisher], source.[PublishedDate], source.[PageCount], source.[Language],
            source.[CategoryId], source.[TotalCopies], source.[AvailableCopies],
            0, SYSUTCDATETIME(), SYSUTCDATETIME());

SET IDENTITY_INSERT [dbo].[Books] OFF;

PRINT '✅ Books seeded: 25 books across multiple categories';
GO

-- =============================================
-- Seed BookAuthors (Many-to-Many relationships)
-- =============================================
PRINT 'Seeding BookAuthors...';

MERGE INTO [dbo].[BookAuthors] AS target
USING (VALUES
    -- Programming books
    (1, 1, 0, N'Author'),           -- Clean Code - Robert Martin
    (2, 1, 0, N'Author'),           -- Clean Architecture - Robert Martin
    (3, 2, 0, N'Author'),           -- Refactoring - Martin Fowler
    (4, 3, 0, N'Author'),           -- Domain-Driven Design - Eric Evans
    (5, 5, 0, N'Author'),           -- Pragmatic Programmer - Andrew Hunt
    (5, 6, 1, N'Co-Author'),        -- Pragmatic Programmer - David Thomas
    (6, 9, 0, N'Author'),           -- Code Complete - Steve McConnell
    (7, 7, 0, N'Author'),           -- Mythical Man-Month - Frederick Brooks
    (8, 8, 0, N'Author'),           -- Test-Driven Development - Kent Beck
    (9, 10, 0, N'Author'),          -- Effective Java - Joshua Bloch
    (10, 4, 0, N'Author')           -- Art of Computer Programming - Donald Knuth
) AS source ([BookId], [AuthorId], [AuthorOrder], [Role])
ON target.[BookId] = source.[BookId] AND target.[AuthorId] = source.[AuthorId]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([BookId], [AuthorId], [AuthorOrder], [Role], [CreatedAt])
    VALUES (source.[BookId], source.[AuthorId], source.[AuthorOrder], source.[Role], SYSUTCDATETIME());

PRINT '✅ BookAuthors seeded: 11 book-author relationships';
GO

-- =============================================
-- Seed Members
-- =============================================
PRINT 'Seeding Members...';

SET IDENTITY_INSERT [dbo].[Members] ON;

MERGE INTO [dbo].[Members] AS target
USING (VALUES
    (1, N'LIB-2024-001', N'Alice', N'Johnson', N'alice.johnson@email.com', N'+1-555-0101',
     '1990-05-15', N'123 Main St, Springfield, IL 62701',
     DATEADD(MONTH, -6, SYSUTCDATETIME()), DATEADD(MONTH, 6, SYSUTCDATETIME()), 1, 5, 0.00),
    (2, N'LIB-2024-002', N'Bob', N'Smith', N'bob.smith@email.com', N'+1-555-0102',
     '1985-08-22', N'456 Oak Ave, Springfield, IL 62702',
     DATEADD(YEAR, -2, SYSUTCDATETIME()), DATEADD(YEAR, 1, SYSUTCDATETIME()), 1, 5, 0.00),
    (3, N'LIB-2024-003', N'Carol', N'Williams', N'carol.williams@email.com', N'+1-555-0103',
     '1995-11-30', N'789 Pine Rd, Springfield, IL 62703',
     DATEADD(MONTH, -3, SYSUTCDATETIME()), DATEADD(MONTH, 9, SYSUTCDATETIME()), 1, 5, 2.50),
    (4, N'LIB-2024-004', N'David', N'Brown', N'david.brown@email.com', N'+1-555-0104',
     '1988-03-10', N'321 Elm St, Springfield, IL 62704',
     DATEADD(YEAR, -1, SYSUTCDATETIME()), DATEADD(MONTH, -1, SYSUTCDATETIME()), 0, 5, 15.00),
    (5, N'LIB-2024-005', N'Eve', N'Davis', N'eve.davis@email.com', N'+1-555-0105',
     '1992-07-18', NULL,
     DATEADD(MONTH, -9, SYSUTCDATETIME()), DATEADD(MONTH, 3, SYSUTCDATETIME()), 1, 5, 0.00),
    (6, N'LIB-2024-006', N'Frank', N'Miller', N'frank.miller@email.com', NULL,
     '1998-12-05', N'654 Birch Ln, Springfield, IL 62705',
     DATEADD(MONTH, -1, SYSUTCDATETIME()), DATEADD(MONTH, 11, SYSUTCDATETIME()), 1, 5, 0.00),
    (7, N'LIB-2024-007', N'Grace', N'Wilson', N'grace.wilson@email.com', N'+1-555-0107',
     '1987-04-20', N'987 Cedar Dr, Springfield, IL 62706',
     DATEADD(YEAR, -3, SYSUTCDATETIME()), DATEADD(YEAR, 2, SYSUTCDATETIME()), 1, 10, 0.00),
    (8, N'LIB-2024-008', N'Henry', N'Moore', N'henry.moore@email.com', N'+1-555-0108',
     '1993-09-25', N'147 Maple Ct, Springfield, IL 62707',
     DATEADD(MONTH, -4, SYSUTCDATETIME()), DATEADD(MONTH, 8, SYSUTCDATETIME()), 1, 5, 5.50)
) AS source ([Id], [MembershipNumber], [FirstName], [LastName], [Email], [PhoneNumber],
              [DateOfBirth], [Address], [MemberSince], [MembershipExpiresAt], [IsActive],
              [MaxBooksAllowed], [OutstandingFees])
ON target.[Id] = source.[Id]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [MembershipNumber], [FirstName], [LastName], [Email], [PhoneNumber],
            [DateOfBirth], [Address], [MemberSince], [MembershipExpiresAt], [IsActive],
            [MaxBooksAllowed], [OutstandingFees], [CreatedAt], [UpdatedAt])
    VALUES (source.[Id], source.[MembershipNumber], source.[FirstName], source.[LastName],
            source.[Email], source.[PhoneNumber], source.[DateOfBirth], source.[Address],
            source.[MemberSince], source.[MembershipExpiresAt], source.[IsActive],
            source.[MaxBooksAllowed], source.[OutstandingFees],
            SYSUTCDATETIME(), SYSUTCDATETIME());

SET IDENTITY_INSERT [dbo].[Members] OFF;

PRINT '✅ Members seeded: 8 members (1 inactive, 1 with expired membership, 3 with fees)';
GO

-- =============================================
-- Seed Loans
-- =============================================
PRINT 'Seeding Loans...';

SET IDENTITY_INSERT [dbo].[Loans] ON;

MERGE INTO [dbo].[Loans] AS target
USING (VALUES
    -- Active loans (not returned)
    (1, 1, 1, DATEADD(DAY, -10, SYSUTCDATETIME()), DATEADD(DAY, 4, SYSUTCDATETIME()),
     NULL, 0, NULL, 0, 0, 2, NULL),  -- Alice borrowed Clean Code, due in 4 days
    (2, 2, 3, DATEADD(DAY, -8, SYSUTCDATETIME()), DATEADD(DAY, 6, SYSUTCDATETIME()),
     NULL, 0, NULL, 0, 1, 2, NULL),  -- Bob borrowed Refactoring, renewed once
    (3, 5, 5, DATEADD(DAY, -5, SYSUTCDATETIME()), DATEADD(DAY, 9, SYSUTCDATETIME()),
     NULL, 0, NULL, 0, 0, 2, NULL),  -- Eve borrowed Pragmatic Programmer

    -- Overdue loans
    (4, 3, 9, DATEADD(DAY, -20, SYSUTCDATETIME()), DATEADD(DAY, -6, SYSUTCDATETIME()),
     NULL, 2, 3.00, 0, 0, 2, NULL),  -- Carol has overdue Effective Java (6 days late)
    (5, 8, 18, DATEADD(DAY, -25, SYSUTCDATETIME()), DATEADD(DAY, -11, SYSUTCDATETIME()),
     NULL, 2, 5.50, 0, 0, 2, NULL),  -- Henry has overdue Atomic Habits (11 days late)

    -- Returned on time
    (6, 1, 6, DATEADD(DAY, -30, SYSUTCDATETIME()), DATEADD(DAY, -16, SYSUTCDATETIME()),
     DATEADD(DAY, -18, SYSUTCDATETIME()), 1, 0.00, 1, 0, 2, NULL),  -- Alice returned Code Complete on time
    (7, 2, 13, DATEADD(DAY, -45, SYSUTCDATETIME()), DATEADD(DAY, -31, SYSUTCDATETIME()),
     DATEADD(DAY, -32, SYSUTCDATETIME()), 1, 0.00, 1, 0, 2, NULL),  -- Bob returned The Hobbit on time
    (8, 7, 16, DATEADD(DAY, -60, SYSUTCDATETIME()), DATEADD(DAY, -46, SYSUTCDATETIME()),
     DATEADD(DAY, -47, SYSUTCDATETIME()), 1, 0.00, 1, 1, 2, NULL),  -- Grace returned Sapiens (renewed once)

    -- Returned late (with fees)
    (9, 3, 11, DATEADD(DAY, -50, SYSUTCDATETIME()), DATEADD(DAY, -36, SYSUTCDATETIME()),
     DATEADD(DAY, -31, SYSUTCDATETIME()), 3, 2.50, 1, 0, 2, NULL),  -- Carol returned 1984 late (5 days)
    (10, 6, 12, DATEADD(DAY, -35, SYSUTCDATETIME()), DATEADD(DAY, -21, SYSUTCDATETIME()),
     DATEADD(DAY, -20, SYSUTCDATETIME()), 3, 0.50, 0, 0, 2, NULL),  -- Frank returned TKAM late (1 day, unpaid)

    -- Lost
    (11, 4, 20, DATEADD(DAY, -90, SYSUTCDATETIME()), DATEADD(DAY, -76, SYSUTCDATETIME()),
     NULL, 4, NULL, 0, 0, 2, N'Member reported book lost'),  -- David lost Brief History of Time

    -- Damaged
    (12, 8, 21, DATEADD(DAY, -40, SYSUTCDATETIME()), DATEADD(DAY, -26, SYSUTCDATETIME()),
     DATEADD(DAY, -28, SYSUTCDATETIME()), 5, 0.00, 1, 0, 2, N'Water damage to cover')  -- Henry returned damaged Da Vinci Code
) AS source ([Id], [MemberId], [BookId], [BorrowedAt], [DueDate], [ReturnedAt],
              [Status], [LateFee], [IsFeePaid], [RenewalCount], [MaxRenewalsAllowed], [Notes])
ON target.[Id] = source.[Id]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Id], [MemberId], [BookId], [BorrowedAt], [DueDate], [ReturnedAt],
            [Status], [LateFee], [IsFeePaid], [RenewalCount], [MaxRenewalsAllowed], [Notes],
            [CreatedAt], [UpdatedAt])
    VALUES (source.[Id], source.[MemberId], source.[BookId], source.[BorrowedAt], source.[DueDate],
            source.[ReturnedAt], source.[Status], source.[LateFee], source.[IsFeePaid],
            source.[RenewalCount], source.[MaxRenewalsAllowed], source.[Notes],
            SYSUTCDATETIME(), SYSUTCDATETIME());

SET IDENTITY_INSERT [dbo].[Loans] OFF;

PRINT '✅ Loans seeded: 12 loans (3 active, 2 overdue, 4 returned on time, 2 returned late, 1 lost, 1 damaged)';
GO

SET NOCOUNT OFF;
GO

-- =============================================
-- Summary
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ Seed Data Migration Completed!';
PRINT '========================================';
PRINT '';
PRINT '📊 Data Summary:';
PRINT '   ✅ 12 Categories (8 top-level, 4 sub-categories)';
PRINT '   ✅ 10 Authors (renowned software engineers and writers)';
PRINT '   ✅ 25 Books (programming, fiction, non-fiction, mystery, history)';
PRINT '   ✅ 11 BookAuthor relationships';
PRINT '   ✅ 8 Members (various statuses and fee amounts)';
PRINT '   ✅ 12 Loans (diverse statuses for testing)';
PRINT '';
PRINT '📚 Book Distribution:';
PRINT '   ✅ Programming: 10 books';
PRINT '   ✅ Fiction: 5 books';
PRINT '   ✅ Non-Fiction: 5 books';
PRINT '   ✅ Mystery: 3 books';
PRINT '   ✅ History (WWII): 2 books';
PRINT '';
PRINT '👥 Member Scenarios:';
PRINT '   ✅ Active members with no fees: 4';
PRINT '   ✅ Active members with outstanding fees: 3';
PRINT '   ✅ Inactive member: 1 (David Brown)';
PRINT '';
PRINT '📖 Loan Scenarios:';
PRINT '   ✅ Active (current): 3 loans';
PRINT '   ✅ Overdue: 2 loans';
PRINT '   ✅ Returned on time: 4 loans';
PRINT '   ✅ Returned late: 2 loans';
PRINT '   ✅ Lost: 1 loan';
PRINT '   ✅ Damaged: 1 loan';
PRINT '';
PRINT '🎯 Ready for Testing!';
PRINT 'All sample data has been loaded and is ready for ADO.NET demonstrations.';
PRINT '========================================';
GO
