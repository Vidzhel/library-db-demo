using System.Text.Json;
using System.Text.Json.Serialization;

namespace DbDemo.Application.DTOs;

/// <summary>
/// Represents flexible metadata for books stored as JSON in the database.
/// Demonstrates JSON support in SQL Server 2016+ with strongly-typed access.
/// </summary>
public class BookMetadata
{
    /// <summary>
    /// Genre of the book (e.g., "Science Fiction", "Fantasy", "Mystery").
    /// Extracted using JSON_VALUE(Metadata, '$.genre')
    /// </summary>
    [JsonPropertyName("genre")]
    public string? Genre { get; init; }

    /// <summary>
    /// Array of tags for categorization and search.
    /// Extracted using JSON_QUERY(Metadata, '$.tags') or OPENJSON
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    /// <summary>
    /// Name of the series this book belongs to (if applicable).
    /// Extracted using JSON_VALUE(Metadata, '$.series')
    /// </summary>
    [JsonPropertyName("series")]
    public string? Series { get; init; }

    /// <summary>
    /// Position in the series (1 for first book, 2 for second, etc.).
    /// Extracted using JSON_VALUE(Metadata, '$.seriesNumber')
    /// </summary>
    [JsonPropertyName("seriesNumber")]
    public int? SeriesNumber { get; init; }

    /// <summary>
    /// Original language of publication.
    /// Useful for translations and international catalogs.
    /// </summary>
    [JsonPropertyName("originalLanguage")]
    public string? OriginalLanguage { get; init; }

    /// <summary>
    /// Notable awards won by the book.
    /// Extracted using JSON_VALUE(Metadata, '$.awards')
    /// </summary>
    [JsonPropertyName("awards")]
    public string? Awards { get; init; }

    /// <summary>
    /// Average rating (e.g., from user reviews).
    /// Demonstrates storing numeric data in JSON.
    /// </summary>
    [JsonPropertyName("rating")]
    public decimal? Rating { get; init; }

    /// <summary>
    /// Additional custom fields not predefined in the schema.
    /// Allows for flexible extension without schema changes.
    /// </summary>
    [JsonPropertyName("customFields")]
    public Dictionary<string, string>? CustomFields { get; init; }

    /// <summary>
    /// Serializes this metadata object to JSON string for database storage.
    /// </summary>
    /// <returns>JSON string representation, or null if all properties are null</returns>
    public string? ToJson()
    {
        // Only serialize if at least one property is set
        if (Genre == null && Tags == null && Series == null && SeriesNumber == null &&
            OriginalLanguage == null && Awards == null && Rating == null && CustomFields == null)
        {
            return null;
        }

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false // Compact JSON for database storage
        };

        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Deserializes JSON string from database into strongly-typed BookMetadata object.
    /// </summary>
    /// <param name="json">JSON string from Books.Metadata column</param>
    /// <returns>BookMetadata instance, or null if json is null/empty</returns>
    public static BookMetadata? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BookMetadata>(json);
        }
        catch (JsonException)
        {
            // Return null if JSON is invalid rather than throwing
            // This allows for graceful handling of malformed data
            return null;
        }
    }

    /// <summary>
    /// Creates a new BookMetadata instance with specified values.
    /// </summary>
    public static BookMetadata Create(
        string? genre = null,
        List<string>? tags = null,
        string? series = null,
        int? seriesNumber = null,
        string? originalLanguage = null,
        string? awards = null,
        decimal? rating = null,
        Dictionary<string, string>? customFields = null)
    {
        return new BookMetadata
        {
            Genre = genre,
            Tags = tags,
            Series = series,
            SeriesNumber = seriesNumber,
            OriginalLanguage = originalLanguage,
            Awards = awards,
            Rating = rating,
            CustomFields = customFields
        };
    }

    /// <summary>
    /// Returns a formatted string representation of the metadata.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Genre))
            parts.Add($"Genre: {Genre}");

        if (!string.IsNullOrEmpty(Series))
            parts.Add($"Series: {Series}" + (SeriesNumber.HasValue ? $" #{SeriesNumber}" : ""));

        if (Tags != null && Tags.Count > 0)
            parts.Add($"Tags: {string.Join(", ", Tags)}");

        if (Rating.HasValue)
            parts.Add($"Rating: {Rating:F1}");

        if (!string.IsNullOrEmpty(Awards))
            parts.Add($"Awards: {Awards}");

        return parts.Count > 0 ? string.Join(" | ", parts) : "No metadata";
    }
}
