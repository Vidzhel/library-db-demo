using DbDemo.Domain.Entities;
using DbDemo.Infrastructure.EFCore.EFModels;
using NetTopologySuite.Geometries;
using DomainAuthor = DbDemo.Domain.Entities.Author;
using DomainBook = DbDemo.Domain.Entities.Book;
using DomainBookAuthor = DbDemo.Domain.Entities.BookAuthor;
using DomainBookAudit = DbDemo.Domain.Entities.BookAudit;
using DomainCategory = DbDemo.Domain.Entities.Category;
using DomainLibraryBranch = DbDemo.Domain.Entities.LibraryBranch;
using DomainLoan = DbDemo.Domain.Entities.Loan;
using DomainMember = DbDemo.Domain.Entities.Member;
using EFAuthor = DbDemo.Infrastructure.EFCore.EFModels.Author;
using EFBook = DbDemo.Infrastructure.EFCore.EFModels.Book;
using EFBookAuthor = DbDemo.Infrastructure.EFCore.EFModels.BookAuthor;
using EFBooksAudit = DbDemo.Infrastructure.EFCore.EFModels.BooksAudit;
using EFCategory = DbDemo.Infrastructure.EFCore.EFModels.Category;
using EFLibraryBranch = DbDemo.Infrastructure.EFCore.EFModels.LibraryBranch;
using EFLoan = DbDemo.Infrastructure.EFCore.EFModels.Loan;
using EFMember = DbDemo.Infrastructure.EFCore.EFModels.Member;

namespace DbDemo.Infrastructure.EFCore.Mappers;

/// <summary>
/// Maps between EF Core entity models (anemic data models) and Domain entities (rich business models).
///
/// ARCHITECTURE DECISION: Why Two Entity Models?
/// ===============================================
///
/// EF ENTITIES (EFModels/)              DOMAIN ENTITIES (DbDemo.Domain/)
/// -----------------------              -------------------------------
/// ✓ Anemic (no behavior)               ✓ Rich (business logic)
/// ✓ Public setters                     ✓ Immutable/controlled mutation
/// ✓ Parameterless constructor          ✓ Factory methods
/// ✓ EF navigation properties           ✓ Encapsulated collections
/// ✓ Database schema focus              ✓ Business domain focus
/// ✓ Easy to scaffold/regenerate        ✓ Manually crafted with care
/// ✓ ORM-optimized                      ✓ Domain-driven design
///
/// MAPPING DIRECTION:
/// - EF → Domain: Used when reading from database (uses Domain.FromDatabase factory methods)
/// - Domain → EF: Used when writing to database (copies Domain state to EF properties)
///
/// NULL HANDLING:
/// - Domain entities may have stricter null rules (invariants)
/// - Mapper preserves null values as-is from EF entities
/// - Domain FromDatabase methods are trusted to handle database values correctly
/// </summary>
/// <remarks>
/// This mapper is STATELESS and uses static methods for performance.
/// All methods are pure functions with no side effects.
/// </remarks>
public static class EntityMapper
{
    #region Author Mapping

    /// <summary>
    /// Converts an EF Author entity to a Domain Author entity.
    /// </summary>
    /// <param name="efAuthor">The EF author entity from the database</param>
    /// <returns>A rich Domain Author entity</returns>
    /// <exception cref="ArgumentNullException">If efAuthor is null</exception>
    public static DomainAuthor ToDomain(this EFAuthor efAuthor)
    {
        ArgumentNullException.ThrowIfNull(efAuthor);

        return DomainAuthor.FromDatabase(
            id: efAuthor.Id,
            firstName: efAuthor.FirstName,
            lastName: efAuthor.LastName,
            biography: efAuthor.Biography,
            dateOfBirth: efAuthor.DateOfBirth,
            nationality: efAuthor.Nationality,
            email: efAuthor.Email,
            createdAt: efAuthor.CreatedAt,
            updatedAt: efAuthor.UpdatedAt
        );
    }

    /// <summary>
    /// Updates an EF Author entity from a Domain Author entity.
    /// Used for INSERT or UPDATE operations.
    /// </summary>
    /// <param name="efAuthor">The EF entity to update</param>
    /// <param name="domainAuthor">The domain entity with source data</param>
    /// <param name="isNewEntity">True if creating new entity (sets Id = 0), false if updating existing</param>
    public static void UpdateFromDomain(this EFAuthor efAuthor, DomainAuthor domainAuthor, bool isNewEntity = false)
    {
        ArgumentNullException.ThrowIfNull(efAuthor);
        ArgumentNullException.ThrowIfNull(domainAuthor);

        // For new entities, EF will generate the ID
        // For updates, preserve the existing ID
        if (!isNewEntity)
        {
            efAuthor.Id = domainAuthor.Id;
        }

        efAuthor.FirstName = domainAuthor.FirstName;
        efAuthor.LastName = domainAuthor.LastName;
        efAuthor.Biography = domainAuthor.Biography;
        efAuthor.DateOfBirth = domainAuthor.DateOfBirth;
        efAuthor.Nationality = domainAuthor.Nationality;
        efAuthor.Email = domainAuthor.Email;

        // Note: FullName is a computed column - don't set it
        // Note: CreatedAt/UpdatedAt have database defaults - EF handles these
    }

    #endregion

    #region Book Mapping

    /// <summary>
    /// Converts an EF Book entity to a Domain Book entity.
    /// </summary>
    /// <param name="efBook">The EF book entity from the database</param>
    /// <returns>A rich Domain Book entity</returns>
    /// <exception cref="ArgumentNullException">If efBook is null</exception>
    public static DomainBook ToDomain(this EFBook efBook)
    {
        ArgumentNullException.ThrowIfNull(efBook);

        return DomainBook.FromDatabase(
            id: efBook.Id,
            isbn: efBook.ISBN,
            title: efBook.Title,
            subtitle: efBook.Subtitle,
            description: efBook.Description,
            publisher: efBook.Publisher,
            publishedDate: efBook.PublishedDate,
            pageCount: efBook.PageCount,
            language: efBook.Language,
            categoryId: efBook.CategoryId,
            totalCopies: efBook.TotalCopies,
            availableCopies: efBook.AvailableCopies,
            shelfLocation: efBook.ShelfLocation,
            isDeleted: efBook.IsDeleted,
            createdAt: efBook.CreatedAt,
            updatedAt: efBook.UpdatedAt,
            metadataJson: efBook.Metadata
        );
    }

    /// <summary>
    /// Updates an EF Book entity from a Domain Book entity.
    /// </summary>
    /// <param name="efBook">The EF entity to update</param>
    /// <param name="domainBook">The domain entity with source data</param>
    /// <param name="isNewEntity">True if creating new entity, false if updating existing</param>
    public static void UpdateFromDomain(this EFBook efBook, DomainBook domainBook, bool isNewEntity = false)
    {
        ArgumentNullException.ThrowIfNull(efBook);
        ArgumentNullException.ThrowIfNull(domainBook);

        if (!isNewEntity)
        {
            efBook.Id = domainBook.Id;
        }

        efBook.ISBN = domainBook.ISBN;
        efBook.Title = domainBook.Title;
        efBook.Subtitle = domainBook.Subtitle;
        efBook.Description = domainBook.Description;
        efBook.Publisher = domainBook.Publisher;
        efBook.PublishedDate = domainBook.PublishedDate;
        efBook.PageCount = domainBook.PageCount;
        efBook.Language = domainBook.Language;
        efBook.CategoryId = domainBook.CategoryId;
        efBook.TotalCopies = domainBook.TotalCopies;
        efBook.AvailableCopies = domainBook.AvailableCopies;
        efBook.ShelfLocation = domainBook.ShelfLocation;
        efBook.IsDeleted = domainBook.IsDeleted;
        efBook.Metadata = domainBook.MetadataJson;

        // Note: YearPublished and PublishedDecade are computed columns
        // Note: CreatedAt/UpdatedAt have database defaults
    }

    #endregion

    #region BookAuthor Mapping

    /// <summary>
    /// Converts an EF BookAuthor entity to a Domain BookAuthor entity.
    /// </summary>
    public static DomainBookAuthor ToDomain(this EFBookAuthor efBookAuthor)
    {
        ArgumentNullException.ThrowIfNull(efBookAuthor);

        return DomainBookAuthor.FromDatabase(
            bookId: efBookAuthor.BookId,
            authorId: efBookAuthor.AuthorId,
            authorOrder: efBookAuthor.AuthorOrder,
            role: efBookAuthor.Role,
            createdAt: efBookAuthor.CreatedAt
        );
    }

    /// <summary>
    /// Updates an EF BookAuthor entity from a Domain BookAuthor entity.
    /// </summary>
    public static void UpdateFromDomain(this EFBookAuthor efBookAuthor, DomainBookAuthor domainBookAuthor)
    {
        ArgumentNullException.ThrowIfNull(efBookAuthor);
        ArgumentNullException.ThrowIfNull(domainBookAuthor);

        efBookAuthor.BookId = domainBookAuthor.BookId;
        efBookAuthor.AuthorId = domainBookAuthor.AuthorId;
        efBookAuthor.AuthorOrder = domainBookAuthor.AuthorOrder;
        efBookAuthor.Role = domainBookAuthor.Role;
    }

    #endregion

    #region BookAudit Mapping

    /// <summary>
    /// Converts an EF BooksAudit entity to a Domain BookAudit entity.
    /// NOTE: This entity is READ-ONLY (populated by database trigger).
    /// </summary>
    public static DomainBookAudit ToDomain(this EFBooksAudit efAudit)
    {
        ArgumentNullException.ThrowIfNull(efAudit);

        return DomainBookAudit.FromDatabase(
            auditId: efAudit.AuditId,
            bookId: efAudit.BookId,
            action: efAudit.Action,
            oldISBN: efAudit.OldISBN,
            newISBN: efAudit.NewISBN,
            oldTitle: efAudit.OldTitle,
            newTitle: efAudit.NewTitle,
            oldAvailableCopies: efAudit.OldAvailableCopies,
            newAvailableCopies: efAudit.NewAvailableCopies,
            oldTotalCopies: efAudit.OldTotalCopies,
            newTotalCopies: efAudit.NewTotalCopies,
            changedAt: efAudit.ChangedAt,
            changedBy: efAudit.ChangedBy
        );
    }

    // No UpdateFromDomain for BookAudit - it's populated by trigger

    #endregion

    #region Category Mapping

    /// <summary>
    /// Converts an EF Category entity to a Domain Category entity.
    /// </summary>
    public static DomainCategory ToDomain(this EFCategory efCategory)
    {
        ArgumentNullException.ThrowIfNull(efCategory);

        return DomainCategory.FromDatabase(
            id: efCategory.Id,
            name: efCategory.Name,
            description: efCategory.Description,
            parentCategoryId: efCategory.ParentCategoryId,
            createdAt: efCategory.CreatedAt,
            updatedAt: efCategory.UpdatedAt
        );
    }

    /// <summary>
    /// Updates an EF Category entity from a Domain Category entity.
    /// </summary>
    public static void UpdateFromDomain(this EFCategory efCategory, DomainCategory domainCategory, bool isNewEntity = false)
    {
        ArgumentNullException.ThrowIfNull(efCategory);
        ArgumentNullException.ThrowIfNull(domainCategory);

        if (!isNewEntity)
        {
            efCategory.Id = domainCategory.Id;
        }

        efCategory.Name = domainCategory.Name;
        efCategory.Description = domainCategory.Description;
        efCategory.ParentCategoryId = domainCategory.ParentCategoryId;
    }

    #endregion

    #region LibraryBranch Mapping

    /// <summary>
    /// Converts an EF LibraryBranch entity to a Domain LibraryBranch entity.
    /// </summary>
    public static DomainLibraryBranch ToDomain(this EFLibraryBranch efBranch)
    {
        ArgumentNullException.ThrowIfNull(efBranch);

        // Convert NetTopologySuite.Geometries.Point to coordinates
        double? latitude = null;
        double? longitude = null;

        if (efBranch.Location != null && efBranch.Location is Point point)
        {
            latitude = point.Y;  // Y coordinate is latitude
            longitude = point.X; // X coordinate is longitude
        }

        return DomainLibraryBranch.FromDatabase(
            id: efBranch.Id,
            branchName: efBranch.BranchName,
            address: efBranch.Address,
            city: efBranch.City,
            postalCode: efBranch.PostalCode,
            phoneNumber: efBranch.PhoneNumber,
            email: efBranch.Email,
            latitude: latitude,
            longitude: longitude,
            createdAt: efBranch.CreatedAt,
            updatedAt: efBranch.UpdatedAt,
            isDeleted: efBranch.IsDeleted
        );
    }

    /// <summary>
    /// Updates an EF LibraryBranch entity from a Domain LibraryBranch entity.
    /// </summary>
    public static void UpdateFromDomain(this EFLibraryBranch efBranch, DomainLibraryBranch domainBranch, bool isNewEntity = false)
    {
        ArgumentNullException.ThrowIfNull(efBranch);
        ArgumentNullException.ThrowIfNull(domainBranch);

        if (!isNewEntity)
        {
            efBranch.Id = domainBranch.Id;
        }

        efBranch.BranchName = domainBranch.BranchName;
        efBranch.Address = domainBranch.Address;
        efBranch.City = domainBranch.City;
        efBranch.PostalCode = domainBranch.PostalCode;
        efBranch.PhoneNumber = domainBranch.PhoneNumber;
        efBranch.Email = domainBranch.Email;
        efBranch.IsDeleted = domainBranch.IsDeleted;

        // Convert coordinates to NetTopologySuite.Geometries.Point
        if (domainBranch.Latitude.HasValue && domainBranch.Longitude.HasValue)
        {
            // Create a Point with SRID 4326 (WGS 84 - standard GPS coordinates)
            var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            efBranch.Location = geometryFactory.CreatePoint(
                new Coordinate(domainBranch.Longitude.Value, domainBranch.Latitude.Value)
            );
        }
        else
        {
            efBranch.Location = null;
        }
    }

    #endregion

    #region Loan Mapping

    /// <summary>
    /// Converts an EF Loan entity to a Domain Loan entity.
    /// </summary>
    public static DomainLoan ToDomain(this EFLoan efLoan)
    {
        ArgumentNullException.ThrowIfNull(efLoan);

        return DomainLoan.FromDatabase(
            id: efLoan.Id,
            memberId: efLoan.MemberId,
            bookId: efLoan.BookId,
            borrowedAt: efLoan.BorrowedAt,
            dueDate: efLoan.DueDate,
            returnedAt: efLoan.ReturnedAt,
            status: (DbDemo.Domain.Entities.LoanStatus)efLoan.Status, // Cast int to enum
            lateFee: efLoan.LateFee,
            isFeePaid: efLoan.IsFeePaid,
            renewalCount: efLoan.RenewalCount,
            maxRenewalsAllowed: efLoan.MaxRenewalsAllowed,
            notes: efLoan.Notes,
            createdAt: efLoan.CreatedAt,
            updatedAt: efLoan.UpdatedAt
        );
    }

    /// <summary>
    /// Updates an EF Loan entity from a Domain Loan entity.
    /// </summary>
    public static void UpdateFromDomain(this EFLoan efLoan, DomainLoan domainLoan, bool isNewEntity = false)
    {
        ArgumentNullException.ThrowIfNull(efLoan);
        ArgumentNullException.ThrowIfNull(domainLoan);

        if (!isNewEntity)
        {
            efLoan.Id = domainLoan.Id;
        }

        efLoan.MemberId = domainLoan.MemberId;
        efLoan.BookId = domainLoan.BookId;
        efLoan.BorrowedAt = domainLoan.BorrowedAt;
        efLoan.DueDate = domainLoan.DueDate;
        efLoan.ReturnedAt = domainLoan.ReturnedAt;
        efLoan.Status = (int)domainLoan.Status; // Cast enum to int
        efLoan.LateFee = domainLoan.LateFee;
        efLoan.IsFeePaid = domainLoan.IsFeePaid;
        efLoan.RenewalCount = domainLoan.RenewalCount;
        efLoan.MaxRenewalsAllowed = domainLoan.MaxRenewalsAllowed;
        efLoan.Notes = domainLoan.Notes;

        // Note: DaysOverdue is a computed column
    }

    #endregion

    #region Member Mapping

    /// <summary>
    /// Converts an EF Member entity to a Domain Member entity.
    /// </summary>
    public static DomainMember ToDomain(this EFMember efMember)
    {
        ArgumentNullException.ThrowIfNull(efMember);

        return DomainMember.FromDatabase(
            id: efMember.Id,
            membershipNumber: efMember.MembershipNumber,
            firstName: efMember.FirstName,
            lastName: efMember.LastName,
            email: efMember.Email,
            phoneNumber: efMember.PhoneNumber,
            dateOfBirth: efMember.DateOfBirth,
            address: efMember.Address,
            memberSince: efMember.MemberSince,
            membershipExpiresAt: efMember.MembershipExpiresAt,
            isActive: efMember.IsActive,
            maxBooksAllowed: efMember.MaxBooksAllowed,
            outstandingFees: efMember.OutstandingFees,
            createdAt: efMember.CreatedAt,
            updatedAt: efMember.UpdatedAt
        );
    }

    /// <summary>
    /// Updates an EF Member entity from a Domain Member entity.
    /// </summary>
    public static void UpdateFromDomain(this EFMember efMember, DomainMember domainMember, bool isNewEntity = false)
    {
        ArgumentNullException.ThrowIfNull(efMember);
        ArgumentNullException.ThrowIfNull(domainMember);

        if (!isNewEntity)
        {
            efMember.Id = domainMember.Id;
        }

        efMember.MembershipNumber = domainMember.MembershipNumber;
        efMember.FirstName = domainMember.FirstName;
        efMember.LastName = domainMember.LastName;
        efMember.Email = domainMember.Email;
        efMember.PhoneNumber = domainMember.PhoneNumber;
        efMember.DateOfBirth = domainMember.DateOfBirth;
        efMember.Address = domainMember.Address;
        efMember.MemberSince = domainMember.MemberSince;
        efMember.MembershipExpiresAt = domainMember.MembershipExpiresAt;
        efMember.IsActive = domainMember.IsActive;
        efMember.MaxBooksAllowed = domainMember.MaxBooksAllowed;
        efMember.OutstandingFees = domainMember.OutstandingFees;

        // Note: Age is a computed column
    }

    #endregion

    #region Bulk Mapping Helpers

    /// <summary>
    /// Converts a collection of EF entities to Domain entities.
    /// </summary>
    public static List<DomainAuthor> ToDomain(this IEnumerable<EFAuthor> efAuthors)
        => efAuthors?.Select(a => a.ToDomain()).ToList() ?? new List<DomainAuthor>();

    /// <summary>
    /// Converts a collection of EF entities to Domain entities.
    /// </summary>
    public static List<DomainBook> ToDomain(this IEnumerable<EFBook> efBooks)
        => efBooks?.Select(b => b.ToDomain()).ToList() ?? new List<DomainBook>();

    /// <summary>
    /// Converts a collection of EF entities to Domain entities.
    /// </summary>
    public static List<DomainCategory> ToDomain(this IEnumerable<EFCategory> efCategories)
        => efCategories?.Select(c => c.ToDomain()).ToList() ?? new List<DomainCategory>();

    /// <summary>
    /// Converts a collection of EF entities to Domain entities.
    /// </summary>
    public static List<DomainLibraryBranch> ToDomain(this IEnumerable<EFLibraryBranch> efBranches)
        => efBranches?.Select(b => b.ToDomain()).ToList() ?? new List<DomainLibraryBranch>();

    /// <summary>
    /// Converts a collection of EF entities to Domain entities.
    /// </summary>
    public static List<DomainLoan> ToDomain(this IEnumerable<EFLoan> efLoans)
        => efLoans?.Select(l => l.ToDomain()).ToList() ?? new List<DomainLoan>();

    /// <summary>
    /// Converts a collection of EF entities to Domain entities.
    /// </summary>
    public static List<DomainMember> ToDomain(this IEnumerable<EFMember> efMembers)
        => efMembers?.Select(m => m.ToDomain()).ToList() ?? new List<DomainMember>();

    /// <summary>
    /// Converts a collection of EF entities to Domain entities.
    /// </summary>
    public static List<DomainBookAudit> ToDomain(this IEnumerable<EFBooksAudit> efAudits)
        => efAudits?.Select(a => a.ToDomain()).ToList() ?? new List<DomainBookAudit>();

    #endregion
}
