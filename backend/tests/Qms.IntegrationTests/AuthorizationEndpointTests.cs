using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qms.Application.Administration;
using Qms.Application.Security;
using Qms.Contracts.Administration;
using Qms.Contracts.Security;

namespace Qms.IntegrationTests;

public sealed class AuthorizationEndpointTests : IClassFixture<SystemInfoApiFactory>
{
    private readonly SystemInfoApiFactory factory;

    public AuthorizationEndpointTests(SystemInfoApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task ReporterProfile_ReceivesDeviationCreateButNotCapaPlanPermission()
    {
        using var client = CreateClient("reporter");

        var user = await client.GetFromJsonAsync<JsonDocument>("/api/v1/auth/me");
        var permissions = user!.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(item => item.GetString()).ToArray();

        Assert.Contains("deviation.create", permissions);
        Assert.DoesNotContain("capa.plan", permissions);
    }

    [Fact]
    public async Task QuickProfiles_CoverEverySystemRoleAndExposeDepartment()
    {
        using var client = factory.CreateClient();

        var profiles = await client.GetFromJsonAsync<List<DevelopmentProfileResponse>>("/api/v1/auth/quick-profiles");

        Assert.NotNull(profiles);
        var assignedRoles = profiles.SelectMany(profile => profile.Roles).Distinct().ToArray();
        Assert.All(QmsRoles.All, role => Assert.Contains(role, assignedRoles));
        Assert.Contains(profiles, profile => profile.Key == "learner" && profile.DepartmentName == "Üretim" && profile.Roles.Contains(QmsRoles.Learner));
        Assert.Contains(profiles, profile => profile.Key == "trainer" && profile.DepartmentName == "Kalite Güvence" && profile.Roles.Contains(QmsRoles.Trainer));
    }

    [Fact]
    public async Task ViewerProfile_CannotCreateDeviation()
    {
        using var client = CreateClient("viewer");

        var response = await client.PostAsJsonAsync("/api/v1/deviations", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewerProfile_CannotCreateComplaint()
    {
        using var client = CreateClient("viewer");

        var response = await client.PostAsJsonAsync("/api/v1/complaints", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewerProfile_CannotManageTrainingLookups()
    {
        using var client = CreateClient("viewer");

        var response = await client.GetAsync("/api/v1/trainings/lookup-definitions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ActionOwnerProfile_CannotVerifyCapaAction()
    {
        using var client = CreateClient("action-owner");
        var id = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/v1/capas/{id}/actions/{id}/verify", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task QualityProfile_CannotOpenAccessAdministration()
    {
        using var client = CreateClient("quality");

        var response = await client.GetAsync("/api/v1/admin/access/overview");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewerProfile_CannotManageInternalAuditLookups()
    {
        using var client = CreateClient("viewer");
        var response = await client.GetAsync("/api/v1/internal-audits/lookup-definitions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorProfile_CanOpenAccessAdministration()
    {
        await using var isolatedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAccessAdministrationService>();
                services.AddSingleton<IAccessAdministrationService, FakeAccessAdministrationService>();
            }));
        using var client = isolatedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-QMS-Profile", "admin");

        var response = await client.GetAsync("/api/v1/admin/access/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClient(string profile)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-QMS-Profile", profile);
        return client;
    }

    private sealed class FakeAccessAdministrationService : IAccessAdministrationService
    {
        public Task<AccessOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new AccessOverviewResponse([], [], [], [], [], []));

        public Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DepartmentResponse?> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<DepartmentResponse?>(null);

        public Task<AccessUserResponse?> UpdateUserAsync(Guid userId, UpdateUserAccessRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<AccessUserResponse?>(null);

        public Task<AccessUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DelegationResponse> CreateDelegationAsync(CreateDelegationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RevokeDelegationAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<WorkflowAssignmentResponse>> GetAssignmentsAsync(string aggregateType, Guid aggregateId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WorkflowAssignmentResponse>>([]);

        public Task<WorkflowAssignmentResponse> CreateAssignmentAsync(CreateWorkflowAssignmentRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
