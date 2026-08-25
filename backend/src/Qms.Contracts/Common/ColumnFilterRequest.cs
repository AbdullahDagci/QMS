namespace Qms.Contracts.Common;

public sealed record ColumnFilterRequest(
    string Field,
    string Operator,
    string? Value = null,
    string? ValueTo = null,
    IReadOnlyList<string>? Values = null);
