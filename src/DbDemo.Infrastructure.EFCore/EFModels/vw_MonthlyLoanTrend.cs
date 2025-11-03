using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_MonthlyLoanTrend
{
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = null!;

    public int? Year { get; set; }

    public int? Month { get; set; }

    [StringLength(4000)]
    public string? YearMonth { get; set; }

    public int? LoanCount { get; set; }

    public int? PrevMonthLoans { get; set; }

    public int? NextMonthLoans { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? GrowthPercentage { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? ThreeMonthMovingAvg { get; set; }
}
