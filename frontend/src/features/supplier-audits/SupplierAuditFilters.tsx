import { useState } from "react";
import { TextField } from "@mui/material";
import { AdvancedFilterPanel } from "../../components/AdvancedFilterPanel";
import { SearchableSelect } from "../../components/SearchableSelect";
import {
  emptySupplierAuditFilters,
  type SupplierAuditFilterState,
} from "./supplierAuditFilterModel";
const options = (values: string[]) =>
  values.map((value) => ({ value, label: value }));
export function SupplierAuditFilters({
  value,
  onApply,
}: {
  value: SupplierAuditFilterState;
  onApply: (v: SupplierAuditFilterState) => void;
}) {
  const [draft, setDraft] = useState(value);
  const set = (k: keyof SupplierAuditFilterState, v: string) =>
    setDraft((x) => ({ ...x, [k]: v }));
  const active = Object.values(value).filter(Boolean).length;
  return (
    <AdvancedFilterPanel
      activeCount={active}
      onClear={() => {
        const cleared = { ...emptySupplierAuditFilters };
        setDraft(cleared);
        onApply(cleared);
      }}
      onApply={() => onApply(draft)}
    >
      {(
        [
          ["recordNumber", "Kayıt numarası"],
          ["supplierCode", "Tedarikçi kodu"],
          ["supplierName", "Tedarikçi adı"],
          ["supplierScope", "Kapsam"],
          ["materialOrService", "Malzeme / hizmet"],
        ] as const
      ).map(([k, l]) => (
        <TextField
          key={k}
          size="small"
          label={`${l} içerir`}
          value={draft[k]}
          onChange={(e) => set(k, e.target.value)}
        />
      ))}
      <SearchableSelect
        label="Kritiklik"
        value={draft.criticality || null}
        options={options(["Kritik", "Yüksek", "Orta", "Düşük"])}
        onChange={(v) => set("criticality", v ?? "")}
      />
      <SearchableSelect
        label="Risk bandı"
        value={draft.riskBand || null}
        options={options(["Kritik", "Yüksek", "Orta", "Düşük"])}
        onChange={(v) => set("riskBand", v ?? "")}
      />
      <SearchableSelect
        label="Nitelendirme"
        value={draft.qualificationStatus || null}
        options={options([
          "Active",
          "Conditional",
          "Suspended",
          "RequalificationRequired",
          "Approved",
          "Rejected",
        ])}
        onChange={(v) => set("qualificationStatus", v ?? "")}
      />
      <SearchableSelect
        label="Durum"
        value={draft.status || null}
        options={options([
          "RiskPlan",
          "ScopeChecklist",
          "AuditorAssignment",
          "Execution",
          "Findings",
          "SupplierResponse",
          "EvidenceVerification",
          "Capa",
          "AuditResult",
          "Closed",
        ])}
        onChange={(v) => set("status", v ?? "")}
      />
      <TextField
        size="small"
        type="date"
        label="Başlangıçtan"
        value={draft.plannedFrom}
        onChange={(e) => set("plannedFrom", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        size="small"
        type="date"
        label="Başlangıca kadar"
        value={draft.plannedTo}
        onChange={(e) => set("plannedTo", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </AdvancedFilterPanel>
  );
}
