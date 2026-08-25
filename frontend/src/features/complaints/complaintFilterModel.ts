import type { ColumnFilter } from "../../api/deviations";
export interface ComplaintFilterState {
  recordNumber: string;
  customerName: string;
  product: string;
  batchNumber: string;
  complaintType: string;
  owner: string;
  severity: string;
  status: string;
  adverse: string;
  trend: string;
  dueFrom: string;
  dueTo: string;
}
export const emptyComplaintFilters: ComplaintFilterState = {
  recordNumber: "",
  customerName: "",
  product: "",
  batchNumber: "",
  complaintType: "",
  owner: "",
  severity: "",
  status: "",
  adverse: "",
  trend: "",
  dueFrom: "",
  dueTo: "",
};
export function toComplaintColumnFilters(
  v: ComplaintFilterState,
): ColumnFilter[] {
  const f: ColumnFilter[] = [];
  for (const [field, value] of Object.entries({
    recordNumber: v.recordNumber,
    customerName: v.customerName,
    product: v.product,
    batchNumber: v.batchNumber,
    complaintType: v.complaintType,
    owner: v.owner,
  }))
    if (value.trim())
      f.push({ field, operator: "contains", value: value.trim() });
  for (const [field, value] of Object.entries({
    severity: v.severity,
    status: v.status,
    suspectedAdverseEvent: v.adverse,
    trendFlagged: v.trend,
  }))
    if (value) f.push({ field, operator: "equals", value });
  if (v.dueFrom || v.dueTo)
    f.push({
      field: "finalResponseDueAtUtc",
      operator: "between",
      value: v.dueFrom || "1970-01-01",
      valueTo: v.dueTo || "2999-12-31",
    });
  return f;
}
