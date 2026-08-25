import { useState } from "react";
import { TextField } from "@mui/material";
import { AdvancedFilterPanel } from "../../components/AdvancedFilterPanel";
import { SearchableSelect } from "../../components/SearchableSelect";
import {
  emptyComplaintFilters,
  toComplaintColumnFilters,
  type ComplaintFilterState,
} from "./complaintFilterModel";
export function ComplaintFilters({
  value,
  onApply,
}: {
  value: ComplaintFilterState;
  onApply: (v: ComplaintFilterState) => void;
}) {
  const [d, setD] = useState(value);
  const set = (k: keyof ComplaintFilterState, v: string) =>
    setD((x) => ({ ...x, [k]: v }));
  return (
    <AdvancedFilterPanel
      activeCount={toComplaintColumnFilters(d).length}
      onClear={() => {
        setD(emptyComplaintFilters);
        onApply(emptyComplaintFilters);
      }}
      onApply={() => onApply(d)}
    >
      <TextField
        size="small"
        label="Kayıt numarası içerir"
        value={d.recordNumber}
        onChange={(e) => set("recordNumber", e.target.value)}
      />
      <TextField
        size="small"
        label="Müşteri içerir"
        value={d.customerName}
        onChange={(e) => set("customerName", e.target.value)}
      />
      <TextField
        size="small"
        label="Ürün içerir"
        value={d.product}
        onChange={(e) => set("product", e.target.value)}
      />
      <TextField
        size="small"
        label="Batch / seri içerir"
        value={d.batchNumber}
        onChange={(e) => set("batchNumber", e.target.value)}
      />
      <TextField
        size="small"
        label="Şikâyet türü içerir"
        value={d.complaintType}
        onChange={(e) => set("complaintType", e.target.value)}
      />
      <TextField
        size="small"
        label="Sorumlu içerir"
        value={d.owner}
        onChange={(e) => set("owner", e.target.value)}
      />
      <SearchableSelect
        label="Önem derecesi"
        value={d.severity}
        options={[
          { value: "", label: "Tüm dereceler" },
          { value: "Minor", label: "Minör" },
          { value: "Major", label: "Majör" },
          { value: "Critical", label: "Kritik" },
        ]}
        onChange={(v) => set("severity", v ?? "")}
      />
      <SearchableSelect
        label="Durum"
        value={d.status}
        options={[
          { value: "", label: "Tüm durumlar" },
          ...[
            "Received",
            "Triage",
            "PreliminaryResponse",
            "Investigation",
            "ImpactAssessment",
            "CapaDecision",
            "FinalResponseApproval",
            "Closed",
          ].map((value) => ({ value, label: statusLabel(value) })),
        ]}
        onChange={(v) => set("status", v ?? "")}
      />
      <SearchableSelect
        label="Advers olay şüphesi"
        value={d.adverse}
        options={[
          { value: "", label: "Tümü" },
          { value: "true", label: "Var" },
          { value: "false", label: "Yok" },
        ]}
        onChange={(v) => set("adverse", v ?? "")}
      />
      <SearchableSelect
        label="Trend sinyali"
        value={d.trend}
        options={[
          { value: "", label: "Tümü" },
          { value: "true", label: "Sinyal var" },
          { value: "false", label: "Sinyal yok" },
        ]}
        onChange={(v) => set("trend", v ?? "")}
      />
      <TextField
        size="small"
        type="date"
        label="Nihai yanıt hedef başlangıç"
        value={d.dueFrom}
        onChange={(e) => set("dueFrom", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        size="small"
        type="date"
        label="Nihai yanıt hedef bitiş"
        value={d.dueTo}
        onChange={(e) => set("dueTo", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </AdvancedFilterPanel>
  );
}
const statusLabel = (v: string) =>
  ({
    Received: "Alındı",
    Triage: "Triyaj",
    PreliminaryResponse: "Ön yanıt",
    Investigation: "Araştırma",
    ImpactAssessment: "Etki değerlendirmesi",
    CapaDecision: "DÖF kararı",
    FinalResponseApproval: "Nihai yanıt",
    Closed: "Kapalı",
  })[v] ?? v;
