import type { SelectOption } from "../../components/SearchableSelect";

export const taskRoleOptions: Array<SelectOption<string>> = [
  { value: "ProcessAuthority", label: "İşlem yetkilisi" },
  { value: "Investigator", label: "Araştırmacı" },
  { value: "Evaluator", label: "Değerlendiren" },
  { value: "Approver", label: "Onaylayan" },
];

export const taskRoleLabel = (taskRole: string) =>
  taskRoleOptions.find((option) => option.value === taskRole)?.label ?? taskRole;
