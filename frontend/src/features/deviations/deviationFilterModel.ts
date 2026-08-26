import type { ColumnFilter } from "../../api/deviations";

export interface DeviationFilterState {
  recordNumber: string;
  title: string;
  department: string;
  riskMin: string;
  riskMax: string;
  classifications: string[];
  statuses: string[];
  capaRequired: "all" | "true" | "false";
  targetFrom: string;
  targetTo: string;
}

export const emptyDeviationFilters: DeviationFilterState = {
  recordNumber: "",
  title: "",
  department: "",
  riskMin: "",
  riskMax: "",
  classifications: [],
  statuses: [],
  capaRequired: "all",
  targetFrom: "",
  targetTo: "",
};

export function toColumnFilters(filters: DeviationFilterState): ColumnFilter[] {
  const result: ColumnFilter[] = [];
  if (filters.recordNumber.trim())
    result.push({
      field: "recordNumber",
      operator: "contains",
      value: filters.recordNumber.trim(),
    });
  if (filters.title.trim())
    result.push({
      field: "title",
      operator: "contains",
      value: filters.title.trim(),
    });
  if (filters.department.trim())
    result.push({
      field: "detectedDepartment",
      operator: "contains",
      value: filters.department.trim(),
    });
  if (filters.classifications.length)
    result.push({
      field: "classification",
      operator: "in",
      values: filters.classifications,
    });
  if (filters.statuses.length)
    result.push({ field: "status", operator: "in", values: filters.statuses });
  if (filters.capaRequired !== "all")
    result.push({
      field: "capaRequired",
      operator: "equals",
      value: filters.capaRequired,
    });
  if (filters.riskMin && filters.riskMax)
    result.push({
      field: "riskScore",
      operator: "between",
      value: filters.riskMin,
      valueTo: filters.riskMax,
    });
  else if (filters.riskMin)
    result.push({
      field: "riskScore",
      operator: "greaterThanOrEqual",
      value: filters.riskMin,
    });
  else if (filters.riskMax)
    result.push({
      field: "riskScore",
      operator: "lessThanOrEqual",
      value: filters.riskMax,
    });
  if (filters.targetFrom && filters.targetTo)
    result.push({
      field: "targetDateUtc",
      operator: "between",
      value: filters.targetFrom,
      valueTo: filters.targetTo,
    });
  else if (filters.targetFrom)
    result.push({
      field: "targetDateUtc",
      operator: "after",
      value: filters.targetFrom,
    });
  else if (filters.targetTo)
    result.push({
      field: "targetDateUtc",
      operator: "before",
      value: filters.targetTo,
    });
  return result;
}
