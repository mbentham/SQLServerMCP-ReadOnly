using ModelContextProtocol;
using SqlServerMcp.Services;
using SqlServerMcp.Tools;

namespace SqlServerMcp.Tests;

public class GetPlantUMLDiagramToolTests
{
    private readonly StubDiagramService _stub = new();
    private readonly GetPlantUMLDiagramTool _tool;

    public GetPlantUMLDiagramToolTests()
    {
        _tool = new GetPlantUMLDiagramTool(_stub, new NoOpRateLimiter());
    }

    [Theory]
    [InlineData("diagram.txt")]
    [InlineData("diagram.plantuml")]
    [InlineData("diagram")]
    [InlineData("diagram.png")]
    public async Task NonPumlExtension_ThrowsMcpException(string filename)
    {
        var path = Path.Combine(Path.GetTempPath(), filename);

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _tool.GetDiagram("srv", "db", path, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains(".puml", ex.Message);
    }

    [Theory]
    [InlineData("diagram.puml")]
    [InlineData("diagram.PUML")]
    [InlineData("diagram.Puml")]
    public async Task PumlExtension_Succeeds(string filename)
    {
        var path = Path.Combine(Path.GetTempPath(), filename);

        try
        {
            var result = await _tool.GetDiagram("srv", "db", path, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Contains(".puml", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class StubDiagramService : IDiagramService
    {
        public Task<string> GenerateDiagramAsync(string serverName, string databaseName,
            IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
            IReadOnlyList<string>? includeTables, IReadOnlyList<string>? excludeTables,
            int maxTables, CancellationToken cancellationToken, bool compact = false)
            => Task.FromResult("@startuml\n@enduml\n");
    }
}
