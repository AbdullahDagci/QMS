import type { ColumnFilter } from "../../api/deviations";
export interface ExternalAuditFilterState {
  recordNumber: string;
  title: string;
  auditorOrganization: string;
  auditKind: string;
  authorityCountry: string;
  owner: string;
  status: string;
  government: string;
  startFrom: string;
  startTo: string;
}
export const emptyExternalAuditFilters: ExternalAuditFilterState = {
  recordNumber: "",
  title: "",
  auditorOrganization: "",
  auditKind: "",
  authorityCountry: "",
  owner: "",
  status: "",
  government: "",
  startFrom: "",
  startTo: "",
};
export function toExternalAuditColumnFilters(
  v: ExternalAuditFilterState,
): ColumnFilter[] {
  const f: ColumnFilter[] = [];
  for (const [field, value] of Object.entries({
    recordNumber: v.recordNumber,
    title: v.title,
    auditorOrganization: v.auditorOrganization,
    auditKind: v.auditKind,
    authorityCountry: v.authorityCountry,
    owner: v.owner,
  }))
    if (value.trim())
      f.push({ field, operator: "contains", value: value.trim() });
  for (const [field, value] of Object.entries({
    status: v.status,
    isGovernmentAuthority: v.government,
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
