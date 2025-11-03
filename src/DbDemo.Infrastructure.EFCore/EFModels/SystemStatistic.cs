using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Index("RecordedAt", Name = "IX_SystemStatistics_RecordedAt", AllDescending = true)]
public partial class SystemStatistic
{
    [Key]
    public long Id { get; set; }

    public DateTime RecordedAt { get; set; }

    public int? ActiveLoansCount { get; set; }

    public int? NewLoansCount { get; set; }

    public int? ReturnedLoansCount { get; set; }

    public int? ActiveMembersCount { get; set; }

    public int? OverdueLoansCount { get; set; }

    public int? TotalBooksAvailable { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? DatabaseSizeMB { get; set; }

    public int? ActiveConnectionsCount { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? AvgQueryTimeMs { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? CPUUsagePercent { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? MemoryUsagePercent { get; set; }

    [StringLength(100)]
    public string? ServerName { get; set; }

    public string? Notes { get; set; }
}
