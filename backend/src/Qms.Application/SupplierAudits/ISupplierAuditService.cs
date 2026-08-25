using Qms.Contracts.Common;
using Qms.Contracts.SupplierAudits;

namespace Qms.Application.SupplierAudits;

public interface ISupplierAuditService
{
    Task<PagedResponse<SupplierAuditListItemResponse>> SearchAsync(SupplierAuditSearchRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<SupplierAuditDetailsResponse> CreateAsync(CreateSupplierAuditRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> TransitionAsync(Guid id, TransitionSupplierAuditRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> AnswerChecklistAsync(Guid id, Guid itemId, AnswerSupplierAuditChecklistRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> AddFindingAsync(Guid id, AddSupplierAuditFindingRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> RespondFindingAsync(Guid id, Guid findingId, RespondSupplierAuditFindingRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> SubmitEvidenceAsync(Guid id, Guid findingId, SubmitSupplierAuditEvidenceRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> CloseFindingAsync(Guid id, Guid findingId, CloseSupplierAuditFindingRequest request, CancellationToken ct);
    Task<SupplierInvitationCreatedResponse?> CreateInvitationAsync(Guid id, CreateSupplierInvitationRequest request, CancellationToken ct);
    Task<SupplierInvitationReceiptResponse> SubmitInvitationResponseAsync(SupplierInvitationSubmissionRequest request, CancellationToken ct);
    Task<SupplierAuditDetailsResponse?> RecordResultAsync(Guid id, RecordSupplierAuditResultRequest request, CancellationToken ct);
}
