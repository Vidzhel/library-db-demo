using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Index("Location", Name = "IX_LibraryBranches_Location")]
public partial class LibraryBranch
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string BranchName { get; set; } = null!;

    [StringLength(200)]
    public string Address { get; set; } = null!;

    [StringLength(100)]
    public string City { get; set; } = null!;

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public Geometry? Location { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}
