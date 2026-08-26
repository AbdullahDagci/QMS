using Qms.Domain.ChangeControls;

namespace Qms.Domain.Tests.ChangeControls;

public sealed class ChangeLookupDefinitionTests
{
    [Fact]
    public void Update_preserves_stable_code_and_changes_only_managed_snapshot_metadata()
    {
        var createdAt = new DateTimeOffset(2026, 8, 26, 8, 0, 0, TimeSpan.Zero);
        var item = ChangeLookupDefinition.Create("ChangeType", "Equipment", "Ekipman", 20, createdAt);

        item.Update("Üretim ekipmanı", 15, false, createdAt.AddHours(1));

        Assert.Equal("ChangeType", item.Category);
        Assert.Equal("Equipment", item.Code);
        Assert.Equal("Üretim ekipmanı", item.Name);
        Assert.Equal(15, item.SortOrder);
        Assert.False(item.IsActive);
        Assert.Equal(createdAt.AddHours(1), item.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("", "Code", "Name")]
    [InlineData("Category", "", "Name")]
    [InlineData("Category", "Code", "")]
    public void Create_rejects_blank_lookup_identity(string category, string code, string name) =>
        Assert.Throws<ArgumentException>(() => ChangeLookupDefinition.Create(category, code, name, 0, DateTimeOffset.UtcNow));
}
