namespace Qms.Contracts.System;

public sealed record SystemInfoResponse(
    string Name,
    string Version,
    bool SingleTenant,
    IReadOnlyCollection<ModuleSummary> Modules);

public sealed record ModuleSummary(string Code, string Name, string Status);
