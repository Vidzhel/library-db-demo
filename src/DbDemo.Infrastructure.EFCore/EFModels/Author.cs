using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Index("FullName", Name = "IX_Authors_FullName")]
public partial class Author
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    public string? Biography { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(100)]
    public string? Nationality { get; set; }

    [StringLength(255)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [StringLength(101)]
    public string FullName { get; set; } = null!;

    [InverseProperty("Author")]
    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
