namespace Qms.Domain.WorkItems;
public sealed class WorkItemLookupDefinition
{
    private WorkItemLookupDefinition() { }
    public Guid Id { get; private set; } public string Category { get; private set; } = ""; public string Code { get; private set; } = ""; public string Name { get; private set; } = ""; public int SortOrder { get; private set; } public bool IsActive { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; }
    public static WorkItemLookupDefinition Create(string category,string code,string name,int order,DateTimeOffset now){Validate(category,code,name,order);return new(){Id=Guid.CreateVersion7(),Category=category.Trim(),Code=code.Trim(),Name=name.Trim(),SortOrder=order,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now};}
    public void Update(string name,int order,bool active,DateTimeOffset now){Validate(Category,Code,name,order);Name=name.Trim();SortOrder=order;IsActive=active;UpdatedAtUtc=now;}
    private static void Validate(string category,string code,string name,int order){if(category.Trim() is not ("Category" or "Priority")||string.IsNullOrWhiteSpace(code)||code.Trim().Length>64||string.IsNullOrWhiteSpace(name)||name.Trim().Length>160||order<0)throw new ArgumentException("M.10 lookup tanımı geçersizdir.");}
}
