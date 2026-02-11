using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;
using static SqlServerMcp.Services.SchemaQueryHelper;

namespace SqlServerMcp.Services;

public sealed class SchemaOverviewService : ISchemaOverviewService
{
    private readonly SqlServerMcpOptions _options;
    private readonly ILogger<SchemaOverviewService> _logger;

    public SchemaOverviewService(
        IOptions<SqlServerMcpOptions> options,
        ILogger<SchemaOverviewService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    internal sealed record ColumnInfo(
        string Schema, string TableName, string ColumnName, string DataType,
        int MaxLength, byte Precision, byte Scale, bool IsNullable,
        bool IsPrimaryKey, bool IsIdentity, string? DefaultDefinition);

    internal sealed record ForeignKeyInfo(
        string FkSchema, string FkTable, string FkColumn,
        string RefSchema, string RefTable, string RefColumn);

    internal sealed record CheckConstraintInfo(
        string Schema, string TableName, string? ColumnName, string Definition);

    internal sealed record UniqueColumnInfo(string Schema, string TableName, string ColumnName);

    public async Task<string> GenerateOverviewAsync(string serverName, string databaseName,
        string? schemaFilter, int maxTables, CancellationToken cancellationToken)
    {
        var serverConfig = _options.ResolveServer(serverName);

        _logger.LogInformation("Generating schema overview on server {Server} database {Database} schema {Schema}",
            serverName, databaseName, schemaFilter ?? "all");

        await using var connection = new SqlConnection(serverConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ChangeDatabaseAsync(databaseName, cancellationToken);

        var tables = await QueryTablesAsync(connection, schemaFilter, maxTables, _options.CommandTimeoutSeconds, cancellationToken);
        if (tables.Count == 0)
            return $"No tables found in database '{databaseName}' on server '{serverName}'" +
                   (schemaFilter is not null ? $" (schema filter: '{schemaFilter}')" : "") + ".";

        var columns = await QueryColumnsAsync(connection, tables, cancellationToken);
        var foreignKeys = await QueryForeignKeysAsync(connection, tables, cancellationToken);
        var checkConstraints = await QueryCheckConstraintsAsync(connection, tables, cancellationToken);
        var uniqueColumns = await QueryUniqueColumnsAsync(connection, tables, cancellationToken);

        return BuildMarkdown(serverName, databaseName, schemaFilter, maxTables,
            tables, columns, foreignKeys, checkConstraints, uniqueColumns);
    }

    private async Task<List<ColumnInfo>> QueryColumnsAsync(SqlConnection connection,
        List<TableInfo> tables, CancellationToken cancellationToken)
    {
        var (cteSql, cteParams) = BuildTableFilterCte(tables);

        var sql = cteSql + """
            SELECT
                c.TABLE_SCHEMA,
                c.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                COALESCE(c.CHARACTER_MAXIMUM_LENGTH, 0) AS MaxLength,
                CAST(COALESCE(c.NUMERIC_PRECISION, 0) AS tinyint) AS [Precision],
                CAST(COALESCE(c.NUMERIC_SCALE, 0) AS tinyint) AS Scale,
                CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable,
                CASE WHEN ixc.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                sc.is_identity AS IsIdentity,
                dc.definition AS DefaultDefinition
            FROM INFORMATION_SCHEMA.COLUMNS c
            INNER JOIN table_filter dt ON dt.SchemaName = c.TABLE_SCHEMA AND dt.TableName = c.TABLE_NAME
            INNER JOIN sys.columns sc
                ON sc.object_id = OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME))
                AND sc.name = c.COLUMN_NAME
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.index_columns ic
                INNER JOIN sys.indexes ix ON ix.object_id = ic.object_id AND ix.index_id = ic.index_id
                WHERE ix.is_primary_key = 1
            ) ixc ON ixc.object_id = OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA) + '.' + QUOTENAME(c.TABLE_NAME))
                AND ixc.column_id = sc.column_id
            LEFT JOIN sys.default_constraints dc
                ON dc.parent_object_id = sc.object_id AND dc.parent_column_id = sc.column_id
            ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
            """;

        await using var cmd = new SqlCommand(sql, connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        cmd.Parameters.AddRange(cteParams);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var columns = new List<ColumnInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo(
                Schema: reader.GetString(0),
                TableName: reader.GetString(1),
                ColumnName: reader.GetString(2),
                DataType: reader.GetString(3),
                MaxLength: reader.GetInt32(4),
                Precision: reader.GetByte(5),
                Scale: reader.GetByte(6),
                IsNullable: reader.GetInt32(7) == 1,
                IsPrimaryKey: reader.GetInt32(8) == 1,
                IsIdentity: reader.GetBoolean(9),
                DefaultDefinition: reader.IsDBNull(10) ? null : reader.GetString(10)));
        }

        return columns;
    }

    private async Task<List<ForeignKeyInfo>> QueryForeignKeysAsync(SqlConnection connection,
        List<TableInfo> tables, CancellationToken cancellationToken)
    {
        var (cteSql, cteParams) = BuildTableFilterCte(tables);

        var sql = cteSql + """
            SELECT
                SCHEMA_NAME(fk_tab.schema_id) AS FkSchema,
                fk_tab.name AS FkTable,
                fk_col.name AS FkColumn,
                SCHEMA_NAME(ref_tab.schema_id) AS RefSchema,
                ref_tab.name AS RefTable,
                ref_col.name AS RefColumn
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            INNER JOIN sys.tables fk_tab ON fk_tab.object_id = fkc.parent_object_id
            INNER JOIN sys.columns fk_col ON fk_col.object_id = fkc.parent_object_id AND fk_col.column_id = fkc.parent_column_id
            INNER JOIN sys.tables ref_tab ON ref_tab.object_id = fkc.referenced_object_id
            INNER JOIN sys.columns ref_col ON ref_col.object_id = fkc.referenced_object_id AND ref_col.column_id = fkc.referenced_column_id
            INNER JOIN table_filter dt
                ON dt.SchemaName = SCHEMA_NAME(fk_tab.schema_id) AND dt.TableName = fk_tab.name
            ORDER BY fk_tab.name, fk_col.name
            """;

        await using var cmd = new SqlCommand(sql, connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        cmd.Parameters.AddRange(cteParams);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var foreignKeys = new List<ForeignKeyInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeyInfo(
                FkSchema: reader.GetString(0),
                FkTable: reader.GetString(1),
                FkColumn: reader.GetString(2),
                RefSchema: reader.GetString(3),
                RefTable: reader.GetString(4),
                RefColumn: reader.GetString(5)));
        }

        return foreignKeys;
    }

    private async Task<List<CheckConstraintInfo>> QueryCheckConstraintsAsync(SqlConnection connection,
        List<TableInfo> tables, CancellationToken cancellationToken)
    {
        var (cteSql, cteParams) = BuildTableFilterCte(tables);

        var sql = cteSql + """
            SELECT
                SCHEMA_NAME(t.schema_id) AS SchemaName,
                t.name AS TableName,
                col.name AS ColumnName,
                cc.definition AS Definition
            FROM sys.check_constraints cc
            INNER JOIN sys.tables t ON t.object_id = cc.parent_object_id
            INNER JOIN table_filter dt
                ON dt.SchemaName = SCHEMA_NAME(t.schema_id) AND dt.TableName = t.name
            LEFT JOIN sys.columns col
                ON col.object_id = cc.parent_object_id AND col.column_id = cc.parent_column_id
                AND cc.parent_column_id > 0
            ORDER BY t.name, cc.name
            """;

        await using var cmd = new SqlCommand(sql, connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        cmd.Parameters.AddRange(cteParams);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var constraints = new List<CheckConstraintInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            constraints.Add(new CheckConstraintInfo(
                Schema: reader.GetString(0),
                TableName: reader.GetString(1),
                ColumnName: reader.IsDBNull(2) ? null : reader.GetString(2),
                Definition: reader.GetString(3)));
        }

        return constraints;
    }

    private async Task<List<UniqueColumnInfo>> QueryUniqueColumnsAsync(SqlConnection connection,
        List<TableInfo> tables, CancellationToken cancellationToken)
    {
        var (cteSql, cteParams) = BuildTableFilterCte(tables);

        // Single-column unique constraints/indexes (excluding PKs)
        var sql = cteSql + """
            SELECT
                SCHEMA_NAME(t.schema_id) AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            INNER JOIN sys.tables t ON t.object_id = i.object_id
            INNER JOIN table_filter dt
                ON dt.SchemaName = SCHEMA_NAME(t.schema_id) AND dt.TableName = t.name
            WHERE i.is_unique = 1
              AND i.is_primary_key = 0
              AND NOT EXISTS (
                  SELECT 1 FROM sys.index_columns ic2
                  WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id
                    AND ic2.column_id <> ic.column_id
              )
            ORDER BY t.name, c.name
            """;

        await using var cmd = new SqlCommand(sql, connection)
        {
            CommandTimeout = _options.CommandTimeoutSeconds
        };
        cmd.Parameters.AddRange(cteParams);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var uniques = new List<UniqueColumnInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            uniques.Add(new UniqueColumnInfo(
                Schema: reader.GetString(0),
                TableName: reader.GetString(1),
                ColumnName: reader.GetString(2)));
        }

        return uniques;
    }

    internal static string BuildMarkdown(string serverName, string databaseName,
        string? schemaFilter, int maxTables, List<TableInfo> tables,
        List<ColumnInfo> columns, List<ForeignKeyInfo> foreignKeys,
        List<CheckConstraintInfo> checkConstraints, List<UniqueColumnInfo> uniqueColumns)
    {
        // Build lookups for FK, check constraint, and unique annotations
        var fkLookup = foreignKeys
            .ToLookup(fk => (fk.FkSchema, fk.FkTable, fk.FkColumn));

        var checkByColumn = checkConstraints
            .Where(c => c.ColumnName is not null)
            .ToLookup(c => (c.Schema, c.TableName, c.ColumnName!));

        var checkByTable = checkConstraints
            .Where(c => c.ColumnName is null)
            .ToLookup(c => (c.Schema, c.TableName));

        var uniqueSet = new HashSet<(string Schema, string Table, string Column)>(
            uniqueColumns.Select(u => (u.Schema, u.TableName, u.ColumnName)));

        // Group columns by table
        var columnsByTable = columns
            .GroupBy(c => new TableInfo(c.Schema, c.TableName))
            .ToDictionary(g => g.Key, g => g.ToList());

        var sb = new StringBuilder();
        sb.AppendLine($"# Schema: {SanitizeMarkdownCell(databaseName)} on {SanitizeMarkdownCell(serverName)}");
        sb.AppendLine($"Tables: {tables.Count}" +
                       (schemaFilter is not null ? $" | Schema: {SanitizeMarkdownCell(schemaFilter)}" : "") +
                       (tables.Count >= maxTables ? $" | **Truncated at {maxTables}**" : ""));
        sb.AppendLine();

        foreach (var table in tables)
        {
            var tableName = table.Schema == "dbo"
                ? SanitizeMarkdownCell(table.Name)
                : $"{SanitizeMarkdownCell(table.Schema)}.{SanitizeMarkdownCell(table.Name)}";

            sb.AppendLine($"## {tableName}");
            sb.AppendLine();

            if (!columnsByTable.TryGetValue(table, out var tableCols))
            {
                sb.AppendLine("*No columns found*");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine("| Column | Type | Null | Key | Extra |");
            sb.AppendLine("|--------|------|------|-----|-------|");

            foreach (var col in tableCols)
            {
                var type = SanitizeMarkdownCell(FormatDataType(col.DataType, col.MaxLength, col.Precision, col.Scale));
                var nullable = col.IsNullable ? "YES" : "NO";

                // Build Key column
                var keys = new List<string>();
                if (col.IsPrimaryKey)
                    keys.Add("PK");
                if (fkLookup.Contains((col.Schema, col.TableName, col.ColumnName)))
                {
                    foreach (var fk in fkLookup[(col.Schema, col.TableName, col.ColumnName)])
                    {
                        var refTable = fk.RefSchema == "dbo"
                            ? SanitizeMarkdownCell(fk.RefTable)
                            : $"{SanitizeMarkdownCell(fk.RefSchema)}.{SanitizeMarkdownCell(fk.RefTable)}";
                        keys.Add($"FK {refTable}.{SanitizeMarkdownCell(fk.RefColumn)}");
                    }
                }
                if (uniqueSet.Contains((col.Schema, col.TableName, col.ColumnName)))
                    keys.Add("UQ");
                var key = string.Join(", ", keys);

                // Build Extra column
                var extras = new List<string>();
                if (col.IsIdentity)
                    extras.Add("IDENTITY");
                if (col.DefaultDefinition is not null)
                    extras.Add($"DEFAULT {SanitizeMarkdownCell(col.DefaultDefinition)}");
                if (checkByColumn.Contains((col.Schema, col.TableName, col.ColumnName)))
                {
                    foreach (var chk in checkByColumn[(col.Schema, col.TableName, col.ColumnName)])
                        extras.Add($"CHK: {SanitizeMarkdownCell(chk.Definition)}");
                }
                var extra = string.Join(", ", extras);

                sb.AppendLine($"| {SanitizeMarkdownCell(col.ColumnName)} | {type} | {nullable} | {key} | {extra} |");
            }

            // Table-level check constraints
            if (checkByTable.Contains((table.Schema, table.Name)))
            {
                foreach (var chk in checkByTable[(table.Schema, table.Name)])
                    sb.AppendLine($"| | | | | CHK: {SanitizeMarkdownCell(chk.Definition)} |");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes characters that could break markdown table structure or inject markdown syntax.
    /// </summary>
    internal static string SanitizeMarkdownCell(string input)
        => input.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
}
