using Qms.Contracts.Common;
using Qms.Contracts.Documents;
namespace Qms.Application.Documents;

public interface IDocumentService
{
    Task<DocumentLookupsResponse> GetLookupsAsync(CancellationToken ct);
    Task<IReadOnlyList<DocumentLookupDefinitionResponse>> ListLookupDefinitionsAsync(CancellationToken ct);
    Task<DocumentLookupDefinitionResponse> CreateLookupDefinitionAsync(CreateDocumentLookupDefinitionRequest request, CancellationToken ct);
    Task<DocumentLookupDefinitionResponse?> UpdateLookupDefinitionAsync(Guid id, UpdateDocumentLookupDefinitionRequest request, CancellationToken ct);
    Task<PagedResponse<ControlledDocumentListItemResponse>> SearchAsync(ControlledDocumentSearchRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse> CreateAsync(CreateControlledDocumentRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> UpdateDraftAsync(Guid id, UpdateDocumentDraftRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> CompleteReviewAsync(Guid id, Guid reviewId, CompleteDocumentReviewRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> CompleteTrainingAsync(Guid id, Guid requirementId, CompleteDocumentTrainingRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> StartRevisionAsync(Guid id, StartDocumentRevisionRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> IssueCopyAsync(Guid id, IssueControlledCopyRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> CloseCopyAsync(Guid id, Guid copyId, CloseControlledCopyRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> AcknowledgeReadAsync(Guid id, AcknowledgeDocumentReadRequest request, CancellationToken ct);
    Task<ControlledDocumentDetailsResponse?> TransitionAsync(Guid id, TransitionDocumentRequest request, CancellationToken ct);
}

public sealed record DocumentFinalReportFile(byte[] Content, string FileName, string Sha256);
public interface IDocumentFinalReportService
{
    Task<DocumentFinalReportFile> EnsureGeneratedAsync(ControlledDocumentDetailsResponse details, CancellationToken ct);
}
