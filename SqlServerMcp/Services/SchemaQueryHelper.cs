using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SqlServerMcp.Services;

internal static class SchemaQueryHelper
{
    internal sealed record TableInfo(string Schema, string Name);

    internal static string FormatDataType(string dataType, int maxLength, byte precision, byte scale)
    {
        return dataType.ToLowerInvariant() switch
        {
            "nvarchar" or "varchar" or "nchar" or "char" or "varbinary" or "binary"
                => maxLength == -1
                    ? $"{dataType}(MAX)"
                    : $"{dataType}({maxLength})",
            "decimal" or "numeric"
                => $"{dataType}({precision},{scale})",
            _ => dataType
        };
    }

    internal static async Task<List<TableInfo>> QueryTablesAsync(SqlConnection connection,
        string? schemaFilter, int maxTables, int commandTimeoutSeconds, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("""
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
            """);

        if (schemaFilter is not null)
            sql.Append(" AND TABLE_SCHEMA = @schemaFilter");

        sql.Append(" ORDER BY TABLE_SCHEMA, TABLE_NAME");

        await using var cmd = new SqlCommand(sql.ToString(), connection)
        {
            CommandTimeout = commandTimeoutSeconds
        };

        if (schemaFilter is not null)
            cmd.Parameters.AddWithValue("@schemaFilter", schemaFilter);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var tables = new List<TableInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (tables.Count >= maxTables)
                break;

            tables.Add(new TableInfo(reader.GetString(0), reader.GetString(1)));
        }

        return tables;
    }

    internal static (string CteSql, SqlParameter[] Parameters) BuildTableFilterCte(List<TableInfo> tables)
    {
        if (tables.Count == 0)
            throw new ArgumentException("At least one table is required to build a filter CTE.", nameof(tables));

        var sb = new StringBuilder();
        sb.Append("WITH table_filter AS (SELECT SchemaName, TableName FROM (VALUES ");

        var parameters = new SqlParameter[tables.Count * 2];
        for (var i = 0; i < tables.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(CultureInfo.InvariantCulture, $"(@s{i}, @t{i})");
            parameters[i * 2] = new SqlParameter($"@s{i}", tables[i].Schema);
            parameters[i * 2 + 1] = new SqlParameter($"@t{i}", tables[i].Name);
        }

        sb.Append(") AS t(SchemaName, TableName)) ");
        return (sb.ToString(), parameters);
    }
}
