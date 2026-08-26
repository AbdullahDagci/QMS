using Qms.Application.Security;

namespace Qms.Infrastructure.Identity;

public sealed record DevelopmentProfile(string Key, Guid UserId, string DisplayName, Guid DepartmentId, IReadOnlyList<string> Roles);

public static class DevelopmentProfiles
{
    public static readonly Guid QualityDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000101");
    public static readonly Guid ProductionDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000102");
    public static readonly Guid RegulatoryDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000103");
    public static readonly Guid ValidationDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000104");
    public static readonly Guid EngineeringDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000105");
    public static readonly Guid InformationTechnologyDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000106");

    public static readonly IReadOnlyList<DevelopmentProfile> All =
    [
        new("quality", Guid.Parse("01991f70-6f40-7000-8000-000000000001"), "Elif Yılmaz", QualityDepartmentId, [QmsRoles.QualityAssurance]),
        new("quality-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000018"), "Mert Kaya", QualityDepartmentId, [QmsRoles.QualityAssurance]),
        new("approver", Guid.Parse("01991f70-6f40-7000-8000-000000000010"), "Zeynep Demir", QualityDepartmentId, [QmsRoles.Approver]),
        new("reporter", Guid.Parse("01991f70-6f40-7000-8000-000000000011"), "Burak Aydın", ProductionDepartmentId, [QmsRoles.DeviationReporter]),
        new("investigator", Guid.Parse("01991f70-6f40-7000-8000-000000000012"), "Selin Arslan", QualityDepartmentId, [QmsRoles.Investigator, QmsRoles.QualityViewer]),
        new("action-owner", Guid.Parse("01991f70-6f40-7000-8000-000000000013"), "Emre Şahin", ProductionDepartmentId, [QmsRoles.ActionOwner, QmsRoles.QualityViewer]),
        new("viewer", Guid.Parse("01991f70-6f40-7000-8000-000000000014"), "Derya Koç", QualityDepartmentId, [QmsRoles.QualityViewer]),
        new("admin", Guid.Parse("01991f70-6f40-7000-8000-000000000015"), "Okan Çelik", QualityDepartmentId, [QmsRoles.Administrator]),
        new("manager", Guid.Parse("01991f70-6f40-7000-8000-000000000016"), "Hakan Özkan", ProductionDepartmentId, [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("qualified-person", Guid.Parse("01991f70-6f40-7000-8000-000000000017"), "Dr. Aylin Kurt", QualityDepartmentId, [QmsRoles.QualifiedPerson, QmsRoles.Approver]),
        new("regulatory", Guid.Parse("01991f70-6f40-7000-8000-000000000019"), "Ece Aksoy", RegulatoryDepartmentId, [QmsRoles.RegulatoryAffairs, QmsRoles.QualityViewer]),
        new("document-controller", Guid.Parse("01991f70-6f40-7000-8000-000000000020"), "Tolga Erdem", QualityDepartmentId, [QmsRoles.DocumentController, QmsRoles.QualityViewer]),
        new("training-coordinator", Guid.Parse("01991f70-6f40-7000-8000-000000000021"), "Gizem Tunç", QualityDepartmentId, [QmsRoles.TrainingCoordinator, QmsRoles.QualityViewer]),
        new("validation-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000022"), "Onur Keskin", ValidationDepartmentId, [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("engineering-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000023"), "Ceren Yıldız", EngineeringDepartmentId, [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("it-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000024"), "Kerem Polat", InformationTechnologyDepartmentId, [QmsRoles.DepartmentManager, QmsRoles.QualityViewer])
    ];

    public static DevelopmentProfile Resolve(string key) =>
        All.FirstOrDefault(profile => profile.Key == key) ?? All[0];
}
