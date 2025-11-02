using DbDemo.ConsoleApp.Models;
using Microsoft.Data.SqlClient;

namespace DbDemo.ConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Repository for managing system statistics and time-series analytics
/// </summary>
public interface ISystemStatisticsRepository
{
    /// <summary>
    /// Creates a new statistics record
    /// </summary>
    Task<SystemStatistic> CreateAsync(SystemStatistic statistic, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics within a specific time range
    /// </summary>
    Task<List<SystemStatistic>> GetByTimeRangeAsync(DateTime startDate, DateTime endDate, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent statistics records
    /// </summary>
    Task<List<SystemStatistic>> GetRecentAsync(int count, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hourly aggregated statistics
    /// </summary>
    Task<List<HourlyStatistic>> GetHourlyStatisticsAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily aggregated statistics
    /// </summary>
    Task<List<DailyStatistic>> GetDailyStatisticsAsync(SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates moving averages for specified metrics
    /// </summary>
    Task<List<MovingAverageResult>> GetMovingAveragesAsync(DateTime startDate, DateTime endDate, int windowSize, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates percentile distributions for key metrics
    /// </summary>
    Task<List<PercentileResult>> GetPercentilesAsync(DateTime startDate, DateTime endDate, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects anomalies using statistical analysis (Z-score method)
    /// </summary>
    Task<List<AnomalyResult>> DetectAnomaliesAsync(DateTime startDate, DateTime endDate, double standardDeviations, SqlTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs trend analysis with growth rates and rankings
    /// </summary>
    Task<List<TrendAnalysisResult>> GetTrendAnalysisAsync(DateTime startDate, DateTime endDate, SqlTransaction transaction, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents hourly aggregated statistics
/// </summary>
public class HourlyStatistic
{
    public DateTime HourBucket { get; set; }
    public int SampleCount { get; set; }
    public double? AvgActiveLoans { get; set; }
    public double? AvgNewLoans { get; set; }
    public double? AvgCPUUsage { get; set; }
    public double? AvgMemoryUsage { get; set; }
    public int? MinActiveLoans { get; set; }
    public int? MaxActiveLoans { get; set; }
}

/// <summary>
/// Represents daily aggregated statistics
/// </summary>
public class DailyStatistic
{
    public DateTime DayDate { get; set; }
    public int SampleCount { get; set; }
    public double? AvgActiveLoans { get; set; }
    public int? MinActiveLoans { get; set; }
    public int? MaxActiveLoans { get; set; }
    public int? TotalNewLoans { get; set; }
    public int? TotalReturnedLoans { get; set; }
    public double? AvgCPUUsage { get; set; }
    public double? AvgMemoryUsage { get; set; }
    public double? MinCPUUsage { get; set; }
    public double? MaxCPUUsage { get; set; }
    public double? StdDevCPUUsage { get; set; }
}

/// <summary>
/// Represents moving average calculation results
/// </summary>
public class MovingAverageResult
{
    public DateTime RecordedAt { get; set; }
    public int? ActiveLoansCount { get; set; }
    public decimal? CPUUsagePercent { get; set; }
    public decimal? MemoryUsagePercent { get; set; }
    public double? MovingAvgActiveLoans { get; set; }
    public double? MovingAvgCPU { get; set; }
    public double? MovingAvgMemory { get; set; }
    public int? RunningTotalNewLoans { get; set; }
    public long RowNum { get; set; }
}

/// <summary>
/// Represents percentile analysis results for a metric
/// </summary>
public class PercentileResult
{
    public string MetricName { get; set; } = string.Empty;
    public double? MinValue { get; set; }
    public double? P25 { get; set; }
    public double? P50_Median { get; set; }
    public double? P75 { get; set; }
    public double? P95 { get; set; }
    public double? P99 { get; set; }
    public double? MaxValue { get; set; }
    public double? AvgValue { get; set; }
    public double? StdDev { get; set; }
}

/// <summary>
/// Represents an anomaly detection result
/// </summary>
public class AnomalyResult
{
    public DateTime RecordedAt { get; set; }
    public decimal? CPUUsagePercent { get; set; }
    public double? AvgCPU { get; set; }
    public string? CPUAnomaly { get; set; }
    public double? CPUZScore { get; set; }
    public decimal? MemoryUsagePercent { get; set; }
    public double? AvgMemory { get; set; }
    public string? MemoryAnomaly { get; set; }
    public double? MemoryZScore { get; set; }
    public int? ActiveLoansCount { get; set; }
    public double? AvgLoans { get; set; }
    public string? LoansAnomaly { get; set; }
    public double? LoansZScore { get; set; }
}

/// <summary>
/// Represents trend analysis results
/// </summary>
public class TrendAnalysisResult
{
    public DateTime DayDate { get; set; }
    public double? AvgActiveLoans { get; set; }
    public double? AvgCPU { get; set; }
    public double? AvgMemory { get; set; }
    public int? TotalNewLoans { get; set; }
    public double? DayOverDayLoansChange { get; set; }
    public double? DayOverDayLoansChangePercent { get; set; }
    public double? SevenDayAvgLoans { get; set; }
    public int? CumulativeNewLoans { get; set; }
    public long? RankByActiveLoans { get; set; }
    public long? RankByCPU { get; set; }
}
