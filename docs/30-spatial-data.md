## 30 - Spatial Data (GEOGRAPHY)

## 📖 What You'll Learn

- Storing geographic coordinates using SQL Server's GEOGRAPHY data type
- Calculating distances between locations using geodetic calculations
- Proximity searches (find nearby locations within a radius)
- Nearest neighbor queries (find N closest locations)
- Creating and using spatial indexes for performance
- Real-world use cases for location-based queries

## 🎯 Why This Matters

Spatial data types enable location-aware applications:

- **Accurate Distance Calculations**: Uses geodetic math (accounts for Earth's curvature)
- **Proximity Searches**: "Find all branches within 10km of my location"
- **Nearest Neighbor**: "Show me the 5 closest branches"
- **Spatial Indexing**: Optimized queries for geographic data
- **Standards-Based**: Uses Well-Known Text (WKT) and SRID 4326 (WGS 84)

**Real-World Use Cases:**
- Store locator ("Find nearest branch")
- Delivery radius calculations
- Service area definitions
- Geofencing and location tracking
- Distance-based pricing

## 🔍 Key Concepts

### GEOGRAPHY vs GEOMETRY

| Type | Use Case | Coordinates | Distance Calculation |
|------|----------|-------------|---------------------|
| **GEOGRAPHY** | Real-world locations (lat/lon) | Latitude/Longitude | Geodetic (accurate) |
| **GEOMETRY** | Flat surfaces, CAD, maps | X/Y coordinates | Planar (Euclidean) |

**We use GEOGRAPHY** because we're storing real-world library locations.

### Creating a GEOGRAPHY Point

```sql
-- Point(latitude, longitude, SRID)
-- SRID 4326 = WGS 84 (standard GPS coordinate system)
geography::Point(48.2082, 16.3738, 4326)  -- Vienna, Austria
```

### Distance Calculations

```sql
-- STDistance returns meters
DECLARE @Vienna GEOGRAPHY = geography::Point(48.2082, 16.3738, 4326);
DECLARE @Graz GEOGRAPHY = geography::Point(47.0707, 15.4395, 4326);

SELECT @Vienna.STDistance(@Graz) / 1000.0 AS DistanceKm;  -- ~150 km
```

## 🎯 Our Implementation

### 1. LibraryBranches Table with GEOGRAPHY Column

```sql
CREATE TABLE LibraryBranches (
    Id INT PRIMARY KEY IDENTITY,
    BranchName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(200) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Location GEOGRAPHY NULL,  -- Stores lat/lon
    -- ... other fields
);
```

### 2. Spatial Index for Performance

```sql
CREATE SPATIAL INDEX IX_LibraryBranches_Location
ON LibraryBranches(Location)
USING GEOGRAPHY_AUTO_GRID
WITH (CELLS_PER_OBJECT = 16);
```

**Benefits:**
- Dramatically speeds up proximity queries
- Essential for large datasets
- Auto grid adapts to data distribution

### 3. Proximity Search (Within Radius)

```sql
CREATE PROCEDURE sp_FindBranchesWithinDistance
    @Latitude FLOAT,
    @Longitude FLOAT,
    @RadiusKm FLOAT
AS
BEGIN
    DECLARE @UserLocation GEOGRAPHY = geography::Point(@Latitude, @Longitude, 4326);

    SELECT BranchName, City,
           @UserLocation.STDistance(Location) / 1000.0 AS DistanceKm
    FROM LibraryBranches
    WHERE @UserLocation.STDistance(Location) / 1000.0 <= @RadiusKm
    ORDER BY DistanceKm;
END
```

**Usage:**
```sql
-- Find all branches within 50km of Vienna city center
EXEC sp_FindBranchesWithinDistance
    @Latitude = 48.2082,
    @Longitude = 16.3738,
    @RadiusKm = 50;
```

### 4. Nearest Neighbor Search

```sql
CREATE PROCEDURE sp_FindNearestBranches
    @Latitude FLOAT,
    @Longitude FLOAT,
    @TopN INT = 5
AS
BEGIN
    DECLARE @UserLocation GEOGRAPHY = geography::Point(@Latitude, @Longitude, 4326);

    SELECT TOP (@TopN)
        BranchName, City,
        @UserLocation.STDistance(Location) / 1000.0 AS DistanceKm
    FROM LibraryBranches
    WHERE Location IS NOT NULL
    ORDER BY @UserLocation.STDistance(Location);
END
```

**Usage:**
```sql
-- Find 3 nearest branches
EXEC sp_FindNearestBranches
    @Latitude = 48.2082,
    @Longitude = 16.3738,
    @TopN = 3;
```

### 5. Distance Between Two Branches

```sql
CREATE FUNCTION fn_CalculateDistanceBetweenBranches
(
    @BranchId1 INT,
    @BranchId2 INT
)
RETURNS FLOAT
AS
BEGIN
    DECLARE @Distance FLOAT;

    SELECT @Distance = b1.Location.STDistance(b2.Location) / 1000.0
    FROM LibraryBranches b1
    CROSS JOIN LibraryBranches b2
    WHERE b1.Id = @BranchId1 AND b2.Id = @BranchId2;

    RETURN @Distance;
END
```

## ✅ Best Practices

### 1. Always Use SRID 4326 for GPS Coordinates

```sql
-- GOOD: Standard GPS coordinate system
geography::Point(48.2082, 16.3738, 4326)

-- BAD: Wrong SRID or omitting it
geography::Point(48.2082, 16.3738, 0)  -- Invalid!
```

### 2. Validate Coordinates

```csharp
public void SetLocation(double latitude, double longitude)
{
    if (latitude < -90 || latitude > 90)
        throw new ArgumentOutOfRangeException(nameof(latitude));
    if (longitude < -180 || longitude > 180)
        throw new ArgumentOutOfRangeException(nameof(longitude));

    Latitude = latitude;
    Longitude = longitude;
}
```

### 3. Convert Meters to Kilometers

```sql
-- STDistance returns meters - convert for readability
@location1.STDistance(@location2) / 1000.0 AS DistanceKm
```

### 4. Use Spatial Indexes

```sql
-- Always create spatial indexes for large datasets
CREATE SPATIAL INDEX IX_Table_Location
ON Table(Location)
USING GEOGRAPHY_AUTO_GRID;
```

## 🧪 Testing

Our tests verify:
1. ✅ Storing and retrieving geographic coordinates
2. ✅ Proximity searches within a radius
3. ✅ Nearest neighbor queries
4. ✅ Distance calculations are accurate
5. ✅ Spatial indexes improve query performance

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~SpatialDataTests"
```

## 🔍 C# Implementation

```csharp
// Create branch with location
var branch = new LibraryBranch("Vienna Central", "Address", "Vienna");
branch.SetLocation(48.2082, 16.3738);
await repository.CreateAsync(branch, transaction);

// Find nearby branches
var nearby = await repository.FindWithinDistanceAsync(
    latitude: 48.2082,
    longitude: 16.3738,
    radiusKm: 10,
    transaction);

// Find 5 nearest
var nearest = await repository.FindNearestAsync(
    latitude: 48.2082,
    longitude: 16.3738,
    topN: 5,
    transaction);
```

## 🔗 Learn More

- [Spatial Data (SQL Server)](https://learn.microsoft.com/en-us/sql/relational-databases/spatial/spatial-data-sql-server) - Microsoft Docs
- [GEOGRAPHY Data Type](https://learn.microsoft.com/en-us/sql/t-sql/spatial-geography/spatial-types-geography) - Reference
- [Spatial Indexes](https://learn.microsoft.com/en-us/sql/relational-databases/spatial/spatial-indexes-overview) - Performance tuning

## ❓ Discussion Questions

1. When would you use GEOGRAPHY vs GEOMETRY?
2. How does spatial indexing improve query performance?
3. What are the limitations of storing location data in a database vs using a dedicated GIS system?
4. How would you handle timezone considerations with location data?

---

**Key Takeaway:** SQL Server's GEOGRAPHY type provides powerful location-based querying with accurate distance calculations. Use spatial indexes for performance and always validate coordinate ranges.
