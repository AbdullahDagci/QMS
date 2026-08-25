namespace Qms.Domain.Organization;

public sealed class Position
{
    private Position() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsManagement { get; private set; }
    public bool IsActive { get; private set; }

    public static Position Create(string code, string name, bool isManagement = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Position { Id = Guid.CreateVersion7(), Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), IsManagement = isManagement, IsActive = true };
    }
}
