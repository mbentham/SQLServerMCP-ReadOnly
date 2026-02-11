using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tests;

public class StoredProcedureServiceBaseTests
{
    /// <summary>
    /// Concrete subclass that exposes protected members for testing.
    /// </summary>
    private sealed class TestableService : StoredProcedureServiceBase
    {
        public TestableService(
            IOptions<SqlServerMcpOptions> options,
            HashSet<string> allowedProcedures,
            string[] blockedParameters)
            : base(options, NullLogger.Instance, allowedProcedures, blockedParameters,
                "blocked for testing", "procedure not found message")
        {
        }

        public Task<string> CallExecuteProcedureAsync(
            string serverName, string procedureName,
            Dictionary<string, object?> parameters, CancellationToken ct)
            => ExecuteProcedureAsync(serverName, procedureName, parameters, ct);

        public static void CallAddBoolParam(Dictionary<string, object?> p, string name, bool? value)
            => AddBoolParam(p, name, value);

        public static void CallAddIfNotNull(Dictionary<string, object?> p, string name, object? value)
            => AddIfNotNull(p, name, value);
    }

    private static IOptions<SqlServerMcpOptions> MakeOptions() =>
        Options.Create(new SqlServerMcpOptions
        {
            Servers = new Dictionary<string, SqlServerConnection>
            {
                ["testserver"] = new() { ConnectionString = "Server=localhost;" }
            }
        });

    // ───────────────────────────────────────────────
    // ExecuteProcedureAsync — whitelist enforcement
    // ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteProcedureAsync_NonWhitelistedProcedure_ThrowsInvalidOperation()
    {
        var service = new TestableService(
            MakeOptions(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sp_Allowed" },
            []);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CallExecuteProcedureAsync("testserver", "sp_Evil",
                new Dictionary<string, object?>(), CancellationToken.None));

        Assert.Contains("sp_Evil", ex.Message);
        Assert.Contains("not in the allowed list", ex.Message);
    }

    // ───────────────────────────────────────────────
    // ExecuteProcedureAsync — blocked parameter enforcement
    // ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteProcedureAsync_BlockedParameter_ThrowsInvalidOperation()
    {
        var service = new TestableService(
            MakeOptions(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sp_Allowed" },
            ["@OutputTableName"]);

        var parameters = new Dictionary<string, object?> { ["@OutputTableName"] = "HackerTable" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CallExecuteProcedureAsync("testserver", "sp_Allowed",
                parameters, CancellationToken.None));

        Assert.Contains("@OutputTableName", ex.Message);
        Assert.Contains("not allowed", ex.Message);
    }

    [Fact]
    public async Task ExecuteProcedureAsync_BlockedParameterCaseInsensitive_ThrowsInvalidOperation()
    {
        var service = new TestableService(
            MakeOptions(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sp_Allowed" },
            ["@OutputTableName"]);

        var parameters = new Dictionary<string, object?> { ["@OUTPUTTABLENAME"] = "HackerTable" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CallExecuteProcedureAsync("testserver", "sp_Allowed",
                parameters, CancellationToken.None));

        Assert.Contains("not allowed", ex.Message);
    }

    // ───────────────────────────────────────────────
    // AddBoolParam
    // ───────────────────────────────────────────────

    [Fact]
    public void AddBoolParam_True_MapsTo1()
    {
        var p = new Dictionary<string, object?>();
        TestableService.CallAddBoolParam(p, "@Flag", true);
        Assert.Equal(1, p["@Flag"]);
    }

    [Fact]
    public void AddBoolParam_False_MapsTo0()
    {
        var p = new Dictionary<string, object?>();
        TestableService.CallAddBoolParam(p, "@Flag", false);
        Assert.Equal(0, p["@Flag"]);
    }

    [Fact]
    public void AddBoolParam_Null_AddsNothing()
    {
        var p = new Dictionary<string, object?>();
        TestableService.CallAddBoolParam(p, "@Flag", null);
        Assert.Empty(p);
    }

    // ───────────────────────────────────────────────
    // AddIfNotNull
    // ───────────────────────────────────────────────

    [Fact]
    public void AddIfNotNull_WithValue_AddsParameter()
    {
        var p = new Dictionary<string, object?>();
        TestableService.CallAddIfNotNull(p, "@Name", "test");
        Assert.Equal("test", p["@Name"]);
    }

    [Fact]
    public void AddIfNotNull_WithNull_AddsNothing()
    {
        var p = new Dictionary<string, object?>();
        TestableService.CallAddIfNotNull(p, "@Name", null);
        Assert.Empty(p);
    }

    // ───────────────────────────────────────────────
    // FormatValue — DateTime
    // ───────────────────────────────────────────────

    [Fact]
    public void FormatValue_DateTime_ReturnsRoundTripFormat()
    {
        var dt = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var result = StoredProcedureServiceBase.FormatValue(dt);
        Assert.IsType<string>(result);
        Assert.Equal(dt.ToString("O"), (string)result);
    }

    [Fact]
    public void FormatValue_DateTime_Unspecified_ReturnsRoundTripFormat()
    {
        var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var result = StoredProcedureServiceBase.FormatValue(dt);
        Assert.Equal(dt.ToString("O"), (string)result);
    }

    // ───────────────────────────────────────────────
    // FormatValue — DateTimeOffset
    // ───────────────────────────────────────────────

    [Fact]
    public void FormatValue_DateTimeOffset_ReturnsRoundTripFormat()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(-5));
        var result = StoredProcedureServiceBase.FormatValue(dto);
        Assert.IsType<string>(result);
        Assert.Equal(dto.ToString("O"), (string)result);
    }

    [Fact]
    public void FormatValue_DateTimeOffset_Utc_ReturnsRoundTripFormat()
    {
        var dto = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var result = StoredProcedureServiceBase.FormatValue(dto);
        Assert.Equal(dto.ToString("O"), (string)result);
    }

    // ───────────────────────────────────────────────
    // FormatValue — byte[]
    // ───────────────────────────────────────────────

    [Fact]
    public void FormatValue_ByteArray_ReturnsBase64String()
    {
        var bytes = new byte[] { 0x00, 0xFF, 0x42, 0xAB };
        var result = StoredProcedureServiceBase.FormatValue(bytes);
        Assert.IsType<string>(result);
        Assert.Equal(Convert.ToBase64String(bytes), (string)result);
    }

    [Fact]
    public void FormatValue_EmptyByteArray_ReturnsEmptyBase64()
    {
        var bytes = Array.Empty<byte>();
        var result = StoredProcedureServiceBase.FormatValue(bytes);
        Assert.Equal(string.Empty, (string)result);
    }

    // ───────────────────────────────────────────────
    // FormatValue — pass-through (default branch)
    // ───────────────────────────────────────────────

    [Fact]
    public void FormatValue_Int_ReturnsUnchanged()
    {
        var result = StoredProcedureServiceBase.FormatValue(42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void FormatValue_String_ReturnsUnchanged()
    {
        var result = StoredProcedureServiceBase.FormatValue("hello");
        Assert.Equal("hello", result);
    }

    [Fact]
    public void FormatValue_Decimal_ReturnsUnchanged()
    {
        var result = StoredProcedureServiceBase.FormatValue(3.14m);
        Assert.Equal(3.14m, result);
    }

    [Fact]
    public void FormatValue_Bool_ReturnsUnchanged()
    {
        var result = StoredProcedureServiceBase.FormatValue(true);
        Assert.Equal(true, result);
    }

    [Fact]
    public void FormatValue_Guid_ReturnsUnchanged()
    {
        var guid = Guid.NewGuid();
        var result = StoredProcedureServiceBase.FormatValue(guid);
        Assert.Equal(guid, result);
    }

    [Fact]
    public void FormatValue_Long_ReturnsUnchanged()
    {
        var result = StoredProcedureServiceBase.FormatValue(9999999999L);
        Assert.Equal(9999999999L, result);
    }
}
