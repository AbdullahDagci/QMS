namespace Qms.Domain.Complaints;

public sealed class ComplaintLookupDefinition
{
    private ComplaintLookupDefinition() { }
    public Guid Id { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ComplaintLookupDefinition Create(string category, string code, string name, int sortOrder, DateTimeOffset now)
    {
        Validate(category, code, name, sortOrder);
        return new() { Id = Guid.CreateVersion7(), Category = category.Trim(), Code = code.Trim(), Name = name.Trim(), SortOrder = sortOrder, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now };
    }

    public void Update(string name, int sortOrder, bool isActive, DateTimeOffset now)
    {
        Validate(Category, Code, name, sortOrder); Name = name.Trim(); SortOrder = sortOrder; IsActive = isActive; UpdatedAtUtc = now;
    }

    private static void Validate(string category, string code, string name, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category); ArgumentException.ThrowIfNullOrWhiteSpace(code); ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (category.Trim().Length > 40 || code.Trim().Length > 64 || name.Trim().Length > 160 || sortOrder < 0) throw new ArgumentException("M.06 lookup tanımı geçersizdir.");
    }
}
