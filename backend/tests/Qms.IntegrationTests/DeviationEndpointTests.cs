using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qms.Application.Deviations;
using Qms.Contracts.Common;
using Qms.Contracts.Deviations;

namespace Qms.IntegrationTests;

public sealed class DeviationEndpointTests
{
    [Fact]
    public async Task SearchDeviations_BindsServerSidePagingSortingAndFilters()
    {
        await using var factory = new SystemInfoApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDeviationService>();
                services.AddSingleton<IDeviationService, FakeDeviationService>();
            }));
        using var client = factory.CreateClient();
        var request = new DeviationSearchRequest(
            Page: 2,
            PageSize: 50,
            SortBy: "riskScore",
            SortDirection: "desc",
            Filters: [new ColumnFilterRequest("classification", "in", Values: ["Major", "Critical"])]);

        var response = await client.PostAsJsonAsync("/api/v1/deviations/search", request);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<DeviationListItemResponse>>();

        response.EnsureSuccessStatusCode();
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task CreateDeviation_ReturnsCreatedRecord()
    {
        await using var factory = new SystemInfoApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDeviationService>();
                services.AddSingleton<IDeviationService, FakeDeviationService>();
            }));
        using var client = factory.CreateClient();
        var now = DateTimeOffset.UtcNow;
        var request = new CreateDeviationRequest(
            "Dolum sıcaklığı sapması",
            "Dolum sıcaklığı onaylı limitin üzerine çıktı.",
            "Sıcaklık 25°C altında olmalıydı.",
            "Hat durduruldu ve ürün karantinaya alındı.",
            "Proses",
            "Üretim",
            "Dolum",
            now.AddHours(-2),
            now.AddHours(-1),
            3,
            3,
            3);

        var response = await client.PostAsJsonAsync("/api/v1/deviations", request);
        var result = await response.Content.ReadFromJsonAsync<DeviationResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal($"SP-{DateTimeOffset.UtcNow.Year}-000001", result.RecordNumber);
        Assert.Equal("Major", result.Classification);
        Assert.True(result.CapaRequired);
    }

    private sealed class FakeDeviationService : IDeviationService
    {
        public Task<IReadOnlyList<DeviationListItemResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DeviationListItemResponse>>([]);

        public Task<PagedResponse<DeviationListItemResponse>> SearchAsync(
            DeviationSearchRequest request,
            CancellationToken cancellationToken) => Task.FromResult(new PagedResponse<DeviationListItemResponse>(
                [], request.Page, request.PageSize, 0, 0));

        public Task<DeviationResponse?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DeviationResponse?>(null);

        public Task<DeviationResponse> CreateAsync(
            CreateDeviationRequest request,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new DeviationResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"SP-{now.Year}-000001",
                request.Title,
                request.Description,
                request.ExpectedState,
                request.ImmediateAction,
                request.DeviationType,
                request.DetectedDepartment,
                request.ProcessStage,
                request.OccurredAtUtc,
                request.DetectedAtUtc,
                now.AddDays(7),
                request.Likelihood,
                request.Severity,
                request.Detectability,
                27,
                "Major",
                true,
                "Draft",
                null,
                null,
                false,
                null,
                null,
                null,
                now,
                now,
                1));
        }

        public Task<DeviationResponse?> SubmitAsync(
            Guid id,
            SubmitDeviationRequest request,
            CancellationToken cancellationToken) => Task.FromResult<DeviationResponse?>(null);

        public Task<DeviationDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<DeviationDetailsResponse?>(null);

        public Task<DeviationDetailsResponse?> AddInvestigationAsync(
            Guid id,
            AddDeviationInvestigationRequest request,
            CancellationToken cancellationToken) => Task.FromResult<DeviationDetailsResponse?>(null);

        public Task<DeviationDetailsResponse?> AddBatchImpactAsync(
            Guid id,
            AddDeviationBatchImpactRequest request,
            CancellationToken cancellationToken) => Task.FromResult<DeviationDetailsResponse?>(null);

        public Task<DeviationDetailsResponse?> TransitionAsync(
            Guid id,
            TransitionDeviationRequest request,
            CancellationToken cancellationToken) => Task.FromResult<DeviationDetailsResponse?>(null);
    }
}
