namespace Qms.Contracts.Deviations;

public sealed record DeviationDepartmentOptionResponse(Guid Id, string Code, string Name);
public sealed record DeviationTypeResponse(Guid Id, string Code, string Name, int SortOrder, bool IsActive);
public sealed record DeviationLookupsResponse(IReadOnlyList<DeviationDepartmentOptionResponse> Departments, IReadOnlyList<DeviationTypeResponse> DeviationTypes);
public sealed record CreateDeviationTypeRequest(string Code, string Name, int SortOrder);
public sealed record UpdateDeviationTypeRequest(string Name, int SortOrder, bool IsActive);
