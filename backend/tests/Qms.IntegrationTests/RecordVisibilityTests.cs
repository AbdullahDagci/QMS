using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Security;

namespace Qms.IntegrationTests;

public sealed class RecordVisibilityTests
{
    [Fact]
    public void RegularUserQuery_IsRestrictedByOwnerDepartmentOrExplicitGrant()
    {
        using var db = CreateContext();
        var user = new TestCurrentUser(Guid.NewGuid(), Guid.NewGuid(),
            new HashSet<string> { "DeviationReporter" });

        var sql = db.VisibleQualityRecords(user).ToQueryString();

        Assert.Contains("CreatedByUserId", sql, StringComparison.Ordinal);
        Assert.Contains("DepartmentId", sql, StringComparison.Ordinal);
        Assert.Contains("record_access_grant", sql, StringComparison.Ordinal);
        Assert.Contains("UserId", sql, StringComparison.Ordinal);
        Assert.Contains("M.15", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentQuery_AppliesRecordScopeAndConfidentialityPredicate()
    {
        using var db = CreateContext();
        var user = new TestCurrentUser(Guid.NewGuid(), Guid.NewGuid(),
            new HashSet<string> { "TrainingCoordinator" });

        var sql = db.VisibleControlledDocuments(user).ToQueryString();

        Assert.Contains("record_access_grant", sql, StringComparison.Ordinal);
        Assert.Contains("Confidentiality", sql, StringComparison.Ordinal);
        Assert.Contains("Confidential", sql, StringComparison.Ordinal);
        Assert.Contains("OwnerUserId", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(QmsRoles.Administrator)]
    [InlineData(QmsRoles.QualityAssurance)]
    public void PrivilegedUserQuery_HasNoRecordScopePredicate(string role)
    {
        using var db = CreateContext();
        var user = new TestCurrentUser(Guid.NewGuid(), Guid.NewGuid(),
            new HashSet<string> { role });

        var sql = db.VisibleQualityRecords(user).ToQueryString();

        Assert.DoesNotContain("record_access_grant", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static QmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QmsDbContext>()
            .UseNpgsql("Host=localhost;Database=query_generation_only;Username=qms;Password=qms")
            .Options;
        return new QmsDbContext(options);
    }

    private sealed class TestCurrentUser(Guid id, Guid? departmentId, IReadOnlySet<string> roles)
        : ICurrentUser
    {
        public Guid Id { get; } = id;
        public string DisplayName => "Scope Test User";
        public Guid? DepartmentId { get; } = departmentId;
        public IReadOnlySet<string> Roles { get; } = roles;
        public bool IsInRole(string role) => Roles.Contains(role);
    }
}
