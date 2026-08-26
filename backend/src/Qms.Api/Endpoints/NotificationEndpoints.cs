using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Contracts.Notifications;
using Qms.Infrastructure.Persistence;

namespace Qms.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").RequireAuthorization().WithTags("Notifications");
        group.MapGet("", async (QmsDbContext db, ICurrentUser currentUser, CancellationToken ct) =>
        {
            var query = db.UserNotifications.AsNoTracking().Where(x => x.UserId == currentUser.Id);
            var unread = await query.CountAsync(x => x.ReadAtUtc == null, ct);
            var items = await query.OrderByDescending(x => x.CreatedAtUtc).Take(50)
                .Select(x => new NotificationResponse(x.Id, x.ModuleCode, x.Title, x.Message, x.Link, x.CreatedAtUtc, x.ReadAtUtc)).ToListAsync(ct);
            return Results.Ok(new NotificationListResponse(unread, items));
        });
        group.MapPost("/{id:guid}/read", async (Guid id, QmsDbContext db, ICurrentUser currentUser, TimeProvider timeProvider, CancellationToken ct) =>
        {
            var item = await db.UserNotifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == currentUser.Id, ct);
            if (item is null) return Results.NotFound();
            item.MarkRead(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        group.MapPost("/read-all", async (QmsDbContext db, ICurrentUser currentUser, TimeProvider timeProvider, CancellationToken ct) =>
        {
            var items = await db.UserNotifications.Where(x => x.UserId == currentUser.Id && x.ReadAtUtc == null).ToListAsync(ct);
            var now = timeProvider.GetUtcNow();
            foreach (var item in items) item.MarkRead(now);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        return app;
    }
}
