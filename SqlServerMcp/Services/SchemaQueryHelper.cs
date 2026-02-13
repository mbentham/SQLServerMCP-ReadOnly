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
        IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
        IReadOnlyList<string>? includeTables = null, IReadOnlyList<string>? excludeTables = null)
    {
        var sql = new StringBuilder("""
            SELECT s.name AS SchemaName, t.name AS TableName
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
              AND t.temporal_type <> 2
            """);

        var parameters = new List<SqlParameter>();

        AppendFilter(sql, parameters, "s.name", includeSchemas, "inclSchema", negate: false);
        AppendFilter(sql, parameters, "s.name", excludeSchemas, "excl", negate: true,
            skip: includeSchemas is { Count: > 0 });

        AppendFilter(sql, parameters, "t.name", includeTables, "incTbl", negate: false);
        AppendFilter(sql, parameters, "t.name", excludeTables, "exclTbl", negate: true,
            skip: includeTables is { Count: > 0 });

        sql.Append(" ORDER BY s.name, t.name");
        return (sql.ToString(), parameters);
    }

    private static void AppendFilter(StringBuilder sql, List<SqlParameter> parameters,
        string column, IReadOnlyList<string>? values, string paramPrefix, bool negate, bool skip = false)
    {
        if (skip || values is not { Count: > 0 })
            return;

        var keyword = negate ? "NOT IN" : "IN";
        for (var i = 0; i < values.Count; i++)
        {
            sql.Append(i == 0 ? $" AND {column} {keyword} (" : ", ");
            sql.Append(CultureInfo.InvariantCulture, $"@{paramPrefix}{i}");
            parameters.Add(new SqlParameter($"@{paramPrefix}{i}", values[i]));
        }
        sql.Append(')');
    }

    internal static async Task<List<TableInfo>> QueryTablesAsync(SqlConnection connection,
        IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
        IReadOnlyList<string>? includeTables, IReadOnlyList<string>? excludeTables,
        int maxTables, int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        var (sql, parameters) = BuildTableQuery(includeSchemas, excludeSchemas, includeTables, excludeTables);

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
