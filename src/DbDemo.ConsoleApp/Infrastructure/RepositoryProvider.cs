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
    /// Entity Framework Core - Full-featured ORM.
    /// Demonstrates: LINQ queries, change tracking, expression trees, compiled queries.
    /// </summary>
    EFCore
}
