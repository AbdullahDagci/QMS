using System.Diagnostics;

namespace Qms.Infrastructure.Integrity;

public static class AuditCorrelation
{
    public static string Current => Activity.Current?.TraceId.ToHexString()
        ?? Guid.CreateVersion7().ToString("N");
}
