namespace SqlServerMcp.Configuration;

public sealed class SqlServerMcpOptions
{
    public Dictionary<string, SqlServerConnection> Servers { get; set; } = new();
    public int MaxRows { get; set; } = 1000;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnableDbaTools { get; set; }
}
