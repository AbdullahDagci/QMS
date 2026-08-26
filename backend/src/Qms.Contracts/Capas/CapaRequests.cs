using Qms.Contracts.Common;

namespace Qms.Contracts.Capas;

public sealed record CreateCapaRequest(
    Guid? SourceDeviationId,
    string SourceType,
    string Title,
    string Description,
    string RootCause,
    string ImmediateActions,
    Guid OwnerUserId,
    DateTimeOffset TargetDateUtc,
    bool EffectivenessRequired,
    string EffectivenessMethod = "",
    string EffectivenessSample = "",
    int ObservationPeriodDays = 0,
    string SuccessCriteria = "",
    Guid? EffectivenessEvaluatorUserId = null);

public sealed record CapaSearchRequest(int Page = 1, int PageSize = 25, string SortBy = "createdAtUtc", string SortDirection = "desc", IReadOnlyList<ColumnFilterRequest>? Filters = null);
public sealed record AddCapaActionRequest(long ExpectedVersion, string ActionType, string Description, Guid OwnerUserId, DateTimeOffset TargetDateUtc);
public sealed record CompleteCapaActionRequest(long ExpectedVersion, string Evidence);
public sealed record VerifyCapaActionRequest(long ExpectedVersion, bool Approved, string Note);
public sealed record TransitionCapaRequest(long ExpectedVersion, string Transition, string? Note = null, bool IsEffective = false, string? SignaturePassword = null, bool SignatureMeaningAccepted = false);
