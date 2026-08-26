using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qms.Application.ChangeControls;
using Qms.Contracts.ChangeControls;
using Qms.Contracts.Common;

namespace Qms.IntegrationTests;

public sealed class ChangeControlEndpointTests
{
    [Fact]
    public async Task Lookups_return_database_codes_and_user_facing_names()
    {
        await using var factory = Factory(); using var client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-QMS-Profile", "admin");
        var response = await client.GetAsync("/api/v1/change-controls/lookups"); var result = await response.Content.ReadFromJsonAsync<ChangeControlLookupsResponse>();
        response.EnsureSuccessStatusCode(); Assert.NotNull(result); Assert.Contains(result.ChangeTypes, x => x.Code == "Equipment" && x.Name == "Ekipman");
    }

    [Fact]
    public async Task Lookup_definition_management_is_administrator_only()
    {
        await using var factory = Factory();
        using var viewer = factory.CreateClient(); viewer.DefaultRequestHeaders.Add("X-QMS-Profile", "viewer");
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/v1/change-controls/lookup-definitions")).StatusCode);
        using var admin = factory.CreateClient(); admin.DefaultRequestHeaders.Add("X-QMS-Profile", "admin");
        var response = await admin.GetAsync("/api/v1/change-controls/lookup-definitions"); response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ChangeLookupDefinitionResponse[]>(); Assert.Single(result!);
    }

    private static WebApplicationFactory<Program> Factory() => new SystemInfoApiFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services => { services.RemoveAll<IChangeControlService>(); services.AddSingleton<IChangeControlService, FakeService>(); }));

    private sealed class FakeService : IChangeControlService
    {
        private static readonly ChangeLookupDefinitionResponse Definition = new(Guid.Parse("019cc000-0000-7000-8000-000000000001"), "ChangeType", "Equipment", "Ekipman", 10, true);
        public Task<ChangeControlLookupsResponse> GetLookupsAsync(CancellationToken ct) => Task.FromResult(new ChangeControlLookupsResponse([], [], [new("Equipment", "Ekipman")], [new("Medium", "Orta")], [new("None", "Ruhsat etkisi yok")], [new("Implementation", "Uygulama")]));
        public Task<IReadOnlyList<ChangeLookupDefinitionResponse>> ListLookupDefinitionsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ChangeLookupDefinitionResponse>>([Definition]);
        public Task<ChangeLookupDefinitionResponse> CreateLookupDefinitionAsync(CreateChangeLookupDefinitionRequest request, CancellationToken ct) => Task.FromResult(Definition);
        public Task<ChangeLookupDefinitionResponse?> UpdateLookupDefinitionAsync(Guid id, UpdateChangeLookupDefinitionRequest request, CancellationToken ct) => Task.FromResult<ChangeLookupDefinitionResponse?>(Definition);
        public Task<PagedResponse<ChangeControlListItemResponse>> SearchAsync(ChangeControlSearchRequest request, CancellationToken ct) => Task.FromResult(new PagedResponse<ChangeControlListItemResponse>([], request.Page, request.PageSize, 0, 0));
        public Task<ChangeControlDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct) => Task.FromResult<ChangeControlDetailsResponse?>(null);
        public Task<ChangeControlDetailsResponse> CreateAsync(CreateChangeControlRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<ChangeControlDetailsResponse?> CompleteAssessmentAsync(Guid id, Guid assessmentId, CompleteChangeAssessmentRequest request, CancellationToken ct) => Task.FromResult<ChangeControlDetailsResponse?>(null);
        public Task<ChangeControlDetailsResponse?> AddActionAsync(Guid id, AddChangeActionRequest request, CancellationToken ct) => Task.FromResult<ChangeControlDetailsResponse?>(null);
        public Task<ChangeControlDetailsResponse?> CompleteActionAsync(Guid id, Guid actionId, CompleteChangeActionRequest request, CancellationToken ct) => Task.FromResult<ChangeControlDetailsResponse?>(null);
        public Task<ChangeControlDetailsResponse?> VerifyActionAsync(Guid id, Guid actionId, VerifyChangeActionRequest request, CancellationToken ct) => Task.FromResult<ChangeControlDetailsResponse?>(null);
        public Task<ChangeControlDetailsResponse?> SetAuthorityApprovalAsync(Guid id, SetAuthorityApprovalRequest request, CancellationToken ct) => Task.FromResult<ChangeControlDetailsResponse?>(null);
        public Task<ChangeControlDetailsResponse?> TransitionAsync(Guid id, TransitionChangeControlRequest request, CancellationToken ct) => Task.FromResult<ChangeControlDetailsResponse?>(null);
    }
}
