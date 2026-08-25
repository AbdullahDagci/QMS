using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.Documents;
using Qms.Application.Security;
using Qms.Contracts.Common;
using Qms.Contracts.Documents;
using Qms.Domain.AuditTrail;
using Qms.Domain.Documents;
using Qms.Domain.QualityRecords;
using Qms.Domain.Trainings;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Documents;

public sealed class DocumentService(QmsDbContext db, TimeProvider clock, ICurrentUser user) : IDocumentService
{
    private static readonly Guid PrototypeDepartmentId = Guid.Parse("01991f70-6f40-7000-8000-000000000002");

    public async Task<PagedResponse<ControlledDocumentListItemResponse>> SearchAsync(ControlledDocumentSearchRequest request, CancellationToken ct)
    {
        if (request.Page < 1 || request.PageSize is not (10 or 25 or 50 or 100)) throw new ArgumentException("Sayfa ve sayfa boyutu geçersizdir.");
        var query = from document in db.ControlledDocuments.AsNoTracking()
                    join record in db.QualityRecords.AsNoTracking() on document.QualityRecordId equals record.Id
                    join revision in db.DocumentRevisions.AsNoTracking() on document.CurrentRevisionId equals revision.Id
                    join source in db.ChangeControls.AsNoTracking() on document.SourceChangeControlId equals source.Id into sources
                    from source in sources.DefaultIfEmpty()
                    join sourceRecord in db.QualityRecords.AsNoTracking() on source.QualityRecordId equals sourceRecord.Id into sourceRecords
                    from sourceRecord in sourceRecords.DefaultIfEmpty()
                    select new DocumentRow { Document = document, Revision = revision, RecordNumber = record.RecordNumber, SourceRecordNumber = sourceRecord == null ? null : sourceRecord.RecordNumber };
        foreach (var filter in request.Filters ?? []) query = ApplyFilter(query, filter);
        var total = await query.LongCountAsync(ct); query = ApplySort(query, request.SortBy, request.SortDirection);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(x => new ControlledDocumentListItemResponse(
            x.Document.Id, x.RecordNumber, x.Document.SourceChangeControlId, x.SourceRecordNumber, x.Document.DocumentCode, x.Document.Title, x.Document.DocumentType, x.Document.Owner, x.Document.Department, x.Document.Confidentiality,
            x.Revision.MajorVersion + "." + x.Revision.MinorVersion, x.Document.Status.ToString(), x.Document.PlannedEffectiveDateUtc, x.Document.NextReviewDateUtc,
            x.Document.Reviews.Count(r => r.RevisionId == x.Document.CurrentRevisionId && r.Status == DocumentReviewStatus.Pending),
            x.Document.TrainingRequirements.Count(t => t.RevisionId == x.Document.CurrentRevisionId && t.Status == DocumentTrainingStatus.Pending), x.Document.CreatedAtUtc, x.Document.Version)).ToListAsync(ct);
        return new(items, request.Page, request.PageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize));
    }

    public async Task<ControlledDocumentDetailsResponse?> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var item = await (from document in db.ControlledDocuments.AsNoTracking().Include(x => x.Revisions).Include(x => x.Reviews).Include(x => x.TrainingRequirements).Include(x => x.Copies).Include(x => x.ReadReceipts)
                          join qr in db.QualityRecords.AsNoTracking() on document.QualityRecordId equals qr.Id
                          join source in db.ChangeControls.AsNoTracking() on document.SourceChangeControlId equals source.Id into sources from source in sources.DefaultIfEmpty()
                          join sourceRecord in db.QualityRecords.AsNoTracking() on source.QualityRecordId equals sourceRecord.Id into sourceRecords from sourceRecord in sourceRecords.DefaultIfEmpty()
                          where document.Id == id select new { Document = document, RecordNumber = qr.RecordNumber, SourceRecordNumber = sourceRecord == null ? null : sourceRecord.RecordNumber }).SingleOrDefaultAsync(ct);
        if (item is null) return null;
        var d = item.Document; var current = d.Revisions.Single(x => x.Id == d.CurrentRevisionId);
        var auditEvents = await db.AuditEvents.AsNoTracking().Where(x => x.AggregateType == "Document" && x.AggregateId == id).OrderByDescending(x => x.OccurredAtUtc).ToListAsync(ct);
        var responseRecord = new ControlledDocumentResponse(d.Id, d.QualityRecordId, item.RecordNumber, d.SourceChangeControlId, item.SourceRecordNumber, d.DocumentCode, d.Title, d.DocumentType, d.Owner, d.Department, d.Confidentiality, d.ReviewPeriodMonths, d.PlannedEffectiveDateUtc, d.NextReviewDateUtc, d.CurrentRevisionId, current.VersionLabel, d.Status.ToString(), d.WithdrawalReason, d.CreatedAtUtc, d.UpdatedAtUtc, d.ArchivedAtUtc, d.Version);
        return new(responseRecord,
            d.Revisions.OrderByDescending(x => x.MajorVersion).ThenByDescending(x => x.MinorVersion).Select(x => new DocumentRevisionResponse(x.Id, x.VersionLabel, x.Content, x.ChangeSummary, x.PreparedBy, x.Status.ToString(), x.CreatedAtUtc, x.ApprovedAtUtc, x.EffectiveAtUtc, x.Id == d.CurrentRevisionId)).ToList(),
            d.Reviews.OrderByDescending(x => x.RevisionId == d.CurrentRevisionId).ThenBy(x => x.Department).Select(x => new DocumentReviewResponse(x.Id, x.RevisionId, x.Department, x.Reviewer, x.Status.ToString(), x.Comment, x.CompletedAtUtc)).ToList(),
            d.TrainingRequirements.OrderByDescending(x => x.RevisionId == d.CurrentRevisionId).ThenBy(x => x.Position).Select(x => new DocumentTrainingResponse(x.Id, x.RevisionId, x.Position, x.AssignedUser, x.Status.ToString(), x.Evidence, x.CompletedAtUtc)).ToList(),
            d.Copies.OrderByDescending(x => x.IssuedAtUtc).Select(x => new ControlledCopyResponse(x.Id, x.RevisionId, x.CopyNumber, x.Recipient, x.Purpose, x.Status.ToString(), x.IssuedAtUtc, x.DueBackAtUtc, x.ReturnedAtUtc, x.DestroyedAtUtc)).ToList(),
            d.ReadReceipts.OrderByDescending(x => x.AcknowledgedAtUtc).Select(x => new DocumentReadReceiptResponse(x.Id, x.RevisionId, x.UserId, x.UserDisplayName, x.SignatureMeaning, x.AcknowledgedAtUtc)).ToList(),
            auditEvents.Select(x => new DocumentAuditEventResponse(x.Id, x.AggregateVersion, x.EventType, x.ActorDisplayNameSnapshot, x.OccurredAtUtc, x.Reason, x.Payload.RootElement.Clone())).ToList(), Transitions(d.Status));
    }

    public async Task<ControlledDocumentDetailsResponse> CreateAsync(CreateControlledDocumentRequest request, CancellationToken ct)
    {
        if (request.SourceChangeControlId is Guid source && !await db.ChangeControls.AnyAsync(x => x.Id == source, ct)) throw new ArgumentException("Kaynak M.03 değişiklik kontrolü bulunamadı.");
        if (await db.ControlledDocuments.AnyAsync(x => x.DocumentCode == request.DocumentCode.Trim(), ct)) throw new ArgumentException("Doküman kodu daha önce kullanılmış.");
        var now = clock.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); var sequence = await NextRecordNumberAsync(now.Year, tx, ct); var number = $"DOC-{now.Year}-{sequence:000000}";
        var qr = QualityRecord.Create(number, "document", user.Id, user.DepartmentId ?? PrototypeDepartmentId, now);
        var document = ControlledDocument.Create(qr.Id, request.SourceChangeControlId, request.DocumentCode, request.Title, request.DocumentType, request.Owner, request.Department, request.Confidentiality, request.ReviewPeriodMonths, request.PlannedEffectiveDateUtc.ToUniversalTime(), request.Content, request.ChangeSummary, request.ReviewDepartments ?? [], request.TrainingPositions ?? [], now);
        db.QualityRecords.Add(qr); db.ControlledDocuments.Add(document); db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Document", document.Id, WorkflowTaskRoles.DocumentAuthor, user.Id, user.DepartmentId, now, request.PlannedEffectiveDateUtc)); db.AuditEvents.Add(Audit(document, "DocumentCreated", now, new { number, request.DocumentCode, request.SourceChangeControlId }));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return (await GetDetailsAsync(document.Id, ct))!;
    }

    public Task<ControlledDocumentDetailsResponse?> UpdateDraftAsync(Guid id, UpdateDocumentDraftRequest r, CancellationToken ct) { EnsureRole(QmsRoles.DocumentController, QmsRoles.QualityAssurance); return Mutate(id, ct, (d, now) => { d.UpdateDraft(r.ExpectedVersion, r.Content, r.ChangeSummary, now); return ("DocumentDraftUpdated", (object)new { d.CurrentRevision.VersionLabel }, r.ChangeSummary); }); }
    public async Task<ControlledDocumentDetailsResponse?> CompleteReviewAsync(Guid id, Guid reviewId, CompleteDocumentReviewRequest r, CancellationToken ct) { await EnsureActorAsync(id, $"DocumentReview:{reviewId}", ct); var result = await Mutate(id, ct, (d, now) => { d.CompleteReview(r.ExpectedVersion, reviewId, r.Approved, r.Comment, now); return (r.Approved ? "DocumentReviewApproved" : "DocumentChangesRequested", (object)new { reviewId, r.Approved }, r.Comment); }); if (result is not null) { await CompleteTasksAsync(id, $"DocumentReview:{reviewId}", clock.GetUtcNow(), ct); await db.SaveChangesAsync(ct); } return result; }
    public async Task<ControlledDocumentDetailsResponse?> CompleteTrainingAsync(Guid id, Guid requirementId, CompleteDocumentTrainingRequest r, CancellationToken ct) { EnsureRole(QmsRoles.TrainingCoordinator, QmsRoles.QualityAssurance); if (await db.TrainingAssignments.AnyAsync(x => x.DocumentTrainingRequirementId == requirementId, ct)) throw new InvalidOperationException("Bu gereksinim M.05 eğitim kaydına bağlıdır ve yalnız M.05 süreci tamamlanarak kapatılabilir."); return await Mutate(id, ct, (d, now) => { d.CompleteTraining(r.ExpectedVersion, requirementId, r.Evidence, now); return ("DocumentTrainingCompleted", (object)new { requirementId }, r.Evidence); }); }
    public Task<ControlledDocumentDetailsResponse?> StartRevisionAsync(Guid id, StartDocumentRevisionRequest r, CancellationToken ct) { EnsureRole(QmsRoles.DocumentController, QmsRoles.QualityAssurance); return Mutate(id, ct, (d, now) => { d.StartRevision(r.ExpectedVersion, r.Major, r.ChangeSummary, r.ReviewDepartments ?? [], r.TrainingPositions ?? [], now); db.DocumentRevisions.Add(d.CurrentRevision); db.DocumentReviews.AddRange(d.Reviews.Where(x => x.RevisionId == d.CurrentRevisionId)); db.DocumentTrainingRequirements.AddRange(d.TrainingRequirements.Where(x => x.RevisionId == d.CurrentRevisionId)); return ("DocumentRevisionStarted", (object)new { r.Major, d.CurrentRevision.VersionLabel }, r.ChangeSummary); }); }
    public Task<ControlledDocumentDetailsResponse?> IssueCopyAsync(Guid id, IssueControlledCopyRequest r, CancellationToken ct) => Mutate(id, ct, (d, now) => { var copy = d.IssueCopy(r.ExpectedVersion, r.CopyNumber, r.Recipient, r.Purpose, r.DueBackAtUtc?.ToUniversalTime(), now); db.ControlledDocumentCopies.Add(copy); return ("ControlledCopyIssued", (object)new { copy.Id, copy.CopyNumber, copy.Recipient }, r.Purpose); });
    public Task<ControlledDocumentDetailsResponse?> CloseCopyAsync(Guid id, Guid copyId, CloseControlledCopyRequest r, CancellationToken ct) => Mutate(id, ct, (d, now) => { d.CloseCopy(r.ExpectedVersion, copyId, r.Destroyed, now); return (r.Destroyed ? "ControlledCopyDestroyed" : "ControlledCopyReturned", (object)new { copyId }, (string?)null); });
    public Task<ControlledDocumentDetailsResponse?> AcknowledgeReadAsync(Guid id, AcknowledgeDocumentReadRequest r, CancellationToken ct) => Mutate(id, ct, (d, now) => { var receipt = d.Acknowledge(r.ExpectedVersion, user.Id, user.DisplayName, r.SignatureMeaning, now); db.DocumentReadReceipts.Add(receipt); return ("DocumentReadAcknowledged", (object)new { receipt.Id, receipt.RevisionId }, r.SignatureMeaning); });

    public async Task<ControlledDocumentDetailsResponse?> TransitionAsync(Guid id, TransitionDocumentRequest request, CancellationToken ct)
    {
        var document = await LoadAsync(id, ct); if (document is null) return null; var qr = await db.QualityRecords.SingleAsync(x => x.Id == document.QualityRecordId, ct); var transition = request.Transition.Trim().ToLowerInvariant(); var now = clock.GetUtcNow(); var from = document.Status;
        if (transition is "start-writing" or "submit-review" or "submit-approval" or "request-revision" or "start-periodic-review" or "complete-periodic-review" or "withdraw" or "archive") EnsureRole(QmsRoles.DocumentController, QmsRoles.QualityAssurance);
        if (transition == "release") EnsureRole(QmsRoles.DocumentController, QmsRoles.QualityAssurance);
        if (transition is "approve" or "release" or "withdraw" or "archive" && qr.CreatedByUserId == user.Id && !user.IsInRole(QmsRoles.Administrator)) throw new QmsForbiddenException("Görev ayrılığı kuralı: Dokümanı hazırlayan kullanıcı aynı dokümanın onay veya yürürlük kararını veremez.");
        await using var tx = await db.Database.BeginTransactionAsync(ct); document.Transition(request.ExpectedVersion, transition, request.Note, request.RequiresRevision, now);
        switch (transition)
        {
            case "start-writing": qr.Submit(now); break;
            case "submit-review": foreach (var review in document.Reviews.Where(x => x.RevisionId == document.CurrentRevisionId)) await AssignRoleAsync(id, $"DocumentReview:{review.Id}", review.Department == "Kalite Güvence" ? QmsRoles.QualityAssurance : review.Department == "Ruhsatlandırma" ? QmsRoles.RegulatoryAffairs : QmsRoles.DepartmentManager, qr.CreatedByUserId, now, document.PlannedEffectiveDateUtc, ct); break;
            case "submit-approval": await AssignRoleAsync(id, WorkflowTaskRoles.DocumentApprover, QmsRoles.Approver, qr.CreatedByUserId, now, document.PlannedEffectiveDateUtc, ct); break;
            case "approve": await EnsureActorAsync(id, WorkflowTaskRoles.DocumentApprover, ct); await CompleteTasksAsync(id, WorkflowTaskRoles.DocumentApprover, now, ct); if (document.Status == ControlledDocumentStatus.TrainingWaiting) { await CreateTrainingAssignmentsAsync(document, now, tx, ct); await AssignRoleAsync(id, WorkflowTaskRoles.TrainingCoordinator, QmsRoles.TrainingCoordinator, null, now, document.PlannedEffectiveDateUtc, ct); } break;
            case "release": await CompleteTasksAsync(id, WorkflowTaskRoles.TrainingCoordinator, now, ct); break;
            case "archive": qr.Close(now, false); break;
        }
        db.AuditEvents.Add(Audit(document, "DocumentStatusChanged", now, new { from = from.ToString(), to = document.Status.ToString(), transition }, request.Note)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct);
    }

    private async Task<ControlledDocumentDetailsResponse?> Mutate(Guid id, CancellationToken ct, Func<ControlledDocument, DateTimeOffset, (string Type, object Payload, string? Reason)> action)
    { var document = await LoadAsync(id, ct); if (document is null) return null; var now = clock.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); var evt = action(document, now); db.AuditEvents.Add(Audit(document, evt.Type, now, evt.Payload, evt.Reason)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await GetDetailsAsync(id, ct); }
    private Task<ControlledDocument?> LoadAsync(Guid id, CancellationToken ct) => db.ControlledDocuments.Include(x => x.Revisions).Include(x => x.Reviews).Include(x => x.TrainingRequirements).Include(x => x.Copies).Include(x => x.ReadReceipts).SingleOrDefaultAsync(x => x.Id == id, ct);
    private AuditEvent Audit(ControlledDocument d, string type, DateTimeOffset now, object payload, string? reason = null) => AuditEvent.Create("Document", d.Id, d.Version, type, user.Id, user.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(payload), reason);

    private static IReadOnlyList<DocumentTransitionResponse> Transitions(ControlledDocumentStatus s) => s switch { ControlledDocumentStatus.Draft => [new("start-writing", "Yazımı başlat", false)], ControlledDocumentStatus.Writing => [new("submit-review", "İncelemeye gönder", false)], ControlledDocumentStatus.Review => [new("submit-approval", "Onaya gönder", false)], ControlledDocumentStatus.Approval => [new("approve", "Dokümanı onayla", false)], ControlledDocumentStatus.Approved or ControlledDocumentStatus.TrainingWaiting => [new("release", "Yürürlüğe al", false)], ControlledDocumentStatus.Effective => [new("request-revision", "Revizyon talep et", false), new("start-periodic-review", "Periyodik gözden geçirme", false), new("withdraw", "Yürürlükten kaldır", true)], ControlledDocumentStatus.RevisionPending => [], ControlledDocumentStatus.PeriodicReview => [new("complete-periodic-review", "Gözden geçirmeyi tamamla", true)], ControlledDocumentStatus.Withdrawn => [new("archive", "Arşivle", false)], _ => [] };
    private static IQueryable<DocumentRow> ApplyFilter(IQueryable<DocumentRow> q, ColumnFilterRequest f) { var field = f.Field.Trim().ToLowerInvariant(); var op = f.Operator.Trim().ToLowerInvariant(); var v = f.Value?.Trim() ?? ""; return field switch { "recordnumber" => Text(q, op, v, 0), "sourcerecordnumber" => Text(q, op, v, 1), "documentcode" => Text(q, op, v, 2), "title" => Text(q, op, v, 3), "owner" => Text(q, op, v, 4), "department" => op == "equals" ? q.Where(x => x.Document.Department == v) : q.Where(x => x.Document.Department.ToLower().Contains(v.ToLower())), "documenttype" => q.Where(x => x.Document.DocumentType == v), "confidentiality" => q.Where(x => x.Document.Confidentiality == v), "status" => Enum.TryParse<ControlledDocumentStatus>(v, true, out var status) ? q.Where(x => x.Document.Status == status) : throw new ArgumentException("Doküman durumu geçersizdir."), "plannedeffectivedateutc" => Date(q, op, f.Value, f.ValueTo, 0), "nextreviewdateutc" => Date(q, op, f.Value, f.ValueTo, 1), "createdatutc" => Date(q, op, f.Value, f.ValueTo, 2), _ => throw new ArgumentException($"Filtrelenmesine izin verilmeyen kolon: {f.Field}") }; }
    private static IQueryable<DocumentRow> Text(IQueryable<DocumentRow> q, string op, string v, int f) => f switch { 0 => op == "equals" ? q.Where(x => x.RecordNumber == v) : q.Where(x => x.RecordNumber.ToLower().Contains(v.ToLower())), 1 => op == "equals" ? q.Where(x => x.SourceRecordNumber == v) : q.Where(x => x.SourceRecordNumber != null && x.SourceRecordNumber.ToLower().Contains(v.ToLower())), 2 => op == "equals" ? q.Where(x => x.Document.DocumentCode == v) : q.Where(x => x.Document.DocumentCode.ToLower().Contains(v.ToLower())), 3 => op == "equals" ? q.Where(x => x.Document.Title == v) : q.Where(x => x.Document.Title.ToLower().Contains(v.ToLower())), _ => op == "equals" ? q.Where(x => x.Document.Owner == v) : q.Where(x => x.Document.Owner.ToLower().Contains(v.ToLower())) };
    private static IQueryable<DocumentRow> Date(IQueryable<DocumentRow> q, string op, string? a, string? b, int f) { var from = DateTimeOffset.Parse(a ?? "", CultureInfo.InvariantCulture).ToUniversalTime(); var to = string.IsNullOrWhiteSpace(b) ? from.AddDays(1) : DateTimeOffset.Parse(b, CultureInfo.InvariantCulture).ToUniversalTime(); return (f, op) switch { (0, "before") => q.Where(x => x.Document.PlannedEffectiveDateUtc < from), (0, "after") => q.Where(x => x.Document.PlannedEffectiveDateUtc > from), (0, _) => q.Where(x => x.Document.PlannedEffectiveDateUtc >= from && x.Document.PlannedEffectiveDateUtc < to), (1, "before") => q.Where(x => x.Document.NextReviewDateUtc < from), (1, "after") => q.Where(x => x.Document.NextReviewDateUtc > from), (1, _) => q.Where(x => x.Document.NextReviewDateUtc >= from && x.Document.NextReviewDateUtc < to), (2, "before") => q.Where(x => x.Document.CreatedAtUtc < from), (2, "after") => q.Where(x => x.Document.CreatedAtUtc > from), _ => q.Where(x => x.Document.CreatedAtUtc >= from && x.Document.CreatedAtUtc < to) }; }
    private static IQueryable<DocumentRow> ApplySort(IQueryable<DocumentRow> q, string f, string direction) { var d = direction.Equals("desc", StringComparison.OrdinalIgnoreCase); return f.Trim().ToLowerInvariant() switch { "recordnumber" => d ? q.OrderByDescending(x => x.RecordNumber) : q.OrderBy(x => x.RecordNumber), "documentcode" => d ? q.OrderByDescending(x => x.Document.DocumentCode) : q.OrderBy(x => x.Document.DocumentCode), "title" => d ? q.OrderByDescending(x => x.Document.Title) : q.OrderBy(x => x.Document.Title), "owner" => d ? q.OrderByDescending(x => x.Document.Owner) : q.OrderBy(x => x.Document.Owner), "status" => d ? q.OrderByDescending(x => x.Document.Status) : q.OrderBy(x => x.Document.Status), "plannedeffectivedateutc" => d ? q.OrderByDescending(x => x.Document.PlannedEffectiveDateUtc) : q.OrderBy(x => x.Document.PlannedEffectiveDateUtc), _ => d ? q.OrderByDescending(x => x.Document.CreatedAtUtc) : q.OrderBy(x => x.Document.CreatedAtUtc) }; }
    private async Task EnsureActorAsync(Guid id, string role, CancellationToken ct) { if (user.IsInRole(QmsRoles.Administrator)) return; var tasks = await db.WorkflowTaskAssignments.AsNoTracking().Where(x => x.AggregateType == "Document" && x.AggregateId == id && x.TaskRole == role && x.Status == WorkflowTaskStatus.Active).ToListAsync(ct); if (tasks.Any(x => x.AssignedUserId == user.Id)) return; var fallback = role.StartsWith("DocumentReview:") ? user.IsInRole(QmsRoles.QualityAssurance) || user.IsInRole(QmsRoles.DepartmentManager) : role == WorkflowTaskRoles.DocumentApprover && (user.IsInRole(QmsRoles.Approver) || user.IsInRole(QmsRoles.QualityAssurance)); if (!fallback) throw new QmsForbiddenException("Bu doküman görevi size veya rolünüze atanmamış."); }
    private void EnsureRole(params string[] roles) { if (user.IsInRole(QmsRoles.Administrator) || roles.Any(user.IsInRole)) return; throw new QmsForbiddenException("Bu doküman işlemi için gerekli sistem rolü sizde bulunmuyor."); }
    private async Task CompleteTasksAsync(Guid id, string role, DateTimeOffset now, CancellationToken ct) { var tasks = await db.WorkflowTaskAssignments.Where(x => x.AggregateType == "Document" && x.AggregateId == id && x.TaskRole == role && x.Status == WorkflowTaskStatus.Active).ToListAsync(ct); foreach (var task in tasks) task.Complete(now); }
    private async Task AssignRoleAsync(Guid id, string taskRole, string roleName, Guid? excluded, DateTimeOffset now, DateTimeOffset? due, CancellationToken ct) { var userId = await (from link in db.UserRoles.AsNoTracking() join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id join target in db.Users.AsNoTracking() on link.UserId equals target.Id where role.Name == roleName && target.IsActive && target.Id != excluded orderby target.DisplayName select target.Id).FirstOrDefaultAsync(ct); if (userId == Guid.Empty) return; var department = await db.Users.Where(x => x.Id == userId).Select(x => x.DepartmentId).SingleAsync(ct); db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Document", id, taskRole, userId, department, now, due)); }
    private async Task CreateTrainingAssignmentsAsync(ControlledDocument document, DateTimeOffset now, IDbContextTransaction tx, CancellationToken ct)
    {
        var candidates = from target in db.Users.AsNoTracking()
                         join link in db.UserPositions.AsNoTracking().Where(x => x.EndsAtUtc == null && x.IsPrimary) on target.Id equals link.UserId
                         join position in db.Positions.AsNoTracking() on link.PositionId equals position.Id
                         where target.IsActive
                         orderby target.DisplayName
                         select new TrainingEmployee(target.Id, target.DisplayName, position.Name, target.DepartmentId);
        var candidateList = await candidates.ToListAsync(ct);
        foreach (var requirement in document.TrainingRequirements.Where(x => x.RevisionId == document.CurrentRevisionId && x.Status == DocumentTrainingStatus.Pending))
        {
            if (await db.TrainingAssignments.AnyAsync(x => x.DocumentTrainingRequirementId == requirement.Id, ct)) continue;
            var employees = candidateList.Where(x => x.Position.Equals(requirement.Position, StringComparison.OrdinalIgnoreCase)).ToList();
            if (employees.Count == 0) employees.Add(candidateList.FirstOrDefault() ?? throw new InvalidOperationException("Eğitim gereksinimine atanabilecek etkin çalışan bulunamadı."));
            foreach (var employee in employees)
            {
                var sequence = await NextRecordNumberAsync(now.Year, tx, ct, "training");
                var number = $"EGT-{now.Year}-{sequence:000000}";
                var record = QualityRecord.Create(number, "training", user.Id, employee.DepartmentId ?? PrototypeDepartmentId, now); record.Submit(now);
                var assignment = TrainingAssignment.Create(record.Id, null, document.Id, document.CurrentRevisionId, requirement.Id, employee.Id, employee.DisplayName, requirement.Position, document.DocumentCode, $"{document.Title} · Sürüm {document.CurrentRevision.VersionLabel}", TrainingAssessmentMode.ReadAndAcknowledge, TrainingDeliveryMethod.Electronic, 100, document.ReviewPeriodMonths, 3, true, document.PlannedEffectiveDateUtc, null, null, true, now);
                db.QualityRecords.Add(record); db.TrainingAssignments.Add(assignment); db.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("Training", assignment.Id, WorkflowTaskRoles.Learner, employee.Id, employee.DepartmentId, now, document.PlannedEffectiveDateUtc));
                db.AuditEvents.Add(AuditEvent.Create("Training", assignment.Id, assignment.Version, "TrainingAssignmentCreatedFromDocument", user.Id, user.DisplayName, now, Guid.CreateVersion7().ToString(), JsonSerializer.SerializeToDocument(new { number, documentId = document.Id, documentRevisionId = document.CurrentRevisionId, requirementId = requirement.Id, employeeId = employee.Id })));
            }
        }
    }
    private async Task<long> NextRecordNumberAsync(int year, IDbContextTransaction tx, CancellationToken ct, string recordType = "document") { var connection = db.Database.GetDbConnection(); if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.Transaction = tx.GetDbTransaction(); command.CommandText = "INSERT INTO core.record_number_sequence (\"RecordType\", \"CalendarYear\", \"LastValue\") VALUES (@recordType, @calendarYear, 1) ON CONFLICT (\"RecordType\", \"CalendarYear\") DO UPDATE SET \"LastValue\" = core.record_number_sequence.\"LastValue\" + 1 RETURNING \"LastValue\";"; Add(command, "recordType", recordType); Add(command, "calendarYear", year); return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture); }
    private static void Add(DbCommand command, string name, object value) { var p = command.CreateParameter(); p.ParameterName = name; p.Value = value; command.Parameters.Add(p); }
    private sealed class DocumentRow { public required ControlledDocument Document { get; init; } public required DocumentRevision Revision { get; init; } public required string RecordNumber { get; init; } public string? SourceRecordNumber { get; init; } }
    private sealed record TrainingEmployee(Guid Id, string DisplayName, string Position, Guid? DepartmentId);
}
