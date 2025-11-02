using Microsoft.Data.SqlClient;

namespace DbDemo.Scaffolding;

public class SchemaReader
{
    private readonly string _connectionString;

    public SchemaReader(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<TableSchema>> ReadSchemaAsync(CancellationToken cancellationToken = default)
    {
        var tables = new List<TableSchema>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Query to get all tables and their columns from INFORMATION_SCHEMA
        const string query = @"
            SELECT
                t.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.CHARACTER_MAXIMUM_LENGTH
            FROM INFORMATION_SCHEMA.TABLES t
            INNER JOIN INFORMATION_SCHEMA.COLUMNS c
                ON t.TABLE_NAME = c.TABLE_NAME
                AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
            WHERE t.TABLE_TYPE = 'BASE TABLE'
                AND t.TABLE_SCHEMA = 'dbo'
                AND t.TABLE_NAME != '__MigrationHistory'
                AND t.TABLE_NAME != 'sysdiagrams'
            ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION";

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        TableSchema? currentTable = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString(0);
            var columnName = reader.GetString(1);
            var dataType = reader.GetString(2);
            var isNullable = reader.GetString(3) == "YES";
            var maxLength = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);

            if (currentTable == null || currentTable.TableName != tableName)
            {
                currentTable = new TableSchema { TableName = tableName };
                tables.Add(currentTable);
            }

            currentTable.Columns.Add(new ColumnSchema
            {
                ColumnName = columnName,
                DataType = dataType,
                IsNullable = isNullable,
                MaxLength = maxLength
            });
        }

        return tables;
    }
}
