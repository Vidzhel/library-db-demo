namespace DbDemo.Application.DTOs;

using DbDemo.Domain.Entities;

/// <summary>
/// DTO for tracking performance metrics of different SQL approaches
/// Used to compare #TempTable vs @TableVariable vs CTE performance
/// </summary>
public class PerformanceComparison
{
    public string MethodName { get; init; } = string.Empty;
    public long ExecutionTimeMs { get; init; }
    public int RowsProcessed { get; init; }

    /// <summary>
    /// Factory method to create instance with performance data
    /// </summary>
    public static PerformanceComparison Create(
        string methodName,
        long executionTimeMs,
        int rowsProcessed)
    {
        return new PerformanceComparison
        {
            MethodName = methodName,
            ExecutionTimeMs = executionTimeMs,
            RowsProcessed = rowsProcessed
        };
    }

    /// <summary>
    /// Calculates throughput (rows per second)
    /// </summary>
    public decimal Throughput
    {
        get
        {
            if (ExecutionTimeMs == 0) return 0;
            return Math.Round((decimal)RowsProcessed / ExecutionTimeMs * 1000, 2);
        }
    }

    /// <summary>
    /// Calculates average time per row (milliseconds)
    /// </summary>
    public decimal AvgTimePerRow
    {
        get
        {
            if (RowsProcessed == 0) return 0;
            return Math.Round((decimal)ExecutionTimeMs / RowsProcessed, 4);
        }
    }

    /// <summary>
    /// Human-readable display format
    /// </summary>
    public override string ToString()
    {
        return $"{MethodName}: {ExecutionTimeMs}ms for {RowsProcessed} rows " +
               $"({Throughput:F2} rows/sec)";
    }
}
