using Qms.Domain.Complaints;

namespace Qms.Domain.Tests.Complaints;

public sealed class ComplaintLookupDefinitionTests
{
    [Fact]
    public void Update_preserves_stable_category_and_code()
    {
        var now = DateTimeOffset.UtcNow; var item = ComplaintLookupDefinition.Create("Channel", "Email", "E-posta", 10, now);
        item.Update("Elektronik posta", 20, false, now.AddHours(1));
        Assert.Equal("Channel", item.Category); Assert.Equal("Email", item.Code); Assert.Equal("Elektronik posta", item.Name); Assert.False(item.IsActive);
    }
}
