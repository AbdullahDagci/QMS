using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Qms.Application.Security;
using Qms.Domain.Organization;
using Qms.Domain.Workflows;
using Qms.Domain.AuditTrail;
using Qms.Infrastructure.Persistence;
using System.Text.Json;

namespace Qms.Infrastructure.Identity;

public sealed class QmsIdentitySeeder(QmsDbContext dbContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IConfiguration configuration)
{
    public async Task SeedAsync(
        bool includeDevelopmentProfiles,
        CancellationToken cancellationToken = default)
    {
        foreach (var roleName in QmsRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName) { Id = Guid.CreateVersion7() });
                Ensure(result, $"{roleName} rolü oluşturulamadı");
            }
        }

        var requiredDepartments = new[] { ("KG", "Kalite Güvence"), ("URT", "Üretim"), ("RUH", "Ruhsatlandırma"), ("SYS", "Sistem Yönetimi"), ("VAL", "Validasyon"), ("MUH", "Mühendislik"), ("BT", "Bilgi Teknolojileri"), ("KK", "Kalite Kontrol"), ("TZ", "Tedarik Zinciri") };
        var departmentCodes = await dbContext.Departments.Select(item => item.Code).ToListAsync(cancellationToken);
        foreach (var (code, name) in requiredDepartments.Where(item => !departmentCodes.Contains(item.Item1))) dbContext.Departments.Add(Department.Create(code, name));

        if (!await dbContext.Positions.AnyAsync(cancellationToken))
        {
            dbContext.Positions.AddRange(
                Position.Create("QA_SPECIALIST", "Kalite Güvence Uzmanı"),
                Position.Create("QA_APPROVER", "Kalite Onaylayanı", true),
                Position.Create("QP", "Mesul Müdür", true),
                Position.Create("DEPT_MANAGER", "Bölüm Yöneticisi", true),
                Position.Create("INVESTIGATOR", "Araştırmacı"),
                Position.Create("ACTION_OWNER", "Aksiyon Sorumlusu"),
                Position.Create("REPORTER", "Sapma Bildiren"),
                Position.Create("REG_AFFAIRS", "Ruhsatlandırma Uzmanı"),
                Position.Create("DOC_CONTROLLER", "Doküman Kontrol Sorumlusu"),
                Position.Create("TRAINING_COORDINATOR", "Eğitim Koordinatörü"),
                Position.Create("EMPLOYEE", "Üretim Operatörü"),
                Position.Create("TRAINER", "Yetkinlik Eğitmeni"),
                Position.Create("SYSTEM_ADMIN", "Sistem Yöneticisi", true));
        }
        else
        {
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "REG_AFFAIRS", cancellationToken)) dbContext.Positions.Add(Position.Create("REG_AFFAIRS", "Ruhsatlandırma Uzmanı"));
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "DOC_CONTROLLER", cancellationToken)) dbContext.Positions.Add(Position.Create("DOC_CONTROLLER", "Doküman Kontrol Sorumlusu"));
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "TRAINING_COORDINATOR", cancellationToken)) dbContext.Positions.Add(Position.Create("TRAINING_COORDINATOR", "Eğitim Koordinatörü"));
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "EMPLOYEE", cancellationToken)) dbContext.Positions.Add(Position.Create("EMPLOYEE", "Üretim Operatörü"));
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "TRAINER", cancellationToken)) dbContext.Positions.Add(Position.Create("TRAINER", "Yetkinlik Eğitmeni"));
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var departments = await dbContext.Departments.ToDictionaryAsync(item => item.Code, cancellationToken);
        var positions = await dbContext.Positions.ToDictionaryAsync(item => item.Code, cancellationToken);
        if (!includeDevelopmentProfiles && !await dbContext.Users.AnyAsync(cancellationToken))
        {
            var email = configuration["BootstrapAdmin:Email"]?.Trim();
            var displayName = configuration["BootstrapAdmin:DisplayName"]?.Trim();
            var password = configuration["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName)
                || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "Boş production kullanıcı tabanı için BootstrapAdmin email, görünen ad ve parola secret'ı zorunludur.");
            var admin = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                DepartmentId = departments["SYS"].Id,
                IsActive = true
            };
            Ensure(await userManager.CreateAsync(admin, password), "İlk production yöneticisi oluşturulamadı");
            Ensure(await userManager.AddToRoleAsync(admin, QmsRoles.Administrator),
                "İlk production yöneticisi rolü atanamadı");
            dbContext.UserPositions.Add(UserPosition.Create(admin.Id, positions["SYSTEM_ADMIN"].Id,
                departments["SYS"].Id, true, DateTimeOffset.UtcNow));
            dbContext.AuditEvents.Add(AuditEvent.Create("ApplicationUser", admin.Id, 1,
                "BootstrapAdministratorCreated", Guid.Empty, "QMS Migrator", DateTimeOffset.UtcNow,
                Qms.Infrastructure.Integrity.AuditCorrelation.Current,
                JsonSerializer.SerializeToDocument(new { admin.Email, admin.DisplayName })));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        if (includeDevelopmentProfiles)
        {
            foreach (var profile in DevelopmentProfiles.All)
            {
                var stored = await userManager.Users.SingleOrDefaultAsync(user => user.ProfileKey == profile.Key, cancellationToken);
                if (stored is null)
                {
                    stored = new ApplicationUser
                    {
                        Id = profile.UserId,
                        UserName = $"{profile.Key}@qms.local",
                        Email = $"{profile.Key}@qms.local",
                        EmailConfirmed = true,
                        DisplayName = profile.DisplayName,
                        ProfileKey = profile.Key,
                        DepartmentId = departments[profile.DepartmentCode].Id,
                        IsActive = true
                    };
                    Ensure(await userManager.CreateAsync(stored), $"{profile.DisplayName} kullanıcısı oluşturulamadı");
                }

                if (stored.DisplayName != profile.DisplayName) { stored.DisplayName = profile.DisplayName; Ensure(await userManager.UpdateAsync(stored), $"{profile.DisplayName} adı güncellenemedi"); }

                var currentRoles = await userManager.GetRolesAsync(stored);
                var rolesToRemove = currentRoles.Except(profile.Roles).ToArray();
                if (rolesToRemove.Length > 0) Ensure(await userManager.RemoveFromRolesAsync(stored, rolesToRemove), $"{profile.DisplayName} eski rolleri kaldırılamadı");
                var rolesToAdd = profile.Roles.Except(currentRoles).ToArray();
                if (rolesToAdd.Length > 0) Ensure(await userManager.AddToRolesAsync(stored, rolesToAdd), $"{profile.DisplayName} rolleri atanamadı");
                if (!await userManager.HasPasswordAsync(stored))
                    Ensure(await userManager.AddPasswordAsync(stored, configuration["DevelopmentAuth:SignaturePassword"] ?? "Qms.Dev!2026"), $"{profile.DisplayName} geliştirme imza parolası oluşturulamadı");

                if (!await dbContext.UserPositions.AnyAsync(item => item.UserId == stored.Id && item.EndsAtUtc == null, cancellationToken))
                {
                    var positionCode = profile.Key switch
                    {
                        "quality" or "quality-reviewer" => "QA_SPECIALIST", "approver" => "QA_APPROVER", "qualified-person" => "QP",
                        "manager" => "DEPT_MANAGER", "investigator" => "INVESTIGATOR", "action-owner" => "ACTION_OWNER",
                        "reporter" => "REPORTER", "regulatory" => "REG_AFFAIRS", "document-controller" => "DOC_CONTROLLER",
                        "training-coordinator" => "TRAINING_COORDINATOR", "admin" => "SYSTEM_ADMIN", "validation-reviewer" or "engineering-reviewer" or "it-reviewer" => "DEPT_MANAGER",
                        "learner" => "EMPLOYEE", "trainer" => "TRAINER", _ => "QA_SPECIALIST"
                    };
                    dbContext.UserPositions.Add(UserPosition.Create(stored.Id, positions[positionCode].Id, stored.DepartmentId!.Value, true, DateTimeOffset.UtcNow));
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var activeAssessmentTasks = await dbContext.WorkflowTaskAssignments.Where(item => item.AggregateType == "ChangeControl" && item.Status == WorkflowTaskStatus.Active && item.TaskRole.StartsWith("Assessment:")).ToListAsync(cancellationToken);
        foreach (var task in activeAssessmentTasks)
        {
            if (!Guid.TryParse(task.TaskRole["Assessment:".Length..], out var assessmentId)) continue;
            var assessment = await dbContext.ChangeAssessments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == assessmentId, cancellationToken); if (assessment is null) continue;
            var department = await dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(item => item.IsActive && item.Name == assessment.Department, cancellationToken); if (department is null) continue;
            var roleName = assessment.Department == "Ruhsatlandırma" ? QmsRoles.RegulatoryAffairs : assessment.Department == "Kalite Güvence" ? QmsRoles.QualityAssurance : QmsRoles.DepartmentManager;
            var evaluatorId = department.ManagerUserId ?? await (from link in dbContext.UserRoles.AsNoTracking() join role in dbContext.Roles.AsNoTracking() on link.RoleId equals role.Id join user in dbContext.Users.AsNoTracking() on link.UserId equals user.Id where role.Name == roleName && user.IsActive && user.DepartmentId == department.Id orderby user.DisplayName select (Guid?)user.Id).FirstOrDefaultAsync(cancellationToken);
            if (!evaluatorId.HasValue || task.AssignedUserId == evaluatorId.Value && task.AssignedDepartmentId == department.Id) continue;
            task.Cancel(); dbContext.WorkflowTaskAssignments.Add(WorkflowTaskAssignment.Create("ChangeControl", task.AggregateId, task.TaskRole, evaluatorId.Value, department.Id, assignedAtUtc: DateTimeOffset.UtcNow, dueAtUtc: task.DueAtUtc));
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        if (includeDevelopmentProfiles)
        {
            var managerProfiles = new Dictionary<string, string> { ["URT"] = "manager", ["VAL"] = "validation-reviewer", ["MUH"] = "engineering-reviewer", ["BT"] = "it-reviewer" };
            foreach (var (departmentCode, profileKey) in managerProfiles)
            {
                var department = departments[departmentCode];
                var managerId = await dbContext.Users.Where(item => item.ProfileKey == profileKey).Select(item => (Guid?)item.Id).SingleOrDefaultAsync(cancellationToken);
                if (managerId.HasValue && department.ManagerUserId != managerId) department.Update(department.Name, department.ParentDepartmentId, managerId, department.IsActive);
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static void Ensure(IdentityResult result, string message)
    {
        if (!result.Succeeded) throw new InvalidOperationException($"{message}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
    }
}
