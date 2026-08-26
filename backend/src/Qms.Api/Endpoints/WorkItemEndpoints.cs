using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Application.WorkItems;
using Qms.Contracts.WorkItems;

namespace Qms.Api.Endpoints;
public static class WorkItemEndpoints
{
    public static IEndpointRouteBuilder MapWorkItemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var g=endpoints.MapGroup("/api/v1/work-items").WithTags("Work Tracking").RequireAuthorization(QmsPolicies.WorkTrackingView);
        g.MapGet("/options",(IWorkItemService s,CancellationToken ct)=>Run(()=>s.GetOptionsAsync(ct)));
        g.MapGet("/lookups",(IWorkItemService s,CancellationToken ct)=>Run(()=>s.ListLookupsAsync(ct)));
        g.MapPost("/lookups",(CreateWorkItemLookupDefinitionRequest r,IWorkItemService s,CancellationToken ct)=>Run(()=>s.CreateLookupAsync(r,ct),true)).RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPut("/lookups/{id:guid}",(Guid id,UpdateWorkItemLookupDefinitionRequest r,IWorkItemService s,CancellationToken ct)=>Run(()=>s.UpdateLookupAsync(id,r,ct))).RequireAuthorization(QmsPolicies.AdministrationManage);
        g.MapPost("/search",(WorkItemSearchRequest r,IWorkItemService s,CancellationToken ct)=>Run(()=>s.SearchAsync(r,ct)));
        g.MapGet("/{id:guid}/details",async(Guid id,IWorkItemService s,CancellationToken ct)=>(await s.GetDetailsAsync(id,ct))is{}x?Results.Ok(x):Results.NotFound());
        g.MapGet("/{id:guid}/final-report",async(Guid id,IWorkItemService s,IWorkItemFinalReportService reports,HttpResponse response,CancellationToken ct)=>{var d=await s.GetDetailsAsync(id,ct);if(d is null)return Results.NotFound();try{var f=await reports.EnsureGeneratedAsync(d,ct);response.Headers.Append("X-Content-SHA256",f.Sha256);return Results.File(f.Content,"application/pdf",f.FileName);}catch(InvalidOperationException e){return Results.Problem(statusCode:409,title:"Nihai iş kaydı çıktısı üretilemedi",detail:e.Message);}});
        g.MapPost("/",(CreateWorkItemRequest r,IWorkItemService s,CancellationToken ct)=>Run(()=>s.CreateAsync(r,ct),true)).RequireAuthorization(QmsPolicies.WorkTrackingCreate);
        g.MapPost("/{id:guid}/transitions",(Guid id,TransitionWorkItemRequest r,IWorkItemService s,CancellationToken ct)=>Run(()=>s.TransitionAsync(id,r,ct))).RequireAuthorization(QmsPolicies.WorkTrackingManage);
        return endpoints;
    }
    private static async Task<IResult> Run<T>(Func<Task<T>> a,bool created=false){try{var x=await a();return created?Results.Json(x,statusCode:201):x is null?Results.NotFound():Results.Ok(x);}catch(ArgumentException e){return Results.Problem(statusCode:400,title:"İş kaydı doğrulanamadı",detail:e.Message);}catch(InvalidOperationException e){return Results.Problem(statusCode:409,title:"İş kuralı geçişi engelledi",detail:e.Message);}catch(DbUpdateConcurrencyException){return Results.Problem(statusCode:409,title:"Eşzamanlı güncelleme çakışması",detail:"Kayıt değişti; ekranı yenileyin.");}}
}
