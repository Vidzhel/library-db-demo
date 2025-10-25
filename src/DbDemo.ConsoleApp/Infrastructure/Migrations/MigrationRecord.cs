namespace DbDemo.ConsoleApp.Infrastructure.Migrations;

/// <summary>
/// Represents a single migration file with its metadata
/// </summary>
public class MigrationRecord
{
    /// <summary>
    /// Migration version number extracted from filename (e.g., "001", "002")
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Original filename (e.g., "V001__initial_schema.sql")
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the migration file on disk
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// SQL content of the migration file
    /// </summary>
    public string SqlContent { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 checksum of the SQL content for tamper detection
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Whether this migration has already been applied to the database
    /// </summary>
    public bool IsApplied { get; set; }

    /// <summary>
    /// When the migration was applied (null if not yet applied)
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// How long the migration took to execute in milliseconds (null if not yet applied)
    /// </summary>
    public int? ExecutionTimeMs { get; set; }

    /// <summary>
    /// The checksum stored in the database for applied migrations
    /// </summary>
    public string? DatabaseChecksum { get; set; }

    /// <summary>
    /// Whether the checksum matches between file and database (tamper detection)
    /// </summary>
    public bool ChecksumMatches => DatabaseChecksum == null || DatabaseChecksum == Checksum;

    public override string ToString()
    {
        var status = IsApplied ? "Applied" : "Pending";
        return $"[{Version}] {FileName} - {status}";
    }
}
