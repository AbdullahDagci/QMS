import type { ColumnFilter } from "../../api/deviations";
export interface DocumentFilterState {
  recordNumber: string;
  sourceRecordNumber: string;
  documentCode: string;
  title: string;
  owner: string;
  department: string;
  documentType: string;
  confidentiality: string;
  status: string;
  effectiveFrom: string;
  effectiveTo: string;
}
export const emptyDocumentFilters: DocumentFilterState = {
  recordNumber: "",
  sourceRecordNumber: "",
  documentCode: "",
  title: "",
  owner: "",
  department: "",
  documentType: "",
  confidentiality: "",
  status: "",
  effectiveFrom: "",
  effectiveTo: "",
};
export function toDocumentColumnFilters(
  v: DocumentFilterState,
): ColumnFilter[] {
  const f: ColumnFilter[] = [];
  for (const [field, value] of Object.entries({
    recordNumber: v.recordNumber,
    sourceRecordNumber: v.sourceRecordNumber,
    documentCode: v.documentCode,
    title: v.title,
    owner: v.owner,
    department: v.department,
  }))
    if (value.trim())
      f.push({ field, operator: "contains", value: value.trim() });
  for (const [field, value] of Object.entries({
    documentType: v.documentType,
    confidentiality: v.confidentiality,
    status: v.status,
  }))
    if (value) f.push({ field, operator: "equals", value });
  if (v.effectiveFrom || v.effectiveTo)
    f.push({
      field: "plannedEffectiveDateUtc",
      operator: "between",
      value: v.effectiveFrom || "1970-01-01",
      valueTo: v.effectiveTo || "2999-12-31",
    });
  return f;
}
