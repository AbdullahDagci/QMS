namespace Qms.Domain.Complaints;

public sealed class ComplaintResponse
{
    private ComplaintResponse() { }
    public Guid Id { get; private set; }
    public Guid ComplaintId { get; private set; }
    public ComplaintResponseType ResponseType { get; private set; }
    public int VersionNumber { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public ComplaintResponseStatus Status { get; private set; }
    public Guid PreparedByUserId { get; private set; }
    public string PreparedBy { get; private set; } = string.Empty;
    public Guid? ApprovedByUserId { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    internal static ComplaintResponse Create(Guid complaintId, ComplaintResponseType type, int version, string content, Guid preparedByUserId, string preparedBy, DateTimeOffset now)
    {
        Text(content, nameof(content), 10000); Text(preparedBy, nameof(preparedBy), 200); if (preparedByUserId == Guid.Empty) throw new ArgumentException("Yanıtı hazırlayan kullanıcı zorunludur.");
        return new() { Id = Guid.CreateVersion7(), ComplaintId = complaintId, ResponseType = type, VersionNumber = version, Content = content.Trim(), PreparedByUserId = preparedByUserId, PreparedBy = preparedBy.Trim(), Status = ComplaintResponseStatus.Draft, CreatedAtUtc = now };
    }
    internal void Approve(Guid approverUserId, string approver, DateTimeOffset now) { if (Status == ComplaintResponseStatus.Approved) throw new InvalidOperationException("Yanıt sürümü zaten onaylanmış."); Text(approver, nameof(approver), 200); if (approverUserId == Guid.Empty || approverUserId == PreparedByUserId) throw new InvalidOperationException("Yanıtı hazırlayan kullanıcı aynı yanıtı onaylayamaz."); Status = ComplaintResponseStatus.Approved; ApprovedByUserId = approverUserId; ApprovedBy = approver.Trim(); ApprovedAtUtc = now; }
    private static void Text(string value, string name, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"{name} zorunludur ve en fazla {max} karakter olabilir."); }
}

