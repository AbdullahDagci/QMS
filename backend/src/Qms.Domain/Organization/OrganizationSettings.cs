namespace Qms.Domain.Organization;

public sealed class OrganizationSettings
{
    private OrganizationSettings()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = string.Empty;

    public string DefaultCulture { get; private set; } = string.Empty;

    public static OrganizationSettings Create(
        string name,
        string timeZoneId = "Europe/Istanbul",
        string defaultCulture = "tr-TR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCulture);

        return new OrganizationSettings
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            TimeZoneId = timeZoneId.Trim(),
            DefaultCulture = defaultCulture.Trim()
        };
    }
}
