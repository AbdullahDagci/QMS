using Qms.Application.SystemCatalog;

namespace Qms.Application.Tests.SystemCatalog;

public sealed class QmsModuleCatalogTests
{
    [Fact]
    public void Catalog_ContainsAllSixteenModulesWithUniqueCodes()
    {
        Assert.Equal(16, QmsModuleCatalog.Modules.Count);
        Assert.Equal(16, QmsModuleCatalog.Modules.Select(module => module.Code).Distinct().Count());
    }
}
