using System.Text.Json;

namespace Qms.Domain.SpecializedRecords;

public enum SpecializedRecordStatus
{
    Draft,
    InReview,
    Reviewed,
    Approved,
    Closed,
    Cancelled,
}

public sealed class SpecializedRecord
{
    private SpecializedRecord() { }

    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public string ModuleCode { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string TypeCode { get; private set; } = "";
    public string TypeName { get; private set; } = "";
    public string SubjectCode { get; private set; } = "";
    public string SubjectName { get; private set; } = "";
    public string ScopeCode { get; private set; } = "";
    public string ScopeName { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string StructuredDataJson { get; private set; } = "{}";
    public DateTimeOffset DueAtUtc { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Owner { get; private set; } = "";
    public Guid ReviewerUserId { get; private set; }
    public string Reviewer { get; private set; } = "";
    public Guid ApproverUserId { get; private set; }
    public string Approver { get; private set; } = "";
    public SpecializedRecordStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static SpecializedRecord Create(
        Guid qr,
        string module,
        string title,
        string typeCode,
        string typeName,
        string subjectCode,
        string subjectName,
        string scopeCode,
        string scopeName,
        string reference,
        string description,
        string dataJson,
        DateTimeOffset due,
        Guid ownerId,
        string owner,
        Guid reviewerId,
        string reviewer,
        Guid approverId,
        string approver,
        DateTimeOffset now
    )
    {
        if (
            qr == Guid.Empty
            || ownerId == Guid.Empty
            || reviewerId == Guid.Empty
            || approverId == Guid.Empty
            || ownerId == reviewerId
            || ownerId == approverId
            || reviewerId == approverId
        )
            throw new ArgumentException(
                "Kayıt görev ayrılığı ve kullanıcı kimlikleri geçersizdir."
            );
        if (module is not ("M.13" or "M.14" or "M.15" or "M.16"))
            throw new ArgumentException("Uzmanlık modülü geçersizdir.");
        foreach (
            var value in new[]
            {
                title,
                typeCode,
                typeName,
                subjectCode,
                subjectName,
                scopeCode,
                scopeName,
                reference,
                description,
                owner,
                reviewer,
                approver,
            }
        )
            Required(value);
        using var _ = JsonDocument.Parse(dataJson);
        if (due <= now)
            throw new ArgumentException("Hedef tarih gelecekte olmalıdır.");
        return new SpecializedRecord
        {
            Id = Guid.CreateVersion7(),
            QualityRecordId = qr,
            ModuleCode = module,
            Title = title.Trim(),
            TypeCode = typeCode.Trim(),
            TypeName = typeName.Trim(),
            SubjectCode = subjectCode.Trim(),
            SubjectName = subjectName.Trim(),
            ScopeCode = scopeCode.Trim(),
            ScopeName = scopeName.Trim(),
            Reference = reference.Trim(),
            Description = description.Trim(),
            StructuredDataJson = dataJson,
            DueAtUtc = due,
            OwnerUserId = ownerId,
            Owner = owner.Trim(),
            ReviewerUserId = reviewerId,
            Reviewer = reviewer.Trim(),
            ApproverUserId = approverId,
            Approver = approver.Trim(),
            Status = SpecializedRecordStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };
    }

    public void Submit(long expected, DateTimeOffset now)
    {
        Ensure(expected, SpecializedRecordStatus.Draft);
        ValidateModuleData();
        Move(SpecializedRecordStatus.InReview, now);
    }

    public void Review(long expected, DateTimeOffset now)
    {
        Ensure(expected, SpecializedRecordStatus.InReview);
        Move(SpecializedRecordStatus.Reviewed, now);
    }

    public void Approve(long expected, DateTimeOffset now)
    {
        Ensure(expected, SpecializedRecordStatus.Reviewed);
        Move(SpecializedRecordStatus.Approved, now);
    }

    public void Close(long expected, DateTimeOffset now)
    {
        Ensure(expected, SpecializedRecordStatus.Approved);
        Move(SpecializedRecordStatus.Closed, now);
    }

    public void Cancel(long expected, DateTimeOffset now)
    {
        if (
            Version != expected
            || Status is SpecializedRecordStatus.Closed or SpecializedRecordStatus.Cancelled
        )
            throw new InvalidOperationException("Kayıt bu aşamada iptal edilemez.");
        Move(SpecializedRecordStatus.Cancelled, now);
    }

    private void ValidateModuleData()
    {
        using var doc = JsonDocument.Parse(StructuredDataJson);
        var root = doc.RootElement;
        string Text(string name) =>
            root.TryGetProperty(name, out var x) && x.ValueKind == JsonValueKind.String
                ? x.GetString() ?? ""
                : "";
        bool Bool(string name) =>
            root.TryGetProperty(name, out var x)
            && x.ValueKind is JsonValueKind.True or JsonValueKind.False
            && x.GetBoolean();
        int Number(string name) =>
            root.TryGetProperty(name, out var x) && x.TryGetInt32(out var value) ? value : -1;
        if (
            ModuleCode == "M.13"
            && (
                Text("proofDocumentNumber").Length == 0
                || Text("proofVersion").Length == 0
                || Text("proofSha256").Length != 64
                || Text("barcode").Length is not (8 or 12 or 13 or 14)
                || Text("regulatoryText").Length == 0
            )
        )
            throw new InvalidOperationException(
                "Artwork proof dokümanı, sürümü ve SHA-256 bütünlük değeri zorunludur."
            );
        if (
            ModuleCode == "M.14"
            && (
                Text("specification").Length == 0
                || Text("observedResult").Length == 0
                || Text("laboratoryInvestigation").Length == 0
                || Text("rootCause").Length == 0
                || Text("disposition").Length == 0
            )
        )
            throw new InvalidOperationException(
                "OOS spesifikasyonu, gözlenen sonuç ve laboratuvar araştırması zorunludur."
            );
        if (ModuleCode == "M.14" && Bool("retestPerformed") && !Bool("retestAuthorized"))
            throw new InvalidOperationException(
                "Bilimsel hipotez ve yetkili onayı olmadan tekrar test yapılamaz."
            );
        if (
            ModuleCode == "M.15"
            && (
                Text("eventTerm").Length == 0
                || Text("patientCode").Length == 0
                || Text("source").Length == 0
                || Text("seriousness").Length == 0
                || !DateTimeOffset.TryParse(Text("initialReceiptAtUtc"), out var receipt)
                || !DateTimeOffset.TryParse(Text("regulatoryDueAtUtc"), out var reportingDue)
            )
        )
            throw new InvalidOperationException(
                "PV vaka kaynağı, anonim hasta kodu, advers olay ve geçerli raporlama tarihleri zorunludur."
            );
        if (ModuleCode == "M.15")
        {
            _ = DateTimeOffset.TryParse(Text("initialReceiptAtUtc"), out var receiptValue);
            _ = DateTimeOffset.TryParse(Text("regulatoryDueAtUtc"), out var reportingDueValue);
            var seriousness = Text("seriousness");
            var allowedDays = seriousness switch
            {
                "FatalOrLifeThreatening" => 7,
                "Serious" => 15,
                "NonSerious" => 90,
                _ => throw new InvalidOperationException(
                    "PV ciddiyet sınıfı yönetilen seçeneklerden seçilmelidir."
                ),
            };
            if (
                reportingDueValue <= receiptValue
                || reportingDueValue > receiptValue.AddDays(allowedDays)
            )
                throw new InvalidOperationException(
                    $"{seriousness} vaka en geç {allowedDays} gün içinde raporlanmalıdır."
                );
        }
        if (
            ModuleCode == "M.16"
            && (
                !root.TryGetProperty("score", out var score)
                || !score.TryGetInt32(out var n)
                || n is < 0 or > 100
                || Text("evaluationPeriod").Length == 0
                || Number("qualityScore") is < 0 or > 100
                || Number("deliveryScore") is < 0 or > 100
                || Text("qualificationDecision").Length == 0
            )
        )
            throw new InvalidOperationException(
                "Tedarikçi dönemi, alt puanları, toplam puanı ve nitelendirme kararı zorunludur."
            );
        if (ModuleCode == "M.16")
        {
            var total = Number("score");
            var expected =
                total >= 80 ? "Approved"
                : total >= 60 ? "Conditional"
                : "Disqualified";
            if (Text("qualificationDecision") != expected)
                throw new InvalidOperationException(
                    $"{total} puan için nitelendirme kararı {expected} olmalıdır."
                );
        }
    }

    private void Ensure(long expected, SpecializedRecordStatus status)
    {
        if (Version != expected)
            throw new InvalidOperationException("Kayıt değişti; ekranı yenileyin.");
        if (Status != status)
            throw new InvalidOperationException($"Geçersiz kayıt aşaması: {Status}.");
    }

    private void Move(SpecializedRecordStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAtUtc = now;
        Version++;
    }

    private static void Required(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 6000)
            throw new ArgumentException("Zorunlu kayıt alanı geçersizdir.");
    }
}

public sealed class SpecializedLookupDefinition
{
    private SpecializedLookupDefinition() { }

    public Guid Id { get; private set; }
    public string ModuleCode { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static SpecializedLookupDefinition Create(
        string module,
        string category,
        string code,
        string name,
        int sort,
        DateTimeOffset now
    )
    {
        if (
            module is not ("M.13" or "M.14" or "M.15" or "M.16")
            || string.IsNullOrWhiteSpace(category)
            || string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(name)
        )
            throw new ArgumentException("Lookup tanımı geçersizdir.");
        return new()
        {
            Id = Guid.CreateVersion7(),
            ModuleCode = module,
            Category = category.Trim(),
            Code = code.Trim(),
            Name = name.Trim(),
            SortOrder = sort,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    public void Update(string name, int sort, bool active, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Lookup adı zorunludur.");
        Name = name.Trim();
        SortOrder = sort;
        IsActive = active;
        UpdatedAtUtc = now;
    }
}
