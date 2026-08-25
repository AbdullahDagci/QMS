using Qms.Contracts.Common;

namespace Qms.Contracts.Deviations;

public sealed record DeviationSearchRequest(
    int Page = 1,
    int PageSize = 25,
    string SortBy = "createdAtUtc",
    string SortDirection = "desc",
    IReadOnlyList<ColumnFilterRequest>? Filters = null);
