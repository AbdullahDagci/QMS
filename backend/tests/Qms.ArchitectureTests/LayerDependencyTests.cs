namespace Qms.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_DoesNotReferenceInfrastructureOrAspNetCore()
    {
        var references = typeof(Qms.Domain.QualityRecords.QualityRecord)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("Qms.Infrastructure", references);
        Assert.DoesNotContain(references, name => name?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
    }
}
