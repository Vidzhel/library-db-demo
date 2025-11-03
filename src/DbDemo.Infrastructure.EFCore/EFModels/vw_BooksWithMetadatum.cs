using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DbDemo.Infrastructure.EFCore.EFModels;

[Keyless]
public partial class vw_BooksWithMetadatum
{
    public int Id { get; set; }

    [StringLength(20)]
    public string ISBN { get; set; } = null!;

    [StringLength(200)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string? Publisher { get; set; }

    public DateTime? PublishedDate { get; set; }

    public string? Metadata { get; set; }

    [StringLength(4000)]
    public string? Genre { get; set; }

    [StringLength(4000)]
    public string? Series { get; set; }

    [StringLength(4000)]
    public string? SeriesNumber { get; set; }

    [StringLength(4000)]
    public string? OriginalLanguage { get; set; }

    [StringLength(4000)]
    public string? Awards { get; set; }

    public string? TagsJson { get; set; }

    public string? CustomFieldsJson { get; set; }
}
