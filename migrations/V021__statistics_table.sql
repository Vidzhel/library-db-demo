-- ============================================================================
-- Migration: V021 - Statistics Table with Time-Series Analytics
-- Description: Creates SystemStatistics table for tracking domain and
--              infrastructure metrics with minute-level granularity.
--              Includes views for time-window aggregations and stored
--              procedures for advanced analytics (moving averages,
--              percentiles, anomaly detection, trend analysis).
-- ============================================================================

PRINT 'Starting V021 migration - Statistics Table with Time-Series Analytics';
GO

-- ============================================================================
-- 1. Create SystemStatistics Table
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemStatistics')
BEGIN
    CREATE TABLE SystemStatistics (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        RecordedAt DATETIME2 NOT NULL,

        -- Domain Metrics (Library Business Metrics)
        ActiveLoansCount INT NULL,
        NewLoansCount INT NULL,
        ReturnedLoansCount INT NULL,
        ActiveMembersCount INT NULL,
        OverdueLoansCount INT NULL,
        TotalBooksAvailable INT NULL,

        -- Infrastructure Metrics (System Health)
        DatabaseSizeMB DECIMAL(10,2) NULL,
        ActiveConnectionsCount INT NULL,
        AvgQueryTimeMs DECIMAL(10,2) NULL,
        CPUUsagePercent DECIMAL(5,2) NULL,
        MemoryUsagePercent DECIMAL(5,2) NULL,

        -- Metadata
        ServerName NVARCHAR(100) NULL,
        Notes NVARCHAR(MAX) NULL,

        -- Constraints
        CONSTRAINT CK_SystemStatistics_RecordedAt CHECK (RecordedAt <= GETUTCDATE()),
        CONSTRAINT CK_SystemStatistics_CPUUsage CHECK (CPUUsagePercent >= 0 AND CPUUsagePercent <= 100),
        CONSTRAINT CK_SystemStatistics_MemoryUsage CHECK (MemoryUsagePercent >= 0 AND MemoryUsagePercent <= 100)
    );

    PRINT 'Created SystemStatistics table';
END
ELSE
BEGIN
    PRINT 'SystemStatistics table already exists';
END
GO

-- ============================================================================
-- 2. Create Indexes for Time-Series Queries
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemStatistics_RecordedAt' AND object_id = OBJECT_ID('SystemStatistics'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SystemStatistics_RecordedAt
    ON SystemStatistics(RecordedAt DESC)
    INCLUDE (ActiveLoansCount, NewLoansCount, CPUUsagePercent, MemoryUsagePercent);

    PRINT 'Created index IX_SystemStatistics_RecordedAt';
END
GO

-- ============================================================================
-- 3. Create View for Hourly Aggregations
-- ============================================================================

IF OBJECT_ID('vw_HourlyStatistics', 'V') IS NOT NULL
    DROP VIEW vw_HourlyStatistics;
GO

CREATE VIEW vw_HourlyStatistics
AS
SELECT
    DATEADD(HOUR, DATEDIFF(HOUR, 0, RecordedAt), 0) AS HourBucket,
    COUNT(*) AS SampleCount,

    -- Domain Metrics - Averages
    AVG(CAST(ActiveLoansCount AS FLOAT)) AS AvgActiveLoans,
    AVG(CAST(NewLoansCount AS FLOAT)) AS AvgNewLoans,
    AVG(CAST(ReturnedLoansCount AS FLOAT)) AS AvgReturnedLoans,
    AVG(CAST(ActiveMembersCount AS FLOAT)) AS AvgActiveMembers,
    AVG(CAST(OverdueLoansCount AS FLOAT)) AS AvgOverdueLoans,
    AVG(CAST(TotalBooksAvailable AS FLOAT)) AS AvgBooksAvailable,

    -- Domain Metrics - Min/Max
    MIN(ActiveLoansCount) AS MinActiveLoans,
    MAX(ActiveLoansCount) AS MaxActiveLoans,

    -- Infrastructure Metrics - Averages
    AVG(DatabaseSizeMB) AS AvgDatabaseSizeMB,
    AVG(ActiveConnectionsCount) AS AvgActiveConnections,
    AVG(AvgQueryTimeMs) AS AvgQueryTimeMs,
    AVG(CPUUsagePercent) AS AvgCPUUsage,
    AVG(MemoryUsagePercent) AS AvgMemoryUsage,

    -- Infrastructure Metrics - Min/Max
    MIN(CPUUsagePercent) AS MinCPUUsage,
    MAX(CPUUsagePercent) AS MaxCPUUsage,
    MIN(MemoryUsagePercent) AS MinMemoryUsage,
    MAX(MemoryUsagePercent) AS MaxMemoryUsage
FROM SystemStatistics
GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, RecordedAt), 0);
GO

PRINT 'Created view vw_HourlyStatistics';
GO

-- ============================================================================
-- 4. Create View for Daily Aggregations
-- ============================================================================

IF OBJECT_ID('vw_DailyStatistics', 'V') IS NOT NULL
    DROP VIEW vw_DailyStatistics;
GO

CREATE VIEW vw_DailyStatistics
AS
SELECT
    CAST(RecordedAt AS DATE) AS DayDate,
    COUNT(*) AS SampleCount,

    -- Domain Metrics
    AVG(CAST(ActiveLoansCount AS FLOAT)) AS AvgActiveLoans,
    MIN(ActiveLoansCount) AS MinActiveLoans,
    MAX(ActiveLoansCount) AS MaxActiveLoans,

    SUM(NewLoansCount) AS TotalNewLoans,
    SUM(ReturnedLoansCount) AS TotalReturnedLoans,

    AVG(CAST(ActiveMembersCount AS FLOAT)) AS AvgActiveMembers,
    AVG(CAST(OverdueLoansCount AS FLOAT)) AS AvgOverdueLoans,
    AVG(CAST(TotalBooksAvailable AS FLOAT)) AS AvgBooksAvailable,

    -- Infrastructure Metrics
    AVG(DatabaseSizeMB) AS AvgDatabaseSizeMB,
    AVG(ActiveConnectionsCount) AS AvgActiveConnections,
    AVG(AvgQueryTimeMs) AS AvgQueryTimeMs,
    AVG(CPUUsagePercent) AS AvgCPUUsage,
    AVG(MemoryUsagePercent) AS AvgMemoryUsage,

    MIN(CPUUsagePercent) AS MinCPUUsage,
    MAX(CPUUsagePercent) AS MaxCPUUsage,
    STDEV(CPUUsagePercent) AS StdDevCPUUsage,

    MIN(MemoryUsagePercent) AS MinMemoryUsage,
    MAX(MemoryUsagePercent) AS MaxMemoryUsage,
    STDEV(MemoryUsagePercent) AS StdDevMemoryUsage
FROM SystemStatistics
GROUP BY CAST(RecordedAt AS DATE);
GO

PRINT 'Created view vw_DailyStatistics';
GO

-- ============================================================================
-- 5. Create Stored Procedure for Moving Averages
-- ============================================================================

IF OBJECT_ID('sp_GetMovingAverages', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetMovingAverages;
GO

CREATE PROCEDURE sp_GetMovingAverages
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @WindowSize INT = 7  -- Default 7-period moving average
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate the preceding rows count
    DECLARE @PrecedingRows INT = @WindowSize - 1;

    -- Use dynamic SQL to work around ROWS BETWEEN limitation
    DECLARE @SQL NVARCHAR(MAX) = N'
    SELECT
        RecordedAt,
        ActiveLoansCount,
        CPUUsagePercent,
        MemoryUsagePercent,

        -- Moving Averages using Window Functions
        AVG(CAST(ActiveLoansCount AS FLOAT)) OVER (
            ORDER BY RecordedAt
            ROWS BETWEEN ' + CAST(@PrecedingRows AS NVARCHAR(10)) + N' PRECEDING AND CURRENT ROW
        ) AS MovingAvgActiveLoans,

        AVG(CPUUsagePercent) OVER (
            ORDER BY RecordedAt
            ROWS BETWEEN ' + CAST(@PrecedingRows AS NVARCHAR(10)) + N' PRECEDING AND CURRENT ROW
        ) AS MovingAvgCPU,

        AVG(MemoryUsagePercent) OVER (
            ORDER BY RecordedAt
            ROWS BETWEEN ' + CAST(@PrecedingRows AS NVARCHAR(10)) + N' PRECEDING AND CURRENT ROW
        ) AS MovingAvgMemory,

        -- Running Totals
        SUM(NewLoansCount) OVER (
            ORDER BY RecordedAt
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS RunningTotalNewLoans,

        -- Row Number for reference
        ROW_NUMBER() OVER (ORDER BY RecordedAt) AS RowNum

    FROM SystemStatistics
    WHERE RecordedAt BETWEEN @StartDate AND @EndDate
    ORDER BY RecordedAt';

    EXEC sp_executesql @SQL,
        N'@StartDate DATETIME2, @EndDate DATETIME2',
        @StartDate = @StartDate,
        @EndDate = @EndDate;
END
GO

PRINT 'Created stored procedure sp_GetMovingAverages';
GO

-- ============================================================================
-- 6. Create Stored Procedure for Percentile Analysis
-- ============================================================================

IF OBJECT_ID('sp_GetPercentiles', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPercentiles;
GO

CREATE PROCEDURE sp_GetPercentiles
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    -- CPU Usage Percentiles
    SELECT
        'CPUUsagePercent' AS MetricName,
        MIN(CPUUsagePercent) AS MinValue,
        (SELECT DISTINCT PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY CPUUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND CPUUsagePercent IS NOT NULL) AS P25,
        (SELECT DISTINCT PERCENTILE_CONT(0.50) WITHIN GROUP (ORDER BY CPUUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND CPUUsagePercent IS NOT NULL) AS P50_Median,
        (SELECT DISTINCT PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY CPUUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND CPUUsagePercent IS NOT NULL) AS P75,
        (SELECT DISTINCT PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY CPUUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND CPUUsagePercent IS NOT NULL) AS P95,
        (SELECT DISTINCT PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY CPUUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND CPUUsagePercent IS NOT NULL) AS P99,
        MAX(CPUUsagePercent) AS MaxValue,
        AVG(CPUUsagePercent) AS AvgValue,
        STDEV(CPUUsagePercent) AS StdDev
    FROM SystemStatistics
    WHERE RecordedAt BETWEEN @StartDate AND @EndDate
        AND CPUUsagePercent IS NOT NULL

    UNION ALL

    -- Memory Usage Percentiles
    SELECT
        'MemoryUsagePercent' AS MetricName,
        MIN(MemoryUsagePercent) AS MinValue,
        (SELECT DISTINCT PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY MemoryUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND MemoryUsagePercent IS NOT NULL) AS P25,
        (SELECT DISTINCT PERCENTILE_CONT(0.50) WITHIN GROUP (ORDER BY MemoryUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND MemoryUsagePercent IS NOT NULL) AS P50_Median,
        (SELECT DISTINCT PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY MemoryUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND MemoryUsagePercent IS NOT NULL) AS P75,
        (SELECT DISTINCT PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY MemoryUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND MemoryUsagePercent IS NOT NULL) AS P95,
        (SELECT DISTINCT PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY MemoryUsagePercent) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND MemoryUsagePercent IS NOT NULL) AS P99,
        MAX(MemoryUsagePercent) AS MaxValue,
        AVG(MemoryUsagePercent) AS AvgValue,
        STDEV(MemoryUsagePercent) AS StdDev
    FROM SystemStatistics
    WHERE RecordedAt BETWEEN @StartDate AND @EndDate
        AND MemoryUsagePercent IS NOT NULL

    UNION ALL

    -- Active Loans Count Percentiles
    SELECT
        'ActiveLoansCount' AS MetricName,
        MIN(ActiveLoansCount) AS MinValue,
        (SELECT DISTINCT PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY ActiveLoansCount) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND ActiveLoansCount IS NOT NULL) AS P25,
        (SELECT DISTINCT PERCENTILE_CONT(0.50) WITHIN GROUP (ORDER BY ActiveLoansCount) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND ActiveLoansCount IS NOT NULL) AS P50_Median,
        (SELECT DISTINCT PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY ActiveLoansCount) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND ActiveLoansCount IS NOT NULL) AS P75,
        (SELECT DISTINCT PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY ActiveLoansCount) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND ActiveLoansCount IS NOT NULL) AS P95,
        (SELECT DISTINCT PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY ActiveLoansCount) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND ActiveLoansCount IS NOT NULL) AS P99,
        MAX(ActiveLoansCount) AS MaxValue,
        AVG(CAST(ActiveLoansCount AS FLOAT)) AS AvgValue,
        STDEV(ActiveLoansCount) AS StdDev
    FROM SystemStatistics
    WHERE RecordedAt BETWEEN @StartDate AND @EndDate
        AND ActiveLoansCount IS NOT NULL

    UNION ALL

    -- Query Time Percentiles
    SELECT
        'AvgQueryTimeMs' AS MetricName,
        MIN(AvgQueryTimeMs) AS MinValue,
        (SELECT DISTINCT PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY AvgQueryTimeMs) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND AvgQueryTimeMs IS NOT NULL) AS P25,
        (SELECT DISTINCT PERCENTILE_CONT(0.50) WITHIN GROUP (ORDER BY AvgQueryTimeMs) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND AvgQueryTimeMs IS NOT NULL) AS P50_Median,
        (SELECT DISTINCT PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY AvgQueryTimeMs) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND AvgQueryTimeMs IS NOT NULL) AS P75,
        (SELECT DISTINCT PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY AvgQueryTimeMs) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND AvgQueryTimeMs IS NOT NULL) AS P95,
        (SELECT DISTINCT PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY AvgQueryTimeMs) OVER ()
         FROM SystemStatistics
         WHERE RecordedAt BETWEEN @StartDate AND @EndDate AND AvgQueryTimeMs IS NOT NULL) AS P99,
        MAX(AvgQueryTimeMs) AS MaxValue,
        AVG(AvgQueryTimeMs) AS AvgValue,
        STDEV(AvgQueryTimeMs) AS StdDev
    FROM SystemStatistics
    WHERE RecordedAt BETWEEN @StartDate AND @EndDate
        AND AvgQueryTimeMs IS NOT NULL

    ORDER BY MetricName;
END
GO

PRINT 'Created stored procedure sp_GetPercentiles';
GO

-- ============================================================================
-- 7. Create Stored Procedure for Anomaly Detection
-- ============================================================================

IF OBJECT_ID('sp_DetectAnomalies', 'P') IS NOT NULL
    DROP PROCEDURE sp_DetectAnomalies;
GO

CREATE PROCEDURE sp_DetectAnomalies
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @StandardDeviations FLOAT = 2.0  -- Default: 2 standard deviations
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate statistics for the period
    DECLARE @AvgCPU FLOAT, @StdDevCPU FLOAT;
    DECLARE @AvgMemory FLOAT, @StdDevMemory FLOAT;
    DECLARE @AvgLoans FLOAT, @StdDevLoans FLOAT;

    SELECT
        @AvgCPU = AVG(CPUUsagePercent),
        @StdDevCPU = STDEV(CPUUsagePercent),
        @AvgMemory = AVG(MemoryUsagePercent),
        @StdDevMemory = STDEV(MemoryUsagePercent),
        @AvgLoans = AVG(CAST(ActiveLoansCount AS FLOAT)),
        @StdDevLoans = STDEV(ActiveLoansCount)
    FROM SystemStatistics
    WHERE RecordedAt BETWEEN @StartDate AND @EndDate;

    -- Find anomalies (values outside mean ± N standard deviations)
    SELECT
        RecordedAt,

        -- CPU Anomalies
        CPUUsagePercent,
        @AvgCPU AS AvgCPU,
        CASE
            WHEN CPUUsagePercent > @AvgCPU + (@StandardDeviations * @StdDevCPU) THEN 'HIGH'
            WHEN CPUUsagePercent < @AvgCPU - (@StandardDeviations * @StdDevCPU) THEN 'LOW'
            ELSE NULL
        END AS CPUAnomaly,
        ABS(CPUUsagePercent - @AvgCPU) / NULLIF(@StdDevCPU, 0) AS CPUZScore,

        -- Memory Anomalies
        MemoryUsagePercent,
        @AvgMemory AS AvgMemory,
        CASE
            WHEN MemoryUsagePercent > @AvgMemory + (@StandardDeviations * @StdDevMemory) THEN 'HIGH'
            WHEN MemoryUsagePercent < @AvgMemory - (@StandardDeviations * @StdDevMemory) THEN 'LOW'
            ELSE NULL
        END AS MemoryAnomaly,
        ABS(MemoryUsagePercent - @AvgMemory) / NULLIF(@StdDevMemory, 0) AS MemoryZScore,

        -- Active Loans Anomalies
        ActiveLoansCount,
        @AvgLoans AS AvgLoans,
        CASE
            WHEN ActiveLoansCount > @AvgLoans + (@StandardDeviations * @StdDevLoans) THEN 'HIGH'
            WHEN ActiveLoansCount < @AvgLoans - (@StandardDeviations * @StdDevLoans) THEN 'LOW'
            ELSE NULL
        END AS LoansAnomaly,
        ABS(ActiveLoansCount - @AvgLoans) / NULLIF(@StdDevLoans, 0) AS LoansZScore

    FROM SystemStatistics
    WHERE RecordedAt BETWEEN @StartDate AND @EndDate
        AND (
            -- CPU is anomalous
            CPUUsagePercent > @AvgCPU + (@StandardDeviations * @StdDevCPU) OR
            CPUUsagePercent < @AvgCPU - (@StandardDeviations * @StdDevCPU) OR
            -- Memory is anomalous
            MemoryUsagePercent > @AvgMemory + (@StandardDeviations * @StdDevMemory) OR
            MemoryUsagePercent < @AvgMemory - (@StandardDeviations * @StdDevMemory) OR
            -- Loans is anomalous
            ActiveLoansCount > @AvgLoans + (@StandardDeviations * @StdDevLoans) OR
            ActiveLoansCount < @AvgLoans - (@StandardDeviations * @StdDevLoans)
        )
    ORDER BY RecordedAt;
END
GO

PRINT 'Created stored procedure sp_DetectAnomalies';
GO

-- ============================================================================
-- 8. Create Stored Procedure for Trend Analysis
-- ============================================================================

IF OBJECT_ID('sp_GetTrendAnalysis', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetTrendAnalysis;
GO

CREATE PROCEDURE sp_GetTrendAnalysis
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    WITH DailyAggregates AS (
        SELECT
            CAST(RecordedAt AS DATE) AS DayDate,
            AVG(CAST(ActiveLoansCount AS FLOAT)) AS AvgActiveLoans,
            AVG(CPUUsagePercent) AS AvgCPU,
            AVG(MemoryUsagePercent) AS AvgMemory,
            SUM(NewLoansCount) AS TotalNewLoans
        FROM SystemStatistics
        WHERE RecordedAt BETWEEN @StartDate AND @EndDate
        GROUP BY CAST(RecordedAt AS DATE)
    )
    SELECT
        DayDate,
        AvgActiveLoans,
        AvgCPU,
        AvgMemory,
        TotalNewLoans,

        -- Growth Rate (Day-over-Day change)
        AvgActiveLoans - LAG(AvgActiveLoans, 1) OVER (ORDER BY DayDate) AS DayOverDayLoansChange,

        CASE
            WHEN LAG(AvgActiveLoans, 1) OVER (ORDER BY DayDate) > 0
            THEN ((AvgActiveLoans - LAG(AvgActiveLoans, 1) OVER (ORDER BY DayDate))
                  / LAG(AvgActiveLoans, 1) OVER (ORDER BY DayDate) * 100)
            ELSE NULL
        END AS DayOverDayLoansChangePercent,

        -- 7-Day Moving Average
        AVG(AvgActiveLoans) OVER (
            ORDER BY DayDate
            ROWS BETWEEN 6 PRECEDING AND CURRENT ROW
        ) AS SevenDayAvgLoans,

        -- Cumulative Sum
        SUM(TotalNewLoans) OVER (
            ORDER BY DayDate
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS CumulativeNewLoans,

        -- Ranking
        DENSE_RANK() OVER (ORDER BY AvgActiveLoans DESC) AS RankByActiveLoans,
        ROW_NUMBER() OVER (ORDER BY AvgCPU DESC) AS RankByCPU

    FROM DailyAggregates
    ORDER BY DayDate;
END
GO

PRINT 'Created stored procedure sp_GetTrendAnalysis';
GO

-- ============================================================================
-- 9. Generate Seed Data (30 days of minute-level statistics)
-- ============================================================================

PRINT 'Generating seed data for 30 days...';
GO

DECLARE @StartDate DATETIME2 = DATEADD(DAY, -30, GETUTCDATE());
DECLARE @EndDate DATETIME2 = GETUTCDATE();
DECLARE @CurrentDate DATETIME2 = @StartDate;
DECLARE @MinuteCounter INT = 0;

-- Use a loop to generate ~43,200 records (30 days * 24 hours * 60 minutes)
WHILE @CurrentDate < @EndDate
BEGIN
    -- Base metrics with time-of-day patterns
    DECLARE @HourOfDay INT = DATEPART(HOUR, @CurrentDate);
    DECLARE @DayOfWeek INT = DATEPART(WEEKDAY, @CurrentDate);

    -- Business hours effect (9 AM - 5 PM weekdays)
    DECLARE @IsBusinessHours BIT = CASE
        WHEN @DayOfWeek BETWEEN 2 AND 6 AND @HourOfDay BETWEEN 9 AND 17 THEN 1
        ELSE 0
    END;

    -- Random variations (using CHECKSUM for pseudo-random values)
    DECLARE @RandomSeed INT = CHECKSUM(NEWID());
    DECLARE @RandomFactor FLOAT = (ABS(@RandomSeed) % 100) / 100.0;

    -- Domain metrics with realistic patterns
    DECLARE @BaseActiveLoans INT = CASE
        WHEN @IsBusinessHours = 1 THEN 150 + (ABS(@RandomSeed) % 50)
        ELSE 80 + (ABS(@RandomSeed) % 30)
    END;

    DECLARE @NewLoans INT = CASE
        WHEN @IsBusinessHours = 1 AND @HourOfDay BETWEEN 10 AND 15 THEN ABS(@RandomSeed) % 5
        WHEN @IsBusinessHours = 1 THEN ABS(@RandomSeed) % 3
        ELSE ABS(@RandomSeed) % 2
    END;

    DECLARE @Returns INT = CASE
        WHEN @IsBusinessHours = 1 THEN ABS(@RandomSeed) % 4
        ELSE ABS(@RandomSeed) % 2
    END;

    -- Infrastructure metrics with realistic patterns
    DECLARE @BaseCPU DECIMAL(5,2) = CASE
        WHEN @IsBusinessHours = 1 THEN 45.0 + (@RandomFactor * 30.0)
        ELSE 15.0 + (@RandomFactor * 20.0)
    END;

    DECLARE @BaseMemory DECIMAL(5,2) = 60.0 + (@RandomFactor * 25.0);

    -- Insert the statistics record
    INSERT INTO SystemStatistics (
        RecordedAt,
        ActiveLoansCount,
        NewLoansCount,
        ReturnedLoansCount,
        ActiveMembersCount,
        OverdueLoansCount,
        TotalBooksAvailable,
        DatabaseSizeMB,
        ActiveConnectionsCount,
        AvgQueryTimeMs,
        CPUUsagePercent,
        MemoryUsagePercent,
        ServerName
    )
    VALUES (
        @CurrentDate,
        @BaseActiveLoans,
        @NewLoans,
        @Returns,
        500 + (ABS(@RandomSeed) % 100),  -- Active members
        (@BaseActiveLoans * 10) / 100,   -- ~10% overdue
        5000 - @BaseActiveLoans,         -- Books available
        250.5 + (@RandomFactor * 10.0),  -- Database size grows slowly
        CASE WHEN @IsBusinessHours = 1 THEN 20 + (ABS(@RandomSeed) % 30) ELSE 5 + (ABS(@RandomSeed) % 10) END,
        10.0 + (@RandomFactor * 50.0),   -- Query time
        @BaseCPU,
        @BaseMemory,
        'DB-PROD-01'
    );

    -- Increment minute counter and date
    SET @MinuteCounter = @MinuteCounter + 1;
    SET @CurrentDate = DATEADD(MINUTE, 1, @CurrentDate);

    -- Print progress every 1000 minutes
    IF @MinuteCounter % 1000 = 0
    BEGIN
        PRINT 'Generated ' + CAST(@MinuteCounter AS VARCHAR) + ' records...';
    END
END

PRINT 'Seed data generation complete. Total records: ' + CAST(@MinuteCounter AS VARCHAR);
GO

-- ============================================================================
-- 10. Add a few anomalies to demonstrate anomaly detection
-- ============================================================================

PRINT 'Adding anomaly data points...';
GO

-- Add some CPU spikes
INSERT INTO SystemStatistics (
    RecordedAt, ActiveLoansCount, CPUUsagePercent, MemoryUsagePercent,
    DatabaseSizeMB, ActiveConnectionsCount, ServerName
)
VALUES
    (DATEADD(DAY, -5, GETUTCDATE()), 150, 95.5, 65.0, 255.0, 50, 'DB-PROD-01'),
    (DATEADD(DAY, -12, GETUTCDATE()), 145, 3.2, 62.0, 253.0, 8, 'DB-PROD-01'),
    (DATEADD(DAY, -20, GETUTCDATE()), 180, 92.8, 88.5, 252.0, 45, 'DB-PROD-01');

PRINT 'Added 3 anomaly data points';
GO

-- ============================================================================
-- 11. Verification Tests
-- ============================================================================

PRINT 'Running verification tests...';
GO

-- Test 1: Verify table exists and has data
DECLARE @RecordCount INT;
SELECT @RecordCount = COUNT(*) FROM SystemStatistics;
PRINT 'Total records in SystemStatistics: ' + CAST(@RecordCount AS VARCHAR);
GO

-- Test 2: Verify views work
PRINT 'Testing vw_HourlyStatistics...';
SELECT TOP 5
    HourBucket,
    SampleCount,
    AvgActiveLoans,
    AvgCPUUsage
FROM vw_HourlyStatistics
ORDER BY HourBucket DESC;
GO

PRINT 'Testing vw_DailyStatistics...';
SELECT TOP 5
    DayDate,
    TotalNewLoans,
    AvgCPUUsage,
    MaxCPUUsage
FROM vw_DailyStatistics
ORDER BY DayDate DESC;
GO

-- Test 3: Test stored procedures
PRINT 'Testing sp_GetMovingAverages...';
EXEC sp_GetMovingAverages
    @StartDate = '2025-01-01',
    @EndDate = '2025-01-02',
    @WindowSize = 7;
GO

PRINT 'Testing sp_GetPercentiles...';
EXEC sp_GetPercentiles
    @StartDate = '2025-01-01',
    @EndDate = '2025-12-31';
GO

PRINT 'Testing sp_DetectAnomalies...';
EXEC sp_DetectAnomalies
    @StartDate = '2025-01-01',
    @EndDate = '2025-12-31',
    @StandardDeviations = 2.0;
GO

PRINT 'Testing sp_GetTrendAnalysis...';
EXEC sp_GetTrendAnalysis
    @StartDate = '2025-01-01',
    @EndDate = '2025-12-31';
GO

-- ============================================================================
-- Migration Complete
-- ============================================================================

PRINT 'V021 migration completed successfully!';
PRINT '';
PRINT 'Summary:';
PRINT '- Created SystemStatistics table with time-series data';
PRINT '- Created indexes for efficient querying';
PRINT '- Created views: vw_HourlyStatistics, vw_DailyStatistics';
PRINT '- Created stored procedures: sp_GetMovingAverages, sp_GetPercentiles, sp_DetectAnomalies, sp_GetTrendAnalysis';
PRINT '- Generated ~43,000+ minute-level statistics records for 30 days';
PRINT '- Added anomaly examples for testing';
GO
