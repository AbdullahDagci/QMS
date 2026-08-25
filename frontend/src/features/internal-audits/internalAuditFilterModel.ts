import type { ColumnFilter } from "../../api/deviations";
export interface InternalAuditFilterState {
  recordNumber: string;
  title: string;
  auditType: string;
  auditeeDepartment: string;
  leadAuditor: string;
  status: string;
  isUnplanned: string;
  planYear: string;
  startFrom: string;
  startTo: string;
}
export const emptyInternalAuditFilters: InternalAuditFilterState = {
  recordNumber: "",
  title: "",
  auditType: "",
  auditeeDepartment: "",
  leadAuditor: "",
  status: "",
  isUnplanned: "",
  planYear: "",
  startFrom: "",
  startTo: "",
};
export function toInternalAuditColumnFilters(
  v: InternalAuditFilterState,
): ColumnFilter[] {
  const f: ColumnFilter[] = [];
  for (const [field, value] of Object.entries({
    recordNumber: v.recordNumber,
    title: v.title,
    auditType: v.auditType,
    auditeeDepartment: v.auditeeDepartment,
    leadAuditor: v.leadAuditor,
  }))
    if (value.trim())
      f.push({ field, operator: "contains", value: value.trim() });
  for (const [field, value] of Object.entries({
    status: v.status,
    isUnplanned: v.isUnplanned,
    planYear: v.planYear,
  }))
    if (value) f.push({ field, operator: "equals", value });
  if (v.startFrom || v.startTo)
    f.push({
      field: "plannedStartUtc",
      operator: "between",
      value: v.startFrom || "1970-01-01",
      valueTo: v.startTo || "2999-12-31",
    });
  return f;
}
