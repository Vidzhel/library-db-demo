using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_BranchDistance
{
    public int FromBranchId { get; set; }

    [StringLength(100)]
    public string FromBranchName { get; set; } = null!;

    [StringLength(100)]
    public string FromCity { get; set; } = null!;

    public int ToBranchId { get; set; }

    [StringLength(100)]
    public string ToBranchName { get; set; } = null!;

    [StringLength(100)]
    public string ToCity { get; set; } = null!;

    public double? DistanceKm { get; set; }
}
