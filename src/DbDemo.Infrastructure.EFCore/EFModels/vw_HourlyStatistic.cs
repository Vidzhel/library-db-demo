using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_HourlyStatistic
{
    [Column(TypeName = "datetime")]
    public DateTime? HourBucket { get; set; }

    public int? SampleCount { get; set; }

    public double? AvgActiveLoans { get; set; }

    public double? AvgNewLoans { get; set; }

    public double? AvgReturnedLoans { get; set; }

    public double? AvgActiveMembers { get; set; }

    public double? AvgOverdueLoans { get; set; }

    public double? AvgBooksAvailable { get; set; }

    public int? MinActiveLoans { get; set; }

    public int? MaxActiveLoans { get; set; }

    [Column(TypeName = "decimal(38, 6)")]
    public decimal? AvgDatabaseSizeMB { get; set; }

    public int? AvgActiveConnections { get; set; }

    [Column(TypeName = "decimal(38, 6)")]
    public decimal? AvgQueryTimeMs { get; set; }

    [Column(TypeName = "decimal(38, 6)")]
    public decimal? AvgCPUUsage { get; set; }

    [Column(TypeName = "decimal(38, 6)")]
    public decimal? AvgMemoryUsage { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? MinCPUUsage { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? MaxCPUUsage { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? MinMemoryUsage { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? MaxMemoryUsage { get; set; }
}
