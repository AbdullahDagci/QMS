import type { ColumnFilter } from "../../api/deviations";
export interface TrainingFilterState {
  recordNumber: string;
  employeeName: string;
  position: string;
  courseCode: string;
  courseTitle: string;
  documentCode: string;
  status: string;
  assessmentMode: string;
  critical: string;
  dueFrom: string;
  dueTo: string;
}
export const emptyTrainingFilters: TrainingFilterState = {
  recordNumber: "",
  employeeName: "",
  position: "",
  courseCode: "",
  courseTitle: "",
  documentCode: "",
  status: "",
  assessmentMode: "",
  critical: "",
  dueFrom: "",
  dueTo: "",
};
export function toTrainingColumnFilters(
  value: TrainingFilterState,
): ColumnFilter[] {
  const filters: ColumnFilter[] = [];
  for (const [field, text] of Object.entries({
    recordNumber: value.recordNumber,
    employeeName: value.employeeName,
    position: value.position,
    courseCode: value.courseCode,
    courseTitle: value.courseTitle,
    documentCode: value.documentCode,
  }))
    if (text.trim())
      filters.push({ field, operator: "contains", value: text.trim() });
  for (const [field, text] of Object.entries({
    status: value.status,
    assessmentMode: value.assessmentMode,
    isCriticalQualification: value.critical,
  }))
    if (text) filters.push({ field, operator: "equals", value: text });
  if (value.dueFrom || value.dueTo)
    filters.push({
      field: "dueAtUtc",
      operator: "between",
      value: value.dueFrom || "1970-01-01",
      valueTo: value.dueTo || "2999-12-31",
    });
  return filters;
}
