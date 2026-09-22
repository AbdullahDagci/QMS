using Qms.Contracts.ElectronicForms;

namespace Qms.Application.ElectronicForms;

public interface IElectronicFormService
{
    Task<IReadOnlyList<ElectronicFormListItemResponse>> ListDefinitionsAsync(CancellationToken cancellationToken);
    Task<ElectronicFormDetailsResponse?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken);
    Task<ElectronicFormDetailsResponse> CreateDefinitionAsync(CreateElectronicFormRequest request, CancellationToken cancellationToken);
    Task<ElectronicFormDetailsResponse?> UpdateDraftAsync(Guid id, UpdateElectronicFormDraftRequest request, CancellationToken cancellationToken);
    Task<ElectronicFormDetailsResponse?> SubmitForReviewAsync(Guid id, TransitionElectronicFormVersionRequest request, CancellationToken cancellationToken);
    Task<ElectronicFormDetailsResponse?> PublishAsync(Guid id, TransitionElectronicFormVersionRequest request, CancellationToken cancellationToken);
    Task<ElectronicFormDetailsResponse?> StartNewVersionAsync(Guid id, StartElectronicFormVersionRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ElectronicFormRecordListItemResponse>> ListRecordsAsync(CancellationToken cancellationToken);
    Task<ElectronicFormRecordDetailsResponse?> GetRecordAsync(Guid id, CancellationToken cancellationToken);
    Task<ElectronicFormRecordDetailsResponse> CreateRecordAsync(CreateElectronicFormRecordRequest request, CancellationToken cancellationToken);
    Task<ElectronicFormRecordDetailsResponse?> UpdateRecordDraftAsync(Guid id, UpdateElectronicFormRecordRequest request, CancellationToken cancellationToken);
    Task<ElectronicFormRecordDetailsResponse?> SubmitRecordAsync(Guid id, SubmitElectronicFormRecordRequest request, CancellationToken cancellationToken);
    Task<ElectronicFormRecordDetailsResponse?> ApproveRecordAsync(Guid id, ApproveElectronicFormRecordRequest request, CancellationToken cancellationToken);
}

public interface IElectronicFormFinalReportService
{
    Task<ElectronicFormFinalReportFile> EnsureGeneratedAsync(ElectronicFormRecordDetailsResponse details, CancellationToken cancellationToken);
}
