using Microsoft.Extensions.Configuration;
using Qms.Application.Security;

namespace Qms.Infrastructure.Identity;

public sealed record DevelopmentProfile(
    string Key,
    Guid UserId,
    string DisplayName,
    string DepartmentCode,
    string DepartmentName,
    IReadOnlyList<string> Roles)
{
    public Guid DepartmentId => DepartmentCode switch
    {
        "URT" => Guid.Parse("01991f70-6f40-7000-8000-000000000102"),
        "RUH" => Guid.Parse("01991f70-6f40-7000-8000-000000000103"),
        "VAL" => Guid.Parse("01991f70-6f40-7000-8000-000000000104"),
        "MUH" => Guid.Parse("01991f70-6f40-7000-8000-000000000105"),
        "BT" => Guid.Parse("01991f70-6f40-7000-8000-000000000106"),
        "SYS" => Guid.Parse("01991f70-6f40-7000-8000-000000000107"),
        _ => Guid.Parse("01991f70-6f40-7000-8000-000000000101")
    };
}

public static class DevelopmentProfiles
{
    // Ücretsiz demo ortamında (DemoMode:Enabled) hızlı giriş profilleri production'da da açılır.
    public static bool AreEnabled(bool isDevelopment, IConfiguration configuration) =>
        isDevelopment || configuration.GetValue("DemoMode:Enabled", false);

    public static readonly IReadOnlyList<DevelopmentProfile> All =
    [
        new("quality", Guid.Parse("01991f70-6f40-7000-8000-000000000001"), "Elif Yılmaz", "KG", "Kalite Güvence", [QmsRoles.QualityAssurance]),
        new("quality-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000018"), "Mert Kaya", "KG", "Kalite Güvence", [QmsRoles.QualityAssurance]),
        new("approver", Guid.Parse("01991f70-6f40-7000-8000-000000000010"), "Zeynep Demir", "KG", "Kalite Güvence", [QmsRoles.Approver]),
        new("reporter", Guid.Parse("01991f70-6f40-7000-8000-000000000011"), "Burak Aydın", "URT", "Üretim", [QmsRoles.DeviationReporter]),
        new("investigator", Guid.Parse("01991f70-6f40-7000-8000-000000000012"), "Selin Arslan", "KG", "Kalite Güvence", [QmsRoles.Investigator, QmsRoles.QualityViewer]),
        new("action-owner", Guid.Parse("01991f70-6f40-7000-8000-000000000013"), "Emre Şahin", "URT", "Üretim", [QmsRoles.ActionOwner, QmsRoles.QualityViewer]),
        new("viewer", Guid.Parse("01991f70-6f40-7000-8000-000000000014"), "Derya Koç", "KG", "Kalite Güvence", [QmsRoles.QualityViewer]),
        new("admin", Guid.Parse("01991f70-6f40-7000-8000-000000000015"), "Okan Çelik", "SYS", "Sistem Yönetimi", [QmsRoles.Administrator]),
        new("manager", Guid.Parse("01991f70-6f40-7000-8000-000000000016"), "Hakan Özkan", "URT", "Üretim", [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("qualified-person", Guid.Parse("01991f70-6f40-7000-8000-000000000017"), "Dr. Aylin Kurt", "KG", "Kalite Güvence", [QmsRoles.QualifiedPerson, QmsRoles.Approver]),
        new("regulatory", Guid.Parse("01991f70-6f40-7000-8000-000000000019"), "Ece Aksoy", "RUH", "Ruhsatlandırma", [QmsRoles.RegulatoryAffairs, QmsRoles.QualityViewer]),
        new("document-controller", Guid.Parse("01991f70-6f40-7000-8000-000000000020"), "Tolga Erdem", "KG", "Kalite Güvence", [QmsRoles.DocumentController, QmsRoles.QualityViewer]),
        new("training-coordinator", Guid.Parse("01991f70-6f40-7000-8000-000000000021"), "Gizem Tunç", "KG", "Kalite Güvence", [QmsRoles.TrainingCoordinator, QmsRoles.QualityViewer]),
        new("validation-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000022"), "Onur Keskin", "VAL", "Validasyon", [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("engineering-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000023"), "Ceren Yıldız", "MUH", "Mühendislik", [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("it-reviewer", Guid.Parse("01991f70-6f40-7000-8000-000000000024"), "Kerem Polat", "BT", "Bilgi Teknolojileri", [QmsRoles.DepartmentManager, QmsRoles.QualityViewer]),
        new("learner", Guid.Parse("01991f70-6f40-7000-8000-000000000025"), "Aslı Çetin", "URT", "Üretim", [QmsRoles.Learner]),
        new("trainer", Guid.Parse("01991f70-6f40-7000-8000-000000000026"), "Barış Şen", "KG", "Kalite Güvence", [QmsRoles.Trainer])
    ];

    public static DevelopmentProfile Resolve(string key) =>
        All.FirstOrDefault(profile => profile.Key == key) ?? All[0];
}
