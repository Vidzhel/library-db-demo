using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_UnpivotedLoanStat
{
    [StringLength(4000)]
    public string? YearMonth { get; set; }

    [StringLength(128)]
    public string? CategoryName { get; set; }

    public int? LoanCount { get; set; }
}
