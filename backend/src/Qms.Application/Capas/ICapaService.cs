using Qms.Contracts.Capas;
using Qms.Contracts.Common;

namespace Qms.Application.Capas;

public interface ICapaService
{
    Task<CapaLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken);
    Task<PagedResponse<CapaListItemResponse>> SearchAsync(CapaSearchRequest request, CancellationToken cancellationToken);
    Task<CapaDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<CapaDetailsResponse> CreateAsync(CreateCapaRequest request, CancellationToken cancellationToken);
    Task<CapaDetailsResponse?> AddActionAsync(Guid id, AddCapaActionRequest request, CancellationToken cancellationToken);
    Task<CapaDetailsResponse?> CompleteActionAsync(Guid id, Guid actionId, CompleteCapaActionRequest request, CancellationToken cancellationToken);
    Task<CapaDetailsResponse?> VerifyActionAsync(Guid id, Guid actionId, VerifyCapaActionRequest request, CancellationToken cancellationToken);
    Task<CapaDetailsResponse?> TransitionAsync(Guid id, TransitionCapaRequest request, CancellationToken cancellationToken);
}

public sealed record CapaFinalReportFile(byte[] Content, string FileName, string Sha256);
public interface ICapaFinalReportService
{
    Task<CapaFinalReportFile> EnsureGeneratedAsync(CapaDetailsResponse details, CancellationToken cancellationToken);
}
