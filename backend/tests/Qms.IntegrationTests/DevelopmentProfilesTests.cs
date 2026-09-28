using Microsoft.Extensions.Configuration;
using Qms.Infrastructure.Identity;

namespace Qms.IntegrationTests;

public sealed class DevelopmentProfilesTests
{
    [Theory]
    [InlineData(true, null, true)]
    [InlineData(false, null, false)]
    [InlineData(false, "false", false)]
    [InlineData(false, "true", true)]
    public void AreEnabled_RequiresDevelopmentOrExplicitDemoMode(bool isDevelopment, string? demoMode, bool expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["DemoMode:Enabled"] = demoMode }).Build();

        Assert.Equal(expected, DevelopmentProfiles.AreEnabled(isDevelopment, configuration));
    }
}
