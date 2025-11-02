namespace DbDemo.ConsoleApp.Models;

/// <summary>
/// Represents a time-series statistics record capturing both domain (business)
/// and infrastructure (system health) metrics at minute-level granularity.
/// Demonstrates SQL Server time-series analytics, window functions, and statistical analysis.
/// </summary>
public class SystemStatistic
{
    // Primary key
    public long Id { get; private set; }

    // Time dimension
    public DateTime RecordedAt { get; private set; }

    // Domain Metrics (Library Business Metrics)
    public int? ActiveLoansCount { get; private set; }
    public int? NewLoansCount { get; private set; }
    public int? ReturnedLoansCount { get; private set; }
    public int? ActiveMembersCount { get; private set; }
    public int? OverdueLoansCount { get; private set; }
    public int? TotalBooksAvailable { get; private set; }

    // Infrastructure Metrics (System Health)
    public decimal? DatabaseSizeMB { get; private set; }
    public int? ActiveConnectionsCount { get; private set; }
    public decimal? AvgQueryTimeMs { get; private set; }
    public decimal? CPUUsagePercent { get; private set; }
    public decimal? MemoryUsagePercent { get; private set; }

    // Metadata
    public string? ServerName { get; private set; }
    public string? Notes { get; private set; }

    // Private constructor for repository use
    private SystemStatistic() { }

    /// <summary>
    /// Creates a new system statistics record
    /// </summary>
    public SystemStatistic(
        DateTime recordedAt,
        int? activeLoansCount = null,
        int? newLoansCount = null,
        int? returnedLoansCount = null,
        int? activeMembersCount = null,
        int? overdueLoansCount = null,
        int? totalBooksAvailable = null,
        decimal? databaseSizeMB = null,
        int? activeConnectionsCount = null,
        decimal? avgQueryTimeMs = null,
        decimal? cpuUsagePercent = null,
        decimal? memoryUsagePercent = null,
        string? serverName = null,
        string? notes = null)
    {
        // Validation
        if (recordedAt > DateTime.UtcNow)
            throw new ArgumentException("RecordedAt cannot be in the future", nameof(recordedAt));

        if (cpuUsagePercent.HasValue && (cpuUsagePercent < 0 || cpuUsagePercent > 100))
            throw new ArgumentOutOfRangeException(nameof(cpuUsagePercent), "CPU usage must be between 0 and 100");

        if (memoryUsagePercent.HasValue && (memoryUsagePercent < 0 || memoryUsagePercent > 100))
            throw new ArgumentOutOfRangeException(nameof(memoryUsagePercent), "Memory usage must be between 0 and 100");

        if (activeLoansCount.HasValue && activeLoansCount < 0)
            throw new ArgumentOutOfRangeException(nameof(activeLoansCount), "Active loans count cannot be negative");

        if (activeMembersCount.HasValue && activeMembersCount < 0)
            throw new ArgumentOutOfRangeException(nameof(activeMembersCount), "Active members count cannot be negative");

        // Set properties
        RecordedAt = recordedAt;
        ActiveLoansCount = activeLoansCount;
        NewLoansCount = newLoansCount;
        ReturnedLoansCount = returnedLoansCount;
        ActiveMembersCount = activeMembersCount;
        OverdueLoansCount = overdueLoansCount;
        TotalBooksAvailable = totalBooksAvailable;
        DatabaseSizeMB = databaseSizeMB;
        ActiveConnectionsCount = activeConnectionsCount;
        AvgQueryTimeMs = avgQueryTimeMs;
        CPUUsagePercent = cpuUsagePercent;
        MemoryUsagePercent = memoryUsagePercent;
        ServerName = serverName;
        Notes = notes;
    }

    /// <summary>
    /// Factory method to reconstruct from database
    /// </summary>
    internal static SystemStatistic FromDatabase(
        long id,
        DateTime recordedAt,
        int? activeLoansCount,
        int? newLoansCount,
        int? returnedLoansCount,
        int? activeMembersCount,
        int? overdueLoansCount,
        int? totalBooksAvailable,
        decimal? databaseSizeMB,
        int? activeConnectionsCount,
        decimal? avgQueryTimeMs,
        decimal? cpuUsagePercent,
        decimal? memoryUsagePercent,
        string? serverName,
        string? notes)
    {
        return new SystemStatistic
        {
            Id = id,
            RecordedAt = recordedAt,
            ActiveLoansCount = activeLoansCount,
            NewLoansCount = newLoansCount,
            ReturnedLoansCount = returnedLoansCount,
            ActiveMembersCount = activeMembersCount,
            OverdueLoansCount = overdueLoansCount,
            TotalBooksAvailable = totalBooksAvailable,
            DatabaseSizeMB = databaseSizeMB,
            ActiveConnectionsCount = activeConnectionsCount,
            AvgQueryTimeMs = avgQueryTimeMs,
            CPUUsagePercent = cpuUsagePercent,
            MemoryUsagePercent = memoryUsagePercent,
            ServerName = serverName,
            Notes = notes
        };
    }

    /// <summary>
    /// Helper method to format the statistics for display
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>();

        if (ActiveLoansCount.HasValue)
            parts.Add($"Loans: {ActiveLoansCount}");
        if (CPUUsagePercent.HasValue)
            parts.Add($"CPU: {CPUUsagePercent:F1}%");
        if (MemoryUsagePercent.HasValue)
            parts.Add($"Memory: {MemoryUsagePercent:F1}%");

        return $"[{RecordedAt:yyyy-MM-dd HH:mm}] {string.Join(", ", parts)}";
    }
}
