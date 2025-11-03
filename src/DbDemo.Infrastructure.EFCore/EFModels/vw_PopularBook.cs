using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_PopularBook
{
    public int BookId { get; set; }

    [StringLength(20)]
    public string ISBN { get; set; } = null!;

    [StringLength(200)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string? Subtitle { get; set; }

    public int CategoryId { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = null!;

    public int? TotalLoans { get; set; }

    public long? RowNumber { get; set; }

    public long? Rank { get; set; }

    public long? DenseRank { get; set; }

    public long? GlobalRowNumber { get; set; }
}
