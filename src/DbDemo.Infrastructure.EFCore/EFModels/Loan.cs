using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

public partial class Loan
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int BookId { get; set; }

    public DateTime BorrowedAt { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public int Status { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? LateFee { get; set; }

    public bool IsFeePaid { get; set; }

    public int RenewalCount { get; set; }

    public int MaxRenewalsAllowed { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? DaysOverdue { get; set; }

    [ForeignKey("BookId")]
    [InverseProperty("Loans")]
    public virtual Book Book { get; set; } = null!;

    [ForeignKey("MemberId")]
    [InverseProperty("Loans")]
    public virtual Member Member { get; set; } = null!;
}
