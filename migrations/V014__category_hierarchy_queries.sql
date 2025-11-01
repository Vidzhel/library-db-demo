-- =============================================
-- Migration: V014 - Category Hierarchy with Recursive CTE
-- Description: Creates table-valued function using recursive CTE to traverse category hierarchy
-- Author: DbDemo Project
-- Date: 2025-11-01
-- =============================================

-- This migration demonstrates:
-- 1. Recursive Common Table Expression (CTE) for hierarchical data
-- 2. Anchor member (base case) + recursive member pattern
-- 3. Building hierarchical paths
-- 4. Level calculation for tree depth
-- 5. MAXRECURSION to prevent infinite loops

SET NOCOUNT ON;
GO

-- =============================================
-- Function: fn_GetCategoryHierarchy
-- Purpose: Returns complete category hierarchy tree using recursive CTE
-- Parameters:
--   @RootCategoryId INT (optional) - If NULL, returns entire tree; otherwise subtree from this root
-- Returns: Table with hierarchy information (Id, Name, ParentId, Level, Path)
-- =============================================

IF OBJECT_ID('dbo.fn_GetCategoryHierarchy', 'IF') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.fn_GetCategoryHierarchy;
END
GO

CREATE FUNCTION dbo.fn_GetCategoryHierarchy
(
    @RootCategoryId INT  -- NULL = entire tree, specific ID = subtree
)
RETURNS TABLE
AS
RETURN
(
    WITH CategoryHierarchy AS
    (
        -- ANCHOR MEMBER: Start with root categories (or specific root if provided)
        SELECT
            C.Id AS CategoryId,
            C.Name,
            C.ParentCategoryId,
            0 AS Level,  -- Root level is 0
            CAST(C.Name AS NVARCHAR(1000)) AS HierarchyPath,  -- Start building path
            CAST('/' + C.Name AS NVARCHAR(1000)) AS FullPath  -- Full path with separators
        FROM Categories C
        WHERE
            -- If @RootCategoryId is NULL: select top-level categories (ParentCategoryId IS NULL)
            -- If @RootCategoryId is provided: select that specific category as root
            (@RootCategoryId IS NULL AND C.ParentCategoryId IS NULL)
            OR
            (C.Id = @RootCategoryId)

        UNION ALL

        -- RECURSIVE MEMBER: Join children with their parents from CTE
        SELECT
            C.Id AS CategoryId,
            C.Name,
            C.ParentCategoryId,
            CH.Level + 1 AS Level,  -- Increment level for each level down
            CAST(CH.HierarchyPath + ' > ' + C.Name AS NVARCHAR(1000)) AS HierarchyPath,  -- Append to path
            CAST(CH.FullPath + '/' + C.Name AS NVARCHAR(1000)) AS FullPath  -- Build full path
        FROM Categories C
        INNER JOIN CategoryHierarchy CH ON C.ParentCategoryId = CH.CategoryId
    )
    SELECT
        CategoryId,
        Name,
        ParentCategoryId,
        Level,
        HierarchyPath,
        FullPath
    FROM CategoryHierarchy
    -- NOTE: MAXRECURSION cannot be specified in inline TVF
    -- When querying this function, add OPTION (MAXRECURSION N) to the calling query if needed
);
GO

-- =============================================
-- Validation: Test the function
-- =============================================

-- You can test this function after migration with:

-- 1. Get entire category tree
-- SELECT * FROM dbo.fn_GetCategoryHierarchy(NULL)
-- ORDER BY FullPath;

-- 2. Get subtree from specific category (e.g., Technology)
-- DECLARE @TechCategoryId INT = (SELECT Id FROM Categories WHERE Name = 'Technology');
-- SELECT * FROM dbo.fn_GetCategoryHierarchy(@TechCategoryId)
-- ORDER BY Level, Name;

-- 3. Show categories by level
-- SELECT
--     Level,
--     COUNT(*) AS CategoryCount,
--     STRING_AGG(Name, ', ') AS Categories
-- FROM dbo.fn_GetCategoryHierarchy(NULL)
-- GROUP BY Level
-- ORDER BY Level;

PRINT 'Migration V014 completed successfully: Created fn_GetCategoryHierarchy function with recursive CTE';
GO
