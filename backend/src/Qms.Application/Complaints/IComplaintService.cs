using Qms.Contracts.Common;
using Qms.Contracts.Complaints;

namespace Qms.Application.Complaints;

public sealed record ComplaintFinalReportFile(byte[] Content, string FileName, string Sha256);
public interface IComplaintFinalReportService { Task<ComplaintFinalReportFile> EnsureGeneratedAsync(ComplaintDetailsResponse details, CancellationToken ct); }

public interface IComplaintService
{
    Task<PagedResponse<ComplaintListItemResponse>> SearchAsync(ComplaintSearchRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<ComplaintDetailsResponse> CreateAsync(CreateComplaintRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> TransitionAsync(Guid id, TransitionComplaintRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> AddResponseAsync(Guid id, AddComplaintResponseRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> ApproveResponseAsync(Guid id, Guid responseId, ApproveComplaintResponseRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> CompleteInvestigationAsync(Guid id, Guid investigationId, CompleteComplaintInvestigationRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> CompleteImpactAsync(Guid id, CompleteComplaintImpactRequest request, CancellationToken ct);
    Task<ComplaintDetailsResponse?> DecideCapaAsync(Guid id, DecideComplaintCapaRequest request, CancellationToken ct);
    Task<ComplaintOptionsResponse> GetOptionsAsync(CancellationToken ct);
    Task<IReadOnlyList<ComplaintLookupDefinitionResponse>> ListLookupDefinitionsAsync(CancellationToken ct);
    Task<ComplaintLookupDefinitionResponse> CreateLookupDefinitionAsync(CreateComplaintLookupDefinitionRequest request, CancellationToken ct);
    Task<ComplaintLookupDefinitionResponse?> UpdateLookupDefinitionAsync(Guid id, UpdateComplaintLookupDefinitionRequest request, CancellationToken ct);
}

