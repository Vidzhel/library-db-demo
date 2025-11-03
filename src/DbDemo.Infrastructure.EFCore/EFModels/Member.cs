using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

public partial class Member
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public string MembershipNumber { get; set; } = null!;

    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime DateOfBirth { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public DateTime MemberSince { get; set; }

    public DateTime MembershipExpiresAt { get; set; }

    public bool IsActive { get; set; }

    public int MaxBooksAllowed { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal OutstandingFees { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? Age { get; set; }

    [InverseProperty("Member")]
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
