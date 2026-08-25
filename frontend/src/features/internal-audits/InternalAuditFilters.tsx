import { useState } from "react";
import { TextField } from "@mui/material";
import { AdvancedFilterPanel } from "../../components/AdvancedFilterPanel";
import { SearchableSelect } from "../../components/SearchableSelect";
import {
  emptyInternalAuditFilters,
  toInternalAuditColumnFilters,
  type InternalAuditFilterState,
} from "./internalAuditFilterModel";
const states = [
  "AnnualPlan",
  "Preparation",
  "PlanApproval",
  "Execution",
  "Findings",
  "ResponseAction",
  "CapaVerification",
  "FindingClosure",
  "AuditClosure",
  "Closed",
];
export function InternalAuditFilters({
  value,
  onApply,
}: {
  value: InternalAuditFilterState;
  onApply: (v: InternalAuditFilterState) => void;
}) {
  const [d, setD] = useState(value);
  const set = (k: keyof InternalAuditFilterState, v: string) =>
    setD((x) => ({ ...x, [k]: v }));
  return (
    <AdvancedFilterPanel
      activeCount={toInternalAuditColumnFilters(d).length}
      onClear={() => {
        setD(emptyInternalAuditFilters);
        onApply(emptyInternalAuditFilters);
      }}
      onApply={() => onApply(d)}
    >
      {[
        ["recordNumber", "Kayıt numarası içerir"],
        ["title", "Başlık içerir"],
        ["auditType", "Denetim türü içerir"],
        ["auditeeDepartment", "Denetlenen bölüm içerir"],
        ["leadAuditor", "Baş denetçi içerir"],
        ["planYear", "Plan yılı"],
      ].map(([k, l]) => (
        <TextField
          key={k}
          size="small"
          label={l}
          value={d[k as keyof InternalAuditFilterState]}
          onChange={(e) =>
            set(k as keyof InternalAuditFilterState, e.target.value)
          }
        />
      ))}
      <SearchableSelect
        label="Durum"
        value={d.status}
        options={[
          { value: "", label: "Tüm durumlar" },
          ...states.map((value) => ({ value, label: statusLabel(value) })),
        ]}
        onChange={(v) => set("status", v ?? "")}
      />
      <SearchableSelect
        label="Plan türü"
        value={d.isUnplanned}
        options={[
          { value: "", label: "Tümü" },
          { value: "false", label: "Yıllık planlı" },
          { value: "true", label: "Plansız" },
        ]}
        onChange={(v) => set("isUnplanned", v ?? "")}
      />
      <TextField
        size="small"
        type="date"
        label="Başlangıçtan"
        value={d.startFrom}
        onChange={(e) => set("startFrom", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        size="small"
        type="date"
        label="Başlangıca kadar"
        value={d.startTo}
        onChange={(e) => set("startTo", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </AdvancedFilterPanel>
  );
}
function statusLabel(s: string) {
  return (
    (
      {
        AnnualPlan: "Yıllık plan",
        Preparation: "Hazırlık",
        PlanApproval: "Plan onayı",
        Execution: "Uygulama",
        Findings: "Bulgular",
        ResponseAction: "Cevap / aksiyon",
        CapaVerification: "DÖF / doğrulama",
        FindingClosure: "Bulgu kapanışı",
        AuditClosure: "Denetim kapanışı",
        Closed: "Kapalı",
      } as Record<string, string>
    )[s] ?? s
  );
}
