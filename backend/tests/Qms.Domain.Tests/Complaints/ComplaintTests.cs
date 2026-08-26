using Qms.Domain.Complaints;

namespace Qms.Domain.Tests.Complaints;

public sealed class ComplaintTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Parallel_Investigations_And_Approved_Responses_Guard_Workflow()
    {
        var complaint = Create(); complaint.StartTriage(1, Now); complaint.CompleteTriage(2, Guid.NewGuid(), Guid.NewGuid(), Now);
        var preparer = Guid.NewGuid(); var approver = Guid.NewGuid();
        var preliminary = complaint.AddResponse(3, ComplaintResponseType.Preliminary, "İnceleme başlatıldı; ürün ve batch kontrol altındadır.", preparer, "Koordinatör", Now);
        Assert.Throws<InvalidOperationException>(() => complaint.StartInvestigation(4, Now));
        complaint.ApproveResponse(4, preliminary.Id, approver, "Onaylayan", Now); complaint.StartInvestigation(5, Now);
        var investigations = complaint.Investigations.ToArray(); complaint.CompleteInvestigation(6, investigations[0].Id, "Üretim kayıtları incelendi.", "Dolum basıncı", Now);
        Assert.Throws<InvalidOperationException>(() => complaint.FinishInvestigations(7, Now));
        complaint.CompleteInvestigation(7, investigations[1].Id, "Laboratuvar sonucu doğrulandı.", "Conta aşınması", Now); complaint.FinishInvestigations(8, Now);
        complaint.CompleteImpact(9, "Tek batch etkilenmiştir.", "Conta aşınması", Now); complaint.DecideCapa(10, true, Guid.NewGuid(), Now);
        var final = complaint.AddResponse(11, ComplaintResponseType.Final, "Araştırma tamamlandı ve düzeltici faaliyet başlatıldı.", preparer, "Koordinatör", Now);
        Assert.Throws<InvalidOperationException>(() => complaint.Close(12, "Kapatıldı", Now));
        complaint.ApproveResponse(12, final.Id, approver, "Onaylayan", Now); complaint.Close(13, "Müşteriye nihai yanıt iletildi.", Now);
        Assert.Equal(ComplaintStatus.Closed, complaint.Status);
    }

    [Fact]
    public void Trend_Is_Flagged_After_Two_Similar_Complaints()
    {
        var complaint = Create(similarCount: 2);
        Assert.True(complaint.TrendFlagged); Assert.Equal(2, complaint.SimilarComplaintCount);
    }

    private static Complaint Create(int similarCount = 0) => Complaint.Create(Guid.NewGuid(), "E-posta", "Örnek Müşteri", "Türkiye", "QMS Tablet 10 mg", "B260825", Now.AddDays(-2), Now.AddDays(-1), "Ürün kalitesi", "Tablet ambalajında kırık ürün bildirildi.", ComplaintSeverity.Major, false, true, true, true, "Fotoğraf ve iade numunesi", Guid.NewGuid(), "Şikâyet Koordinatörü", Now.AddDays(2), Now.AddDays(20), similarCount, [(Guid.NewGuid(), "Üretim", Guid.NewGuid(), "Üretim Araştırmacısı"), (Guid.NewGuid(), "Kalite Kontrol", Guid.NewGuid(), "KK Araştırmacısı")], Now);
}
