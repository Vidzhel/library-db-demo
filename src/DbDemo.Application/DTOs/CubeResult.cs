namespace DbDemo.Application.DTOs;

using DbDemo.Domain.Entities;

/// <summary>
/// DTO for results from sp_GetLibraryStatsCube
/// Demonstrates CUBE with all possible grouping combinations
/// 2^3 = 8 combinations for three dimensions (Category, Year, Status)
/// </summary>
public class CubeResult
{
    public string? CategoryName { get; init; }
    public int? LoanYear { get; init; }
    public string? LoanStatus { get; init; }
    public int TotalLoans { get; init; }
    public int IsCategoryAggregated { get; init; }
    public int IsYearAggregated { get; init; }
    public int IsStatusAggregated { get; init; }
    public int GroupingId { get; init; }

    /// <summary>
    /// Human-readable description of aggregation level
    /// Based on which dimensions are aggregated
    /// </summary>
    public string AggregationDescription
    {
        get
        {
            return GroupingId switch
            {
                0 => "Detail (Category + Year + Status)",        // 000 binary
                1 => "Subtotal (Category + Year)",               // 001 binary
                2 => "Subtotal (Category + Status)",             // 010 binary
                3 => "Subtotal (Category only)",                 // 011 binary
                4 => "Subtotal (Year + Status)",                 // 100 binary
                5 => "Subtotal (Year only)",                     // 101 binary
                6 => "Subtotal (Status only)",                   // 110 binary
                7 => "Grand Total",                              // 111 binary
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Number of dimensions being aggregated (0-3)
    /// </summary>
    public int AggregatedDimensionCount
    {
        get
        {
            return IsCategoryAggregated + IsYearAggregated + IsStatusAggregated;
        }
    }

    /// <summary>
    /// Checks if this is a grand total row (all dimensions aggregated)
    /// </summary>
    public bool IsGrandTotal => GroupingId == 7;

    /// <summary>
    /// Checks if this is detail level (no dimensions aggregated)
    /// </summary>
    public bool IsDetail => GroupingId == 0;

    /// <summary>
    /// Factory method to create instance from database results
    /// </summary>
    public static CubeResult FromDatabase(
        string? categoryName,
        int? loanYear,
        string? loanStatus,
        int totalLoans,
        int isCategoryAggregated,
        int isYearAggregated,
        int isStatusAggregated,
        int groupingId)
    {
        return new CubeResult
        {
            CategoryName = categoryName,
            LoanYear = loanYear,
            LoanStatus = loanStatus,
            TotalLoans = totalLoans,
            IsCategoryAggregated = isCategoryAggregated,
            IsYearAggregated = isYearAggregated,
            IsStatusAggregated = isStatusAggregated,
            GroupingId = groupingId
        };
    }

    public override string ToString()
    {
        var categoryDisplay = CategoryName ?? "(All)";
        var yearDisplay = LoanYear?.ToString() ?? "(All)";
        var statusDisplay = LoanStatus ?? "(All)";

        return $"[ID:{GroupingId}] {AggregationDescription}: " +
               $"{categoryDisplay} / {yearDisplay} / {statusDisplay} = {TotalLoans} loans";
    }
}
