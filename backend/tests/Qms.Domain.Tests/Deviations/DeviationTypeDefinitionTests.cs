using Qms.Domain.Deviations;

namespace Qms.Domain.Tests.Deviations;

public sealed class DeviationTypeDefinitionTests
{
    [Fact]
    public void Update_CanDeactivateDefinitionWithoutLosingIdentity()
    {
        var now = DateTimeOffset.UtcNow;
        var definition = DeviationTypeDefinition.Create("proses", "Proses", 10, now);

        definition.Update("Üretim Prosesi", 20, false, now.AddMinutes(1));

        Assert.Equal("PROSES", definition.Code);
        Assert.Equal("Üretim Prosesi", definition.Name);
        Assert.False(definition.IsActive);
        Assert.Equal(20, definition.SortOrder);
    }

    [Fact]
    public void Create_RejectsNegativeSortOrder() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => DeviationTypeDefinition.Create("X", "Test", -1, DateTimeOffset.UtcNow));
}
