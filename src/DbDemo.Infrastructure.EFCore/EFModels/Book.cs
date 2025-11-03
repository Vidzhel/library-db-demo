using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Index("PublishedDecade", Name = "IX_Books_PublishedDecade")]
public partial class Book
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public string ISBN { get; set; } = null!;

    [StringLength(200)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string? Subtitle { get; set; }

    public string? Description { get; set; }

    [StringLength(200)]
    public string? Publisher { get; set; }

    public DateTime? PublishedDate { get; set; }

    public int? PageCount { get; set; }

    [StringLength(50)]
    public string? Language { get; set; }

    public int CategoryId { get; set; }

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    [StringLength(50)]
    public string? ShelfLocation { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Metadata { get; set; }

    public int? YearPublished { get; set; }

    public int? PublishedDecade { get; set; }

    [InverseProperty("Book")]
    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Books")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("Book")]
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
