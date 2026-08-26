import type { ColumnFilter } from "../../api/deviations";

export interface ChangeControlFilterState {
  recordNumber: string;
  sourceRecordNumber: string;
  title: string;
  owner: string;
  changeType: string;
  status: string;
  riskLevel: string;
  regulatoryImpact: string;
  targetFrom: string;
  targetTo: string;
}
export const emptyChangeControlFilters: ChangeControlFilterState = {
  recordNumber: "",
  sourceRecordNumber: "",
  title: "",
  owner: "",
  changeType: "",
  status: "",
  riskLevel: "",
  regulatoryImpact: "",
  targetFrom: "",
  targetTo: "",
};
export function toChangeControlColumnFilters(
  value: ChangeControlFilterState,
): ColumnFilter[] {
  const filters: ColumnFilter[] = [];
  const contains = (field: string, text: string) => {
    if (text.trim())
      filters.push({ field, operator: "contains", value: text.trim() });
  };
  const equals = (field: string, text: string) => {
    if (text) filters.push({ field, operator: "equals", value: text });
  };
  contains("recordNumber", value.recordNumber);
  contains("sourceRecordNumber", value.sourceRecordNumber);
  contains("title", value.title);
  contains("owner", value.owner);
  equals("changeType", value.changeType);
  equals("status", value.status);
  equals("riskLevel", value.riskLevel);
  equals("regulatoryImpact", value.regulatoryImpact);
  if (value.targetFrom || value.targetTo)
    filters.push({
      field: "targetDateUtc",
      operator: "between",
      value: value.targetFrom || "1970-01-01",
      valueTo: value.targetTo || "2999-12-31",
    });
  return filters;
}
