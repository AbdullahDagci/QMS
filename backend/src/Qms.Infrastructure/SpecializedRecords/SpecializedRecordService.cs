using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.ElectronicSignatures;
using Qms.Application.Security;
using Qms.Application.SpecializedRecords;
using Qms.Contracts.Common;
using Qms.Contracts.SpecializedRecords;
using Qms.Domain.AuditTrail;
using Qms.Domain.ElectronicSignatures;
using Qms.Domain.Notifications;
using Qms.Domain.QualityRecords;
using Qms.Domain.SpecializedRecords;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.SpecializedRecords;

public sealed class SpecializedRecordService(
    QmsDbContext db,
    TimeProvider time,
    ICurrentUser user,
    IElectronicSignatureService signatures
) : ISpecializedRecordService
{
    public async Task<SpecializedOptionsResponse> GetOptionsAsync(
        string module,
        CancellationToken ct
    )
    {
        ValidateModule(module);
        var people = await (
            from u in db.Users.AsNoTracking()
            join d in db.Departments.AsNoTracking() on u.DepartmentId equals d.Id into ds
            from d in ds.DefaultIfEmpty()
            where u.IsActive
            orderby u.DisplayName
            select new SpecializedUserOption(u.Id, u.DisplayName, d == null ? null : d.Name)
        ).ToListAsync(ct);
        var defs = await db
            .SpecializedLookupDefinitions.AsNoTracking()
            .Where(x => x.ModuleCode == module && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);
        IReadOnlyList<SpecializedCodeOption> Of(string c) =>
            defs.Where(x => x.Category == c)
                .Select(x => new SpecializedCodeOption(x.Code, x.Name))
                .ToList();
        return new(people, Of("Type"), Of("Subject"), Of("Scope"));
    }

    public async Task<IReadOnlyList<SpecializedLookupResponse>> ListLookupsAsync(
        string module,
        CancellationToken ct
    )
    {
        ValidateModule(module);
        return await db
            .SpecializedLookupDefinitions.AsNoTracking()
            .Where(x => x.ModuleCode == module)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SortOrder)
            .Select(x => new SpecializedLookupResponse(
                x.Id,
                x.ModuleCode,
                x.Category,
                x.Code,
                x.Name,
                x.SortOrder,
                x.IsActive
            ))
            .ToListAsync(ct);
    }

    public async Task<SpecializedLookupResponse> CreateLookupAsync(
        string module,
        CreateSpecializedLookupRequest r,
        CancellationToken ct
    )
    {
        ValidateModule(module);
        if (
            await db.SpecializedLookupDefinitions.AnyAsync(
                x =>
                    x.ModuleCode == module
                    && x.Category == r.Category.Trim()
                    && x.Code == r.Code.Trim(),
                ct
            )
        )
            throw new InvalidOperationException("Aynı lookup kodu mevcut.");
        var now = time.GetUtcNow();
        var x = SpecializedLookupDefinition.Create(
            module,
            r.Category,
            r.Code,
            r.Name,
            r.SortOrder,
            now
        );
        db.Add(x);
        db.Add(
            Event(
                "SpecializedLookupDefinition",
                x.Id,
                1,
                "LookupCreated",
                now,
                new
                {
                    module,
                    x.Category,
                    x.Code,
                    x.Name,
                }
            )
        );
        await db.SaveChangesAsync(ct);
        return Map(x);
    }

    public async Task<SpecializedLookupResponse?> UpdateLookupAsync(
        string module,
        Guid id,
        UpdateSpecializedLookupRequest r,
        CancellationToken ct
    )
    {
        var x = await db.SpecializedLookupDefinitions.SingleOrDefaultAsync(
            x => x.Id == id && x.ModuleCode == module,
            ct
        );
        if (x is null)
            return null;
        var now = time.GetUtcNow();
        x.Update(r.Name, r.SortOrder, r.IsActive, now);
        db.Add(
            Event(
                "SpecializedLookupDefinition",
                x.Id,
                1,
                "LookupUpdated",
                now,
                new
                {
                    module,
                    x.Category,
                    x.Code,
                    x.Name,
                    x.IsActive,
                }
            )
        );
        await db.SaveChangesAsync(ct);
        return Map(x);
    }

    public async Task<PagedResponse<SpecializedListItemResponse>> SearchAsync(
        string module,
        SpecializedSearchRequest r,
        CancellationToken ct
    )
    {
        ValidateModule(module);
        if (r.Page < 1 || r.PageSize is not (10 or 25 or 50 or 100))
            throw new ArgumentException("Sayfa geçersizdir.");
        var q =
            from x in db.SpecializedRecords.AsNoTracking()
            join qr in db.QualityRecords.AsNoTracking() on x.QualityRecordId equals qr.Id
            where x.ModuleCode == module
            select new { Record = x, qr.RecordNumber };
        foreach (var f in r.Filters ?? [])
        {
            var v = f.Value?.Trim() ?? "";
            q = f.Field.Trim().ToLowerInvariant() switch
            {
                "recordnumber" => q.Where(x => x.RecordNumber.ToLower().Contains(v.ToLower())),
                "title" => q.Where(x => x.Record.Title.ToLower().Contains(v.ToLower())),
                "status" => Enum.TryParse<SpecializedRecordStatus>(v, true, out var s)
                    ? q.Where(x => x.Record.Status == s)
                    : throw new ArgumentException("Durum geçersizdir."),
                _ => throw new ArgumentException("Filtre kolonu geçersizdir."),
            };
        }
        var total = await q.LongCountAsync(ct);
        q = r.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? q.OrderByDescending(x => x.Record.CreatedAtUtc)
            : q.OrderBy(x => x.Record.CreatedAtUtc);
        var items = await q.Skip((r.Page - 1) * r.PageSize)
            .Take(r.PageSize)
            .Select(x => new SpecializedListItemResponse(
                x.Record.Id,
                x.RecordNumber,
                x.Record.Title,
                x.Record.TypeName,
                x.Record.SubjectName,
                x.Record.ScopeName,
                x.Record.Owner,
                x.Record.Status.ToString(),
                x.Record.DueAtUtc,
                x.Record.CreatedAtUtc,
                x.Record.Version
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

    public async Task<SpecializedDetailsResponse?> GetDetailsAsync(
        string module,
        Guid id,
        CancellationToken ct
    )
    {
        ValidateModule(module);
        var x = await db
            .SpecializedRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.ModuleCode == module, ct);
        if (x is null)
            return null;
        var qr = await db
            .QualityRecords.AsNoTracking()
            .SingleAsync(q => q.Id == x.QualityRecordId, ct);
        var events = await db
            .AuditEvents.AsNoTracking()
            .Where(e => e.AggregateType == "SpecializedRecord" && e.AggregateId == id)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToListAsync(ct);
        var signatures = await db
            .ElectronicSignatures.AsNoTracking()
            .Where(s => s.QualityRecordId == x.QualityRecordId)
            .OrderByDescending(s => s.SignedAtUtc)
            .ToListAsync(ct);
        var transitions = new List<SpecializedTransitionResponse>();
        foreach (var t in Transitions(x.Status))
            if (await CanAct(module, id, Role(t.Code), ct))
                transitions.Add(t);
        using var data = JsonDocument.Parse(x.StructuredDataJson);
        var record = new SpecializedRecordResponse(
            x.Id,
            x.QualityRecordId,
            x.ModuleCode,
            qr.RecordNumber,
            x.Title,
            x.TypeCode,
            x.TypeName,
            x.SubjectCode,
            x.SubjectName,
            x.ScopeCode,
            x.ScopeName,
            x.Reference,
            x.Description,
            data.RootElement.Clone(),
            x.DueAtUtc,
            x.OwnerUserId,
            x.Owner,
            x.ReviewerUserId,
            x.Reviewer,
            x.ApproverUserId,
            x.Approver,
            x.Status.ToString(),
            x.CreatedAtUtc,
            x.UpdatedAtUtc,
            x.Version
        );
        return new(
            record,
            events
                .Select(e => new SpecializedEventResponse(
                    e.Id,
                    e.AggregateVersion,
                    e.EventType,
                    e.ActorDisplayNameSnapshot,
                    e.OccurredAtUtc,
                    e.Reason,
                    e.Payload.RootElement.Clone()
                ))
                .ToList(),
            signatures
                .Select(s => new SpecializedSignatureResponse(
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

    public async Task<SpecializedDetailsResponse> CreateAsync(
        string module,
        CreateSpecializedRecordRequest r,
        CancellationToken ct
    )
    {
        ValidateModule(module);
        if (!user.DepartmentId.HasValue)
            throw new InvalidOperationException("Kaydı oluşturan kullanıcının bölümü olmalıdır.");
        var owner = await Resolve(r.OwnerUserId, "Sorumlu", ct);
        var reviewer = await Resolve(r.ReviewerUserId, "İnceleyen", ct);
        var approver = await Resolve(r.ApproverUserId, "Onaylayan", ct);
        if (
            owner.Id != user.Id
            && !user.IsInRole(QmsRoles.Administrator)
            && !user.IsInRole(QmsRoles.QualityAssurance)
        )
            throw new QmsForbiddenException(
                "Kaydı yalnız seçilen sorumlu veya kalite yöneticisi oluşturabilir."
            );
        var type = await Lookup(module, "Type", r.TypeCode, ct);
        var subject = await Lookup(module, "Subject", r.SubjectCode, ct);
        var scope = await Lookup(module, "Scope", r.ScopeCode, ct);
        var now = time.GetUtcNow();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var number =
            $"{module.Replace(".", "")}-{now.Year}-{await Next(module.Replace(".", "").ToLowerInvariant(), now.Year, tx, ct):000000}";
        var qr = QualityRecord.Create(number, module, user.Id, user.DepartmentId.Value, now);
        qr.Submit(now);
        var x = SpecializedRecord.Create(
            qr.Id,
            module,
            r.Title,
            type.Code,
            type.Name,
            subject.Code,
            subject.Name,
            scope.Code,
            scope.Name,
            r.Reference,
            r.Description,
            r.StructuredData.GetRawText(),
            r.DueAtUtc,
            owner.Id,
            owner.Name,
            reviewer.Id,
            reviewer.Name,
            approver.Id,
            approver.Name,
            now
        );
        db.Add(qr);
        db.Add(x);
        await Assign(module, x.Id, WorkflowTaskRoles.SpecializedOwner, owner, now);
        await Assign(module, x.Id, WorkflowTaskRoles.SpecializedReviewer, reviewer, now);
        await Assign(module, x.Id, WorkflowTaskRoles.SpecializedApprover, approver, now);
        db.Add(
            Audit(
                x,
                "RecordCreated",
                now,
                new
                {
                    module,
                    number,
                    x.TypeCode,
                    x.SubjectCode,
                    x.ScopeCode,
                }
            )
        );
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (await GetDetailsAsync(module, x.Id, ct))!;
    }

    public async Task<SpecializedDetailsResponse?> TransitionAsync(
        string module,
        Guid id,
        TransitionSpecializedRecordRequest r,
        CancellationToken ct
    )
    {
        ValidateModule(module);
        var x = await db.SpecializedRecords.SingleOrDefaultAsync(
            x => x.Id == id && x.ModuleCode == module,
            ct
        );
        if (x is null)
            return null;
        await EnsureActor(module, id, Role(r.Transition), ct);
        var now = time.GetUtcNow();
        var qr = await db.QualityRecords.SingleAsync(q => q.Id == x.QualityRecordId, ct);
        string eventType;
        switch (r.Transition)
        {
            case "submit":
                x.Submit(r.ExpectedVersion, now);
                await Complete(id, WorkflowTaskRoles.SpecializedOwner, now, ct);
                eventType = "SubmittedForReview";
                break;
            case "review":
                await signatures.AuthenticateAsync(
                    r.SignaturePassword,
                    r.SignatureMeaningAccepted,
                    ct
                );
                x.Review(r.ExpectedVersion, now);
                db.Add(Signature(x, Meaning(module, "review"), now, r.Note, "review"));
                await Complete(id, WorkflowTaskRoles.SpecializedReviewer, now, ct);
                eventType = "Reviewed";
                break;
            case "approve":
                await signatures.AuthenticateAsync(
                    r.SignaturePassword,
                    r.SignatureMeaningAccepted,
                    ct
                );
                x.Approve(r.ExpectedVersion, now);
                db.Add(Signature(x, Meaning(module, "approve"), now, r.Note, "approve"));
                eventType = "Approved";
                break;
            case "close":
                await signatures.AuthenticateAsync(
                    r.SignaturePassword,
                    r.SignatureMeaningAccepted,
                    ct
                );
                x.Close(r.ExpectedVersion, now);
                qr.Close(now, false);
                db.Add(Signature(x, Meaning(module, "close"), now, r.Note, "close"));
                await CompleteAll(id, now, ct);
                eventType = "Closed";
                break;
            case "cancel":
                await signatures.AuthenticateAsync(
                    r.SignaturePassword,
                    r.SignatureMeaningAccepted,
                    ct
                );
                x.Cancel(r.ExpectedVersion, now);
                qr.Close(now, false);
                db.Add(Signature(x, "Kontrollü kayıt iptal onayı", now, r.Note, "cancel"));
                await CompleteAll(id, now, ct);
                eventType = "Cancelled";
                break;
            default:
                throw new ArgumentException("Geçiş geçersizdir.");
        }
        db.Add(Audit(x, eventType, now, new { module, r.Transition }, r.Note));
        await db.SaveChangesAsync(ct);
        return await GetDetailsAsync(module, id, ct);
    }

    private static void ValidateModule(string module)
    {
        if (module is not ("M.13" or "M.14" or "M.15" or "M.16"))
            throw new ArgumentException("Modül kodu geçersizdir.");
    }

    private async Task<(string Code, string Name)> Lookup(
        string module,
        string category,
        string code,
        CancellationToken ct
    )
    {
        var x = await db
            .SpecializedLookupDefinitions.AsNoTracking()
            .Where(x =>
                x.ModuleCode == module
                && x.Category == category
                && x.Code == code.Trim()
                && x.IsActive
            )
            .Select(x => new { x.Code, x.Name })
            .SingleOrDefaultAsync(ct);
        return x is null
            ? throw new ArgumentException(
                $"{category} etkin {module} lookup kayıtlarından seçilmelidir."
            )
            : (x.Code, x.Name);
    }

    private async Task<(Guid Id, string Name, Guid? DepartmentId, string? Department)> Resolve(
        Guid id,
        string field,
        CancellationToken ct
    )
    {
        var x = await (
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
        return x is null
            ? throw new ArgumentException($"{field} etkin kullanıcı olmalıdır.")
            : (x.Id, x.DisplayName, x.DepartmentId, x.Department);
    }

    private async Task Assign(
        string module,
        Guid id,
        string role,
        (Guid Id, string Name, Guid? DepartmentId, string? Department) person,
        DateTimeOffset now
    )
    {
        db.Add(
            WorkflowTaskAssignment.Create(
                "SpecializedRecord",
                id,
                role,
                person.Id,
                person.DepartmentId,
                now,
                assignedUserNameSnapshot: person.Name,
                assignedDepartmentNameSnapshot: person.Department
            )
        );
        db.Add(
            UserNotification.Create(
                person.Id,
                module,
                $"Yeni {module} görevi",
                $"{role} görevi size atandı.",
                $"/modules/{module.Replace(".", "").ToLowerInvariant()}?open={id}",
                now
            )
        );
        await Task.CompletedTask;
    }

    private async Task EnsureActor(string module, Guid id, string role, CancellationToken ct)
    {
        if (!await CanAct(module, id, role, ct))
            throw new QmsForbiddenException(
                "Bu görev size veya etkin delegasyonla size atanmamış."
            );
    }

    private async Task<bool> CanAct(string module, Guid id, string role, CancellationToken ct)
    {
        var ids = await db
            .WorkflowTaskAssignments.AsNoTracking()
            .Where(t =>
                t.AggregateType == "SpecializedRecord"
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
                        && (d.Scope == module || d.Scope == "ALL"),
                    ct
                );
    }

    private async Task Complete(Guid id, string role, DateTimeOffset now, CancellationToken ct)
    {
        foreach (
            var t in await db
                .WorkflowTaskAssignments.Where(t =>
                    t.AggregateType == "SpecializedRecord"
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
                    t.AggregateType == "SpecializedRecord"
                    && t.AggregateId == id
                    && t.Status == WorkflowTaskStatus.Active
                )
                .ToListAsync(ct)
        )
            t.Complete(now);
    }

    private static IReadOnlyList<SpecializedTransitionResponse> Transitions(
        SpecializedRecordStatus s
    ) =>
        s switch
        {
            SpecializedRecordStatus.Draft => [new("submit", "İncelemeye gönder")],
            SpecializedRecordStatus.InReview => [new("review", "İncelemeyi tamamla", true)],
            SpecializedRecordStatus.Reviewed => [new("approve", "Kalite onayı ver", true)],
            SpecializedRecordStatus.Approved => [new("close", "Kontrollü kapat / yayınla", true)],
            _ => [],
        };

    private static string Role(string transition) =>
        transition switch
        {
            "submit" => WorkflowTaskRoles.SpecializedOwner,
            "review" => WorkflowTaskRoles.SpecializedReviewer,
            _ => WorkflowTaskRoles.SpecializedApprover,
        };

    private static string Meaning(string module, string operation) =>
        (module, operation) switch
        {
            ("M.13", "review") => "Artwork metin ve proof inceleme onayı",
            ("M.13", "approve") => "Artwork ruhsat ve kalite onayı",
            ("M.13", _) => "Artwork kontrollü yayın onayı",
            ("M.14", "review") => "OOS laboratuvar araştırması inceleme onayı",
            ("M.14", "approve") => "OOS bilimsel değerlendirme onayı",
            ("M.14", _) => "OOS nihai dispozisyon onayı",
            ("M.15", "review") => "Farmakovijilans tıbbi değerlendirme onayı",
            ("M.15", "approve") => "Farmakovijilans raporlanabilirlik onayı",
            ("M.15", _) => "Farmakovijilans vaka kapanış onayı",
            ("M.16", "review") => "Tedarikçi puanlama inceleme onayı",
            ("M.16", "approve") => "Tedarikçi nitelendirme kararı onayı",
            _ => "Tedarikçi değerlendirme kapanış onayı",
        };

    private ElectronicSignature Signature(
        SpecializedRecord x,
        string meaning,
        DateTimeOffset now,
        string? note,
        string operation
    ) =>
        signatures.CreateInternal(
            x.QualityRecordId,
            "SpecializedRecord",
            x.Id,
            x.Version,
            operation,
            meaning,
            new
            {
                record = x,
                structuredData = x.StructuredDataJson,
                note,
            },
            now,
            note
        );

    private AuditEvent Audit(
        SpecializedRecord x,
        string type,
        DateTimeOffset now,
        object payload,
        string? reason = null
    ) => Event("SpecializedRecord", x.Id, x.Version, type, now, payload, reason);

    private AuditEvent Event(
        string aggregate,
        Guid id,
        long version,
        string type,
        DateTimeOffset now,
        object payload,
        string? reason = null
    ) =>
        AuditEvent.Create(
            aggregate,
            id,
            version,
            type,
            user.Id,
            user.DisplayName,
            now,
            Guid.CreateVersion7().ToString(),
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
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.Transaction = tx.GetDbTransaction();
        command.CommandText =
            "INSERT INTO core.record_number_sequence (\"RecordType\",\"CalendarYear\",\"LastValue\") VALUES (@type,@year,1) ON CONFLICT (\"RecordType\",\"CalendarYear\") DO UPDATE SET \"LastValue\"=core.record_number_sequence.\"LastValue\"+1 RETURNING \"LastValue\";";
        Add(command, "type", type);
        Add(command, "year", year);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }

    private static SpecializedLookupResponse Map(SpecializedLookupDefinition x) =>
        new(x.Id, x.ModuleCode, x.Category, x.Code, x.Name, x.SortOrder, x.IsActive);
}
