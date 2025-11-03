using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_LibraryDashboard
{
    [StringLength(100)]
    public string? CategoryName { get; set; }

    public int? LoanYear { get; set; }

    public int? LoanMonth { get; set; }

    [StringLength(8)]
    [Unicode(false)]
    public string LoanStatus { get; set; } = null!;

    public int? TotalLoans { get; set; }

    public int? UniqueMembers { get; set; }

    public int? UniqueBooks { get; set; }

    public int? AvgDurationDays { get; set; }

    public byte IsCategoryGrouped { get; set; }

    public byte IsYearGrouped { get; set; }

    public byte IsMonthGrouped { get; set; }

    public byte IsStatusGrouped { get; set; }

    public int GroupingId { get; set; }

    [StringLength(11)]
    [Unicode(false)]
    public string AggregationType { get; set; } = null!;
}
