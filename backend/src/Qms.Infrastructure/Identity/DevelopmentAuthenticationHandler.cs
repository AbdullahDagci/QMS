using Qms.Application.Security;

namespace Qms.Infrastructure.Identity;

public sealed record DevelopmentProfile(string Key, Guid UserId, string DisplayName, Guid DepartmentId, IReadOnlyList<string> Roles);

public static class DevelopmentProfiles
{
    public static readonly Guid QualityDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000101");
    public static readonly Guid ProductionDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000102");
    public static readonly Guid RegulatoryDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000103");

    public static readonly IReadOnlyList<DevelopmentProfile> All =
    [
        new("quality", Guid.Parse("01991f70-6f40-7000-8000-000000000001"), "Kalite Güvence Uzmanı", QualityDepartmentId, [QmsRoles.QualityAssurance]),
        new("quality-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000018"), "Kalite Güvence Değerlendiricisi", QualityDepartmentId, [QmsRoles.QualityAssurance]),
        new("approver", Guid.Parse("01991f70-6f40-7000-8000-000000000010"), "Kalite Onaylayanı", QualityDepartmentId, [QmsRoles.Approver]),
        new("reporter", Guid.Parse("01991f70-6f40-7000-8000-000000000011"), "Sapma Bildiren", ProductionDepartmentId, [QmsRoles.DeviationReporter]),
        new("investigator", Guid.Parse("01991f70-6f40-7000-8000-000000000012"), "Araştırmacı", QualityDepartmentId, [QmsRoles.Investigator, QmsRoles.QualityViewer]),
        new("action-owner", Guid.Parse("01991f70-6f40-7000-8000-000000000013"), "Aksiyon Sorumlusu", ProductionDepartmentId, [QmsRoles.ActionOwner, QmsRoles.QualityViewer]),
        new("viewer", Guid.Parse("01991f70-6f40-7000-8000-000000000014"), "Kalite İzleyici", QualityDepartmentId, [QmsRoles.QualityViewer]),
        new("admin", Guid.Parse("01991f70-6f40-7000-8000-000000000015"), "Sistem Yöneticisi", QualityDepartmentId, [QmsRoles.Administrator]),
        new("manager", Guid.Parse("01991f70-6f40-7000-8000-000000000016"), "Bölüm Yöneticisi", ProductionDepartmentId, [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("qualified-person", Guid.Parse("01991f70-6f40-7000-8000-000000000017"), "Mesul Müdür", QualityDepartmentId, [QmsRoles.QualifiedPerson, QmsRoles.Approver]),
        new("regulatory", Guid.Parse("01991f70-6f40-7000-8000-000000000019"), "Ruhsatlandırma Uzmanı", RegulatoryDepartmentId, [QmsRoles.RegulatoryAffairs, QmsRoles.QualityViewer]),
        new("document-controller", Guid.Parse("01991f70-6f40-7000-8000-000000000020"), "Doküman Kontrol Sorumlusu", QualityDepartmentId, [QmsRoles.DocumentController, QmsRoles.QualityViewer]),
        new("training-coordinator", Guid.Parse("01991f70-6f40-7000-8000-000000000021"), "Eğitim Koordinatörü", QualityDepartmentId, [QmsRoles.TrainingCoordinator, QmsRoles.QualityViewer])
    ];

    public static DevelopmentProfile Resolve(string key) =>
        All.FirstOrDefault(profile => profile.Key == key) ?? All[0];
}
