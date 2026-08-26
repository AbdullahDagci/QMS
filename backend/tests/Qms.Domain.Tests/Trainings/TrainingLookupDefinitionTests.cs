using Qms.Domain.Trainings;

namespace Qms.Domain.Tests.Trainings;

public sealed class TrainingLookupDefinitionTests
{
    [Fact]
    public void Update_preserves_category_and_code_while_versioned_records_keep_their_snapshots()
    {
        var createdAt = new DateTimeOffset(2026, 8, 26, 8, 0, 0, TimeSpan.Zero);
        var item = TrainingLookupDefinition.Create("DeliveryMethod", "Electronic", "Elektronik", 10, createdAt);

        item.Update("Dijital öğrenme", 20, false, createdAt.AddHours(1));

        Assert.Equal("DeliveryMethod", item.Category);
        Assert.Equal("Electronic", item.Code);
        Assert.Equal("Dijital öğrenme", item.Name);
        Assert.Equal(20, item.SortOrder);
        Assert.False(item.IsActive);
        Assert.Equal(createdAt.AddHours(1), item.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("", "Code", "Name", 0)]
    [InlineData("Category", "", "Name", 0)]
    [InlineData("Category", "Code", "", 0)]
    [InlineData("Category", "Code", "Name", -1)]
    public void Create_rejects_invalid_lookup_identity(string category, string code, string name, int sortOrder) =>
        Assert.Throws<ArgumentException>(() => TrainingLookupDefinition.Create(category, code, name, sortOrder, DateTimeOffset.UtcNow));
}
