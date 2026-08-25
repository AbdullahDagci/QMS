namespace Qms.Domain.Capas;

public sealed class CapaAction
{
    private CapaAction() { }

    public Guid Id { get; private set; }
    public Guid CapaId { get; private set; }
    public string ActionType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Owner { get; private set; } = string.Empty;
    public DateTimeOffset TargetDateUtc { get; private set; }
    public CapaActionStatus Status { get; private set; }
    public string? CompletionEvidence { get; private set; }
    public string? VerificationNote { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static CapaAction Create(Guid capaId, string actionType, string description, string owner, DateTimeOffset targetDateUtc, DateTimeOffset now)
    {
        Validate(actionType, nameof(actionType), 80);
        Validate(description, nameof(description), 2000);
        Validate(owner, nameof(owner), 160);
        if (targetDateUtc <= now) throw new ArgumentException("Aksiyon hedef tarihi gelecekte olmalıdır.");
        return new CapaAction { Id = Guid.CreateVersion7(), CapaId = capaId, ActionType = actionType.Trim(), Description = description.Trim(), Owner = owner.Trim(), TargetDateUtc = targetDateUtc, Status = CapaActionStatus.Planned, CreatedAtUtc = now };
    }

    internal void RequestCompletion(string evidence, DateTimeOffset now)
    {
        if (Status is not (CapaActionStatus.Planned or CapaActionStatus.Rejected)) throw new InvalidOperationException("Yalnız planlanan veya reddedilen aksiyon tamamlanabilir.");
        Validate(evidence, nameof(evidence), 4000);
        CompletionEvidence = evidence.Trim();
        CompletedAtUtc = now;
        Status = CapaActionStatus.CompletionRequested;
    }

    internal void Verify(bool approved, string note, DateTimeOffset now)
    {
        if (Status != CapaActionStatus.CompletionRequested) throw new InvalidOperationException("Yalnız tamamlanma talebi bulunan aksiyon doğrulanabilir.");
        Validate(note, nameof(note), 2000);
        VerificationNote = note.Trim();
        VerifiedAtUtc = now;
        Status = approved ? CapaActionStatus.Verified : CapaActionStatus.Rejected;
    }

    private static void Validate(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
