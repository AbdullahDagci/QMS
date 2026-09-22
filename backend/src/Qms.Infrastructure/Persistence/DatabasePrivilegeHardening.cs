using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Qms.Infrastructure.Persistence;

public sealed partial class DatabasePrivilegeHardening(QmsDbContext db, IConfiguration configuration)
{
    private static readonly string[] Schemas =
    [
        "identity", "organization", "core", "deviation", "capa", "change_control",
        "document", "training", "complaint", "internal_audit", "external_audit",
        "supplier_audit", "work_tracking", "risk_management", "mbr", "specialized", "workflow",
        "audit", "integration", "notification", "quality", "files", "forms"
    ];

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var runtimeRole = configuration["Database:RuntimeUsername"];
        if (string.IsNullOrWhiteSpace(runtimeRole)) return;
        if (!IdentifierPattern().IsMatch(runtimeRole))
            throw new InvalidOperationException("Database:RuntimeUsername geçerli bir PostgreSQL rol adı olmalıdır.");
        var exists = await db.Database.SqlQueryRaw<bool>(
            "SELECT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = {0}) AS \"Value\"", runtimeRole)
            .SingleAsync(cancellationToken);
        if (!exists)
            throw new InvalidOperationException(
                $"Production runtime veritabanı rolü bulunamadı: {runtimeRole}. Yeni volume'u init script ile oluşturun.");

        var role = $"\"{runtimeRole}\"";
        foreach (var schemaName in Schemas)
        {
            var schema = $"\"{schemaName}\"";
            await ExecuteIdentifierCommandAsync($"GRANT USAGE ON SCHEMA {schema} TO {role}", cancellationToken);
            await ExecuteIdentifierCommandAsync(
                $"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {schema} TO {role}", cancellationToken);
            await ExecuteIdentifierCommandAsync(
                $"GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA {schema} TO {role}", cancellationToken);
            await ExecuteIdentifierCommandAsync(
                $"ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {role}", cancellationToken);
            await ExecuteIdentifierCommandAsync(
                $"ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO {role}", cancellationToken);
        }
        await ExecuteIdentifierCommandAsync(
            $"REVOKE UPDATE, DELETE ON audit.audit_event, core.electronic_signature, files.managed_file FROM {role}",
            cancellationToken);
        await ExecuteIdentifierCommandAsync(
            $"REVOKE DELETE ON forms.form_definition, forms.form_version, forms.output_template, forms.form_record FROM {role}",
            cancellationToken);
    }

    private Task ExecuteIdentifierCommandAsync(string sql, CancellationToken cancellationToken)
    {
        // PostgreSQL identifiers cannot be parameters. Every identifier reaching this method is
        // either a compile-time schema name or a role accepted by IdentifierPattern above.
#pragma warning disable EF1002
        return db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
#pragma warning restore EF1002
    }

    [GeneratedRegex("^[a-z_][a-z0-9_]{0,62}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();
}
