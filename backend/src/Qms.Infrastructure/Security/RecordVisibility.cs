using Microsoft.EntityFrameworkCore;
using Qms.Application.Security;
using Qms.Domain.Documents;
using Qms.Domain.QualityRecords;
using Qms.Infrastructure.Persistence;

namespace Qms.Infrastructure.Security;

public static class RecordVisibility
{
    public static IQueryable<QualityRecord> VisibleQualityRecords(this QmsDbContext db,
        ICurrentUser currentUser)
    {
        var records = db.QualityRecords.AsNoTracking();
        if (currentUser.IsInRole(QmsRoles.Administrator)
            || currentUser.IsInRole(QmsRoles.QualityAssurance)) return records;
        var userId = currentUser.Id;
        var departmentId = currentUser.DepartmentId;
        return records.Where(record => record.CreatedByUserId == userId
            || departmentId.HasValue && record.DepartmentId == departmentId.Value
                && record.RecordType != "M.15"
            || db.RecordAccessGrants.Any(grant => grant.QualityRecordId == record.Id && grant.UserId == userId));
    }

    public static IQueryable<ControlledDocument> VisibleControlledDocuments(this QmsDbContext db,
        ICurrentUser currentUser)
    {
        var canViewConfidential = currentUser.IsInRole(QmsRoles.Administrator)
            || currentUser.IsInRole(QmsRoles.QualityAssurance)
            || currentUser.IsInRole(QmsRoles.DocumentController);
        var userId = currentUser.Id;
        return db.ControlledDocuments.AsNoTracking().Where(document =>
            db.VisibleQualityRecords(currentUser).Any(record => record.Id == document.QualityRecordId)
            && (canViewConfidential || document.Confidentiality != "Confidential"
                || document.OwnerUserId == userId
                || db.RecordAccessGrants.Any(grant => grant.QualityRecordId == document.QualityRecordId
                    && grant.UserId == userId)));
    }
}
