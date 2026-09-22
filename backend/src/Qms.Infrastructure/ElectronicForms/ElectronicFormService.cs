using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qms.Application.ElectronicForms;
using Qms.Application.ElectronicSignatures;
using Qms.Application.Security;
using Qms.Contracts.ElectronicForms;
using Qms.Domain.AuditTrail;
using Qms.Domain.ElectronicForms;
using Qms.Domain.QualityRecords;
using Qms.Infrastructure.Integrity;
using Qms.Infrastructure.Persistence;
using Qms.Infrastructure.Security;

namespace Qms.Infrastructure.ElectronicForms;

public sealed class ElectronicFormService(
    QmsDbContext db,
    ICurrentUser currentUser,
    IElectronicSignatureService signatures,
    ElectronicFormSchemaValidator schemaValidator,
    TimeProvider timeProvider) : IElectronicFormService
{
    public async Task<IReadOnlyList<ElectronicFormListItemResponse>> ListDefinitionsAsync(
        CancellationToken cancellationToken)
    {
        var query = db.ElectronicFormDefinitions.AsNoTracking();
        if (!CanManage()) query = query.Where(item => item.IsActive && item.CurrentPublishedVersionId != null);
        var definitions = await query.OrderBy(item => item.Category).ThenBy(item => item.Name).ToListAsync(cancellationToken);
        if (definitions.Count == 0) return [];
        var ids = definitions.Select(item => item.Id).ToArray();
        var versions = await db.ElectronicFormVersions.AsNoTracking()
            .Where(item => ids.Contains(item.FormDefinitionId)).ToListAsync(cancellationToken);
        return definitions.Select(item => MapList(item, versions.Where(version => version.FormDefinitionId == item.Id))).ToList();
    }

    public async Task<ElectronicFormDetailsResponse?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await db.ElectronicFormDefinitions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null || !CanManage() && (!definition.IsActive || definition.CurrentPublishedVersionId is null)) return null;
        var versionsQuery = db.ElectronicFormVersions.AsNoTracking().Where(item => item.FormDefinitionId == id);
        if (!CanManage()) versionsQuery = versionsQuery.Where(item => item.Id == definition.CurrentPublishedVersionId);
        var versions = await versionsQuery.OrderByDescending(item => item.VersionNumber).ToListAsync(cancellationToken);
        var versionIds = versions.Select(item => item.Id).ToArray();
        var templates = await db.ElectronicFormOutputTemplates.AsNoTracking()
            .Where(item => versionIds.Contains(item.FormVersionId)).ToListAsync(cancellationToken);
        return MapDetails(definition, versions, templates);
    }

    public async Task<ElectronicFormDetailsResponse> CreateDefinitionAsync(
        CreateElectronicFormRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCanManage();
        var schema = schemaValidator.ParseAndValidateSchema(request.Schema);
        _ = schema;
        var output = ValidateOutputTemplate(request.OutputTemplate ?? DefaultOutputTemplate());
        var kind = ParseKind(request.Kind);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.ElectronicFormDefinitions.AnyAsync(item => item.Code == code, cancellationToken))
            throw new ArgumentException("Bu form kodu zaten kullanılıyor.");
        var departmentId = currentUser.DepartmentId ?? throw new InvalidOperationException("Form hazırlayan kullanıcının bölümü zorunludur.");
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var sequence = await NextNumberAsync("form-definition", now.Year, transaction, cancellationToken);
        var qualityRecord = QualityRecord.Create($"FRM-{now.Year}-{sequence:000000}", "form-definition", currentUser.Id,
            departmentId, now, JsonSerializer.SerializeToDocument(new { formCode = code }));
        var definition = ElectronicFormDefinition.Create(code, request.Name, request.Description, request.Category,
            kind, qualityRecord.Id, currentUser.Id, currentUser.DisplayName, now);
        var version = ElectronicFormVersion.CreateDraft(definition.Id, 1, request.Schema, request.ChangeSummary,
            NormalizeWorkflow(request.WorkflowType), currentUser.Id, currentUser.DisplayName, now);
        var template = ElectronicFormOutputTemplate.CreateDefault(version.Id, output, now);
        db.QualityRecords.Add(qualityRecord);
        db.ElectronicFormDefinitions.Add(definition);
        db.ElectronicFormVersions.Add(version);
        db.ElectronicFormOutputTemplates.Add(template);
        db.AuditEvents.Add(AuditDefinition(definition, version.Version, "ElectronicFormCreated", now,
            new { definition.Code, version.VersionNumber, schemaHash = Hash(request.Schema), outputTemplateId = template.Id }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetDefinitionAsync(definition.Id, cancellationToken))!;
    }

    public async Task<ElectronicFormDetailsResponse?> UpdateDraftAsync(
        Guid id,
        UpdateElectronicFormDraftRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCanManage();
        schemaValidator.ParseAndValidateSchema(request.Schema);
        var output = ValidateOutputTemplate(request.OutputTemplate ?? DefaultOutputTemplate());
        var definition = await db.ElectronicFormDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null) return null;
        var version = await db.ElectronicFormVersions.Where(item => item.FormDefinitionId == id)
            .OrderByDescending(item => item.VersionNumber).FirstAsync(cancellationToken);
        EnsureDraftOwner(version);
        var template = await db.ElectronicFormOutputTemplates.SingleAsync(item => item.FormVersionId == version.Id, cancellationToken);
        var now = timeProvider.GetUtcNow();
        definition.UpdateMetadata(request.DefinitionExpectedVersion, request.Name, request.Description, request.Category,
            ParseKind(request.Kind), now);
        version.UpdateDraft(request.FormVersionExpectedVersion, request.Schema, request.ChangeSummary,
            NormalizeWorkflow(request.WorkflowType), now);
        template.Update(request.OutputTemplateExpectedVersion, output, now);
        db.AuditEvents.Add(AuditDefinition(definition, version.Version, "ElectronicFormDraftUpdated", now,
            new { version.VersionNumber, schemaHash = Hash(request.Schema), outputHash = Hash(output) }, request.ChangeSummary));
        await db.SaveChangesAsync(cancellationToken);
        return await GetDefinitionAsync(id, cancellationToken);
    }

    public async Task<ElectronicFormDetailsResponse?> SubmitForReviewAsync(
        Guid id,
        TransitionElectronicFormVersionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCanManage();
        var definition = await db.ElectronicFormDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null) return null;
        var version = await db.ElectronicFormVersions.Where(item => item.FormDefinitionId == id)
            .OrderByDescending(item => item.VersionNumber).FirstAsync(cancellationToken);
        EnsureDraftOwner(version);
        schemaValidator.ParseAndValidateSchema(version.Schema);
        var template = await db.ElectronicFormOutputTemplates.SingleAsync(item => item.FormVersionId == version.Id, cancellationToken);
        ValidateOutputTemplate(template.Configuration);
        var now = timeProvider.GetUtcNow();
        version.SubmitForReview(request.ExpectedVersion, now);
        db.AuditEvents.Add(AuditDefinition(definition, version.Version, "ElectronicFormSubmittedForReview", now,
            new { version.Id, version.VersionNumber }, request.Comment));
        await db.SaveChangesAsync(cancellationToken);
        return await GetDefinitionAsync(id, cancellationToken);
    }

    public async Task<ElectronicFormDetailsResponse?> PublishAsync(
        Guid id,
        TransitionElectronicFormVersionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCanPublish();
        var definition = await db.ElectronicFormDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null) return null;
        var version = await db.ElectronicFormVersions.Where(item => item.FormDefinitionId == id)
            .OrderByDescending(item => item.VersionNumber).FirstAsync(cancellationToken);
        var template = await db.ElectronicFormOutputTemplates.SingleAsync(item => item.FormVersionId == version.Id, cancellationToken);
        schemaValidator.ParseAndValidateSchema(version.Schema);
        ValidateOutputTemplate(template.Configuration);
        await signatures.AuthenticateAsync(request.Password, request.MeaningAccepted, cancellationToken);
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        version.Publish(request.ExpectedVersion, currentUser.Id, currentUser.DisplayName, now);
        template.Publish(now);
        definition.Publish(version.Id, version.VersionNumber, now);
        db.ElectronicSignatures.Add(signatures.CreateInternal(definition.QualityRecordId, "ElectronicFormDefinition",
            definition.Id, version.Version, "publish", "Form sürümünü yayımlama",
            new { definition.Code, definition.Name, formVersionId = version.Id, version.VersionNumber,
                schemaHash = Hash(version.Schema), outputHash = Hash(template.Configuration) }, now, request.Comment));
        db.AuditEvents.Add(AuditDefinition(definition, version.Version, "ElectronicFormPublished", now,
            new { version.Id, version.VersionNumber, outputTemplateId = template.Id }, request.Comment));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDefinitionAsync(id, cancellationToken);
    }

    public async Task<ElectronicFormDetailsResponse?> StartNewVersionAsync(
        Guid id,
        StartElectronicFormVersionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCanManage();
        var definition = await db.ElectronicFormDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (definition is null) return null;
        if (await db.ElectronicFormVersions.AnyAsync(item => item.FormDefinitionId == id
                && item.Status != ElectronicFormVersionStatus.Published, cancellationToken))
            throw new InvalidOperationException("Formun tamamlanmamış bir taslak veya inceleme sürümü zaten var.");
        if (definition.CurrentPublishedVersionId is null) throw new InvalidOperationException("Yeni sürüm için yayımlanmış kaynak sürüm bulunamadı.");
        var source = await db.ElectronicFormVersions.AsNoTracking()
            .SingleAsync(item => item.Id == definition.CurrentPublishedVersionId, cancellationToken);
        var sourceTemplate = await db.ElectronicFormOutputTemplates.AsNoTracking()
            .SingleAsync(item => item.FormVersionId == source.Id, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var next = definition.ReserveNextVersion(request.DefinitionExpectedVersion, now);
        var version = ElectronicFormVersion.CreateDraft(definition.Id, next, source.Schema, request.ChangeSummary,
            source.WorkflowType, currentUser.Id, currentUser.DisplayName, now);
        var template = ElectronicFormOutputTemplate.CreateDefault(version.Id, sourceTemplate.Configuration, now);
        db.ElectronicFormVersions.Add(version);
        db.ElectronicFormOutputTemplates.Add(template);
        db.AuditEvents.Add(AuditDefinition(definition, definition.Version, "ElectronicFormVersionCreated", now,
            new { sourceVersionId = source.Id, newVersionId = version.Id, version.VersionNumber }, request.ChangeSummary));
        await db.SaveChangesAsync(cancellationToken);
        return await GetDefinitionAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<ElectronicFormRecordListItemResponse>> ListRecordsAsync(
        CancellationToken cancellationToken) => await (
            from record in db.ElectronicFormRecords.AsNoTracking()
            join qualityRecord in db.VisibleQualityRecords(currentUser) on record.QualityRecordId equals qualityRecord.Id
            orderby record.UpdatedAtUtc descending
            select new ElectronicFormRecordListItemResponse(record.Id, record.QualityRecordId,
                qualityRecord.RecordNumber, record.FormCodeSnapshot, record.FormNameSnapshot,
                record.FormVersionNumber, record.Status.ToString(), record.CreatedByDisplayName,
                record.CreatedByUserId, record.CreatedAtUtc, record.UpdatedAtUtc, record.Version))
            .Take(500).ToListAsync(cancellationToken);

    public async Task<ElectronicFormRecordDetailsResponse?> GetRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await (
            from record in db.ElectronicFormRecords.AsNoTracking()
            join qualityRecord in db.VisibleQualityRecords(currentUser) on record.QualityRecordId equals qualityRecord.Id
            join version in db.ElectronicFormVersions.AsNoTracking() on record.FormVersionId equals version.Id
            join template in db.ElectronicFormOutputTemplates.AsNoTracking() on record.OutputTemplateId equals template.Id
            where record.Id == id
            select new { record, qualityRecord, version, template }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;
        var audit = await db.AuditEvents.AsNoTracking().Where(item => item.AggregateType == "ElectronicFormRecord" && item.AggregateId == id)
            .OrderBy(item => item.OccurredAtUtc).Select(item => new ElectronicFormRecordAuditResponse(item.EventType,
                item.ActorDisplayNameSnapshot, item.OccurredAtUtc, item.AggregateVersion, item.Reason)).ToListAsync(cancellationToken);
        var recordSignatures = await db.ElectronicSignatures.AsNoTracking()
            .Where(item => item.AggregateType == "ElectronicFormRecord" && item.AggregateId == id)
            .OrderBy(item => item.SignedAtUtc).Select(item => new ElectronicFormRecordSignatureResponse(item.Id,
                item.Meaning, item.SignerDisplayNameSnapshot, item.SignedAtUtc, item.RecordVersion,
                item.ContentHash, item.Comment)).ToListAsync(cancellationToken);
        return new ElectronicFormRecordDetailsResponse(MapRecord(row.record, row.qualityRecord.RecordNumber),
            row.record.FormDefinitionId, row.version.Id, row.template.Id, Clone(row.version.Schema),
            Clone(row.template.Configuration), Clone(row.record.Data), audit, recordSignatures);
    }

    public async Task<ElectronicFormRecordDetailsResponse> CreateRecordAsync(
        CreateElectronicFormRecordRequest request,
        CancellationToken cancellationToken)
    {
        var definition = await db.ElectronicFormDefinitions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == request.FormDefinitionId && item.IsActive && item.CurrentPublishedVersionId != null, cancellationToken)
            ?? throw new KeyNotFoundException("Yayımlanmış elektronik form bulunamadı.");
        var version = await db.ElectronicFormVersions.AsNoTracking()
            .SingleAsync(item => item.Id == definition.CurrentPublishedVersionId, cancellationToken);
        var template = await db.ElectronicFormOutputTemplates.AsNoTracking()
            .SingleAsync(item => item.FormVersionId == version.Id && item.IsPublished, cancellationToken);
        var normalized = schemaValidator.ValidateAndNormalizeData(version.Schema, request.Data, false);
        var departmentId = currentUser.DepartmentId ?? throw new InvalidOperationException("Form kullanıcısının bölümü zorunludur.");
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var sequence = await NextNumberAsync("electronic-form", now.Year, transaction, cancellationToken);
        var qualityRecord = QualityRecord.Create($"EF-{now.Year}-{sequence:000000}", "electronic-form", currentUser.Id,
            departmentId, now, JsonSerializer.SerializeToDocument(new { definition.Id, formVersionId = version.Id, version.VersionNumber }));
        var record = ElectronicFormRecord.Create(qualityRecord.Id, definition.Id, version.Id, template.Id,
            definition.Code, definition.Name, version.VersionNumber, normalized, currentUser.Id, currentUser.DisplayName, now);
        db.QualityRecords.Add(qualityRecord);
        db.ElectronicFormRecords.Add(record);
        db.AuditEvents.Add(AuditRecord(record, "ElectronicFormRecordCreated", now,
            new { qualityRecord.RecordNumber, definition.Code, version.VersionNumber, dataHash = Hash(normalized) }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetRecordAsync(record.Id, cancellationToken))!;
    }

    public async Task<ElectronicFormRecordDetailsResponse?> UpdateRecordDraftAsync(
        Guid id,
        UpdateElectronicFormRecordRequest request,
        CancellationToken cancellationToken)
    {
        var (record, _) = await LoadRecordForMutationAsync(id, cancellationToken);
        if (record is null) return null;
        EnsureRecordOwner(record);
        var version = await db.ElectronicFormVersions.AsNoTracking().SingleAsync(item => item.Id == record.FormVersionId, cancellationToken);
        var normalized = schemaValidator.ValidateAndNormalizeData(version.Schema, request.Data, false);
        var now = timeProvider.GetUtcNow();
        record.UpdateDraft(request.ExpectedVersion, normalized, now);
        db.AuditEvents.Add(AuditRecord(record, "ElectronicFormRecordDraftUpdated", now,
            new { dataHash = Hash(normalized) }));
        await db.SaveChangesAsync(cancellationToken);
        return await GetRecordAsync(id, cancellationToken);
    }

    public async Task<ElectronicFormRecordDetailsResponse?> SubmitRecordAsync(
        Guid id,
        SubmitElectronicFormRecordRequest request,
        CancellationToken cancellationToken)
    {
        var (record, qualityRecord) = await LoadRecordForMutationAsync(id, cancellationToken);
        if (record is null || qualityRecord is null) return null;
        EnsureRecordOwner(record);
        var version = await db.ElectronicFormVersions.AsNoTracking().SingleAsync(item => item.Id == record.FormVersionId, cancellationToken);
        var normalized = schemaValidator.ValidateAndNormalizeData(version.Schema, record.Data, true);
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        record.Submit(request.ExpectedVersion, normalized, now);
        qualityRecord.Submit(now);
        db.AuditEvents.Add(AuditRecord(record, "ElectronicFormRecordSubmitted", now,
            new { dataHash = Hash(normalized), formVersionId = version.Id }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetRecordAsync(id, cancellationToken);
    }

    public async Task<ElectronicFormRecordDetailsResponse?> ApproveRecordAsync(
        Guid id,
        ApproveElectronicFormRecordRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCanApproveRecord();
        var (record, qualityRecord) = await LoadRecordForMutationAsync(id, cancellationToken);
        if (record is null || qualityRecord is null) return null;
        if (record.CreatedByUserId == currentUser.Id) throw new QmsForbiddenException("Form kaydını oluşturan kullanıcı aynı kaydı onaylayamaz.");
        var version = await db.ElectronicFormVersions.AsNoTracking().SingleAsync(item => item.Id == record.FormVersionId, cancellationToken);
        schemaValidator.ValidateAndNormalizeData(version.Schema, record.Data, true);
        await signatures.AuthenticateAsync(request.Password, request.MeaningAccepted, cancellationToken);
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        record.Close(request.ExpectedVersion, now);
        qualityRecord.Close(now, false);
        db.ElectronicSignatures.Add(signatures.CreateInternal(record.QualityRecordId, "ElectronicFormRecord", record.Id,
            record.Version, "approve", "Elektronik form kaydını onaylama",
            new { record.FormDefinitionId, record.FormVersionId, record.FormVersionNumber,
                data = record.Data.RootElement.Clone(), dataHash = Hash(record.Data) }, now, request.Comment));
        db.AuditEvents.Add(AuditRecord(record, "ElectronicFormRecordApproved", now,
            new { dataHash = Hash(record.Data), record.FormVersionNumber }, request.Comment));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetRecordAsync(id, cancellationToken);
    }

    private async Task<(ElectronicFormRecord? Record, QualityRecord? QualityRecord)> LoadRecordForMutationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await db.ElectronicFormRecords.SingleOrDefaultAsync(item => item.Id == id
            && db.VisibleQualityRecords(currentUser).Any(quality => quality.Id == item.QualityRecordId), cancellationToken);
        if (record is null) return (null, null);
        var qualityRecord = await db.QualityRecords.SingleAsync(item => item.Id == record.QualityRecordId, cancellationToken);
        return (record, qualityRecord);
    }

    private async Task<long> NextNumberAsync(string recordType, int year, IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "INSERT INTO core.record_number_sequence (\"RecordType\", \"CalendarYear\", \"LastValue\") VALUES (@recordType, @calendarYear, 1) ON CONFLICT (\"RecordType\",\"CalendarYear\") DO UPDATE SET \"LastValue\"=core.record_number_sequence.\"LastValue\"+1 RETURNING \"LastValue\";";
        AddParameter(command, "recordType", recordType);
        AddParameter(command, "calendarYear", year);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private ElectronicFormDetailsResponse MapDetails(ElectronicFormDefinition definition,
        IReadOnlyList<ElectronicFormVersion> versions, IReadOnlyList<ElectronicFormOutputTemplate> templates)
    {
        var templateMap = templates.ToDictionary(item => item.FormVersionId);
        return new ElectronicFormDetailsResponse(MapList(definition, versions), versions.Select(version =>
        {
            var template = templateMap[version.Id];
            return new ElectronicFormVersionResponse(version.Id, version.VersionNumber, version.Status.ToString(),
                version.EngineSchemaVersion, Clone(version.Schema), version.ChangeSummary, version.WorkflowType,
                version.CreatedByUserId, version.CreatedByDisplayName, version.CreatedAtUtc, version.UpdatedAtUtc,
                version.SubmittedAtUtc, version.PublishedByUserId, version.PublishedByDisplayName,
                version.PublishedAtUtc, version.Version, template.Id, Clone(template.Configuration), template.Version);
        }).ToList());
    }

    private static ElectronicFormListItemResponse MapList(ElectronicFormDefinition definition,
        IEnumerable<ElectronicFormVersion> source)
    {
        var versions = source.ToList();
        var latest = versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();
        var published = versions.SingleOrDefault(item => item.Id == definition.CurrentPublishedVersionId);
        return new ElectronicFormListItemResponse(definition.Id, definition.Code, definition.Name,
            definition.Description, definition.Category, definition.Kind.ToString(), definition.IsActive,
            definition.LatestVersionNumber, definition.CurrentPublishedVersionId, published?.VersionNumber,
            latest?.Status.ToString(), latest?.Id, definition.Version, definition.UpdatedAtUtc);
    }

    private static ElectronicFormRecordListItemResponse MapRecord(ElectronicFormRecord record, string recordNumber) =>
        new(record.Id, record.QualityRecordId, recordNumber, record.FormCodeSnapshot, record.FormNameSnapshot,
            record.FormVersionNumber, record.Status.ToString(), record.CreatedByDisplayName, record.CreatedByUserId,
            record.CreatedAtUtc, record.UpdatedAtUtc, record.Version);

    private AuditEvent AuditDefinition(ElectronicFormDefinition definition, long version, string eventType,
        DateTimeOffset now, object payload, string? reason = null) => AuditEvent.Create("ElectronicFormDefinition",
        definition.Id, version, eventType, currentUser.Id, currentUser.DisplayName, now, AuditCorrelation.Current,
        JsonSerializer.SerializeToDocument(payload), reason);

    private AuditEvent AuditRecord(ElectronicFormRecord record, string eventType, DateTimeOffset now,
        object payload, string? reason = null) => AuditEvent.Create("ElectronicFormRecord", record.Id, record.Version,
        eventType, currentUser.Id, currentUser.DisplayName, now, AuditCorrelation.Current,
        JsonSerializer.SerializeToDocument(payload), reason);

    private void EnsureCanManage()
    {
        if (!CanManage()) throw new QmsForbiddenException("Elektronik form tasarlama yetkiniz bulunmuyor.");
    }

    private bool CanManage() => currentUser.IsInRole(QmsRoles.Administrator)
        || currentUser.IsInRole(QmsRoles.QualityAssurance)
        || currentUser.IsInRole(QmsRoles.DocumentController);

    private void EnsureCanPublish()
    {
        if (!currentUser.IsInRole(QmsRoles.Administrator) && !currentUser.IsInRole(QmsRoles.QualityAssurance))
            throw new QmsForbiddenException("Elektronik form yayımlama yetkiniz bulunmuyor.");
    }

    private void EnsureCanApproveRecord()
    {
        if (!currentUser.IsInRole(QmsRoles.Administrator) && !currentUser.IsInRole(QmsRoles.QualityAssurance))
            throw new QmsForbiddenException("Elektronik form kaydı onaylama yetkiniz bulunmuyor.");
    }

    private void EnsureDraftOwner(ElectronicFormVersion version)
    {
        if (version.CreatedByUserId != currentUser.Id)
            throw new QmsForbiddenException("Taslak form sürümünü yalnız hazırlayan kullanıcı değiştirebilir veya incelemeye gönderebilir.");
    }

    private void EnsureRecordOwner(ElectronicFormRecord record)
    {
        if (record.CreatedByUserId != currentUser.Id)
            throw new QmsForbiddenException("Taslak form kaydını yalnız oluşturan kullanıcı değiştirebilir veya gönderebilir.");
    }

    private static ElectronicFormKind ParseKind(string value) => Enum.TryParse<ElectronicFormKind>(value, true, out var kind)
        ? kind : throw new ArgumentException("Form türü Standard, Logbook, Checklist veya Assessment olmalıdır.");

    private static string NormalizeWorkflow(string value) => value switch
    {
        "ReviewApprove" => value,
        _ => throw new ArgumentException("İlk sürümde yalnız İnceleme ve Onay iş akışı desteklenir."),
    };

    private static JsonDocument ValidateOutputTemplate(JsonDocument value)
    {
        if (value.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Çıktı şablonu geçersizdir.");
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "title", "footerText", "primaryColor", "includeEmptyFields", "includeAuditTrail", "includeSignatures"
        };
        foreach (var property in value.RootElement.EnumerateObject())
        {
            if (!allowed.Contains(property.Name)) throw new ArgumentException($"Desteklenmeyen çıktı ayarı: {property.Name}");
            if (property.Name is "title" or "footerText" or "primaryColor" && property.Value.ValueKind != JsonValueKind.String)
                throw new ArgumentException($"{property.Name} çıktı ayarı metin olmalıdır.");
            if (property.Name.StartsWith("include", StringComparison.Ordinal) && property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new ArgumentException($"{property.Name} çıktı ayarı doğru/yanlış olmalıdır.");
        }
        if (value.RootElement.TryGetProperty("title", out var title) && (title.GetString()?.Length ?? 0) > 200)
            throw new ArgumentException("PDF başlığı 200 karakteri aşamaz.");
        if (value.RootElement.TryGetProperty("footerText", out var footer) && (footer.GetString()?.Length ?? 0) > 500)
            throw new ArgumentException("PDF altbilgisi 500 karakteri aşamaz.");
        var raw = value.RootElement.GetRawText();
        if (Encoding.UTF8.GetByteCount(raw) > 64 * 1024) throw new ArgumentException("Çıktı şablonu 64 KB sınırını aşamaz.");
        if (value.RootElement.TryGetProperty("primaryColor", out var color)
            && !System.Text.RegularExpressions.Regex.IsMatch(color.GetString() ?? string.Empty, "^#[0-9A-Fa-f]{6}$"))
            throw new ArgumentException("Çıktı ana rengi #RRGGBB biçiminde olmalıdır.");
        return JsonDocument.Parse(raw);
    }

    private static JsonDocument DefaultOutputTemplate() => JsonSerializer.SerializeToDocument(new
    {
        title = "",
        footerText = "Kontrollü elektronik kayıt",
        primaryColor = "#0F7773",
        includeEmptyFields = false,
        includeAuditTrail = true,
        includeSignatures = true,
    });

    private static JsonDocument Clone(JsonDocument document) => JsonDocument.Parse(document.RootElement.GetRawText());

    private static string Hash(JsonDocument document) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(document.RootElement.GetRawText())));
}
