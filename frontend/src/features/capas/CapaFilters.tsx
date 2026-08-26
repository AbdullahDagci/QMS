import { useState } from "react";
import { TextField } from "@mui/material";
import { AdvancedFilterPanel } from "../../components/AdvancedFilterPanel";
import {
  SearchableSelect,
  type SelectOption,
} from "../../components/SearchableSelect";
import {
  emptyCapaFilters,
  toCapaColumnFilters,
  type CapaFilterState,
} from "./capaFilterModel";

export function CapaFilters({
  value,
  statusOptions,
  onApply,
}: {
  value: CapaFilterState;
  statusOptions: Array<SelectOption<string>>;
  onApply: (value: CapaFilterState) => void;
}) {
  const [draft, setDraft] = useState(value);
  const update = <K extends keyof CapaFilterState>(
    field: K,
    next: CapaFilterState[K],
  ) => setDraft((current) => ({ ...current, [field]: next }));

  return (
    <AdvancedFilterPanel
      activeCount={toCapaColumnFilters(draft).length}
      onClear={() => {
        setDraft(emptyCapaFilters);
        onApply(emptyCapaFilters);
      }}
      onApply={() => onApply(draft)}
    >
      <TextField
        size="small"
        label="DÖF numarası içerir"
        value={draft.recordNumber}
        onChange={(event) => update("recordNumber", event.target.value)}
      />
      <TextField
        size="small"
        label="Kaynak kayıt içerir"
        value={draft.sourceRecordNumber}
        onChange={(event) => update("sourceRecordNumber", event.target.value)}
      />
      <TextField
        size="small"
        label="Başlık içerir"
        value={draft.title}
        onChange={(event) => update("title", event.target.value)}
      />
      <TextField
        size="small"
        label="Sorumlu içerir"
        value={draft.owner}
        onChange={(event) => update("owner", event.target.value)}
      />
      <SearchableSelect
        label="Durum"
        value={draft.status}
        options={[{ value: "", label: "Tüm durumlar" }, ...statusOptions]}
        onChange={(next) => update("status", next ?? "")}
      />
      <TextField
        size="small"
        label="Hedef tarih başlangıç"
        type="date"
        value={draft.targetFrom}
        onChange={(event) => update("targetFrom", event.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        size="small"
        label="Hedef tarih bitiş"
        type="date"
        value={draft.targetTo}
        onChange={(event) => update("targetTo", event.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </AdvancedFilterPanel>
  );
}
