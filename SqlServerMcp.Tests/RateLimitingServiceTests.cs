using Microsoft.Extensions.Options;
using SqlServerMcp.Configuration;
using SqlServerMcp.Services;

namespace SqlServerMcp.Tests;

public class RateLimitingServiceTests
{
    private static IOptions<SqlServerMcpOptions> MakeOptions(
        int maxConcurrent = 5, int maxPerMinute = 60) =>
        Options.Create(new SqlServerMcpOptions
        {
            Servers = new Dictionary<string, SqlServerConnection>
            {
                ["test"] = new() { ConnectionString = "Server=localhost;" }
            },
            MaxConcurrentQueries = maxConcurrent,
            MaxQueriesPerMinute = maxPerMinute
        });

    [Fact]
    public async Task AcquireAsync_SingleRequest_Succeeds()
    {
        using var service = new RateLimitingService(MakeOptions());

        using var lease = await service.AcquireAsync(CancellationToken.None);

        Assert.NotNull(lease);
    }

    [Fact]
    public async Task AcquireAsync_WithinConcurrencyLimit_AllSucceed()
    {
        using var service = new RateLimitingService(MakeOptions(maxConcurrent: 3));

        var leases = new List<IDisposable>();
        for (var i = 0; i < 3; i++)
        {
            leases.Add(await service.AcquireAsync(CancellationToken.None));
        }

        Assert.Equal(3, leases.Count);

        foreach (var lease in leases)
            lease.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_ThroughputExhausted_ThrowsInvalidOperation()
    {
        using var service = new RateLimitingService(MakeOptions(maxPerMinute: 2));

        // Consume all tokens
        using var lease1 = await service.AcquireAsync(CancellationToken.None);
        lease1.Dispose();
        using var lease2 = await service.AcquireAsync(CancellationToken.None);
        lease2.Dispose();

        // Third should fail
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AcquireAsync(CancellationToken.None));

        Assert.Contains("Rate limit exceeded", ex.Message);
    }

    [Fact]
    public async Task AcquireAsync_DisposingLease_ReleasesConcurrencySlot()
    {
        using var service = new RateLimitingService(MakeOptions(maxConcurrent: 1, maxPerMinute: 100));

        // Acquire and release
        var lease = await service.AcquireAsync(CancellationToken.None);
        lease.Dispose();

        // Should succeed because the slot was released
        using var lease2 = await service.AcquireAsync(CancellationToken.None);
        Assert.NotNull(lease2);
    }

    [Fact]
    public async Task AcquireAsync_CancellationToken_Respected()
    {
        using var service = new RateLimitingService(MakeOptions(maxConcurrent: 1));

        // Hold the only slot
        using var lease = await service.AcquireAsync(CancellationToken.None);

        // Try to acquire with an already-cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.AcquireAsync(cts.Token));
    }
}
