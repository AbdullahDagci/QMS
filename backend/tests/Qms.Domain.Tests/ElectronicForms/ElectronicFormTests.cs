using System.Text.Json;
using Qms.Domain.ElectronicForms;

namespace Qms.Domain.Tests.ElectronicForms;

public sealed class ElectronicFormTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Form_version_requires_maker_checker_before_publication()
    {
        var creatorId = Guid.NewGuid();
        var version = CreateVersion(creatorId);
        version.SubmitForReview(1, Now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            version.Publish(2, creatorId, "Form Hazırlayan", Now.AddMinutes(2)));

        var publisherId = Guid.NewGuid();
        version.Publish(2, publisherId, "Kalite Onaylayan", Now.AddMinutes(2));

        Assert.Equal(ElectronicFormVersionStatus.Published, version.Status);
        Assert.Equal(publisherId, version.PublishedByUserId);
        Assert.Equal(3, version.Version);
    }

    [Fact]
    public void Published_form_version_cannot_be_changed()
    {
        var version = CreateVersion(Guid.NewGuid());
        version.SubmitForReview(1, Now.AddMinutes(1));
        version.Publish(2, Guid.NewGuid(), "Kalite Onaylayan", Now.AddMinutes(2));

        Assert.Throws<InvalidOperationException>(() =>
            version.UpdateDraft(3, Schema(), "İzinsiz değişiklik", "ReviewApprove", Now.AddMinutes(3)));
    }

    [Fact]
    public void Published_output_template_is_immutable()
    {
        var template = ElectronicFormOutputTemplate.CreateDefault(Guid.NewGuid(), Output(), Now);
        template.Publish(Now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => template.Update(2, Output(), Now.AddMinutes(2)));
    }

    [Fact]
    public void Published_form_identity_cannot_be_changed_by_an_unapproved_draft()
    {
        var definition = ElectronicFormDefinition.Create("FR-001", "Kontrol formu", "Açıklama", "Genel",
            ElectronicFormKind.Standard, Guid.NewGuid(), Guid.NewGuid(), "Form Hazırlayan", Now);
        definition.Publish(Guid.NewGuid(), 1, Now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => definition.UpdateMetadata(2, "Yeni ad", "Açıklama",
            "Genel", ElectronicFormKind.Standard, Now.AddMinutes(2)));
    }

    [Fact]
    public void Form_code_rejects_path_and_control_characters()
    {
        Assert.Throws<ArgumentException>(() => ElectronicFormDefinition.Create("../../form", "Kontrol formu",
            "Açıklama", "Genel", ElectronicFormKind.Standard, Guid.NewGuid(), Guid.NewGuid(),
            "Form Hazırlayan", Now));
    }

    [Fact]
    public void Electronic_record_is_pinned_to_versions_and_closes_only_after_submission()
    {
        var formVersionId = Guid.NewGuid();
        var outputTemplateId = Guid.NewGuid();
        var record = ElectronicFormRecord.Create(Guid.NewGuid(), Guid.NewGuid(), formVersionId, outputTemplateId,
            "FR-EKP-001", "Ekipman kontrolü", 4, JsonDocument.Parse("{\"sonuc\":\"uygun\"}"),
            Guid.NewGuid(), "Kayıt Sahibi", Now);

        Assert.Throws<InvalidOperationException>(() => record.Close(1, Now.AddMinutes(1)));

        record.Submit(1, JsonDocument.Parse("{\"sonuc\":\"uygun\"}"), Now.AddMinutes(1));
        record.Close(2, Now.AddMinutes(2));

        Assert.Equal(ElectronicFormRecordStatus.Closed, record.Status);
        Assert.Equal(formVersionId, record.FormVersionId);
        Assert.Equal(outputTemplateId, record.OutputTemplateId);
        Assert.Equal(4, record.FormVersionNumber);
    }

    [Fact]
    public void Electronic_record_rejects_stale_updates()
    {
        var record = ElectronicFormRecord.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "FR-001", "Kontrol", 1, JsonDocument.Parse("{}"), Guid.NewGuid(), "Kayıt Sahibi", Now);

        Assert.Throws<InvalidOperationException>(() =>
            record.UpdateDraft(2, JsonDocument.Parse("{\"alan\":\"değer\"}"), Now.AddMinutes(1)));
    }

    private static ElectronicFormVersion CreateVersion(Guid creatorId) =>
        ElectronicFormVersion.CreateDraft(Guid.NewGuid(), 1, Schema(), "İlk sürüm", "ReviewApprove",
            creatorId, "Form Hazırlayan", Now);

    private static JsonDocument Schema() => JsonDocument.Parse("""
        {"engineVersion":1,"sections":[{"key":"genel","title":"Genel","description":"","order":1,"columns":1,"fields":[{"key":"aciklama","label":"Açıklama","type":"shortText","required":true,"helpText":"","placeholder":"","width":12,"order":1,"maxLength":200,"min":null,"max":null,"unit":"","options":[],"visibilityCondition":null,"requiredCondition":null}]}]}
        """);

    private static JsonDocument Output() => JsonDocument.Parse("""
        {"title":"Kontrollü form","footerText":"QMS","primaryColor":"#0F7773","includeEmptyFields":false,"includeAuditTrail":true,"includeSignatures":true}
        """);
}
