namespace SqlServerMcp.Services;

public interface ITableDescribeService
{
    Task<string> DescribeTableAsync(string serverName, string databaseName,
        string schemaName, string tableName, CancellationToken cancellationToken);
}
