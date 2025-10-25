-- =============================================
-- Migration V001: Initial Database Schema
-- =============================================
-- Creates all core tables for the Library Management System
--
-- Tables created:
--   1. Categories - Book categories with hierarchical support
--   2. Authors - Author information
--   3. Books - Book catalog with inventory tracking
--   4. Members - Library members
--   5. Loans - Book borrowing records
--   6. BookAuthors - Many-to-many relationship between Books and Authors
--
-- All tables include audit timestamps (CreatedAt, UpdatedAt)
-- All migrations are idempotent (safe to run multiple times)
-- =============================================

USE LibraryDb;
GO

-- =============================================
-- Table 1: Categories
-- =============================================
-- Supports hierarchical categories (e.g., Fiction > Science Fiction)
-- ParentCategoryId allows for unlimited nesting depth

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Categories]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [ParentCategoryId] INT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Categories_ParentCategory] FOREIGN KEY ([ParentCategoryId])
            REFERENCES [dbo].[Categories] ([Id])
            ON DELETE NO ACTION  -- Prevent deletion of parent if children exist
    );

    PRINT '✅ Table [Categories] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Table [Categories] already exists.';
END
GO

-- =============================================
-- Table 2: Authors
-- =============================================
-- Stores author biographical information
-- Email is optional for privacy

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Authors]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Authors]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FirstName] NVARCHAR(50) NOT NULL,
        [LastName] NVARCHAR(50) NOT NULL,
        [Biography] NVARCHAR(MAX) NULL,
        [DateOfBirth] DATETIME2(7) NULL,
        [Nationality] NVARCHAR(100) NULL,
        [Email] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_Authors] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT '✅ Table [Authors] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Table [Authors] already exists.';
END
GO

-- =============================================
-- Table 3: Books
-- =============================================
-- Core book catalog with inventory management
-- ISBN can be 10 or 13 digits (with or without hyphens)
-- AvailableCopies must always be <= TotalCopies

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Books]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Books]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ISBN] NVARCHAR(20) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Subtitle] NVARCHAR(200) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Publisher] NVARCHAR(200) NULL,
        [PublishedDate] DATETIME2(7) NULL,
        [PageCount] INT NULL,
        [Language] NVARCHAR(50) NULL,
        [CategoryId] INT NOT NULL,
        [TotalCopies] INT NOT NULL DEFAULT 1,
        [AvailableCopies] INT NOT NULL DEFAULT 1,
        [ShelfLocation] NVARCHAR(50) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_Books] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Books_Categories] FOREIGN KEY ([CategoryId])
            REFERENCES [dbo].[Categories] ([Id])
            ON DELETE NO ACTION,  -- Prevent category deletion if books exist

        -- Business rule: Available copies cannot exceed total copies
        CONSTRAINT [CK_Books_AvailableCopies] CHECK ([AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]),
        CONSTRAINT [CK_Books_TotalCopies] CHECK ([TotalCopies] >= 0),
        CONSTRAINT [CK_Books_PageCount] CHECK ([PageCount] IS NULL OR [PageCount] > 0)
    );

    PRINT '✅ Table [Books] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Table [Books] already exists.';
END
GO

-- =============================================
-- Table 4: Members
-- =============================================
-- Library membership records
-- MembershipNumber is business key (unique identifier)
-- Email is normalized to lowercase for consistency

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Members]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Members]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [MembershipNumber] NVARCHAR(20) NOT NULL,
        [FirstName] NVARCHAR(50) NOT NULL,
        [LastName] NVARCHAR(50) NOT NULL,
        [Email] NVARCHAR(255) NOT NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [DateOfBirth] DATETIME2(7) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [MemberSince] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [MembershipExpiresAt] DATETIME2(7) NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [MaxBooksAllowed] INT NOT NULL DEFAULT 5,
        [OutstandingFees] DECIMAL(10,2) NOT NULL DEFAULT 0.00,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_Members] PRIMARY KEY CLUSTERED ([Id] ASC),

        -- Business rules
        CONSTRAINT [CK_Members_MaxBooksAllowed] CHECK ([MaxBooksAllowed] > 0),
        CONSTRAINT [CK_Members_OutstandingFees] CHECK ([OutstandingFees] >= 0),
        CONSTRAINT [CK_Members_MembershipExpiry] CHECK ([MembershipExpiresAt] > [MemberSince])
    );

    PRINT '✅ Table [Members] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Table [Members] already exists.';
END
GO

-- =============================================
-- Table 5: Loans
-- =============================================
-- Tracks book borrowing history
-- Status: 0=Active, 1=Returned, 2=Overdue, 3=ReturnedLate, 4=Lost, 5=Damaged, 6=Cancelled
-- LateFee calculated at $0.50 per day overdue

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Loans]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Loans]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [MemberId] INT NOT NULL,
        [BookId] INT NOT NULL,
        [BorrowedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [DueDate] DATETIME2(7) NOT NULL,
        [ReturnedAt] DATETIME2(7) NULL,
        [Status] INT NOT NULL DEFAULT 0,  -- LoanStatus enum
        [LateFee] DECIMAL(10,2) NULL,
        [IsFeePaid] BIT NOT NULL DEFAULT 0,
        [RenewalCount] INT NOT NULL DEFAULT 0,
        [MaxRenewalsAllowed] INT NOT NULL DEFAULT 2,
        [Notes] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_Loans] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Loans_Members] FOREIGN KEY ([MemberId])
            REFERENCES [dbo].[Members] ([Id])
            ON DELETE NO ACTION,  -- Preserve loan history even if member deleted
        CONSTRAINT [FK_Loans_Books] FOREIGN KEY ([BookId])
            REFERENCES [dbo].[Books] ([Id])
            ON DELETE NO ACTION,  -- Preserve loan history even if book deleted

        -- Business rules
        CONSTRAINT [CK_Loans_DueDate] CHECK ([DueDate] > [BorrowedAt]),
        CONSTRAINT [CK_Loans_ReturnedAt] CHECK ([ReturnedAt] IS NULL OR [ReturnedAt] >= [BorrowedAt]),
        CONSTRAINT [CK_Loans_Status] CHECK ([Status] >= 0 AND [Status] <= 6),
        CONSTRAINT [CK_Loans_LateFee] CHECK ([LateFee] IS NULL OR [LateFee] >= 0),
        CONSTRAINT [CK_Loans_RenewalCount] CHECK ([RenewalCount] >= 0),
        CONSTRAINT [CK_Loans_MaxRenewals] CHECK ([MaxRenewalsAllowed] >= 0 AND [RenewalCount] <= [MaxRenewalsAllowed])
    );

    PRINT '✅ Table [Loans] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Table [Loans] already exists.';
END
GO

-- =============================================
-- Table 6: BookAuthors (Many-to-Many Junction)
-- =============================================
-- Links books to their authors
-- Supports multiple authors per book with ordering
-- Role allows specifying "Editor", "Translator", etc.

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BookAuthors]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[BookAuthors]
    (
        [BookId] INT NOT NULL,
        [AuthorId] INT NOT NULL,
        [AuthorOrder] INT NOT NULL DEFAULT 0,  -- For multi-author books (1st, 2nd, 3rd author)
        [Role] NVARCHAR(50) NULL,  -- e.g., "Author", "Editor", "Translator"
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_BookAuthors] PRIMARY KEY CLUSTERED ([BookId] ASC, [AuthorId] ASC),
        CONSTRAINT [FK_BookAuthors_Books] FOREIGN KEY ([BookId])
            REFERENCES [dbo].[Books] ([Id])
            ON DELETE CASCADE,  -- If book deleted, remove all author associations
        CONSTRAINT [FK_BookAuthors_Authors] FOREIGN KEY ([AuthorId])
            REFERENCES [dbo].[Authors] ([Id])
            ON DELETE CASCADE,  -- If author deleted, remove all book associations

        CONSTRAINT [CK_BookAuthors_Order] CHECK ([AuthorOrder] >= 0)
    );

    PRINT '✅ Table [BookAuthors] created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Table [BookAuthors] already exists.';
END
GO

-- =============================================
-- Summary
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ Initial Schema Migration Completed!';
PRINT '========================================';
PRINT '';
PRINT '📋 Tables Created:';
PRINT '   1. Categories (with hierarchical support)';
PRINT '   2. Authors';
PRINT '   3. Books (with inventory management)';
PRINT '   4. Members';
PRINT '   5. Loans (with status tracking)';
PRINT '   6. BookAuthors (many-to-many junction)';
PRINT '';
PRINT '🔗 Relationships:';
PRINT '   ✅ Books → Categories (FK)';
PRINT '   ✅ Categories → Categories (self-referencing FK)';
PRINT '   ✅ Loans → Members (FK)';
PRINT '   ✅ Loans → Books (FK)';
PRINT '   ✅ BookAuthors → Books (FK, CASCADE DELETE)';
PRINT '   ✅ BookAuthors → Authors (FK, CASCADE DELETE)';
PRINT '';
PRINT '📏 Business Rules Enforced:';
PRINT '   ✅ Available copies <= Total copies';
PRINT '   ✅ Outstanding fees >= 0';
PRINT '   ✅ Due date > Borrowed date';
PRINT '   ✅ Renewal count <= Max renewals allowed';
PRINT '';
PRINT '⏭️  Next: Run V002__add_indexes.sql for performance optimization';
PRINT '========================================';
GO
