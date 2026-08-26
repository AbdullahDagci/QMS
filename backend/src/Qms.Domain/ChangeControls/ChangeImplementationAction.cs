namespace Qms.Domain.ChangeControls;

public enum ChangeImplementationActionStatus { Planned, CompletionRequested, Verified, Rejected }

public sealed class ChangeImplementationAction
{
    private ChangeImplementationAction() { }

    public Guid Id { get; private set; }
    public Guid ChangeControlId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Owner { get; private set; } = string.Empty;
    public Guid? OwnerUserId { get; private set; }
    public DateTimeOffset TargetDateUtc { get; private set; }
    public bool IsBlocking { get; private set; }
    public ChangeImplementationActionStatus Status { get; private set; }
    public string? CompletionEvidence { get; private set; }
    public string? VerificationNote { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    internal static ChangeImplementationAction Create(Guid changeControlId, string category, string description, Guid ownerUserId, string owner, DateTimeOffset targetDateUtc, bool isBlocking, DateTimeOffset now)
    {
        Text(category, nameof(category), 80); Text(description, nameof(description), 2000); Text(owner, nameof(owner), 160);
        if (targetDateUtc <= now) throw new ArgumentException("Uygulama aksiyonu hedef tarihi gelecekte olmalıdır.");
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Aksiyon sorumlusu zorunludur.");
        return new ChangeImplementationAction { Id = Guid.CreateVersion7(), ChangeControlId = changeControlId, Category = category.Trim(), Description = description.Trim(), OwnerUserId = ownerUserId, Owner = owner.Trim(), TargetDateUtc = targetDateUtc, IsBlocking = isBlocking, Status = ChangeImplementationActionStatus.Planned };
    }

    internal void RequestCompletion(string evidence, DateTimeOffset now)
    {
        if (Status is not (ChangeImplementationActionStatus.Planned or ChangeImplementationActionStatus.Rejected)) throw new InvalidOperationException("Aksiyon tamamlanma bildirimine uygun durumda değil.");
        Text(evidence, nameof(evidence), 3000); CompletionEvidence = evidence.Trim(); CompletedAtUtc = now; VerificationNote = null; VerifiedAtUtc = null; Status = ChangeImplementationActionStatus.CompletionRequested;
    }

    internal void Verify(bool approved, string note, DateTimeOffset now)
    {
        if (Status != ChangeImplementationActionStatus.CompletionRequested) throw new InvalidOperationException("Aksiyon KG doğrulamasını beklemiyor.");
        Text(note, nameof(note), 2000); VerificationNote = note.Trim(); VerifiedAtUtc = now; Status = approved ? ChangeImplementationActionStatus.Verified : ChangeImplementationActionStatus.Rejected;
    }

    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}
