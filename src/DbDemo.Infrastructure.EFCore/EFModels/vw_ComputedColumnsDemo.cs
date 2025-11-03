using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_ComputedColumnsDemo
{
    public int BookId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public DateTime? PublishedDate { get; set; }

    public int? YearPublished { get; set; }

    public int? PublishedDecade { get; set; }

    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    [StringLength(101)]
    public string? FullName { get; set; }

    [StringLength(50)]
    public string? MemberFirstName { get; set; }

    [StringLength(50)]
    public string? MemberLastName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public int? Age { get; set; }

    public DateTime? BorrowedAt { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public int? DaysOverdue { get; set; }

    public int? Status { get; set; }
}
