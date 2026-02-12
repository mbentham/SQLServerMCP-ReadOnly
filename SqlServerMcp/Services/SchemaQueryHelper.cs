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

    internal static (string Sql, List<SqlParameter> Parameters) BuildTableQuery(
        string? includeSchema, IReadOnlyList<string>? excludeSchemas)
    {
        var sql = new StringBuilder("""
            SELECT s.name AS SchemaName, t.name AS TableName
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
              AND t.temporal_type <> 2
            """);

        var parameters = new List<SqlParameter>();

        if (includeSchema is not null)
        {
            sql.Append(" AND s.name = @includeSchema");
            parameters.Add(new SqlParameter("@includeSchema", includeSchema));
        }
        else if (excludeSchemas is { Count: > 0 })
        {
            for (var i = 0; i < excludeSchemas.Count; i++)
            {
                sql.Append(i == 0 ? " AND s.name NOT IN (" : ", ");
                sql.Append(CultureInfo.InvariantCulture, $"@excl{i}");
                parameters.Add(new SqlParameter($"@excl{i}", excludeSchemas[i]));
            }
            sql.Append(')');
        }

        sql.Append(" ORDER BY s.name, t.name");
        return (sql.ToString(), parameters);
    }

    internal static async Task<List<TableInfo>> QueryTablesAsync(SqlConnection connection,
        string? includeSchema, IReadOnlyList<string>? excludeSchemas, int maxTables, int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var (sql, parameters) = BuildTableQuery(includeSchema, excludeSchemas);

        await using var cmd = new SqlCommand(sql, connection)
        {
            CommandTimeout = commandTimeoutSeconds
        };

        foreach (var p in parameters)
            cmd.Parameters.Add(p);

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
