using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qms.Application.Dashboard;
using Qms.Contracts.Dashboard;

namespace Qms.IntegrationTests;

public sealed class DashboardEndpointTests
{
    [Fact]
    public async Task GetSummary_ReturnsOperationalMetrics()
    {
        await using var factory = new SystemInfoApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDashboardService>();
                services.AddSingleton<IDashboardService, FakeDashboardService>();
            }));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard/summary");
        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalDeviations);
        Assert.Equal(1, result.OpenDeviations);
        Assert.Single(result.RecentDeviations);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new DashboardSummaryResponse(
                3,
                1,
                1,
                0,
                1,
                [new DashboardDeviationResponse(
                    Guid.Parse("01991f70-6f40-7000-8000-000000000010"),
                    "SP-2026-000003",
                    "Sterilizasyon çevrimi sapması",
                    "QualityAssessment",
                    "Major",
                    36,
                    DateTimeOffset.UtcNow.AddDays(7))]));
    }
}
