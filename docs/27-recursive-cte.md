# 24 - Recursive CTEs for Hierarchical Data

## 📖 What You'll Learn

- Recursive Common Table Expressions (CTEs) for tree structures
- Anchor member + recursive member pattern
- Building hierarchical paths and calculating tree depth
- Preventing infinite loops with MAXRECURSION
- Querying organizational charts, category trees, bill of materials

## 🎯 Why This Matters

Hierarchical data is everywhere:
- **Category Trees**: Product categories, file systems
- **Org Charts**: Employee reporting structures
- **Thread Conversations**: Nested comments/replies
- **Bill of Materials**: Assembly hierarchies

Recursive CTEs traverse these structures in a single query without loops or cursors.

## 🔍 Key Concepts

### What is a Recursive CTE?

A **Common Table Expression (CTE)** is a temporary named result set. A **recursive CTE** references itself to traverse hierarchical data:

```sql
WITH RecursiveCTE AS
(
    -- ANCHOR MEMBER: Base case (starting point)
    SELECT Id, Name, ParentId, 0 AS Level
    FROM Table
    WHERE ParentId IS NULL  -- Top level

    UNION ALL

    -- RECURSIVE MEMBER: Joins CTE with itself
    SELECT T.Id, T.Name, T.ParentId, CTE.Level + 1
    FROM Table T
    INNER JOIN RecursiveCTE CTE ON T.ParentId = CTE.Id
)
SELECT * FROM RecursiveCTE;
```

**How it works:**
1. Execute anchor member → get root nodes
2. Execute recursive member using anchor results
3. Execute recursive member using step 2 results
4. Repeat until no new rows (termination condition)
5. UNION ALL combines all iterations

### Our Implementation: Category Hierarchy

Migration V014 creates `fn_GetCategoryHierarchy` using recursive CTE:

```sql
CREATE FUNCTION dbo.fn_GetCategoryHierarchy(@RootCategoryId INT)
RETURNS TABLE
AS
RETURN
(
    WITH CategoryHierarchy AS
    (
        -- ANCHOR: Start with root(s)
        SELECT
            C.Id AS CategoryId,
            C.Name,
            C.ParentCategoryId,
            0 AS Level,
            CAST(C.Name AS NVARCHAR(1000)) AS HierarchyPath,
            CAST('/' + C.Name AS NVARCHAR(1000)) AS FullPath
        FROM Categories C
        WHERE
            -- If NULL: get all roots, otherwise specific root
            (@RootCategoryId IS NULL AND C.ParentCategoryId IS NULL)
            OR (C.Id = @RootCategoryId)

        UNION ALL

        -- RECURSIVE: Get children of current level
        SELECT
            C.Id,
            C.Name,
            C.ParentCategoryId,
            CH.Level + 1,  -- Increment depth
            CAST(CH.HierarchyPath + ' > ' + C.Name AS NVARCHAR(1000)),
            CAST(CH.FullPath + '/' + C.Name AS NVARCHAR(1000))
        FROM Categories C
        INNER JOIN CategoryHierarchy CH ON C.ParentCategoryId = CH.CategoryId
    )
    SELECT CategoryId, Name, ParentCategoryId, Level, HierarchyPath, FullPath
    FROM CategoryHierarchy
);
```

**Key Features:**
- **Flexible Root**: `@RootCategoryId = NULL` returns entire forest, specific ID returns subtree
- **Level Tracking**: Starts at 0, increments with each level
- **Path Building**: Constructs human-readable paths (e.g., "Technology > Programming")
- **FullPath**: Slash-separated paths (e.g., "/Technology/Programming")

### Anchor vs Recursive Member

| Component | Purpose | Example |
|-----------|---------|---------|
| **Anchor** | Starting point (base case) | `WHERE ParentId IS NULL` |
| **Recursive** | How to get next level | `JOIN CTE ON Parent = CTE.Id` |
| **Termination** | When recursion stops | No more children found |

### Preventing Infinite Loops

Circular references cause infinite recursion:

```sql
-- BAD: Category 1 → Category 2 → Category 1 (loop!)
```

**Protection:**
1. **Data Integrity**: Use FK constraints, prevent circular references
2. **MAXRECURSION**: Limit recursion depth (default = 100)

```sql
-- Apply MAXRECURSION when calling the function
SELECT * FROM dbo.fn_GetCategoryHierarchy(NULL)
OPTION (MAXRECURSION 32);
```

**Note:** Inline TVFs cannot specify OPTION internally - add it to calling query if needed.

### Querying from C#

```csharp
public async Task<List<CategoryHierarchy>> GetHierarchyAsync(
    int? rootCategoryId,
    SqlTransaction transaction,
    CancellationToken cancellationToken = default)
{
    const string sql = @"
        SELECT CategoryId, Name, ParentCategoryId, Level, HierarchyPath, FullPath
        FROM dbo.fn_GetCategoryHierarchy(@RootCategoryId)
        ORDER BY FullPath;";

    var connection = transaction.Connection;
    await using var command = new SqlCommand(sql, connection, transaction);
    command.Parameters.Add("@RootCategoryId", SqlDbType.Int).Value =
        (object?)rootCategoryId ?? DBNull.Value;

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    var hierarchy = new List<CategoryHierarchy>();
    while (await reader.ReadAsync(cancellationToken))
    {
        hierarchy.Add(MapReaderToCategoryHierarchy(reader));
    }

    return hierarchy;
}
```

## 🎯 Practical Use Cases

### 1. Display Entire Tree

```csharp
var tree = await categoryRepository.GetHierarchyAsync(null, tx);
foreach (var category in tree)
{
    Console.WriteLine(category.GetIndentedName());
    // Output:
    // Technology
    //   Programming
    //     C#
    //     Python
}
```

### 2. Get Subtree

```sql
-- Get all subcategories under "Technology"
DECLARE @TechId INT = (SELECT Id FROM Categories WHERE Name = 'Technology');
SELECT * FROM dbo.fn_GetCategoryHierarchy(@TechId);
```

### 3. Find Leaf Nodes

```sql
-- Categories with no children
SELECT c.*
FROM dbo.fn_GetCategoryHierarchy(NULL) c
WHERE NOT EXISTS (
    SELECT 1 FROM Categories WHERE ParentCategoryId = c.CategoryId
);
```

### 4. Calculate Tree Statistics

```sql
-- Count categories by depth level
SELECT
    Level,
    COUNT(*) AS CategoryCount,
    STRING_AGG(Name, ', ') AS Categories
FROM dbo.fn_GetCategoryHierarchy(NULL)
GROUP BY Level
ORDER BY Level;
```

## ⚠️ Common Pitfalls

### ❌ Don't: Forget UNION ALL

```sql
-- WRONG: UNION removes duplicates (slow, incorrect for recursion)
SELECT ... UNION SELECT ...

-- CORRECT: UNION ALL keeps all rows
SELECT ... UNION ALL SELECT ...
```

### ❌ Don't: Create Circular References

```sql
-- BAD: Will cause infinite loop
UPDATE Categories SET ParentCategoryId = 5 WHERE Id = 1;
UPDATE Categories SET ParentCategoryId = 1 WHERE Id = 5;
```

### ❌ Don't: Use SELECT * in Recursive Member

```sql
-- BAD: Column mismatch between anchor and recursive
WITH CTE AS (
    SELECT Id, Name FROM Table  -- 2 columns
    UNION ALL
    SELECT * FROM Table  -- Different column count!
)
```

### ✅ Do: Match Column Count and Types

Both anchor and recursive members must have:
- Same number of columns
- Compatible data types
- Same column order

## ✅ Best Practices

1. **Index Parent Columns** - `CREATE INDEX IX_Categories_ParentId ON Categories(ParentCategoryId)`
2. **Use CAST for Paths** - Prevent truncation with explicit sizes
3. **Add Level Limit** - Reasonable MAXRECURSION for your domain
4. **Validate Data** - Check constraints to prevent cycles
5. **Order Results** - Use FullPath or Level for meaningful order
6. **Consider Materialized Path** - For very deep/wide trees, store paths directly

## 🧪 Testing This Feature

Our tests (`CategoryHierarchyTests.cs`) verify:
1. **Entire tree traversal** - Returns all nodes
2. **Level calculation** - Correct depth from root
3. **Path construction** - Hierarchical and full paths
4. **Subtree queries** - Filtering from specific root
5. **Leaf nodes** - Categories with no children
6. **Empty database** - Handles no data gracefully

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~CategoryHierarchyTests"
```

## 🔗 Learn More

- [Recursive CTEs (SQL Server)](https://learn.microsoft.com/en-us/sql/t-sql/queries/with-common-table-expression-transact-sql)
- [Hierarchical Data Models](https://www.sqlshack.com/implement-hierarchical-relationships-sql-server/)
- [Performance Tips](https://www.red-gate.com/simple-talk/databases/sql-server/t-sql-programming-sql-server/sql-server-cte-basics/)

## ❓ Discussion Questions

1. When would you use recursive CTE vs adjacency list with loops?
2. How does MAXRECURSION protect against infinite loops?
3. What are the performance implications of deep hierarchies?
4. When would you use materialized path instead of recursive CTE?

## 💡 Try It Yourself

### Exercise 1: Depth Limit
Modify the function to accept a `@MaxDepth` parameter and stop recursion at that level.

### Exercise 2: Breadth Calculation
Add a column that counts the number of children for each node.

### Exercise 3: Ancestor Path
Create a query that returns all ancestors of a given category.

### Exercise 4: Employee Org Chart
Adapt the recursive CTE for an employee table with ManagerId.

---

**Key Takeaway:** Recursive CTEs elegantly traverse hierarchical data using the anchor + recursive pattern, enabling tree queries without procedural loops.
