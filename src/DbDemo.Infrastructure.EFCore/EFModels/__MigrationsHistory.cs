using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Table("__MigrationsHistory")]
public partial class __MigrationsHistory
{
    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public string MigrationVersion { get; set; } = null!;

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(64)]
    [Unicode(false)]
    public string Checksum { get; set; } = null!;

    public DateTime AppliedAt { get; set; }

    public int ExecutionTimeMs { get; set; }
}
