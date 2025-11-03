using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_MonthlyLoansByCategory
{
    public int? Year { get; set; }

    public int? Month { get; set; }

    [StringLength(4000)]
    public string? YearMonth { get; set; }

    public int? Fiction { get; set; }

    [Column("Non-Fiction")]
    public int? Non_Fiction { get; set; }

    public int? Science { get; set; }

    public int? History { get; set; }

    public int? Technology { get; set; }

    public int? Biography { get; set; }

    public int? Children { get; set; }

    public int? TotalLoans { get; set; }
}
