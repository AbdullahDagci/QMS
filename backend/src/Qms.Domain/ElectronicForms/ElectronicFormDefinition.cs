using System.Text.RegularExpressions;

namespace Qms.Domain.ElectronicForms;

public enum ElectronicFormKind
{
    Standard,
    Logbook,
    Checklist,
    Assessment,
}

public sealed partial class ElectronicFormDefinition
{
    private ElectronicFormDefinition() { }

    public Guid Id { get; private set; }
    public Guid QualityRecordId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public ElectronicFormKind Kind { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string CreatedByDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid? CurrentPublishedVersionId { get; private set; }
    public int LatestVersionNumber { get; private set; }
    public bool IsActive { get; private set; }
    public long Version { get; private set; }

    public static ElectronicFormDefinition Create(
        string code,
        string name,
        string description,
        string category,
        ElectronicFormKind kind,
        Guid qualityRecordId,
        Guid creatorId,
        string creatorName,
        DateTimeOffset now)
    {
        Required(code, 64);
        Required(name, 200);
        Required(category, 100);
        Required(creatorName, 200);
        if (qualityRecordId == Guid.Empty || creatorId == Guid.Empty) throw new ArgumentException("Form kayıt ve hazırlayan kullanıcı kimliği zorunludur.");
        if (description.Trim().Length > 2000) throw new ArgumentException("Form açıklaması 2000 karakteri aşamaz.");
        var normalizedCode = code.Trim().ToUpperInvariant();
        if (!CodePattern().IsMatch(normalizedCode))
            throw new ArgumentException("Form kodu harf veya rakamla başlamalı; yalnız A-Z, 0-9, nokta, tire ve alt çizgi içermelidir.");

        return new ElectronicFormDefinition
        {
            Id = Guid.CreateVersion7(),
            QualityRecordId = qualityRecordId,
            Code = normalizedCode,
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category.Trim(),
            Kind = kind,
            CreatedByUserId = creatorId,
            CreatedByDisplayName = creatorName.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            LatestVersionNumber = 1,
            IsActive = true,
            Version = 1,
        };
    }

    public void UpdateMetadata(
        long expectedVersion,
        string name,
        string description,
        string category,
        ElectronicFormKind kind,
        DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        Required(name, 200);
        Required(category, 100);
        if (description.Trim().Length > 2000) throw new ArgumentException("Form açıklaması 2000 karakteri aşamaz.");
        var normalizedName = name.Trim();
        var normalizedDescription = description.Trim();
        var normalizedCategory = category.Trim();
        var changed = Name != normalizedName || Description != normalizedDescription
            || Category != normalizedCategory || Kind != kind;
        if (!changed) return;
        if (CurrentPublishedVersionId.HasValue)
            throw new InvalidOperationException("Yayımlanmış formun temel bilgileri değiştirilemez; yalnız şema ve çıktı için yeni sürüm açılabilir.");
        Name = normalizedName;
        Description = normalizedDescription;
        Category = normalizedCategory;
        Kind = kind;
        Touch(now);
    }

    public int ReserveNextVersion(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        LatestVersionNumber++;
        Touch(now);
        return LatestVersionNumber;
    }

    public void Publish(Guid formVersionId, int versionNumber, DateTimeOffset now)
    {
        if (formVersionId == Guid.Empty || versionNumber < 1 || versionNumber > LatestVersionNumber)
            throw new ArgumentException("Yayımlanacak form sürümü geçersizdir.");
        CurrentPublishedVersionId = formVersionId;
        IsActive = true;
        Touch(now);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAtUtc = now;
        Version++;
    }

    private void EnsureVersion(long expected)
    {
        if (Version != expected) throw new InvalidOperationException("Form başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyin.");
    }

    private static void Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"Zorunlu form alanı 1-{max} karakter arasında olmalıdır.");
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
