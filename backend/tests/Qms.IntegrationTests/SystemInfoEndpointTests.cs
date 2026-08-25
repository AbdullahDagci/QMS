using Microsoft.AspNetCore.Mvc.Testing;

namespace Qms.IntegrationTests;

public sealed class SystemInfoEndpointTests : IClassFixture<SystemInfoApiFactory>
{
    private readonly HttpClient _client;

    public SystemInfoEndpointTests(SystemInfoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSystemInfo_ReturnsSingleTenantAndSixteenModules()
    {
        var response = await _client.GetAsync("/api/v1/system/info", CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.EnsureSuccessStatusCode();
        Assert.Contains("\"singleTenant\":true", body, StringComparison.Ordinal);
        Assert.Equal(16, body.Split("\"code\":", StringSplitOptions.None).Length - 1);
    }
}

public sealed class SystemInfoApiFactory : WebApplicationFactory<Program>;
