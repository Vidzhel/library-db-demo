-- ============================================================================
-- Migration: V020 - Spatial Data Support
-- Description: Demonstrates SQL Server spatial data types (GEOGRAPHY) for
--              storing and querying location data. Includes distance calculations,
--              proximity searches, and nearest neighbor queries.
-- ============================================================================

PRINT 'Starting V020 migration - Spatial Data...';
GO

-- ======================
-- 1. Create LibraryBranches Table with Spatial Column
-- ======================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LibraryBranches')
BEGIN
    CREATE TABLE dbo.LibraryBranches (
        Id INT PRIMARY KEY IDENTITY(1,1),
        BranchName NVARCHAR(100) NOT NULL,
        Address NVARCHAR(200) NOT NULL,
        City NVARCHAR(100) NOT NULL,
        PostalCode NVARCHAR(20) NULL,
        PhoneNumber NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,

        -- Spatial column: stores location as latitude/longitude
        Location GEOGRAPHY NULL,

        -- Standard audit columns
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsDeleted BIT NOT NULL DEFAULT 0,

        -- Constraints
        CONSTRAINT CK_LibraryBranches_Email CHECK (Email LIKE '%@%.%')
    );

    PRINT 'Created LibraryBranches table with GEOGRAPHY column';
END
ELSE
BEGIN
    PRINT 'LibraryBranches table already exists';
END
GO

-- ======================
-- 2. Create Spatial Index on Location Column
-- ======================

-- Spatial indexes for GEOGRAPHY type (uses automatic grid)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_LibraryBranches_Location'
)
BEGIN
    CREATE SPATIAL INDEX IX_LibraryBranches_Location
    ON dbo.LibraryBranches(Location)
    USING GEOGRAPHY_AUTO_GRID
    WITH (CELLS_PER_OBJECT = 16);

    PRINT 'Created spatial index IX_LibraryBranches_Location';
END
ELSE
BEGIN
    PRINT 'Spatial index IX_LibraryBranches_Location already exists';
END
GO

-- ======================
-- 3. Insert Sample Library Branches
-- ======================

-- Only insert if table is empty
IF NOT EXISTS (SELECT 1 FROM dbo.LibraryBranches WHERE IsDeleted = 0)
BEGIN
    -- Sample library branches in Vienna, Austria and surrounding cities
    INSERT INTO dbo.LibraryBranches (BranchName, Address, City, PostalCode, PhoneNumber, Email, Location)
    VALUES
        -- Vienna branches
        ('Vienna Central Library', 'Urban-Loritz-Platz 2a', 'Vienna', '1070', '+43-1-4000-84500', 'central@wienbibliothek.at',
         geography::Point(48.2082, 16.3738, 4326)),  -- Vienna City Center

        ('Vienna University Library', 'Universitätsring 1', 'Vienna', '1010', '+43-1-4277-15100', 'info@ub.univie.ac.at',
         geography::Point(48.2108, 16.3608, 4326)),  -- University of Vienna

        ('Donaustadt Branch', 'Bernoullistraße 1', 'Vienna', '1220', '+43-1-4000-22100', 'donaustadt@wienbibliothek.at',
         geography::Point(48.2319, 16.4440, 4326)),  -- Donaustadt district

        ('Favoriten Branch', 'Laxenburger Straße 90', 'Vienna', '1100', '+43-1-4000-10100', 'favoriten@wienbibliothek.at',
         geography::Point(48.1580, 16.3728, 4326)),  -- Favoriten district

        -- Nearby cities
        ('Graz City Library', 'Lauzilgasse 21', 'Graz', '8010', '+43-316-872-4969', 'info@stadtbibliothek.graz.at',
         geography::Point(47.0707, 15.4395, 4326)),  -- Graz (about 200km from Vienna)

        ('Linz Public Library', 'Wissensturm Kärntnerstraße 26', 'Linz', '4020', '+43-732-7070-0', 'info@stadtbibliothek.linz.at',
         geography::Point(48.3066, 14.2855, 4326)),  -- Linz (about 180km from Vienna)

        ('Salzburg City Library', 'Schumacherstraße 14', 'Salzburg', '5020', '+43-662-8044-73700', 'stadtbibliothek@stadt-salzburg.at',
         geography::Point(47.8095, 13.0550, 4326)),  -- Salzburg (about 300km from Vienna)

        ('Innsbruck University Library', 'Innrain 50', 'Innsbruck', '6020', '+43-512-507-25301', 'info@uibk.ac.at',
         geography::Point(47.2654, 11.3955, 4326));  -- Innsbruck (about 480km from Vienna)

    PRINT 'Inserted 8 sample library branches';
END
ELSE
BEGIN
    PRINT 'Sample data already exists in LibraryBranches';
END
GO

-- ======================
-- 4. Stored Procedure - Find Branches Within Distance
-- ======================

IF OBJECT_ID('dbo.sp_FindBranchesWithinDistance', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_FindBranchesWithinDistance;
GO

CREATE PROCEDURE dbo.sp_FindBranchesWithinDistance
    @Latitude FLOAT,
    @Longitude FLOAT,
    @RadiusKm FLOAT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserLocation GEOGRAPHY = geography::Point(@Latitude, @Longitude, 4326);

    SELECT
        Id,
        BranchName,
        Address,
        City,
        PostalCode,
        PhoneNumber,
        Email,
        -- Calculate distance in kilometers
        @UserLocation.STDistance(Location) / 1000.0 AS DistanceKm
    FROM dbo.LibraryBranches
    WHERE IsDeleted = 0
        AND Location IS NOT NULL
        AND @UserLocation.STDistance(Location) / 1000.0 <= @RadiusKm
    ORDER BY DistanceKm;
END
GO

PRINT 'Created stored procedure sp_FindBranchesWithinDistance';
GO

-- ======================
-- 5. Stored Procedure - Find Nearest Branches
-- ======================

IF OBJECT_ID('dbo.sp_FindNearestBranches', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_FindNearestBranches;
GO

CREATE PROCEDURE dbo.sp_FindNearestBranches
    @Latitude FLOAT,
    @Longitude FLOAT,
    @TopN INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserLocation GEOGRAPHY = geography::Point(@Latitude, @Longitude, 4326);

    SELECT TOP (@TopN)
        Id,
        BranchName,
        Address,
        City,
        PostalCode,
        PhoneNumber,
        Email,
        Location.Lat AS Latitude,
        Location.Long AS Longitude,
        -- Calculate distance in kilometers
        @UserLocation.STDistance(Location) / 1000.0 AS DistanceKm
    FROM dbo.LibraryBranches
    WHERE IsDeleted = 0
        AND Location IS NOT NULL
    ORDER BY @UserLocation.STDistance(Location);
END
GO

PRINT 'Created stored procedure sp_FindNearestBranches';
GO

-- ======================
-- 6. Function - Calculate Distance Between Branches
-- ======================

IF OBJECT_ID('dbo.fn_CalculateDistanceBetweenBranches', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_CalculateDistanceBetweenBranches;
GO

CREATE FUNCTION dbo.fn_CalculateDistanceBetweenBranches
(
    @BranchId1 INT,
    @BranchId2 INT
)
RETURNS FLOAT
AS
BEGIN
    DECLARE @Distance FLOAT;

    SELECT @Distance =
        b1.Location.STDistance(b2.Location) / 1000.0  -- Convert meters to kilometers
    FROM dbo.LibraryBranches b1
    CROSS JOIN dbo.LibraryBranches b2
    WHERE b1.Id = @BranchId1
        AND b2.Id = @BranchId2
        AND b1.Location IS NOT NULL
        AND b2.Location IS NOT NULL;

    RETURN @Distance;
END
GO

PRINT 'Created function fn_CalculateDistanceBetweenBranches';
GO

-- ======================
-- 7. View - Branch Distances Matrix
-- ======================

IF OBJECT_ID('dbo.vw_BranchDistances', 'V') IS NOT NULL
    DROP VIEW dbo.vw_BranchDistances;
GO

CREATE VIEW dbo.vw_BranchDistances
AS
    SELECT
        b1.Id AS FromBranchId,
        b1.BranchName AS FromBranchName,
        b1.City AS FromCity,
        b2.Id AS ToBranchId,
        b2.BranchName AS ToBranchName,
        b2.City AS ToCity,
        b1.Location.STDistance(b2.Location) / 1000.0 AS DistanceKm
    FROM dbo.LibraryBranches b1
    CROSS JOIN dbo.LibraryBranches b2
    WHERE b1.Id <> b2.Id
        AND b1.IsDeleted = 0
        AND b2.IsDeleted = 0
        AND b1.Location IS NOT NULL
        AND b2.Location IS NOT NULL;
GO

PRINT 'Created view vw_BranchDistances';
GO

-- ======================
-- 8. Verification Tests
-- ======================

PRINT '';
PRINT 'Running verification tests...';
PRINT '';

-- Test 1: Verify all branches have locations
PRINT 'Test 1: Branches with location data:';
SELECT
    BranchName,
    City,
    Location.Lat AS Latitude,
    Location.Long AS Longitude
FROM dbo.LibraryBranches
WHERE Location IS NOT NULL
ORDER BY BranchName;
GO

-- Test 2: Find branches within 50km of Vienna city center
PRINT '';
PRINT 'Test 2: Branches within 50km of Vienna city center (48.2082, 16.3738):';
EXEC dbo.sp_FindBranchesWithinDistance
    @Latitude = 48.2082,
    @Longitude = 16.3738,
    @RadiusKm = 50;
GO

-- Test 3: Find 3 nearest branches to Graz
PRINT '';
PRINT 'Test 3: 3 nearest branches to Graz (47.0707, 15.4395):';
EXEC dbo.sp_FindNearestBranches
    @Latitude = 47.0707,
    @Longitude = 15.4395,
    @TopN = 3;
GO

-- Test 4: Calculate distance between Vienna Central and Graz
PRINT '';
PRINT 'Test 4: Distance between Vienna Central Library and Graz City Library:';
SELECT
    dbo.fn_CalculateDistanceBetweenBranches(1, 5) AS DistanceKm;
GO

-- Test 5: Show distance matrix for Vienna branches
PRINT '';
PRINT 'Test 5: Distance matrix for Vienna branches:';
SELECT
    FromBranchName,
    ToBranchName,
    ROUND(DistanceKm, 2) AS DistanceKm
FROM dbo.vw_BranchDistances
WHERE FromCity = 'Vienna' AND ToCity = 'Vienna'
ORDER BY FromBranchName, DistanceKm;
GO

PRINT '';
PRINT '================================================================';
PRINT 'V020 migration completed successfully!';
PRINT 'Spatial data features have been added:';
PRINT '  - LibraryBranches table with GEOGRAPHY column';
PRINT '  - Spatial index for optimized location queries';
PRINT '  - sp_FindBranchesWithinDistance - proximity search';
PRINT '  - sp_FindNearestBranches - nearest neighbor search';
PRINT '  - fn_CalculateDistanceBetweenBranches - distance function';
PRINT '  - vw_BranchDistances - distance matrix view';
PRINT '';
PRINT 'Key Benefits:';
PRINT '  ✓ Store precise geographic locations (latitude/longitude)';
PRINT '  ✓ Calculate accurate distances using geodetic calculations';
PRINT '  ✓ Perform proximity searches (find nearby branches)';
PRINT '  ✓ Spatial indexing for optimized queries';
PRINT '================================================================';
GO
