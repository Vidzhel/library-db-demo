namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// DTO representing a category within a hierarchical tree structure.
/// Returned by fn_GetCategoryHierarchy recursive CTE function.
/// </summary>
public class CategoryHierarchy
{
    /// <summary>
    /// The category ID.
    /// </summary>
    public int CategoryId { get; init; }

    /// <summary>
    /// Category name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Parent category ID (NULL for root categories).
    /// </summary>
    public int? ParentCategoryId { get; init; }

    /// <summary>
    /// Depth level in the hierarchy (0 = root, 1 = child, 2 = grandchild, etc.).
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// Human-readable path from root to this category (e.g., "Technology > Programming").
    /// </summary>
    public string HierarchyPath { get; init; } = string.Empty;

    /// <summary>
    /// Full path with slashes (e.g., "/Technology/Programming").
    /// </summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>
    /// Creates a CategoryHierarchy instance from database reader results.
    /// </summary>
    internal static CategoryHierarchy FromDatabase(
        int categoryId,
        string name,
        int? parentCategoryId,
        int level,
        string hierarchyPath,
        string fullPath)
    {
        return new CategoryHierarchy
        {
            CategoryId = categoryId,
            Name = name,
            ParentCategoryId = parentCategoryId,
            Level = level,
            HierarchyPath = hierarchyPath,
            FullPath = fullPath
        };
    }

    /// <summary>
    /// Returns a formatted summary of the category hierarchy.
    /// </summary>
    public override string ToString()
    {
        return $"{new string(' ', Level * 2)}{Name} (Level {Level})";
    }

    /// <summary>
    /// Returns a detailed multi-line description.
    /// </summary>
    public string ToDetailedString()
    {
        return $@"Category: {Name}
ID: {CategoryId}
Parent ID: {(ParentCategoryId.HasValue ? ParentCategoryId.Value.ToString() : "None (Root)")}
Level: {Level}
Hierarchy Path: {HierarchyPath}
Full Path: {FullPath}";
    }

    /// <summary>
    /// Indicates whether this is a root category (no parent).
    /// </summary>
    public bool IsRoot => !ParentCategoryId.HasValue;

    /// <summary>
    /// Returns the category indented for display based on its level.
    /// </summary>
    public string GetIndentedName(string indentChar = "  ")
    {
        return new string(' ', Level * indentChar.Length) + Name;
    }
}
