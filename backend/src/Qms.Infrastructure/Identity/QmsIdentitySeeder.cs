using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Domain.Organization;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Identity;

public sealed class QmsIdentitySeeder(QmsDbContext dbContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in QmsRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName) { Id = Guid.CreateVersion7() });
                Ensure(result, $"{roleName} rolü oluşturulamadı");
            }
        }

        if (!await dbContext.Departments.AnyAsync(cancellationToken))
        {
            dbContext.Departments.AddRange(
                Department.Create("KG", "Kalite Güvence"),
                Department.Create("URT", "Üretim"),
                Department.Create("RUH", "Ruhsatlandırma"),
                Department.Create("SYS", "Sistem Yönetimi"));
        }

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
                Position.Create("SYSTEM_ADMIN", "Sistem Yöneticisi", true));
        }
        else
        {
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "REG_AFFAIRS", cancellationToken)) dbContext.Positions.Add(Position.Create("REG_AFFAIRS", "Ruhsatlandırma Uzmanı"));
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "DOC_CONTROLLER", cancellationToken)) dbContext.Positions.Add(Position.Create("DOC_CONTROLLER", "Doküman Kontrol Sorumlusu"));
            if (!await dbContext.Positions.AnyAsync(item => item.Code == "TRAINING_COORDINATOR", cancellationToken)) dbContext.Positions.Add(Position.Create("TRAINING_COORDINATOR", "Eğitim Koordinatörü"));
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var departments = await dbContext.Departments.ToDictionaryAsync(item => item.Code, cancellationToken);
        var positions = await dbContext.Positions.ToDictionaryAsync(item => item.Code, cancellationToken);
        foreach (var profile in DevelopmentProfiles.All)
        {
            var stored = await userManager.Users.SingleOrDefaultAsync(user => user.ProfileKey == profile.Key, cancellationToken);
            if (stored is null)
            {
                var departmentCode = profile.Key is "reporter" or "action-owner" or "manager" ? "URT" : profile.Key == "regulatory" ? "RUH" : profile.Key == "admin" ? "SYS" : "KG";
                stored = new ApplicationUser
                {
                    Id = profile.UserId,
                    UserName = $"{profile.Key}@qms.local",
                    Email = $"{profile.Key}@qms.local",
                    EmailConfirmed = true,
                    DisplayName = profile.DisplayName,
                    ProfileKey = profile.Key,
                    DepartmentId = departments[departmentCode].Id,
                    IsActive = true
                };
                Ensure(await userManager.CreateAsync(stored), $"{profile.DisplayName} kullanıcısı oluşturulamadı");
            }

            var currentRoles = await userManager.GetRolesAsync(stored);
            var rolesToRemove = currentRoles.Except(profile.Roles).ToArray();
            if (rolesToRemove.Length > 0) Ensure(await userManager.RemoveFromRolesAsync(stored, rolesToRemove), $"{profile.DisplayName} eski rolleri kaldırılamadı");
            var rolesToAdd = profile.Roles.Except(currentRoles).ToArray();
            if (rolesToAdd.Length > 0) Ensure(await userManager.AddToRolesAsync(stored, rolesToAdd), $"{profile.DisplayName} rolleri atanamadı");

            if (!await dbContext.UserPositions.AnyAsync(item => item.UserId == stored.Id && item.EndsAtUtc == null, cancellationToken))
            {
                var positionCode = profile.Key switch
                {
                    "quality" or "quality-reviewer" => "QA_SPECIALIST", "approver" => "QA_APPROVER", "qualified-person" => "QP",
                    "manager" => "DEPT_MANAGER", "investigator" => "INVESTIGATOR", "action-owner" => "ACTION_OWNER",
                    "reporter" => "REPORTER", "regulatory" => "REG_AFFAIRS", "document-controller" => "DOC_CONTROLLER",
                    "training-coordinator" => "TRAINING_COORDINATOR", "admin" => "SYSTEM_ADMIN", _ => "QA_SPECIALIST"
                };
                dbContext.UserPositions.Add(UserPosition.Create(stored.Id, positions[positionCode].Id, stored.DepartmentId!.Value, true, DateTimeOffset.UtcNow));
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Ensure(IdentityResult result, string message)
    {
        if (!result.Succeeded) throw new InvalidOperationException($"{message}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
    }
}
