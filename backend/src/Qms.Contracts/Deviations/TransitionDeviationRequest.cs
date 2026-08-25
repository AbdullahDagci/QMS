namespace Qms.Contracts.Deviations;

public sealed record TransitionDeviationRequest(
    string Transition,
    long ExpectedVersion,
    string? Note = null,
    bool EffectivenessRequired = false,
    bool IsEffective = false);
