import type { ColumnFilter } from "../../api/deviations";
export interface SupplierAuditFilterState {
  recordNumber: string;
  supplierCode: string;
  supplierName: string;
  supplierScope: string;
  materialOrService: string;
  criticality: string;
  riskBand: string;
  qualificationStatus: string;
  status: string;
  plannedFrom: string;
  plannedTo: string;
}
export const emptySupplierAuditFilters: SupplierAuditFilterState = {
  recordNumber: "",
  supplierCode: "",
  supplierName: "",
  supplierScope: "",
  materialOrService: "",
  criticality: "",
  riskBand: "",
  qualificationStatus: "",
  status: "",
  plannedFrom: "",
  plannedTo: "",
};
export function toSupplierAuditColumnFilters(
  v: SupplierAuditFilterState,
): ColumnFilter[] {
  const out: ColumnFilter[] = [];
  for (const field of [
    "recordNumber",
    "supplierCode",
    "supplierName",
    "supplierScope",
    "materialOrService",
  ] as const)
    if (v[field].trim())
      out.push({ field, operator: "contains", value: v[field].trim() });
  for (const field of [
    "criticality",
    "riskBand",
    "qualificationStatus",
    "status",
  ] as const)
    if (v[field]) out.push({ field, operator: "equals", value: v[field] });
  if (v.plannedFrom || v.plannedTo)
    out.push({
      field: "plannedStartUtc",
      operator: "between",
      value: v.plannedFrom || "1900-01-01",
      valueTo: v.plannedTo || "2999-12-31",
    });
  return out;
}
