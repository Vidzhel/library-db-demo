namespace DbDemo.Domain.Entities;

/// <summary>
/// Represents a physical library branch with geographic location data.
/// Demonstrates SQL Server spatial data types (GEOGRAPHY) for location-based queries.
/// </summary>
public class LibraryBranch
{
    // Primary key
    public int Id { get; private set; }

    // Branch information
    public string BranchName { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }

    // Geographic location (stored as GEOGRAPHY in database)
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    // Audit fields
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // EF Core requires a parameterless constructor
    private LibraryBranch() { }

    /// <summary>
    /// Creates a new library branch (call method, not a constructor pattern here for simplicity)
    /// </summary>
    public LibraryBranch(
        string branchName,
        string address,
        string city,
        string? postalCode = null,
        string? phoneNumber = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty", nameof(branchName));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        BranchName = branchName;
        Address = address;
        City = city;
        PostalCode = postalCode;
        PhoneNumber = phoneNumber;
        Email = email;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the geographic location of the branch
    /// </summary>
    public void SetLocation(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90");
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180");

        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates branch contact information
    /// </summary>
    public void UpdateContactInfo(string? phoneNumber, string? email)
    {
        PhoneNumber = phoneNumber;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates branch details
    /// </summary>
    public void UpdateDetails(string branchName, string address, string city, string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty", nameof(branchName));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        BranchName = branchName;
        Address = address;
        City = city;
        PostalCode = postalCode;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the branch
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to reconstruct from database
    /// </summary>
    public static LibraryBranch FromDatabase(
        int id,
        string branchName,
        string address,
        string city,
        string? postalCode,
        string? phoneNumber,
        string? email,
        double? latitude,
        double? longitude,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted)
    {
        return new LibraryBranch
        {
            Id = id,
            BranchName = branchName,
            Address = address,
            City = city,
            PostalCode = postalCode,
            PhoneNumber = phoneNumber,
            Email = email,
            Latitude = latitude,
            Longitude = longitude,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted
        };
    }
}
