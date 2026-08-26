namespace Qms.Domain.Deviations;

public sealed class DeviationTypeDefinition
{
    private DeviationTypeDefinition() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static DeviationTypeDefinition Create(string code, string name, int sortOrder, DateTimeOffset now)
    {
        Validate(code, name, sortOrder);
        return new DeviationTypeDefinition { Id = Guid.CreateVersion7(), Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), SortOrder = sortOrder, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
    }

    public void Update(string name, int sortOrder, bool isActive, DateTimeOffset now)
    {
        Validate(Code, name, sortOrder);
        Name = name.Trim(); SortOrder = sortOrder; IsActive = isActive; UpdatedAtUtc = now;
    }

    private static void Validate(string code, string name, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (code.Trim().Length > 32) throw new ArgumentException("Sapma türü kodu en fazla 32 karakter olabilir.", nameof(code));
        if (name.Trim().Length > 80) throw new ArgumentException("Sapma türü adı en fazla 80 karakter olabilir.", nameof(name));
        if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sıra sıfırdan küçük olamaz.");
    }
}
