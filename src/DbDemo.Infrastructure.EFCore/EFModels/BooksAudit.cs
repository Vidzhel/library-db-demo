using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Table("BooksAudit")]
[Index("BookId", "ChangedAt", Name = "IX_BooksAudit_BookId", IsDescending = new[] { false, true })]
public partial class BooksAudit
{
    [Key]
    public int AuditId { get; set; }

    public int BookId { get; set; }

    [StringLength(10)]
    public string Action { get; set; } = null!;

    [StringLength(20)]
    public string? OldISBN { get; set; }

    [StringLength(20)]
    public string? NewISBN { get; set; }

    [StringLength(200)]
    public string? OldTitle { get; set; }

    [StringLength(200)]
    public string? NewTitle { get; set; }

    public int? OldAvailableCopies { get; set; }

    public int? NewAvailableCopies { get; set; }

    public int? OldTotalCopies { get; set; }

    public int? NewTotalCopies { get; set; }

    public DateTime ChangedAt { get; set; }

    [StringLength(128)]
    public string ChangedBy { get; set; } = null!;
}
