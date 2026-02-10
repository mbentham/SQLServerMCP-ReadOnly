using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using SqlServerMcp.Configuration;
using SqlServerMcp.Services;

var builder = Host.CreateApplicationBuilder(args);

// Route all logging to stderr (stdout is reserved for MCP stdio transport)
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Bind and validate configuration
builder.Services.Configure<SqlServerMcpOptions>(
    builder.Configuration.GetSection("SqlServerMcp"));
builder.Services.AddSingleton<IValidateOptions<SqlServerMcpOptions>, SqlServerMcpOptionsValidator>();

// Register services
builder.Services.AddSingleton<IRateLimitingService, RateLimitingService>();
builder.Services.AddSingleton<ISqlServerService, SqlServerService>();
builder.Services.AddSingleton<IDiagramService, DiagramService>();
builder.Services.AddSingleton<ITableDescribeService, TableDescribeService>();
builder.Services.AddSingleton<IFirstResponderService, FirstResponderService>();
builder.Services.AddSingleton<IDarlingDataService, DarlingDataService>();
builder.Services.AddSingleton<IWhoIsActiveService, WhoIsActiveService>();

// Read EnableDbaTools directly from configuration (needed before DI container is built)
var enableDbaTools = string.Equals(
    builder.Configuration["SqlServerMcp:EnableDbaTools"],
    "true",
    StringComparison.OrdinalIgnoreCase);

// Configure MCP server with appropriate tool set
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "sqlserver-mcp",
            Version = "1.0.0"
        };
    })
    .WithStdioServerTransport()
    .WithTools(ToolRegistry.GetToolTypes(enableDbaTools));

await builder.Build().RunAsync();
