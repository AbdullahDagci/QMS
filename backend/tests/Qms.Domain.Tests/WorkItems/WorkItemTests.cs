using Qms.Domain.WorkItems;

namespace Qms.Domain.Tests.WorkItems;
public sealed class WorkItemTests
{
    [Fact]
    public void Lifecycle_requires_evidence_and_independent_verification()
    {
        var now=DateTimeOffset.UtcNow;var item=WorkItem.Create(Guid.NewGuid(),"M.01",Guid.NewGuid(),"SP-1","FollowUp","High","Takip","Kabul kriteri",Guid.NewGuid(),"Emre",Guid.NewGuid(),"Üretim",Guid.NewGuid(),"Zeynep",now.AddDays(3),now);
        item.Assign(1,now);item.Start(2,now);item.Submit(3,"Kalibrasyon sertifikası",now);item.Verify(4,true,"Kanıt uygundur",now);
        Assert.Equal(WorkItemStatus.Completed,item.Status);Assert.Equal(5,item.Version);Assert.NotNull(item.CompletedAtUtc);
    }
    [Fact]
    public void Reject_returns_work_to_owner()
    {
        var now=DateTimeOffset.UtcNow;var item=WorkItem.Create(Guid.NewGuid(),null,null,null,"GeneralAction","Normal","İş","Açıklama",Guid.NewGuid(),"Sorumlu",null,null,Guid.NewGuid(),"Doğrulayıcı",now.AddDays(1),now);
        item.Assign(1,now);item.Start(2,now);item.Submit(3,"Kanıt",now);item.Verify(4,false,"Kanıt yetersiz",now);
        Assert.Equal(WorkItemStatus.InProgress,item.Status);Assert.Equal("Kanıt yetersiz",item.VerificationNote);
    }
    [Fact]
    public void Due_date_must_be_future()=>Assert.Throws<ArgumentException>(()=>WorkItem.Create(Guid.NewGuid(),null,null,null,"GeneralAction","Normal","İş","Açıklama",Guid.NewGuid(),"Sorumlu",null,null,Guid.NewGuid(),"Doğrulayıcı",DateTimeOffset.UtcNow.AddMinutes(-1),DateTimeOffset.UtcNow));
}
