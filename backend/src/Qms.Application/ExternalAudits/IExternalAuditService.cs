using Qms.Contracts.Common; using Qms.Contracts.ExternalAudits;
namespace Qms.Application.ExternalAudits;
public interface IExternalAuditService
{
    Task<PagedResponse<ExternalAuditListItemResponse>> SearchAsync(ExternalAuditSearchRequest request,CancellationToken ct);
    Task<ExternalAuditDetailsResponse?> GetDetailsAsync(Guid id,CancellationToken ct);
    Task<ExternalAuditDetailsResponse> CreateAsync(CreateExternalAuditRequest request,CancellationToken ct);
    Task<ExternalAuditDetailsResponse?> TransitionAsync(Guid id,TransitionExternalAuditRequest request,CancellationToken ct);
    Task<ExternalAuditDetailsResponse?> ExportDocumentAsync(Guid id,Guid requestId,ExportExternalAuditDocumentRequest request,CancellationToken ct);
    Task<ExternalAuditDetailsResponse?> AddFindingAsync(Guid id,AddExternalAuditFindingRequest request,CancellationToken ct);
    Task<ExternalAuditDetailsResponse?> RespondFindingAsync(Guid id,Guid findingId,RespondExternalAuditFindingRequest request,CancellationToken ct);
    Task<ExternalAuditDetailsResponse?> CloseFindingAsync(Guid id,Guid findingId,CloseExternalAuditFindingRequest request,CancellationToken ct);
    Task<ExternalAuditDetailsResponse?> RecordClosureLetterAsync(Guid id,RecordExternalAuditClosureLetterRequest request,CancellationToken ct);
}
