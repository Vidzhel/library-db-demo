namespace DbDemo.ConsoleApp.Models;

public class Category
{
    private string _name = string.Empty;

    // Parameterless constructor for ORM/ADO.NET materialization
    private Category() { }

    public Category(string name, string? description = null, int? parentCategoryId = null)
    {
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string Name
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Category name cannot be empty", nameof(Name));

            if (value.Length > 100)
                throw new ArgumentException("Category name cannot exceed 100 characters", nameof(Name));

            _name = value.Trim();
        }
    }

    public string? Description { get; private set; }
    public int? ParentCategoryId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Category? ParentCategory { get; private set; }
    public List<Category> ChildCategories { get; private set; } = new();
    public List<Book> Books { get; private set; } = new();

    public bool IsTopLevel => ParentCategoryId == null;

    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"{Name} (ID: {Id})";

    /// <summary>
    /// Internal factory method for repository hydration - bypasses validation since data comes from database
    /// </summary>
    internal static Category FromDatabase(
        int id,
        string name,
        string? description,
        int? parentCategoryId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var category = new Category();
        category.Id = id;
        category._name = name;
        category.Description = description;
        category.ParentCategoryId = parentCategoryId;
        category.CreatedAt = createdAt;
        category.UpdatedAt = updatedAt;
        return category;
    }
}
