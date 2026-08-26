using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qms.Application.Administration;
using Qms.Application.Security;
using Qms.Contracts.Administration;
using Qms.Domain.Notifications;
using Qms.Domain.Organization;
using Qms.Domain.Workflows;
using Qms.Infrastructure.Identity;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Administration;

public sealed class AccessAdministrationService(
    QmsDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    TimeProvider timeProvider
) : IAccessAdministrationService
{
    public async Task<AccessOverviewResponse> GetOverviewAsync(CancellationToken ct)
    {
        var departments = await dbContext
            .Departments.AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(ct);
        var positions = await dbContext
            .Positions.AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(ct);
        var users = await dbContext
            .Users.AsNoTracking()
            .OrderBy(item => item.DisplayName)
            .ToListAsync(ct);
        var userPositions = await dbContext
            .UserPositions.AsNoTracking()
            .Where(item => item.EndsAtUtc == null)
            .ToListAsync(ct);
        var userRoles = await (
            from link in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id
            select new { link.UserId, Role = role.Name! }
        ).ToListAsync(ct);
        var delegations = await dbContext
            .Delegations.AsNoTracking()
            .OrderByDescending(item => item.StartsAtUtc)
            .Take(100)
            .ToListAsync(ct);
        var assignments = await dbContext
            .WorkflowTaskAssignments.AsNoTracking()
            .Where(item => item.Status == WorkflowTaskStatus.Active)
            .OrderBy(item => item.DueAtUtc)
            .Take(100)
            .ToListAsync(ct);

        var userNames = users.ToDictionary(item => item.Id, item => item.DisplayName);
        var departmentNames = departments.ToDictionary(item => item.Id, item => item.Name);
        var positionMap = positions.ToDictionary(item => item.Id);
        var departmentResponses = departments
            .Select(item => new DepartmentResponse(
                item.Id,
                item.Code,
                item.Name,
                item.ManagerUserId,
                item.ManagerUserId is Guid managerId
                && userNames.TryGetValue(managerId, out var manager)
                    ? manager
                    : null,
                item.IsActive
            ))
            .ToList();
        var positionResponses = positions.Select(MapPosition).ToList();
        var userResponses = users
            .Select(user => new AccessUserResponse(
                user.Id,
                user.ProfileKey ?? string.Empty,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.DepartmentId,
                user.DepartmentId is Guid departmentId
                && departmentNames.TryGetValue(departmentId, out var department)
                    ? department
                    : null,
                user.IsActive,
                userRoles
                    .Where(item => item.UserId == user.Id)
                    .Select(item => item.Role)
                    .Order()
                    .ToList(),
                userPositions
                    .Where(item =>
                        item.UserId == user.Id && positionMap.ContainsKey(item.PositionId)
                    )
                    .Select(item => MapPosition(positionMap[item.PositionId]))
                    .ToList()
            ))
            .ToList();

        return new AccessOverviewResponse(
            departmentResponses,
            positionResponses,
            userResponses,
            delegations.Select(item => MapDelegation(item, userNames)).ToList(),
            RoleDefinitions(),
            assignments.Select(item => MapAssignment(item, userNames, departmentNames)).ToList()
        );
    }

    public async Task<DepartmentResponse> CreateDepartmentAsync(
        CreateDepartmentRequest request,
        CancellationToken ct
    )
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Departments.AnyAsync(item => item.Code == code, ct))
            throw new InvalidOperationException("Bu bölüm kodu zaten kullanılıyor.");
        await EnsureActiveManagerAsync(request.ManagerUserId, ct);
        var department = Department.Create(
            code,
            request.Name,
            managerUserId: request.ManagerUserId
        );
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(ct);
        return await MapDepartmentAsync(department, ct);
    }

    public async Task<DepartmentResponse?> UpdateDepartmentAsync(
        Guid departmentId,
        UpdateDepartmentRequest request,
        CancellationToken ct
    )
    {
        var department = await dbContext.Departments.SingleOrDefaultAsync(
            item => item.Id == departmentId,
            ct
        );
        if (department is null)
            return null;
        await EnsureActiveManagerAsync(request.ManagerUserId, ct);
        department.Update(
            request.Name,
            department.ParentDepartmentId,
            request.ManagerUserId,
            request.IsActive
        );
        await dbContext.SaveChangesAsync(ct);
        return await MapDepartmentAsync(department, ct);
    }

    public async Task<AccessUserResponse?> UpdateUserAsync(
        Guid userId,
        UpdateUserAccessRequest request,
        CancellationToken ct
    )
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return null;
        var invalidRoles = request.Roles.Except(QmsRoles.All).ToArray();
        if (invalidRoles.Length > 0)
            throw new ArgumentException($"Geçersiz roller: {string.Join(", ", invalidRoles)}");
        if (
            request.DepartmentId is Guid departmentId
            && !await dbContext.Departments.AnyAsync(
                item => item.Id == departmentId && item.IsActive,
                ct
            )
        )
            throw new ArgumentException("Bölüm bulunamadı.");
        var validPositionIds = await dbContext
            .Positions.Where(item => request.PositionIds.Contains(item.Id) && item.IsActive)
            .Select(item => item.Id)
            .ToListAsync(ct);
        if (validPositionIds.Count != request.PositionIds.Distinct().Count())
            throw new ArgumentException("Pozisyonlardan biri bulunamadı.");

        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        Ensure(await userManager.UpdateAsync(user), "Kullanıcı güncellenemedi");
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            Ensure(
                await userManager.RemoveFromRolesAsync(user, currentRoles),
                "Eski roller kaldırılamadı"
            );
        if (request.Roles.Count > 0)
            Ensure(await userManager.AddToRolesAsync(user, request.Roles), "Yeni roller atanamadı");

        var now = timeProvider.GetUtcNow();
        var existingPositions = await dbContext
            .UserPositions.Where(item => item.UserId == userId && item.EndsAtUtc == null)
            .ToListAsync(ct);
        foreach (var existing in existingPositions)
            existing.End(now);
        if (request.DepartmentId is Guid assignedDepartmentId)
            foreach (var positionId in validPositionIds)
                dbContext.UserPositions.Add(
                    UserPosition.Create(
                        userId,
                        positionId,
                        assignedDepartmentId,
                        positionId == validPositionIds[0],
                        now
                    )
                );
        await dbContext.SaveChangesAsync(ct);
        return (await GetOverviewAsync(ct)).Users.Single(item => item.Id == userId);
    }

    public async Task<AccessUserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken ct
    )
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Ad soyad ve e-posta zorunludur.");
        if (
            !await dbContext.Departments.AnyAsync(
                x => x.Id == request.DepartmentId && x.IsActive,
                ct
            )
        )
            throw new ArgumentException("Aktif bölüm bulunamadı.");
        var invalidRoles = request.Roles.Except(QmsRoles.All).ToArray();
        if (invalidRoles.Length > 0 || request.Roles.Count == 0)
            throw new ArgumentException("En az bir geçerli rol seçilmelidir.");
        var positionIds = await dbContext
            .Positions.Where(x => request.PositionIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (positionIds.Count != request.PositionIds.Distinct().Count())
            throw new ArgumentException("Pozisyonlardan biri bulunamadı.");
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            DisplayName = request.DisplayName.Trim(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DepartmentId = request.DepartmentId,
            IsActive = true,
        };
        Ensure(await userManager.CreateAsync(user, request.Password), "Kullanıcı oluşturulamadı");
        Ensure(
            await userManager.AddToRolesAsync(user, request.Roles.Distinct()),
            "Kullanıcı rolleri atanamadı"
        );
        var now = timeProvider.GetUtcNow();
        foreach (var positionId in positionIds)
            dbContext.UserPositions.Add(
                UserPosition.Create(
                    user.Id,
                    positionId,
                    request.DepartmentId,
                    positionId == positionIds.FirstOrDefault(),
                    now
                )
            );
        await dbContext.SaveChangesAsync(ct);
        return (await GetOverviewAsync(ct)).Users.Single(x => x.Id == user.Id);
    }

    public async Task<DelegationResponse> CreateDelegationAsync(
        CreateDelegationRequest request,
        CancellationToken ct
    )
    {
        var startsAtUtc = request.StartsAtUtc.ToUniversalTime();
        var endsAtUtc = request.EndsAtUtc.ToUniversalTime();
        var users = await dbContext
            .Users.Where(item =>
                item.Id == request.DelegatorUserId || item.Id == request.DelegateUserId
            )
            .ToListAsync(ct);
        if (users.Count != 2 || users.Any(item => !item.IsActive))
            throw new ArgumentException("Delegasyon kullanıcıları bulunamadı veya aktif değil.");
        var overlaps = await dbContext.Delegations.AnyAsync(
            item =>
                item.DelegatorUserId == request.DelegatorUserId
                && item.Scope == request.Scope
                && item.RevokedAtUtc == null
                && item.StartsAtUtc < endsAtUtc
                && item.EndsAtUtc > startsAtUtc,
            ct
        );
        if (overlaps)
            throw new InvalidOperationException(
                "Aynı kapsam ve tarih aralığında aktif delegasyon bulunuyor."
            );
        var delegation = Delegation.Create(
            request.DelegatorUserId,
            request.DelegateUserId,
            request.Scope,
            request.Reason,
            startsAtUtc,
            endsAtUtc,
            currentUser.Id
        );
        dbContext.Delegations.Add(delegation);
        await dbContext.SaveChangesAsync(ct);
        return MapDelegation(
            delegation,
            users.ToDictionary(item => item.Id, item => item.DisplayName)
        );
    }

    public async Task<bool> RevokeDelegationAsync(Guid id, CancellationToken ct)
    {
        var delegation = await dbContext.Delegations.SingleOrDefaultAsync(
            item => item.Id == id,
            ct
        );
        if (delegation is null)
            return false;
        delegation.Revoke(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<WorkflowAssignmentResponse>> GetAssignmentsAsync(
        string aggregateType,
        Guid aggregateId,
        CancellationToken ct
    )
    {
        var assignments = await dbContext
            .WorkflowTaskAssignments.AsNoTracking()
            .Where(item => item.AggregateType == aggregateType && item.AggregateId == aggregateId)
            .OrderByDescending(item => item.AssignedAtUtc)
            .ToListAsync(ct);
        var users = await dbContext
            .Users.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, ct);
        var departments = await dbContext
            .Departments.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item.Name, ct);
        return assignments.Select(item => MapAssignment(item, users, departments)).ToList();
    }

    public async Task<WorkflowAssignmentResponse> CreateAssignmentAsync(
        CreateWorkflowAssignmentRequest request,
        CancellationToken ct
    )
    {
        var assessmentRole =
            request.AggregateType.Equals("ChangeControl", StringComparison.OrdinalIgnoreCase)
            && request.TaskRole.StartsWith("Assessment:", StringComparison.Ordinal)
            && Guid.TryParse(request.TaskRole["Assessment:".Length..], out var assessmentId)
            && await dbContext.ChangeAssessments.AnyAsync(
                item => item.Id == assessmentId && item.ChangeControlId == request.AggregateId,
                ct
            );
        if (!KnownTaskRoles.Contains(request.TaskRole) && !assessmentRole)
            throw new ArgumentException("Geçersiz kayıt görevi.");
        var user =
            await dbContext
                .Users.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == request.AssignedUserId && item.IsActive,
                    ct
                )
            ?? throw new ArgumentException("Atanacak kullanıcı bulunamadı.");
        var creatorId = await CreatorId(request.AggregateType, request.AggregateId, ct);
        if (
            request.TaskRole
                is WorkflowTaskRoles.Approver
                    or WorkflowTaskRoles.QualifiedPerson
                    or WorkflowTaskRoles.WorkItemVerifier
                    or WorkflowTaskRoles.RiskApprover
                    or WorkflowTaskRoles.MbrReviewer
                    or WorkflowTaskRoles.MbrApprover
                    or WorkflowTaskRoles.SpecializedReviewer
                    or WorkflowTaskRoles.SpecializedApprover
            && creatorId == request.AssignedUserId
        )
            throw new InvalidOperationException(
                "Kaydı oluşturan kullanıcı aynı kaydın onaylayanı olamaz."
            );
        var now = timeProvider.GetUtcNow();
        var existing = await dbContext
            .WorkflowTaskAssignments.Where(item =>
                item.AggregateType == request.AggregateType
                && item.AggregateId == request.AggregateId
                && item.TaskRole == request.TaskRole
                && item.Status == WorkflowTaskStatus.Active
            )
            .ToListAsync(ct);
        foreach (var item in existing)
            item.Cancel();
        var assignment = WorkflowTaskAssignment.Create(
            request.AggregateType,
            request.AggregateId,
            request.TaskRole,
            user.Id,
            request.AssignedDepartmentId ?? user.DepartmentId,
            now,
            request.DueAtUtc?.ToUniversalTime()
        );
        dbContext.WorkflowTaskAssignments.Add(assignment);
        var destination = AssignmentDestination(request.AggregateType, request.AggregateId);
        if (request.AggregateType.Equals("SpecializedRecord", StringComparison.OrdinalIgnoreCase))
        {
            var module = await dbContext
                .SpecializedRecords.Where(x => x.Id == request.AggregateId)
                .Select(x => x.ModuleCode)
                .SingleAsync(ct);
            destination = (
                module,
                $"/modules/{module.Replace(".", "").ToLowerInvariant()}?open={request.AggregateId}"
            );
        }
        dbContext.UserNotifications.Add(
            UserNotification.Create(
                user.Id,
                destination.Module,
                "Yeni kayıt görevi",
                $"{request.TaskRole} görevi size atandı.",
                destination.Route,
                now
            )
        );
        await dbContext.SaveChangesAsync(ct);
        var departmentName = assignment.AssignedDepartmentId is Guid id
            ? await dbContext
                .Departments.Where(item => item.Id == id)
                .Select(item => item.Name)
                .SingleOrDefaultAsync(ct)
            : null;
        return new WorkflowAssignmentResponse(
            assignment.Id,
            assignment.AggregateType,
            assignment.AggregateId,
            assignment.TaskRole,
            user.Id,
            user.DisplayName,
            assignment.AssignedDepartmentId,
            departmentName,
            assignment.Status.ToString(),
            assignment.AssignedAtUtc,
            assignment.DueAtUtc,
            assignment.CompletedAtUtc
        );
    }

    private async Task<Guid> CreatorId(string aggregateType, Guid aggregateId, CancellationToken ct)
    {
        var qualityRecordId = aggregateType.ToLowerInvariant() switch
        {
            "deviation" => await dbContext
                .Deviations.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "capa" => await dbContext
                .Capas.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "changecontrol" => await dbContext
                .ChangeControls.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "document" => await dbContext
                .ControlledDocuments.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "training" => await dbContext
                .TrainingAssignments.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "complaint" => await dbContext
                .Complaints.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "internalaudit" => await dbContext
                .InternalAudits.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "externalaudit" => await dbContext
                .ExternalAudits.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "supplieraudit" => await dbContext
                .SupplierAudits.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "workitem" => await dbContext
                .WorkItems.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "riskassessment" => await dbContext
                .RiskAssessments.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "masterbatchrecord" => await dbContext
                .MasterBatchRecords.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            "specializedrecord" => await dbContext
                .SpecializedRecords.Where(x => x.Id == aggregateId)
                .Select(x => x.QualityRecordId)
                .SingleOrDefaultAsync(ct),
            _ => Guid.Empty,
        };
        if (qualityRecordId == Guid.Empty)
            throw new ArgumentException("Kalite kaydı bulunamadı.");
        return await dbContext
            .QualityRecords.Where(item => item.Id == qualityRecordId)
            .Select(item => item.CreatedByUserId)
            .SingleAsync(ct);
    }

    private static (string Module, string Route) AssignmentDestination(
        string aggregateType,
        Guid id
    ) =>
        aggregateType.ToLowerInvariant() switch
        {
            "deviation" => ("M.01", $"/modules/deviations?open={id}"),
            "capa" => ("M.02", $"/modules/m02?open={id}"),
            "changecontrol" => ("M.03", $"/modules/m03?open={id}"),
            "document" => ("M.04", $"/modules/m04?open={id}"),
            "training" => ("M.05", $"/modules/m05?open={id}"),
            "complaint" => ("M.06", $"/modules/m06?open={id}"),
            "internalaudit" => ("M.07", $"/modules/m07?open={id}"),
            "externalaudit" => ("M.08", $"/modules/m08?open={id}"),
            "supplieraudit" => ("M.09", $"/modules/m09?open={id}"),
            "workitem" => ("M.10", $"/modules/m10?open={id}"),
            "riskassessment" => ("M.11", $"/modules/m11?open={id}"),
            "masterbatchrecord" => ("M.12", $"/modules/m12?open={id}"),
            "specializedrecord" => ("QMS", $"/?open={id}"),
            _ => ("QMS", "/"),
        };

    private static PositionResponse MapPosition(Position item) =>
        new(item.Id, item.Code, item.Name, item.IsManagement, item.IsActive);

    private async Task EnsureActiveManagerAsync(Guid? userId, CancellationToken ct)
    {
        if (
            userId is Guid id
            && !await dbContext.Users.AnyAsync(item => item.Id == id && item.IsActive, ct)
        )
            throw new ArgumentException("Bölüm yöneticisi bulunamadı veya aktif değil.");
    }

    private async Task<DepartmentResponse> MapDepartmentAsync(Department item, CancellationToken ct)
    {
        var manager = item.ManagerUserId is Guid id
            ? await dbContext
                .Users.Where(user => user.Id == id)
                .Select(user => user.DisplayName)
                .SingleOrDefaultAsync(ct)
            : null;
        return new(item.Id, item.Code, item.Name, item.ManagerUserId, manager, item.IsActive);
    }

    private static DelegationResponse MapDelegation(
        Delegation item,
        IReadOnlyDictionary<Guid, string> users
    ) =>
        new(
            item.Id,
            item.DelegatorUserId,
            users.GetValueOrDefault(item.DelegatorUserId, "Bilinmeyen"),
            item.DelegateUserId,
            users.GetValueOrDefault(item.DelegateUserId, "Bilinmeyen"),
            item.Scope,
            item.Reason,
            item.StartsAtUtc,
            item.EndsAtUtc,
            item.RevokedAtUtc
        );

    private static WorkflowAssignmentResponse MapAssignment(
        WorkflowTaskAssignment item,
        IReadOnlyDictionary<Guid, string> users,
        IReadOnlyDictionary<Guid, string> departments
    ) =>
        new(
            item.Id,
            item.AggregateType,
            item.AggregateId,
            item.TaskRole,
            item.AssignedUserId,
            item.AssignedUserNameSnapshot
                ?? users.GetValueOrDefault(item.AssignedUserId, "Bilinmeyen"),
            item.AssignedDepartmentId,
            item.AssignedDepartmentNameSnapshot
                ?? (
                    item.AssignedDepartmentId is Guid id ? departments.GetValueOrDefault(id) : null
                ),
            item.Status.ToString(),
            item.AssignedAtUtc,
            item.DueAtUtc,
            item.CompletedAtUtc
        );

    private static readonly string[] KnownTaskRoles =
    [
        WorkflowTaskRoles.Initiator,
        WorkflowTaskRoles.ProcessAuthority,
        WorkflowTaskRoles.Investigator,
        WorkflowTaskRoles.ActionOwner,
        WorkflowTaskRoles.Evaluator,
        WorkflowTaskRoles.Approver,
        WorkflowTaskRoles.QualifiedPerson,
        WorkflowTaskRoles.ChangeBoard,
        WorkflowTaskRoles.DocumentAuthor,
        WorkflowTaskRoles.DocumentApprover,
        WorkflowTaskRoles.TrainingCoordinator,
        WorkflowTaskRoles.Learner,
        WorkflowTaskRoles.Trainer,
        WorkflowTaskRoles.ComplaintCoordinator,
        WorkflowTaskRoles.ComplaintInvestigator,
        WorkflowTaskRoles.ResponseApprover,
        WorkflowTaskRoles.PharmacovigilanceReviewer,
        WorkflowTaskRoles.AuditPlanner,
        WorkflowTaskRoles.LeadAuditor,
        WorkflowTaskRoles.AuditeeResponder,
        WorkflowTaskRoles.ExternalAuditCoordinator,
        WorkflowTaskRoles.DocumentPackageController,
        WorkflowTaskRoles.ExternalAuditeeResponder,
        WorkflowTaskRoles.ExternalAuditAuthorizedCloser,
        WorkflowTaskRoles.SupplierAuditPlanner,
        WorkflowTaskRoles.SupplierAuditLeadAuditor,
        WorkflowTaskRoles.SupplierResponder,
        WorkflowTaskRoles.SupplierAuditVerifier,
        WorkflowTaskRoles.SupplierQualityApprover,
        WorkflowTaskRoles.WorkItemOwner,
        WorkflowTaskRoles.WorkItemVerifier,
        WorkflowTaskRoles.RiskOwner,
        WorkflowTaskRoles.RiskApprover,
        WorkflowTaskRoles.MbrAuthor,
        WorkflowTaskRoles.MbrReviewer,
        WorkflowTaskRoles.MbrApprover,
        WorkflowTaskRoles.SpecializedOwner,
        WorkflowTaskRoles.SpecializedReviewer,
        WorkflowTaskRoles.SpecializedApprover,
    ];

    private static IReadOnlyList<RoleDefinitionResponse> RoleDefinitions() =>
        [
            new(
                QmsRoles.Administrator,
                "Sistem Yöneticisi",
                "Kullanıcı, organizasyon ve tüm sistem ayarlarını yönetir."
            ),
            new(
                QmsRoles.QualityAssurance,
                "Kalite Güvence",
                "Kalite kayıtlarını değerlendirir ve DÖF planlar."
            ),
            new(
                QmsRoles.Approver,
                "Onaylayan",
                "Kendisi tarafından oluşturulmayan kayıtların kontrollü onaylarını verir."
            ),
            new(
                QmsRoles.DeviationReporter,
                "Sapma Bildiren",
                "Sapma oluşturur ve iş akışına gönderir."
            ),
            new(QmsRoles.Investigator, "Araştırmacı", "Kök neden ve etki araştırmalarını yürütür."),
            new(QmsRoles.ActionOwner, "Aksiyon Sorumlusu", "Atanan aksiyonu kanıtla tamamlar."),
            new(
                QmsRoles.QualityViewer,
                "Kalite İzleyici",
                "Kalite kayıtlarını salt okunur görüntüler."
            ),
            new(
                QmsRoles.QualifiedPerson,
                "Mesul Müdür",
                "Serbest bırakma ve düzenleyici kapanış kararlarını verir."
            ),
            new(
                QmsRoles.DepartmentManager,
                "Bölüm Yöneticisi",
                "Geciken işleri ve bölüm görevlerini yönetir."
            ),
            new(
                QmsRoles.RegulatoryAffairs,
                "Ruhsat Sorumlusu",
                "Ruhsat ve varyasyon değerlendirmelerini yürütür."
            ),
            new(QmsRoles.DocumentController, "Doküman Kontrol", "Doküman yaşam döngüsünü yönetir."),
            new(
                QmsRoles.TrainingCoordinator,
                "Eğitim Koordinatörü",
                "Pozisyon bazlı eğitim atamalarını yönetir."
            ),
            new(QmsRoles.Learner, "Eğitim Katılımcısı", "Kendisine atanan eğitimleri tamamlar ve okuma kanıtı verir."),
            new(QmsRoles.Trainer, "Eğitmen", "Eğitim değerlendirmesini ve yeterlilik kararını kaydeder."),
        ];

    private static void Ensure(IdentityResult result, string message)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"{message}: {string.Join(", ", result.Errors.Select(error => error.Description))}"
            );
    }
}
