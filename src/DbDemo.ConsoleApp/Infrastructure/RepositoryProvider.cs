namespace DbDemo.ConsoleApp.Infrastructure;

/// <summary>
/// Enum representing the available repository providers.
/// </summary>
public enum RepositoryProvider
{
    /// <summary>
    /// ADO.NET - Raw SQL with maximum control and performance.
    /// Demonstrates: SqlCommand, SqlDataReader, manual mapping, stored procedures.
    /// </summary>
    AdoNet,

    /// <summary>
    /// SqlKata Query Builder - Middle ground between ORM and raw SQL.
    /// Demonstrates: Fluent query building, compile-time safety, database portability.
    /// </summary>
    SqlKata,

    /// <summary>
    /// Entity Framework Core (Database-First) - Full-featured ORM with scaffolded entities.
    /// Demonstrates: LINQ queries, change tracking, expression trees, compiled queries.
    /// </summary>
    EFCore,

    /// <summary>
    /// Entity Framework Core Code-First - Full-featured ORM with code-first migrations.
    /// Demonstrates: Entity definition, Fluent API, migrations, data seeding, simplified schema.
    /// </summary>
    EFCoreCodeFirst
}
