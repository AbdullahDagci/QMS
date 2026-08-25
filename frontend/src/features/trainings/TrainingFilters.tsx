import { useState } from "react";
import { TextField } from "@mui/material";
import { AdvancedFilterPanel } from "../../components/AdvancedFilterPanel";
import { SearchableSelect } from "../../components/SearchableSelect";
import {
  emptyTrainingFilters,
  toTrainingColumnFilters,
  type TrainingFilterState,
} from "./trainingFilterModel";
export function TrainingFilters({
  value,
  statuses,
  onApply,
}: {
  value: TrainingFilterState;
  statuses: Array<{ value: string; label: string }>;
  onApply: (value: TrainingFilterState) => void;
}) {
  const [draft, setDraft] = useState(value);
  const set = <K extends keyof TrainingFilterState>(
    key: K,
    next: TrainingFilterState[K],
  ) => setDraft((current) => ({ ...current, [key]: next }));
  return (
    <AdvancedFilterPanel
      activeCount={toTrainingColumnFilters(draft).length}
      onClear={() => {
        setDraft(emptyTrainingFilters);
        onApply(emptyTrainingFilters);
      }}
      onApply={() => onApply(draft)}
    >
      <TextField
        size="small"
        label="Kayıt numarası içerir"
        value={draft.recordNumber}
        onChange={(e) => set("recordNumber", e.target.value)}
      />
      <TextField
        size="small"
        label="Çalışan içerir"
        value={draft.employeeName}
        onChange={(e) => set("employeeName", e.target.value)}
      />
      <TextField
        size="small"
        label="Pozisyon içerir"
        value={draft.position}
        onChange={(e) => set("position", e.target.value)}
      />
      <TextField
        size="small"
        label="Eğitim kodu içerir"
        value={draft.courseCode}
        onChange={(e) => set("courseCode", e.target.value)}
      />
      <TextField
        size="small"
        label="Eğitim adı içerir"
        value={draft.courseTitle}
        onChange={(e) => set("courseTitle", e.target.value)}
      />
      <TextField
        size="small"
        label="Doküman kodu içerir"
        value={draft.documentCode}
        onChange={(e) => set("documentCode", e.target.value)}
      />
      <SearchableSelect
        label="Durum"
        value={draft.status}
        options={[{ value: "", label: "Tüm durumlar" }, ...statuses]}
        onChange={(v) => set("status", v ?? "")}
      />
      <SearchableSelect
        label="Değerlendirme"
        value={draft.assessmentMode}
        options={[
          { value: "", label: "Tüm yöntemler" },
          { value: "ReadAndAcknowledge", label: "Oku ve anla" },
          { value: "Exam", label: "Sınav" },
          { value: "Practical", label: "Pratik" },
          { value: "ExamAndPractical", label: "Sınav + pratik" },
        ]}
        onChange={(v) => set("assessmentMode", v ?? "")}
      />
      <SearchableSelect
        label="Kritik yeterlilik"
        value={draft.critical}
        options={[
          { value: "", label: "Tümü" },
          { value: "true", label: "Kritik" },
          { value: "false", label: "Standart" },
        ]}
        onChange={(v) => set("critical", v ?? "")}
      />
      <TextField
        size="small"
        type="date"
        label="Hedef başlangıç"
        value={draft.dueFrom}
        onChange={(e) => set("dueFrom", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <TextField
        size="small"
        type="date"
        label="Hedef bitiş"
        value={draft.dueTo}
        onChange={(e) => set("dueTo", e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </AdvancedFilterPanel>
  );
}
