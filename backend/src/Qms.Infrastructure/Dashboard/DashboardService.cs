using Microsoft.EntityFrameworkCore;
using Qms.Application.Dashboard;
using Qms.Contracts.Dashboard;
using Qms.Domain.Deviations;
using Qms.Infrastructure.Persistence;
using Qms.Application.Security;
using Qms.Infrastructure.Security;

namespace Qms.Infrastructure.Dashboard;

public sealed class DashboardService(QmsDbContext dbContext, TimeProvider timeProvider,
    ICurrentUser currentUser) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var deviations = from deviation in dbContext.Deviations.AsNoTracking()
                         join record in dbContext.VisibleQualityRecords(currentUser)
                             on deviation.QualityRecordId equals record.Id
                         select deviation;
        var open = deviations.Where(item => item.Status != DeviationStatus.Closed && item.Status != DeviationStatus.Voided);

        var totalCount = await deviations.LongCountAsync(cancellationToken);
        var openCount = await open.LongCountAsync(cancellationToken);
        var highRiskCount = await open.LongCountAsync(
            item => item.Classification == DeviationClassification.Major ||
                    item.Classification == DeviationClassification.Critical,
            cancellationToken);
        var overdueCount = await open.LongCountAsync(item => item.TargetDateUtc < now, cancellationToken);
        var capaCount = await open.LongCountAsync(item => item.CapaRequired, cancellationToken);
        var recent = await (
                from deviation in deviations
                join record in dbContext.VisibleQualityRecords(currentUser)
                    on deviation.QualityRecordId equals record.Id
                orderby deviation.CreatedAtUtc descending
                select new DashboardDeviationResponse(
                    deviation.Id,
                    record.RecordNumber,
                    deviation.Title,
                    deviation.Status.ToString(),
                    deviation.Classification.ToString(),
                    deviation.RiskScore,
                    deviation.TargetDateUtc))
            .Take(5)
            .ToListAsync(cancellationToken);

        return new DashboardSummaryResponse(
            totalCount,
            openCount,
            highRiskCount,
            overdueCount,
            capaCount,
            recent);
    }
}
