using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Security;

namespace Qms.IntegrationTests;

public sealed class RecordVisibilityTrackingTests
{
    // EF Core bir alt sorgudaki AsNoTracking'i tüm sorguya uygular; görünürlük filtresiyle yüklenen
    // aggregate'ler takip edilmezse iş akışı geçişleri sessizce kaybolur.
    [Theory]
    [InlineData(QmsRoles.QualityAssurance)]
    [InlineData(QmsRoles.DeviationReporter)]
    public void VisibleQualityRecords_DoesNotDisableTracking(string role)
    {
        var options = new DbContextOptionsBuilder<QmsDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=not_used;Username=not_used;Password=not_used")
            .Options;
        using var db = new QmsDbContext(options);

        var visible = db.VisibleQualityRecords(new TestUser(role));

        Assert.DoesNotContain(nameof(EntityFrameworkQueryableExtensions.AsNoTracking), visible.Expression.ToString());
    }

    private sealed class TestUser(string role) : ICurrentUser
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string DisplayName => "Test";
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public IReadOnlySet<string> Roles => new HashSet<string> { role };
        public bool IsInRole(string candidate) => candidate == role;
    }
}
