using Qms.Contracts.Common; using Qms.Contracts.WorkItems;
namespace Qms.Application.WorkItems;
public interface IWorkItemService
{
    Task<PagedResponse<WorkItemListItemResponse>> SearchAsync(WorkItemSearchRequest request,CancellationToken ct);
    Task<WorkItemOptionsResponse> GetOptionsAsync(CancellationToken ct);
    Task<IReadOnlyList<WorkItemLookupDefinitionResponse>> ListLookupsAsync(CancellationToken ct);
    Task<WorkItemLookupDefinitionResponse> CreateLookupAsync(CreateWorkItemLookupDefinitionRequest request,CancellationToken ct);
    Task<WorkItemLookupDefinitionResponse?> UpdateLookupAsync(Guid id,UpdateWorkItemLookupDefinitionRequest request,CancellationToken ct);
    Task<WorkItemDetailsResponse?> GetDetailsAsync(Guid id,CancellationToken ct);
    Task<WorkItemDetailsResponse> CreateAsync(CreateWorkItemRequest request,CancellationToken ct);
    Task<WorkItemDetailsResponse?> TransitionAsync(Guid id,TransitionWorkItemRequest request,CancellationToken ct);
}
public sealed record WorkItemFinalReportFile(byte[] Content,string FileName,string Sha256);
public interface IWorkItemFinalReportService{Task<WorkItemFinalReportFile> EnsureGeneratedAsync(WorkItemDetailsResponse details,CancellationToken ct);}
