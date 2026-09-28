import type { SelectOption } from "../../components/SearchableSelect";

// Görev-atama matrisinde yapılandırılabilen roller.
export const taskRoleOptions: Array<SelectOption<string>> = [
  { value: "ProcessAuthority", label: "İşlem yetkilisi" },
  { value: "Investigator", label: "Araştırmacı" },
  { value: "Evaluator", label: "Değerlendiren" },
  { value: "Approver", label: "Onaylayan" },
];

// Hazırlayan görevi matristen değil, sapmayı oluşturan kullanıcıya doğrudan atanır.
const taskRoleLabels: Record<string, string> = {
  Initiator: "Hazırlayan",
  ...Object.fromEntries(taskRoleOptions.map((option) => [option.value, option.label])),
};

export const taskRoleLabel = (taskRole: string) => taskRoleLabels[taskRole] ?? taskRole;
