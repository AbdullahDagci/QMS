using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.RiskManagement;
using Qms.Application.ElectronicSignatures;
using Qms.Application.Security;
using Qms.Contracts.Common;
using Qms.Contracts.RiskManagement;
using Qms.Domain.AuditTrail;
using Qms.Domain.Notifications;
using Qms.Domain.QualityRecords;
using Qms.Domain.RiskManagement;
using Qms.Infrastructure.Security;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.RiskManagement;

public sealed class RiskManagementService(
    QmsDbContext db,
    TimeProvider time,
    ICurrentUser user,
    IElectronicSignatureService signatures
) : IRiskManagementService
{
    public async Task<RiskOptionsResponse> GetOptionsAsync(CancellationToken ct)
    {
        var people = await (
            from u in db.Users.AsNoTracking()
            join d in db.Departments.AsNoTracking() on u.DepartmentId equals d.Id into ds
            from d in ds.DefaultIfEmpty()
            where u.IsActive
            orderby u.DisplayName
            select new RiskIdentityOption(u.Id, u.DisplayName, d == null ? null : d.Name)
        ).ToListAsync(ct);
        var defs = await db
            .RiskLookupDefinitions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);
        IReadOnlyList<RiskCodeOption> Of(string c) =>
            defs.Where(x => x.Category == c)
                .Select(x => new RiskCodeOption(x.Code, x.Name))
                .ToList();
        return new(people, Of("Category"), Of("Methodology"), Of("MatrixVersion"));
    }

    public async Task<IReadOnlyList<RiskLookupResponse>> ListLookupsAsync(CancellationToken ct) =>
        await db
            .RiskLookupDefinitions.AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SortOrder)
            .Select(x => new RiskLookupResponse(
                x.Id,
                x.Category,
                x.Code,
                x.Name,
                x.SortOrder,
                x.IsActive
            ))
            .ToListAsync(ct);

    public async Task<RiskLookupResponse> CreateLookupAsync(
        CreateRiskLookupRequest r,
        CancellationToken ct
    )
    {
        if (
            await db.RiskLookupDefinitions.AnyAsync(
                x => x.Category == r.Category.Trim() && x.Code == r.Code.Trim(),
                ct
            )
        )
            throw new InvalidOperationException("Aynı M.11 lookup kodu mevcut.");
        var now = time.GetUtcNow();
        var x = RiskLookupDefinition.Create(r.Category, r.Code, r.Name, r.SortOrder, now);
        db.Add(x);
        db.Add(
            Event(
                "RiskLookupDefinition",
                x.Id,
                1,
                "RiskLookupCreated",
                now,
                new
                {
                    x.Category,
                    x.Code,
                    x.Name,
                }
            )
        );
        await db.SaveChangesAsync(ct);
        return new(x.Id, x.Category, x.Code, x.Name, x.SortOrder, x.IsActive);
    }

    public async Task<RiskLookupResponse?> UpdateLookupAsync(
        Guid id,
        UpdateRiskLookupRequest r,
        CancellationToken ct
    )
    {
        var x = await db.RiskLookupDefinitions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (x is null)
            return null;
        var now = time.GetUtcNow();
        x.Update(r.Name, r.SortOrder, r.IsActive, now);
        db.Add(
            Event(
                "RiskLookupDefinition",
                x.Id,
                1,
                "RiskLookupUpdated",
                now,
                new
                {
                    x.Category,
                    x.Code,
                    x.Name,
                    x.IsActive,
                }
            )
        );
        await db.SaveChangesAsync(ct);
        return new(x.Id, x.Category, x.Code, x.Name, x.SortOrder, x.IsActive);
    }

    public async Task<PagedResponse<RiskListItemResponse>> SearchAsync(
        RiskSearchRequest r,
        CancellationToken ct
    )
    {
        if (r.Page < 1 || r.PageSize is not (10 or 25 or 50 or 100))
            throw new ArgumentException("Sayfa geçersizdir.");
        var q =
            from x in db.RiskAssessments.AsNoTracking()
            join qr in db.VisibleQualityRecords(user) on x.QualityRecordId equals qr.Id
            select new Row { Risk = x, Number = qr.RecordNumber };
        foreach (var f in r.Filters ?? [])
        {
            var v = f.Value?.Trim() ?? "";
            q = f.Field.Trim().ToLowerInvariant() switch
            {
                "recordnumber" => q.Where(x => x.Number.ToLower().Contains(v.ToLower())),
                "process" => q.Where(x => x.Risk.Process.ToLower().Contains(v.ToLower())),
                "category" => q.Where(x => x.Risk.Category == v),
                "status" => Enum.TryParse<RiskAssessmentStatus>(v, true, out var s)
                    ? q.Where(x => x.Risk.Status == s)
                    : throw new ArgumentException("Durum geçersizdir."),
                _ => throw new ArgumentException($"Filtre kolonu geçersiz: {f.Field}"),
            };
        }
        var total = await q.LongCountAsync(ct);
        var desc = r.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        q =
            r.SortBy.ToLowerInvariant() == "recordnumber"
                ? (desc ? q.OrderByDescending(x => x.Number) : q.OrderBy(x => x.Number))
                : (
                    desc
                        ? q.OrderByDescending(x => x.Risk.CreatedAtUtc)
                        : q.OrderBy(x => x.Risk.CreatedAtUtc)
                );
        var items = await q.Skip((r.Page - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(x => new RiskListItemResponse(
                x.Risk.Id,
                x.Number,
                x.Risk.Process,
                x.Risk.Category,
                x.Risk.Methodology,
                x.Risk.Items.Select(i => (int?)(i.Severity * i.Occurrence * i.Detectability)).Max() ?? 0,
                x.Risk.Items.Count(i => i.Status == RiskItemStatus.ActionRequired),
                x.Risk.Owner,
                x.Risk.Status.ToString(),
                x.Risk.CreatedAtUtc,
                x.Risk.Version
            ))
            .ToListAsync(ct);
        return new(
            items,
            r.Page,
            r.PageSize,
            total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)r.PageSize)
        );
    }

    public async Task<RiskDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var x = await db
            .RiskAssessments.AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (x is null)
            return null;
        var qr = await db
            .VisibleQualityRecords(user)
            .SingleOrDefaultAsync(q => q.Id == x.QualityRecordId, ct);
        if (qr is null) return null;
        var ev = await db
            .AuditEvents.AsNoTracking()
            .Where(e => e.AggregateType == "RiskAssessment" && e.AggregateId == id)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToListAsync(ct);
        var sig = await db
            .ElectronicSignatures.AsNoTracking()
            .Where(s => s.QualityRecordId == x.QualityRecordId)
            .OrderByDescending(s => s.SignedAtUtc)
            .ToListAsync(ct);
        var transitions = new List<RiskTransitionResponse>();
        foreach (var t in Transitions(x.Status))
        {
            var role = t.Code is "approve" or "close"
                ? WorkflowTaskRoles.RiskApprover
                : WorkflowTaskRoles.RiskOwner;
            if (await CanAct(id, role, ct))
                transitions.Add(t);
        }
        var record = new RiskRecordResponse(
            x.Id,
            x.QualityRecordId,
            qr.RecordNumber,
            x.Process,
            x.Scope,
            x.Category,
            x.Methodology,
            x.MatrixVersion,
            x.ActionThreshold,
            x.OwnerUserId,
            x.Owner,
            x.OwnerDepartmentId,
            x.OwnerDepartment,
            x.ApproverUserId,
            x.Approver,
            x.Status.ToString(),
            x.CreatedAtUtc,
            x.UpdatedAtUtc,
            x.ClosedAtUtc,
            x.Version
        );
        return new(
            record,
            x.Items.OrderByDescending(i => i.InitialRpn)
                .Select(i => new RiskItemResponse(
                    i.Id,
                    i.FailureMode,
                    i.Effect,
                    i.Cause,
                    i.ExistingControls,
                    i.Severity,
                    i.Occurrence,
                    i.Detectability,
                    i.InitialRpn,
                    i.Action,
                    i.ActionOwnerUserId,
                    i.ActionOwner,
                    i.ActionDueAtUtc,
                    i.ActionEvidence,
                    i.ResidualSeverity,
                    i.ResidualOccurrence,
                    i.ResidualDetectability,
                    i.ResidualRpn,
                    i.ResidualRationale,
                    i.Status.ToString()
                ))
                .ToList(),
            ev.Select(e => new RiskEventResponse(
                    e.Id,
                    e.AggregateVersion,
                    e.EventType,
                    e.ActorDisplayNameSnapshot,
                    e.OccurredAtUtc,
                    e.Reason,
                    e.Payload.RootElement.Clone()
                ))
                .ToList(),
            sig.Select(s => new RiskSignatureResponse(
                    s.Id,
                    s.RecordVersion,
                    s.SignerDisplayNameSnapshot,
                    s.Meaning,
                    s.SignedAtUtc,
                    s.ContentHash,
                    s.Comment
                ))
                .ToList(),
            transitions
        );
    }

    public async Task<RiskDetailsResponse> CreateAsync(
        CreateRiskAssessmentRequest r,
        CancellationToken ct
    )
    {
        if (!user.DepartmentId.HasValue)
            throw new InvalidOperationException(
                "Risk kaydı oluşturan kullanıcının bölümü olmalıdır."
            );
        await Lookup("Category", r.Category, ct);
        await Lookup("Methodology", r.Methodology, ct);
        await Lookup("MatrixVersion", r.MatrixVersion, ct);
        var owner = await Resolve(r.OwnerUserId, "Risk sorumlusu", ct);
        var approver = await Resolve(r.ApproverUserId, "Risk onaylayanı", ct);
        if (owner.Id == approver.Id || user.Id == approver.Id)
            throw new InvalidOperationException(
                "Risk sahibi/oluşturan ile bağımsız onaylayan aynı kişi olamaz."
            );
        var now = time.GetUtcNow();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var no = $"RSK-{now.Year}-{await Next("risk-assessment", now.Year, tx, ct):000000}";
        var qr = QualityRecord.Create(no, "M.11", user.Id, user.DepartmentId.Value, now);
        qr.Submit(now);
        var x = RiskAssessment.Create(
            qr.Id,
            r.Process,
            r.Scope,
            r.Category,
            r.Methodology,
            r.MatrixVersion,
            r.ActionThreshold,
            owner.Id,
            owner.Name,
            owner.DepartmentId,
            owner.Department,
            approver.Id,
            approver.Name,
            now
        );
        db.Add(qr);
        db.Add(x);
        await Assign(x.Id, WorkflowTaskRoles.RiskOwner, owner.Id, now, null, ct);
        await Assign(x.Id, WorkflowTaskRoles.RiskApprover, approver.Id, now, null, ct);
        db.Add(
            Audit(
                x,
                "RiskAssessmentCreated",
                now,
                new
                {
                    no,
                    r.Category,
                    r.Methodology,
                    r.MatrixVersion,
                    r.ActionThreshold,
                }
            )
        );
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await GetDetailsAsync(x.Id, ct))!;
    }

    public async Task<RiskDetailsResponse?> AddItemAsync(
        Guid id,
        AddRiskItemRequest r,
        CancellationToken ct
    )
    {
        await EnsureActor(id, WorkflowTaskRoles.RiskOwner, ct);
        var x = await Load(id, ct);
        if (x is null)
            return null;
        var now = time.GetUtcNow();
        (Guid Id, string Name, Guid? DepartmentId, string? Department)? owner = null;
        if (r.ActionOwnerUserId.HasValue)
            owner = await Resolve(r.ActionOwnerUserId.Value, "Aksiyon sorumlusu", ct);
        var item = RiskItem.Create(
            id,
            r.FailureMode,
            r.Effect,
            r.Cause,
            r.ExistingControls,
            r.Severity,
            r.Occurrence,
            r.Detectability,
            r.Action,
            owner?.Id,
            owner?.Name,
            r.ActionDueAtUtc?.ToUniversalTime(),
            x.ActionThreshold,
            now
        );
        x.AddItem(r.ExpectedVersion, item, now);
        db.Add(item);
        if (item.ActionOwnerUserId.HasValue)
            await Assign(
                id,
                $"RiskAction:{item.Id}",
                item.ActionOwnerUserId.Value,
                now,
                item.ActionDueAtUtc,
                ct
            );
        db.Add(
            Audit(
                x,
                "RiskItemAdded",
                now,
                new
                {
                    item.Id,
                    item.InitialRpn,
                    item.Status,
                }
            )
        );
        await db.SaveChangesAsync(ct);
        return await GetDetailsAsync(id, ct);
    }

    public async Task<RiskDetailsResponse?> CompleteActionAsync(
        Guid id,
        Guid itemId,
        CompleteRiskActionRequest r,
        CancellationToken ct
    )
    {
        await EnsureActor(id, $"RiskAction:{itemId}", ct);
        var x = await Load(id, ct);
        if (x is null)
            return null;
        var now = time.GetUtcNow();
        x.CompleteAction(r.ExpectedVersion, itemId, r.Evidence, now);
        await Complete(id, $"RiskAction:{itemId}", now, ct);
        db.Add(Audit(x, "RiskActionCompleted", now, new { itemId }, r.Evidence));
        await db.SaveChangesAsync(ct);
        return await GetDetailsAsync(id, ct);
    }

    public async Task<RiskDetailsResponse?> SetResidualAsync(
        Guid id,
        Guid itemId,
        SetResidualRiskRequest r,
        CancellationToken ct
    )
    {
        await EnsureActor(id, WorkflowTaskRoles.RiskOwner, ct);
        var x = await Load(id, ct);
        if (x is null)
            return null;
        var now = time.GetUtcNow();
        x.SetResidual(
            r.ExpectedVersion,
            itemId,
            r.Severity,
            r.Occurrence,
            r.Detectability,
            r.Rationale,
            now
        );
        db.Add(
            Audit(
                x,
                "ResidualRiskAssessed",
                now,
                new
                {
                    itemId,
                    r.Severity,
                    r.Occurrence,
                    r.Detectability,
                },
                r.Rationale
            )
        );
        await db.SaveChangesAsync(ct);
        return await GetDetailsAsync(id, ct);
    }

    public async Task<RiskDetailsResponse?> TransitionAsync(
        Guid id,
        TransitionRiskRequest r,
        CancellationToken ct
    )
    {
        var x = await Load(id, ct);
        if (x is null)
            return null;
        var role = r.Transition is "approve" or "close"
            ? WorkflowTaskRoles.RiskApprover
            : WorkflowTaskRoles.RiskOwner;
        await EnsureActor(id, role, ct);
        var now = time.GetUtcNow();
        var qr = await db.QualityRecords.SingleAsync(q => q.Id == x.QualityRecordId, ct);
        string type;
        switch (r.Transition)
        {
            case "start-scoring":
                x.StartScoring(r.ExpectedVersion, now);
                type = "RiskScoringStarted";
                break;
            case "complete-scoring":
                x.CompleteScoring(r.ExpectedVersion, now);
                type = "RiskScoringCompleted";
                break;
            case "start-residual":
                x.StartResidualReview(r.ExpectedVersion, now);
                type = "ResidualReviewStarted";
                break;
            case "approve":
                await signatures.AuthenticateAsync(r.SignaturePassword, r.SignatureMeaningAccepted, ct);
                x.Approve(r.ExpectedVersion, now);
                db.Add(
                    signatures.CreateInternal(
                        x.QualityRecordId,
                        "RiskAssessment",
                        x.Id,
                        x.Version,
                        "approve",
                        "FMEA kalıntı risk kabul ve onayı",
                        new { assessment = x, items = x.Items, r.Note },
                        now,
                        r.Note)
                );
                type = "RiskAssessmentApproved";
                break;
            case "close":
                await signatures.AuthenticateAsync(r.SignaturePassword, r.SignatureMeaningAccepted, ct);
                x.Close(r.ExpectedVersion, now);
                qr.Close(now, false);
                db.Add(
                    signatures.CreateInternal(
                        x.QualityRecordId,
                        "RiskAssessment",
                        x.Id,
                        x.Version,
                        "close",
                        "FMEA nihai kapanış onayı",
                        new { assessment = x, items = x.Items, r.Note },
                        now,
                        r.Note)
                );
                await CompleteAll(id, now, ct);
                type = "RiskAssessmentClosed";
                break;
            default:
                throw new ArgumentException("Risk geçişi geçersizdir.");
        }
        db.Add(Audit(x, type, now, new { r.Transition }, r.Note));
        await db.SaveChangesAsync(ct);
        return await GetDetailsAsync(id, ct);
    }

    private Task<RiskAssessment?> Load(Guid id, CancellationToken ct) =>
        db.RiskAssessments.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id
            && db.VisibleQualityRecords(user).Any(record => record.Id == x.QualityRecordId), ct);

    private static IReadOnlyList<RiskTransitionResponse> Transitions(RiskAssessmentStatus s) =>
        s switch
        {
            RiskAssessmentStatus.Draft => [new("start-scoring", "Puanlamayı başlat")],
            RiskAssessmentStatus.Scoring =>
            [
                new("complete-scoring", "Puanlamayı ve aksiyon planını tamamla"),
            ],
            RiskAssessmentStatus.ActionPlan =>
            [
                new("start-residual", "Kalıntı risk değerlendirmesine geç"),
            ],
            RiskAssessmentStatus.ResidualReview =>
            [
                new("approve", "Kalıntı riskleri onayla", true),
            ],
            RiskAssessmentStatus.Approved => [new("close", "FMEA kaydını kapat", true)],
            _ => [],
        };

    private async Task Assign(
        Guid id,
        string role,
        Guid uid,
        DateTimeOffset now,
        DateTimeOffset? due,
        CancellationToken ct
    )
    {
        if (
            await db.WorkflowTaskAssignments.AnyAsync(
                t =>
                    t.AggregateType == "RiskAssessment"
                    && t.AggregateId == id
                    && t.TaskRole == role
                    && t.Status == WorkflowTaskStatus.Active,
                ct
            )
        )
            return;
        var p = await Resolve(uid, "Görev kullanıcısı", ct);
        db.Add(
            WorkflowTaskAssignment.Create(
                "RiskAssessment",
                id,
                role,
                p.Id,
                p.DepartmentId,
                now,
                due,
                assignedUserNameSnapshot: p.Name,
                assignedDepartmentNameSnapshot: p.Department
            )
        );
        db.Add(
            UserNotification.Create(
                p.Id,
                "M.11",
                "Yeni risk yönetimi görevi",
                role.StartsWith("RiskAction:") ? "Bir risk azaltma aksiyonu size atandı."
                    : role == WorkflowTaskRoles.RiskApprover ? "Bir FMEA onay görevi size atandı."
                    : "Bir FMEA risk sahipliği görevi size atandı.",
                $"/modules/m11?open={id}",
                now
            )
        );
    }

    private async Task Complete(Guid id, string role, DateTimeOffset now, CancellationToken ct)
    {
        foreach (
            var t in await db
                .WorkflowTaskAssignments.Where(t =>
                    t.AggregateType == "RiskAssessment"
                    && t.AggregateId == id
                    && t.TaskRole == role
                    && t.Status == WorkflowTaskStatus.Active
                )
                .ToListAsync(ct)
        )
            t.Complete(now);
    }

    private async Task CompleteAll(Guid id, DateTimeOffset now, CancellationToken ct)
    {
        foreach (
            var t in await db
                .WorkflowTaskAssignments.Where(t =>
                    t.AggregateType == "RiskAssessment"
                    && t.AggregateId == id
                    && t.Status == WorkflowTaskStatus.Active
                )
                .ToListAsync(ct)
        )
            t.Complete(now);
    }

    private async Task EnsureActor(Guid id, string role, CancellationToken ct)
    {
        if (!await CanAct(id, role, ct))
            throw new QmsForbiddenException(
                "Bu risk görevi size veya etkin delegasyonla size atanmamış."
            );
    }

    private async Task<bool> CanAct(Guid id, string role, CancellationToken ct)
    {
        var ids = await db
            .WorkflowTaskAssignments.AsNoTracking()
            .Where(t =>
                t.AggregateType == "RiskAssessment"
                && t.AggregateId == id
                && t.TaskRole == role
                && t.Status == WorkflowTaskStatus.Active
            )
            .Select(t => t.AssignedUserId)
            .ToListAsync(ct);
        if (ids.Contains(user.Id))
            return true;
        var now = time.GetUtcNow();
        return ids.Count > 0
            && await db
                .Delegations.AsNoTracking()
                .AnyAsync(
                    d =>
                        ids.Contains(d.DelegatorUserId)
                        && d.DelegateUserId == user.Id
                        && d.RevokedAtUtc == null
                        && d.StartsAtUtc <= now
                        && d.EndsAtUtc >= now
                        && (d.Scope == "M.11" || d.Scope == "ALL"),
                    ct
                );
    }


    private async Task Lookup(string c, string code, CancellationToken ct)
    {
        if (
            !await db.RiskLookupDefinitions.AnyAsync(
                x => x.Category == c && x.Code == code.Trim() && x.IsActive,
                ct
            )
        )
            throw new ArgumentException($"{c} etkin M.11 lookup kayıtlarından seçilmelidir.");
    }

    private async Task<(Guid Id, string Name, Guid? DepartmentId, string? Department)> Resolve(
        Guid id,
        string field,
        CancellationToken ct
    )
    {
        var p = await (
            from u in db.Users.AsNoTracking()
            join d in db.Departments.AsNoTracking() on u.DepartmentId equals d.Id into ds
            from d in ds.DefaultIfEmpty()
            where u.Id == id && u.IsActive
            select new
            {
                u.Id,
                u.DisplayName,
                u.DepartmentId,
                Department = d == null ? null : d.Name,
            }
        ).SingleOrDefaultAsync(ct);
        return p is null
            ? throw new ArgumentException($"{field} etkin kullanıcılardan seçilmelidir.")
            : (p.Id, p.DisplayName, p.DepartmentId, p.Department);
    }

    private AuditEvent Audit(
        RiskAssessment x,
        string type,
        DateTimeOffset now,
        object payload,
        string? reason = null
    ) => Event("RiskAssessment", x.Id, x.Version, type, now, payload, reason);

    private AuditEvent Event(
        string aggregate,
        Guid id,
        long v,
        string type,
        DateTimeOffset now,
        object payload,
        string? reason = null
    ) =>
        AuditEvent.Create(
            aggregate,
            id,
            v,
            type,
            user.Id,
            user.DisplayName,
            now,
            Qms.Infrastructure.Integrity.AuditCorrelation.Current,
            JsonSerializer.SerializeToDocument(payload),
            reason
        );


    private async Task<long> Next(
        string type,
        int year,
        IDbContextTransaction tx,
        CancellationToken ct
    )
    {
        var c = db.Database.GetDbConnection();
        if (c.State != ConnectionState.Open)
            await c.OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx.GetDbTransaction();
        cmd.CommandText =
            "INSERT INTO core.record_number_sequence (\"RecordType\",\"CalendarYear\",\"LastValue\") VALUES (@type,@year,1) ON CONFLICT (\"RecordType\",\"CalendarYear\") DO UPDATE SET \"LastValue\"=core.record_number_sequence.\"LastValue\"+1 RETURNING \"LastValue\";";
        Add(cmd, "type", type);
        Add(cmd, "year", year);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private static void Add(DbCommand c, string n, object v)
    {
        var p = c.CreateParameter();
        p.ParameterName = n;
        p.Value = v;
        c.Parameters.Add(p);
    }

    private sealed class Row
    {
        public required RiskAssessment Risk { get; init; }
        public required string Number { get; init; }
    }
}
