import { useState } from "react";
import { TextField } from "@mui/material";
import { AdvancedFilterPanel } from "../../components/AdvancedFilterPanel";
import { SearchableSelect } from "../../components/SearchableSelect";
import {
  emptyExternalAuditFilters,
  toExternalAuditColumnFilters,
  type ExternalAuditFilterState,
} from "./externalAuditFilterModel";
const states = [
  "Notified",
  "Preparation",
  "AuditInProgress",
  "Findings",
  "ResponsePlan",
  "CapaAction",
  "ClosureLetter",
  "AuthorityClosure",
  "Closed",
];
export function ExternalAuditFilters({
  value,
  onApply,
}: {
  value: ExternalAuditFilterState;
  onApply: (v: ExternalAuditFilterState) => void;
}) {
  const [d, setD] = useState(value);
  const set = (k: keyof ExternalAuditFilterState, v: string) =>
    setD((x) => ({ ...x, [k]: v }));
  return (
    <AdvancedFilterPanel
      activeCount={toExternalAuditColumnFilters(d).length}
      onClear={() => {
        setD(emptyExternalAuditFilters);
        onApply(emptyExternalAuditFilters);
      }}
      onApply={() => onApply(d)}
    >
      {[
        ["recordNumber", "Kayıt numarası içerir"],
        ["title", "Başlık içerir"],
        ["auditorOrganization", "Denetleyen kurum içerir"],
        ["auditKind", "Denetim türü içerir"],
        ["authorityCountry", "Ülke içerir"],
        ["owner", "Koordinatör içerir"],
      ].map(([k, l]) => (
        <TextField
          key={k}
          size="small"
          label={l}
          value={d[k as keyof ExternalAuditFilterState]}
          onChange={(e) =>
            set(k as keyof ExternalAuditFilterState, e.target.value)
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
        label="Denetleyen taraf"
        value={d.government}
        options={[
          { value: "", label: "Tümü" },
          { value: "true", label: "Devlet kurumu" },
          { value: "false", label: "Müşteri / belgelendirme" },
        ]}
        onChange={(v) => set("government", v ?? "")}
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
        Notified: "Planlandı / bildirildi",
        Preparation: "Hazırlık",
        AuditInProgress: "Denetim",
        Findings: "Bulgular",
        ResponsePlan: "Cevap planı",
        CapaAction: "DÖF / aksiyon",
        ClosureLetter: "Kapanış mektubu",
        AuthorityClosure: "Yetkili kapanışı",
        Closed: "Kapalı",
      } as Record<string, string>
    )[s] ?? s
  );
}
