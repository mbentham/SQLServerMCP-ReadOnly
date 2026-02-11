namespace SqlServerMcp.Configuration;

public static class SqlServerMcpOptionsExtensions
{
    public static SqlServerConnection ResolveServer(this SqlServerMcpOptions options, string serverName)
    {
        if (!options.Servers.TryGetValue(serverName, out var serverConfig))
        {
            throw new ArgumentException(
                $"Server '{serverName}' not found. Use list_servers to see available names.");
        }
        return serverConfig;
    }
}
