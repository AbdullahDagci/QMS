import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AddRounded,
  FactCheckRounded,
  HistoryRounded,
  HubRounded,
  LockRounded,
  PlaylistAddCheckRounded,
  ReportProblemRounded,
  RuleRounded,
  SettingsRounded,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  FormControlLabel,
  Paper,
  Stack,
  Step,
  StepLabel,
  Stepper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import {
  addAuditFinding,
  answerAuditQuestion,
  closeAuditFinding,
  createInternalAudit,
  createInternalAuditLookupDefinition,
  downloadInternalAuditFinalReport,
  getInternalAuditDetails,
  getInternalAuditOptions,
  getInternalAuditLookupDefinitions,
  respondAuditFinding,
  searchInternalAudits,
  transitionInternalAudit,
  updateInternalAuditLookupDefinition,
  type CreateInternalAuditInput,
  type InternalAuditDetails,
  type InternalAuditListItem,
} from "../../api/internalAudits";
import { AdvancedFilterButton } from "../../components/AdvancedFilterPanel";
import { AuditTimeline } from "../../components/AuditTimeline";
import { ModalHeader } from "../../components/ModalHeader";
import { RecordActionMenu } from "../../components/RecordActionMenu";
import {
  ModuleGuideDialog,
  ModuleInfoButton,
} from "../../components/ModuleGuideDialog";
import { RecordAssignments } from "../../components/RecordAssignments";
import { SearchableSelect } from "../../components/SearchableSelect";
import { TableLoadingRows } from "../../components/TableLoadingRows";
import { Permissions, useAuth } from "../../security/AuthContext";
import { InternalAuditFilters } from "./InternalAuditFilters";
import {
  emptyInternalAuditFilters,
  toInternalAuditColumnFilters,
  type InternalAuditFilterState,
} from "./internalAuditFilterModel";

const statusLabels: Record<string, string> = {
  AnnualPlan: "Yıllık plan",
  Preparation: "Denetim hazırlığı",
  PlanApproval: "Plan onayı",
  Execution: "Uygulama",
  Findings: "Bulgular",
  ResponseAction: "Cevap / aksiyon",
  CapaVerification: "DÖF / doğrulama",
  FindingClosure: "Bulgu kapanışı",
  AuditClosure: "Denetim kapanışı",
  Closed: "Kapalı",
  Cancelled: "İptal",
};
const stages = [
  "Yıllık plan",
  "Hazırlık",
  "Plan onayı",
  "Uygulama",
  "Bulgular",
  "Cevap / aksiyon",
  "DÖF / doğrulama",
  "Bulgu kapanışı",
  "Denetim kapanışı",
  "Kapalı",
];
const stageStatus = [
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
const classification: Record<string, string> = {
  Critical: "Kritik",
  Major: "Majör",
  Minor: "Minör",
  Observation: "Gözlem",
};
const answerLabels: Record<string, string> = {
  Pending: "Bekliyor",
  Conform: "Uygun",
  Nonconform: "Uygunsuz",
  NotApplicable: "Uygulanamaz",
};
const auditEventLabels: Record<string, string> = {
  InternalAuditCreated: "İç denetim planı oluşturuldu",
  InternalAuditStatusChanged: "Denetim aşaması değiştirildi",
  AuditQuestionAnswered: "Soru ve objektif kanıt kaydedildi",
  AuditFindingCreated: "Risk sınıflı bulgu açıldı",
  AuditFindingResponseSubmitted: "Bulgu yanıtı ve aksiyonu kaydedildi",
  AuditFindingClosed: "Bulgu doğrulanarak kapatıldı",
};
const dt = (v: string | null) =>
  v
    ? new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(v))
    : "—";

export function InternalAuditWorkspace() {
  const [params, setParams] = useSearchParams();
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState<InternalAuditFilterState>(
    emptyInternalAuditFilters,
  );
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25);
  const [sortBy, setSortBy] = useState("createdAtUtc");
  const [direction, setDirection] = useState<"asc" | "desc">("desc");
  const [createOpen, setCreateOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  const [lookupOpen, setLookupOpen] = useState(false);
  const { can } = useAuth();
  const columnFilters = useMemo(
    () => toInternalAuditColumnFilters(filters),
    [filters],
  );
  const request = useMemo(
    () => ({
      page: page + 1,
      pageSize,
      sortBy,
      sortDirection: direction,
      filters: columnFilters,
    }),
    [page, pageSize, sortBy, direction, columnFilters],
  );
  const query = useQuery({
    queryKey: ["internal-audits", request],
    queryFn: ({ signal }) => searchInternalAudits(request, signal),
    placeholderData: (p) => p,
    retry: false,
  });
  const optionsQuery = useQuery({
    queryKey: ["internal-audit-options"],
    queryFn: ({ signal }) => getInternalAuditOptions(signal),
    retry: false,
  });
  const items = query.data?.items ?? [];
  const sort = (field: string) => {
    if (field === sortBy) setDirection((x) => (x === "asc" ? "desc" : "asc"));
    else {
      setSortBy(field);
      setDirection("asc");
    }
    setPage(0);
  };
  return (
    <Box component="section" className="module-unified-page">
      <Stack
        direction={{ xs: "column", sm: "row" }}
        sx={{
          justifyContent: "space-between",
          alignItems: { sm: "center" },
          gap: 2,
        }}
      >
        <Box>
          <Stack direction="row" spacing={1.2} sx={{ alignItems: "center" }}>
            <Box className="section-heading-icon tone-teal">
              <FactCheckRounded />
            </Box>
            <Typography variant="h2">İç denetimler</Typography>
            <Chip size="small" color="success" label="M.07 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Plan, kilitli soru listesi, kanıt, risk sınıflı bulgu ve M.02 DÖF
            kapanışını tek denetim zincirinde yönetin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2}>
          <ModuleInfoButton module="M.07" onClick={() => setGuideOpen(true)} />
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setLookupOpen(true)}
            >
              Denetim tanımları
            </Button>
          )}
          {can(Permissions.internalAuditPlan) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
            >
              Yeni denetim
            </Button>
          )}
        </Stack>
      </Stack>
      <InternalAuditLookupDialog
        open={lookupOpen}
        onClose={() => setLookupOpen(false)}
      />
      <Box className="complaint-kpi-grid">
        <Kpi
          icon={<FactCheckRounded />}
          label="Görünen denetim"
          value={query.data?.totalCount ?? 0}
        />
        <Kpi
          icon={<ReportProblemRounded />}
          label="Açık bulgu"
          value={items.reduce((n, x) => n + x.openFindingCount, 0)}
          tone="rose"
        />
        <Kpi
          icon={<HubRounded />}
          label="Plansız"
          value={items.filter((x) => x.isUnplanned).length}
          tone="amber"
        />
        <Kpi
          icon={<LockRounded />}
          label="Kapanan"
          value={items.filter((x) => x.status === "Closed").length}
          tone="teal"
        />
      </Box>
      <Stack
        className="module-list-toolbar"
        direction="row"
        spacing={1.5}
        sx={{ mt: 2.5, alignItems: "center" }}
      >
        <AdvancedFilterButton
          open={filtersOpen}
          activeCount={columnFilters.length}
          onClick={() => setFiltersOpen((x) => !x)}
        />
        <Typography variant="body2" color="text.secondary">
          {query.data
            ? `${query.data.totalCount} iç denetim kaydı`
            : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>
      {filtersOpen && (
        <InternalAuditFilters
          value={filters}
          onApply={(v) => {
            setFilters(v);
            setPage(0);
          }}
        />
      )}
      <Paper className="deviation-table-card" elevation={0} sx={{ mt: 2.5 }}>
        {query.isError && (
          <Alert severity="error" sx={{ m: 2 }}>
            {query.error.message}
          </Alert>
        )}
        <TableContainer>
          <Table aria-label="İç denetim iş listesi">
            <TableHead>
              <TableRow>
                <Sort
                  field="recordNumber"
                  text="Kayıt"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <Sort
                  field="title"
                  text="Denetim / kapsam"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell>Denetlenen / denetçi</TableCell>
                <Sort
                  field="plannedStartUtc"
                  text="Takvim"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell>Bulgular</TableCell>
                <Sort
                  field="status"
                  text="Durum"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell align="right">İşlem</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {query.isFetching ? (
                <TableLoadingRows columns={7} rows={6} />
              ) : (
                items.map((item) => (
                  <AuditRow
                    key={item.id}
                    item={item}
                    auditTypeName={
                      optionsQuery.data?.auditTypes.find(
                        (x) => x.code === item.auditType,
                      )?.name ?? item.auditType
                    }
                    onOpen={() => setParams({ open: item.id })}
                  />
                ))
              )}
              {!query.isFetching && !items.length && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Alert severity="info" sx={{ my: 2 }}>
                      Filtrelerle eşleşen denetim bulunamadı.
                    </Alert>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={query.data?.totalCount ?? 0}
          page={page}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage="Sayfa başına"
          onPageChange={(_, p) => setPage(p)}
          onRowsPerPageChange={(e) => {
            setPageSize(Number(e.target.value) as 10 | 25 | 50 | 100);
            setPage(0);
          }}
        />
      </Paper>
      <CreateAuditDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(d) => {
          setCreateOpen(false);
          setParams({ open: d.record.id });
        }}
      />
      <AuditDetailsDialog
        id={params.get("open")}
        onClose={() => setParams({})}
      />
      <ModuleGuideDialog
        open={guideOpen}
        module="M.07"
        onClose={() => setGuideOpen(false)}
      />
    </Box>
  );
}

function Kpi({
  icon,
  label,
  value,
  tone = "indigo",
}: {
  icon: ReactNode;
  label: string;
  value: number;
  tone?: string;
}) {
  return (
    <Paper elevation={0} className="complaint-kpi-card">
      <Box className={`section-heading-icon tone-${tone}`}>{icon}</Box>
      <Box>
        <Typography variant="h4">{value}</Typography>
        <Typography variant="caption" color="text.secondary">
          {label}
        </Typography>
      </Box>
    </Paper>
  );
}
function Sort({
  field,
  text,
  current,
  direction,
  onSort,
}: {
  field: string;
  text: string;
  current: string;
  direction: "asc" | "desc";
  onSort: (f: string) => void;
}) {
  return (
    <TableCell>
      <TableSortLabel
        active={current === field}
        direction={current === field ? direction : "asc"}
        onClick={() => onSort(field)}
      >
        {text}
      </TableSortLabel>
    </TableCell>
  );
}
function AuditRow({
  item,
  auditTypeName,
  onOpen,
}: {
  item: InternalAuditListItem;
  auditTypeName: string;
  onOpen: () => void;
}) {
  return (
    <TableRow hover onClick={onOpen} sx={{ cursor: "pointer" }}>
      <TableCell>
        <Typography color="primary" sx={{ fontWeight: 800 }}>
          {item.recordNumber}
        </Typography>
        <Chip
          size="small"
          variant="outlined"
          label={item.isUnplanned ? "Plansız" : `${item.planYear} planı`}
          sx={{ mt: 0.7 }}
        />
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 760 }}>{item.title}</Typography>
        <Typography variant="caption" color="text.secondary">
          {auditTypeName}
        </Typography>
      </TableCell>
      <TableCell>
        {item.auditeeDepartment}
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: "block" }}
        >
          {item.leadAuditor}
        </Typography>
      </TableCell>
      <TableCell>
        {dt(item.plannedStartUtc)}
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: "block" }}
        >
          Bitiş: {dt(item.plannedEndUtc)}
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={item.openFindingCount ? "warning" : "success"}
          label={`${item.openFindingCount} açık / ${item.findingCount}`}
        />
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={item.status === "Closed" ? "success" : "primary"}
          label={statusLabels[item.status] ?? item.status}
        />
      </TableCell>
      <TableCell align="right">
        <RecordActionMenu onOpen={onOpen} />
      </TableCell>
    </TableRow>
  );
}

function InternalAuditLookupDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [sortOrder, setSortOrder] = useState(100);
  const q = useQuery({
    queryKey: ["internal-audit-lookups"],
    queryFn: getInternalAuditLookupDefinitions,
    enabled: open,
    retry: false,
  });
  const refresh = async () => {
    await qc.invalidateQueries({ queryKey: ["internal-audit-lookups"] });
    await qc.invalidateQueries({ queryKey: ["internal-audit-options"] });
  };
  const create = useMutation({
    mutationFn: () =>
      createInternalAuditLookupDefinition({
        category: "AuditType",
        code,
        name,
        sortOrder,
      }),
    onSuccess: async () => {
      setCode("");
      setName("");
      await refresh();
    },
  });
  const update = useMutation({
    mutationFn: (x: {
      id: string;
      name: string;
      sortOrder: number;
      isActive: boolean;
    }) => updateInternalAuditLookupDefinition(x.id, x),
    onSuccess: refresh,
  });
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.07 · YÖNETİLEN LOOKUP</Typography>
          <Typography variant="h5">İç denetim tanımları</Typography>
          <Typography color="text.secondary">
            Kod geçmiş kayıtlarda değişmez snapshot olarak korunur; görünen ad
            ve aktiflik yeni seçimleri yönetir.
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.2}
          sx={{ mb: 2 }}
        >
          <TextField
            required
            label="Değişmez kod"
            value={code}
            onChange={(e) => setCode(e.target.value)}
          />
          <TextField
            required
            fullWidth
            label="Görünen ad"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          <TextField
            type="number"
            label="Sıra"
            value={sortOrder}
            onChange={(e) => setSortOrder(Number(e.target.value))}
            sx={{ width: 100 }}
          />
          <Button
            variant="contained"
            disabled={!code || !name || create.isPending}
            onClick={() => create.mutate()}
          >
            Ekle
          </Button>
        </Stack>
        {(q.isError || create.isError || update.isError) && (
          <Alert severity="error">
            {q.error?.message ?? create.error?.message ?? update.error?.message}
          </Alert>
        )}
        <Stack spacing={1}>
          {q.data?.map((x) => (
            <InternalAuditLookupRow
              key={x.id}
              item={x}
              saving={update.isPending}
              save={(name, order, active) =>
                update.mutate({
                  id: x.id,
                  name,
                  sortOrder: order,
                  isActive: active,
                })
              }
            />
          ))}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button variant="outlined" onClick={onClose}>
          Kapat
        </Button>
      </DialogActions>
    </Dialog>
  );
}
function InternalAuditLookupRow({
  item,
  saving,
  save,
}: {
  item: { code: string; name: string; sortOrder: number; isActive: boolean };
  saving: boolean;
  save: (name: string, order: number, active: boolean) => void;
}) {
  const [name, setName] = useState(item.name);
  const [order, setOrder] = useState(item.sortOrder);
  const [active, setActive] = useState(item.isActive);
  return (
    <Paper variant="outlined" sx={{ p: 1.5 }}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={1.2}
        sx={{ alignItems: { md: "center" } }}
      >
        <Typography sx={{ minWidth: 150, fontFamily: "monospace" }}>
          {item.code}
        </Typography>
        <TextField
          size="small"
          fullWidth
          label="Görünen ad"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <TextField
          size="small"
          type="number"
          label="Sıra"
          value={order}
          onChange={(e) => setOrder(Number(e.target.value))}
          sx={{ width: 90 }}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={active}
              onChange={(e) => setActive(e.target.checked)}
            />
          }
          label="Aktif"
        />
        <Button
          variant="outlined"
          disabled={!name || saving}
          onClick={() => save(name, order, active)}
        >
          Kaydet
        </Button>
      </Stack>
    </Paper>
  );
}

function CreateAuditDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (d: InternalAuditDetails) => void;
}) {
  const now = new Date();
  const plus = new Date(now.getTime() + 7 * 86400000);
  const [form, setForm] = useState<CreateInternalAuditInput>({
    planYear: now.getFullYear(),
    title: "",
    auditType: "",
    scope: "",
    objectives: "",
    criteria: "",
    auditeeDepartment: "",
    auditeeDepartmentId: "",
    leadAuditorUserId: "",
    leadAuditor: "",
    leadAuditorDepartment: "",
    plannedStartUtc: now.toISOString().slice(0, 16),
    plannedEndUtc: plus.toISOString().slice(0, 16),
    isUnplanned: false,
    unplannedReason: null,
    checklistVersion: `${now.getFullYear()}.1`,
    questions: [],
  });
  const optionsQuery = useQuery({
    queryKey: ["internal-audit-options"],
    queryFn: ({ signal }) => getInternalAuditOptions(signal),
    enabled: open,
    retry: false,
  });
  useEffect(() => {
    const first = optionsQuery.data?.auditTypes[0];
    if (first && !form.auditType) set("auditType", first.code);
  }, [optionsQuery.data, form.auditType]);
  const [questions, setQuestions] = useState(
    form.questions.map((x) => `${x.question} | ${x.reference}`).join("\n"),
  );
  const qc = useQueryClient();
  const mutation = useMutation({
    mutationFn: createInternalAudit,
    onSuccess: (d) => {
      qc.invalidateQueries({ queryKey: ["internal-audits"] });
      onCreated(d);
    },
  });
  const set = <K extends keyof CreateInternalAuditInput>(
    k: K,
    v: CreateInternalAuditInput[K],
  ) => setForm((x) => ({ ...x, [k]: v }));
  const parsed = questions
    .split("\n")
    .map((x) => x.trim())
    .filter(Boolean)
    .map((line) => {
      const [q, ...r] = line.split("|");
      return {
        question: q.trim(),
        reference: r.join("|").trim() || "Şirket standardı",
      };
    });
  const valid =
    form.title &&
    form.scope &&
    form.objectives &&
    form.auditeeDepartmentId &&
    form.leadAuditorUserId &&
    parsed.length > 0 &&
    (!form.isUnplanned || form.unplannedReason);
  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="md"
      slotProps={{ paper: { className: "standard-modal-paper" } }}
    >
      <ModalHeader onClose={onClose}>
        <Typography variant="overline">M.07 · KONTROLLÜ PLANLAMA</Typography>
        <Typography variant="h4">Yeni iç denetim</Typography>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
          <Box className="form-grid two-column">
            <TextField
              label="Denetim başlığı"
              value={form.title}
              onChange={(e) => set("title", e.target.value)}
            />
            <SearchableSelect
              label="Denetim türü"
              value={form.auditType}
              options={(optionsQuery.data?.auditTypes ?? []).map((x) => ({
                value: x.code,
                label: x.name,
              }))}
              onChange={(v) => set("auditType", v ?? "")}
            />
            <SearchableSelect
              required
              label="Denetlenen bölüm"
              value={form.auditeeDepartmentId}
              options={(optionsQuery.data?.departments ?? []).map((x) => ({
                value: x.id,
                label: x.name,
              }))}
              onChange={(v) => {
                const x = optionsQuery.data?.departments.find(
                  (y) => y.id === v,
                );
                setForm((f) => ({
                  ...f,
                  auditeeDepartmentId: v ?? "",
                  auditeeDepartment: x?.name ?? "",
                }));
              }}
            />
            <TextField
              label="Plan yılı"
              type="number"
              value={form.planYear}
              onChange={(e) => set("planYear", Number(e.target.value))}
            />
            <SearchableSelect
              required
              label="Baş denetçi"
              value={form.leadAuditorUserId}
              options={(optionsQuery.data?.leadAuditors ?? [])
                .filter((x) => x.department !== form.auditeeDepartment)
                .map((x) => ({
                  value: x.id,
                  label: `${x.name} · ${x.department ?? "Bölümsüz"}`,
                }))}
              onChange={(v) => {
                const x = optionsQuery.data?.leadAuditors.find(
                  (y) => y.id === v,
                );
                setForm((f) => ({
                  ...f,
                  leadAuditorUserId: v ?? "",
                  leadAuditor: x?.name ?? "",
                  leadAuditorDepartment: x?.department ?? "",
                }));
              }}
            />
            <TextField
              type="datetime-local"
              label="Planlanan başlangıç"
              value={form.plannedStartUtc}
              onChange={(e) => set("plannedStartUtc", e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              type="datetime-local"
              label="Planlanan bitiş"
              value={form.plannedEndUtc}
              onChange={(e) => set("plannedEndUtc", e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Box>
          <TextField
            multiline
            minRows={2}
            label="Kapsam"
            value={form.scope}
            onChange={(e) => set("scope", e.target.value)}
          />
          <TextField
            multiline
            minRows={2}
            label="Amaçlar"
            value={form.objectives}
            onChange={(e) => set("objectives", e.target.value)}
          />
          <TextField
            label="Kriterler"
            value={form.criteria}
            onChange={(e) => set("criteria", e.target.value)}
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={form.isUnplanned}
                onChange={(e) => set("isUnplanned", e.target.checked)}
              />
            }
            label="Plansız / gerekçeli denetim"
          />
          {form.isUnplanned && (
            <TextField
              required
              label="Plansız denetim gerekçesi"
              value={form.unplannedReason ?? ""}
              onChange={(e) => set("unplannedReason", e.target.value)}
            />
          )}
          <TextField
            label="Soru listesi sürümü"
            value={form.checklistVersion}
            onChange={(e) => set("checklistVersion", e.target.value)}
          />
          <TextField
            multiline
            minRows={5}
            label="Soru listesi — her satır: Soru | Referans"
            value={questions}
            onChange={(e) => setQuestions(e.target.value)}
            helperText="Plan onaylandığında bu sürüm ve sorular kilitlenir."
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={!valid || mutation.isPending}
          onClick={() =>
            mutation.mutate({
              ...form,
              questions: parsed,
              plannedStartUtc: new Date(form.plannedStartUtc).toISOString(),
              plannedEndUtc: new Date(form.plannedEndUtc).toISOString(),
            })
          }
        >
          Denetim oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function AuditDetailsDialog({
  id,
  onClose,
}: {
  id: string | null;
  onClose: () => void;
}) {
  const [tab, setTab] = useState(0);
  const query = useQuery({
    queryKey: ["internal-audit", id],
    queryFn: ({ signal }) => getInternalAuditDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const qc = useQueryClient();
  const update = (d: InternalAuditDetails) => {
    qc.setQueryData(["internal-audit", id], d);
    qc.invalidateQueries({ queryKey: ["internal-audits"] });
  };
  const r = query.data?.record;
  const findings = query.data?.findings ?? [];
  const openFindings = findings.filter(
    (finding) => finding.status !== "Closed",
  ).length;
  return (
    <Dialog
      open={Boolean(id)}
      onClose={onClose}
      fullScreen
      slotProps={{ paper: { className: "standard-modal-paper" } }}
    >
      {r && (
        <>
          <ModalHeader onClose={onClose}>
            <Stack
              direction={{ xs: "column", md: "row" }}
              sx={{ justifyContent: "space-between", gap: 2, pr: 7 }}
            >
              <Box>
                <Stack
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: "center" }}
                >
                  <Typography variant="overline">{r.recordNumber}</Typography>
                  <Chip
                    size="small"
                    label={r.isUnplanned ? "PLANSIZ DENETİM" : "İÇ DENETİM"}
                  />
                </Stack>
                <Typography variant="h4">{r.title}</Typography>
              </Box>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                {r.status === "Closed" && (
                  <Button
                    variant="outlined"
                    onClick={() => downloadInternalAuditFinalReport(r.id)}
                  >
                    Nihai PDF
                  </Button>
                )}
                <Chip
                  icon={<RuleRounded />}
                  label={statusLabels[r.status] ?? r.status}
                />
                <Chip
                  color={openFindings ? "warning" : "success"}
                  label={`${openFindings} açık bulgu`}
                />
              </Stack>
            </Stack>
          </ModalHeader>
          <Tabs
            value={tab}
            onChange={(_, v) => setTab(v)}
            variant="scrollable"
            className="modal-sticky-tabs"
          >
            <Tab
              icon={<FactCheckRounded />}
              iconPosition="start"
              label="Genel bakış"
            />
            <Tab
              icon={<PlaylistAddCheckRounded />}
              iconPosition="start"
              label={`Soru listesi (${query.data!.checklist.length})`}
            />
            <Tab
              icon={<ReportProblemRounded />}
              iconPosition="start"
              label={`Bulgular (${findings.length})`}
            />
            <Tab
              icon={<HubRounded />}
              iconPosition="start"
              label="Roller ve bağlar"
            />
            <Tab
              icon={<HistoryRounded />}
              iconPosition="start"
              label={`Geçmiş (${query.data!.auditTrail.length})`}
            />
          </Tabs>
          <DialogContent dividers className="detail-modal-content">
            {tab === 0 && <Overview details={query.data!} />}{" "}
            {tab === 1 && <Checklist details={query.data!} update={update} />}{" "}
            {tab === 2 && <Findings details={query.data!} update={update} />}{" "}
            {tab === 3 && (
              <Stack spacing={2}>
                <RecordAssignments
                  aggregateType="InternalAudit"
                  aggregateId={r.id}
                />
                <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}>
                  <Typography variant="h6" sx={{ mb: 1.5 }}>
                    Elektronik imzalar
                  </Typography>
                  {query.data!.signatures.length === 0 ? (
                    <Typography color="text.secondary">
                      Henüz elektronik imza yok.
                    </Typography>
                  ) : (
                    <Stack spacing={1}>
                      {query.data!.signatures.map((s) => (
                        <Box
                          key={s.id}
                          sx={{
                            display: "grid",
                            gridTemplateColumns: { md: "2fr 1fr 1fr" },
                            gap: 1,
                            p: 1.25,
                            borderBottom: "1px solid",
                            borderColor: "divider",
                          }}
                        >
                          <Box>
                            <Typography sx={{ fontWeight: 750 }}>
                              {s.meaning}
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Hash: {s.contentHash.slice(0, 20)}…
                            </Typography>
                          </Box>
                          <Typography>{s.signer}</Typography>
                          <Typography>
                            {dt(s.signedAtUtc)} · v{s.recordVersion}
                          </Typography>
                        </Box>
                      ))}
                    </Stack>
                  )}
                </Paper>
              </Stack>
            )}{" "}
            {tab === 4 && (
              <AuditTimeline
                events={query.data!.auditTrail}
                labels={auditEventLabels}
              />
            )}
          </DialogContent>
          <AuditActions details={query.data!} update={update} />
        </>
      )}
      {query.isLoading && (
        <DialogContent>
          <TableLoadingRows columns={1} rows={8} />
        </DialogContent>
      )}
      {query.isError && (
        <DialogContent>
          <Alert severity="error">{query.error.message}</Alert>
        </DialogContent>
      )}
    </Dialog>
  );
}

function Overview({
  details: { record: r, findings, checklist },
}: {
  details: InternalAuditDetails;
}) {
  const active = Math.max(0, stageStatus.indexOf(r.status));
  const openFindings = findings.filter(
    (finding) => finding.status !== "Closed",
  ).length;
  return (
    <Stack spacing={3}>
      <Paper
        variant="outlined"
        sx={{
          p: 2.5,
          borderRadius: 3,
          background: "linear-gradient(135deg,#f4fbf8,#f7f8ff)",
        }}
      >
        <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 2 }}>
          <Box className="section-heading-icon tone-teal">
            <HubRounded />
          </Box>
          <Box>
            <Typography sx={{ fontWeight: 850 }}>
              Kontrollü denetim zinciri
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Soru listesi kilidi ve bulgu kapanış kapıları
            </Typography>
          </Box>
        </Stack>
        <Stepper activeStep={active} alternativeLabel>
          {stages.map((x, i) => (
            <Step key={x} completed={i < active}>
              <StepLabel>{x}</StepLabel>
            </Step>
          ))}
        </Stepper>
      </Paper>
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", md: "repeat(3,1fr)" },
          gap: 2,
        }}
      >
        <Info title="Kapsam" value={r.scope} />
        <Info title="Amaç" value={r.objectives} />
        <Info title="Kriter" value={r.criteria} />
        <Info title="Denetlenen bölüm" value={r.auditeeDepartment} />
        <Info
          title="Baş denetçi / bağımsızlık"
          value={`${r.leadAuditor} · ${r.leadAuditorDepartment}${r.independenceConfirmed ? " · Doğrulandı" : " · Bekliyor"}`}
        />
        <Info
          title="Takvim"
          value={`${dt(r.plannedStartUtc)} — ${dt(r.plannedEndUtc)}`}
        />
      </Box>
      <Alert
        severity={r.checklistLockedAtUtc ? "success" : "info"}
        icon={<LockRounded />}
      >
        Soru listesi {r.checklistVersion} ·{" "}
        {r.checklistLockedAtUtc
          ? `${dt(r.checklistLockedAtUtc)} tarihinde kilitlendi`
          : "plan onayında kilitlenecek"}{" "}
        · {checklist.filter((x) => x.status !== "Pending").length}/
        {checklist.length} yanıtlandı.
      </Alert>
      {findings.length > 0 && (
        <Alert severity={openFindings ? "warning" : "success"}>
          {findings.length} bulgunun {openFindings} tanesi açık. Denetim ancak
          tüm bulgular kapandığında kapanabilir.
        </Alert>
      )}
    </Stack>
  );
}
function Info({ title, value }: { title: string; value: string }) {
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.2, borderRadius: 3, borderLeft: "4px solid #7bb9a8" }}
    >
      <Typography variant="overline" color="text.secondary">
        {title}
      </Typography>
      <Typography sx={{ fontWeight: 700, mt: 0.4 }}>{value}</Typography>
    </Paper>
  );
}

function Checklist({
  details,
  update,
}: {
  details: InternalAuditDetails;
  update: (d: InternalAuditDetails) => void;
}) {
  return (
    <Stack spacing={1.5}>
      <Alert severity="info" icon={<LockRounded />}>
        Soru listesi sürümü: <b>{details.record.checklistVersion}</b>. Plan
        onayından sonra soru metni değiştirilemez; yalnızca sonuç, kanıt ve not
        kaydedilir.
      </Alert>
      {details.checklist.map((item) => (
        <ChecklistCard
          key={item.id}
          item={item}
          details={details}
          update={update}
        />
      ))}
    </Stack>
  );
}
function ChecklistCard({
  item,
  details,
  update,
}: {
  item: InternalAuditDetails["checklist"][number];
  details: InternalAuditDetails;
  update: (d: InternalAuditDetails) => void;
}) {
  const [status, setStatus] = useState(
    item.status === "Pending" ? "Conform" : item.status,
  );
  const [evidence, setEvidence] = useState(item.evidence ?? "");
  const [note, setNote] = useState(item.note ?? "");
  const m = useMutation({
    mutationFn: () =>
      answerAuditQuestion(
        details.record.id,
        item.id,
        details.record.version,
        status,
        evidence,
        note,
      ),
    onSuccess: update,
  });
  return (
    <Paper variant="outlined" sx={{ p: 2, borderRadius: 3 }}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        sx={{ justifyContent: "space-between", gap: 2 }}
      >
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1}>
            <Chip size="small" label={item.order} />
            <Typography sx={{ fontWeight: 800 }}>{item.question}</Typography>
          </Stack>
          <Typography variant="caption" color="text.secondary">
            Referans: {item.reference}
          </Typography>
        </Box>
        <Chip
          color={
            item.status === "Conform"
              ? "success"
              : item.status === "Nonconform"
                ? "error"
                : "default"
          }
          label={answerLabels[item.status] ?? item.status}
        />
      </Stack>
      {details.record.status === "Execution" && (
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.5}
          sx={{ mt: 2 }}
        >
          <SearchableSelect
            label="Sonuç"
            value={status}
            options={["Conform", "Nonconform", "NotApplicable"].map(
              (value) => ({ value, label: answerLabels[value] }),
            )}
            onChange={(v) => setStatus(v ?? "Conform")}
          />
          <TextField
            fullWidth
            label="Kanıt / kayıt referansı"
            value={evidence}
            onChange={(e) => setEvidence(e.target.value)}
          />
          <TextField
            fullWidth
            label="Denetçi notu"
            value={note}
            onChange={(e) => setNote(e.target.value)}
          />
          <Button
            variant="contained"
            disabled={!evidence || m.isPending}
            onClick={() => m.mutate()}
          >
            Kaydet
          </Button>
        </Stack>
      )}
      {m.isError && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {m.error.message}
        </Alert>
      )}
    </Paper>
  );
}

function Findings({
  details,
  update,
}: {
  details: InternalAuditDetails;
  update: (d: InternalAuditDetails) => void;
}) {
  return (
    <Stack spacing={2}>
      {["Execution", "Findings"].includes(details.record.status) && (
        <NewFinding details={details} update={update} />
      )}{" "}
      {!details.findings.length && (
        <Alert severity="info">
          Henüz bulgu kaydedilmedi. Uygunsuz soru sonucu varsa kanıtıyla
          birlikte bulgu açın.
        </Alert>
      )}
      {details.findings.map((f) => (
        <FindingCard key={f.id} finding={f} details={details} update={update} />
      ))}
    </Stack>
  );
}
function NewFinding({
  details,
  update,
}: {
  details: InternalAuditDetails;
  update: (d: InternalAuditDetails) => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [reference, setReference] = useState("");
  const [impact, setImpact] = useState(2);
  const [likelihood, setLikelihood] = useState(2);
  const [owner, setOwner] = useState("");
  const [ownerUserId, setOwnerUserId] = useState("");
  const optionsQuery = useQuery({
    queryKey: ["internal-audit-options"],
    queryFn: ({ signal }) => getInternalAuditOptions(signal),
    retry: false,
  });
  const [capa, setCapa] = useState(false);
  const [target, setTarget] = useState(() =>
    new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 16),
  );
  const m = useMutation({
    mutationFn: () =>
      addAuditFinding(details.record.id, details.record.version, {
        title,
        description,
        requirementReference: reference,
        impact,
        likelihood,
        capaRequired: capa,
        ownerUserId,
        owner,
        targetDateUtc: new Date(target).toISOString(),
      }),
    onSuccess: (d) => {
      update(d);
      setTitle("");
      setDescription("");
    },
  });
  const score = impact * likelihood;
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.5, borderRadius: 3, background: "#fbfcfd" }}
    >
      <Typography variant="h6" sx={{ mb: 2 }}>
        Yeni denetim bulgusu
      </Typography>
      <Box className="form-grid two-column">
        <TextField
          label="Bulgu başlığı"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <TextField
          label="Gereklilik referansı"
          value={reference}
          onChange={(e) => setReference(e.target.value)}
        />
        <TextField
          multiline
          minRows={2}
          label="Bulgu ve objektif kanıt"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <SearchableSelect
          required
          label="Bulgu sorumlusu"
          value={ownerUserId}
          options={(optionsQuery.data?.findingOwners ?? []).map((x) => ({
            value: x.id,
            label: `${x.name} · ${x.department ?? "Bölümsüz"}`,
          }))}
          onChange={(v) => {
            const x = optionsQuery.data?.findingOwners.find((y) => y.id === v);
            setOwnerUserId(v ?? "");
            setOwner(x?.name ?? "");
          }}
        />
        <TextField
          type="number"
          label="Etki (1-5)"
          value={impact}
          onChange={(e) => setImpact(Number(e.target.value))}
        />
        <TextField
          type="number"
          label="Olasılık (1-5)"
          value={likelihood}
          onChange={(e) => setLikelihood(Number(e.target.value))}
        />
        <TextField
          type="datetime-local"
          label="Hedef tarih"
          value={target}
          onChange={(e) => setTarget(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={capa || score >= 12}
              disabled={score >= 12}
              onChange={(e) => setCapa(e.target.checked)}
            />
          }
          label={`DÖF gerekli · risk puanı ${score}${score >= 12 ? " (zorunlu)" : ""}`}
        />
      </Box>
      {m.isError && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {m.error.message}
        </Alert>
      )}
      <Button
        sx={{ mt: 2 }}
        variant="contained"
        startIcon={<AddRounded />}
        disabled={
          !title || !description || !reference || !ownerUserId || m.isPending
        }
        onClick={() => m.mutate()}
      >
        Bulgu ekle
      </Button>
    </Paper>
  );
}
function FindingCard({
  finding: f,
  details,
  update,
}: {
  finding: InternalAuditDetails["findings"][number];
  details: InternalAuditDetails;
  update: (d: InternalAuditDetails) => void;
}) {
  const [response, setResponse] = useState(f.response ?? "");
  const [action, setAction] = useState(f.correctiveAction ?? "");
  const [verification, setVerification] = useState(f.verificationNote ?? "");
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureAccepted, setSignatureAccepted] = useState(false);
  const respond = useMutation({
    mutationFn: () =>
      respondAuditFinding(
        details.record.id,
        f.id,
        details.record.version,
        response,
        action,
      ),
    onSuccess: update,
  });
  const close = useMutation({
    mutationFn: () =>
      closeAuditFinding(
        details.record.id,
        f.id,
        details.record.version,
        verification,
        signaturePassword,
        signatureAccepted,
      ),
    onSuccess: update,
  });
  return (
    <Paper
      variant="outlined"
      sx={{
        p: 2.5,
        borderRadius: 3,
        borderLeft: `5px solid ${f.classification === "Critical" ? "#a61e42" : f.classification === "Major" ? "#f1416c" : "#50cd89"}`,
      }}
    >
      <Stack
        direction={{ xs: "column", md: "row" }}
        sx={{ justifyContent: "space-between", gap: 2 }}
      >
        <Box>
          <Stack
            direction="row"
            spacing={1}
            sx={{ alignItems: "center", flexWrap: "wrap" }}
          >
            <Typography variant="h6">
              {f.number} · {f.title}
            </Typography>
            <Chip
              className={`semantic-risk-badge risk-${f.classification.toLowerCase()}`}
              size="small"
              color={f.classification === "Minor" ? "success" : "error"}
              label={`${classification[f.classification]} · ${f.riskScore}`}
            />
            <Chip size="small" variant="outlined" label={f.status} />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.7 }}>
            {f.description}
          </Typography>
          <Typography variant="caption">
            Referans: {f.requirementReference} · Sorumlu: {f.owner} · Hedef:{" "}
            {dt(f.targetDateUtc)}
          </Typography>
        </Box>
        {f.capaRequired && (
          <Chip
            color="primary"
            icon={<HubRounded />}
            label={
              f.linkedCapaNumber ? `Bağlı ${f.linkedCapaNumber}` : "DÖF gerekli"
            }
          />
        )}
      </Stack>
      {details.record.status === "ResponseAction" && f.status === "Open" && (
        <Stack spacing={1.2} sx={{ mt: 2 }}>
          <TextField
            multiline
            minRows={2}
            label="Bölüm yanıtı / neden"
            value={response}
            onChange={(e) => setResponse(e.target.value)}
          />
          <TextField
            multiline
            minRows={2}
            label="Düzeltici aksiyon"
            value={action}
            onChange={(e) => setAction(e.target.value)}
          />
          <Button
            variant="contained"
            disabled={!response || !action || respond.isPending}
            onClick={() => respond.mutate()}
          >
            Yanıtı kaydet
          </Button>
        </Stack>
      )}
      {["CapaVerification", "FindingClosure"].includes(details.record.status) &&
        f.status !== "Closed" && (
          <Stack spacing={1.2} sx={{ mt: 2 }}>
            {f.capaRequired && (
              <Alert severity="warning">
                Bu bulgu, bağlı {f.linkedCapaNumber ?? "DÖF"} kapanmadan
                kapatılamaz.
              </Alert>
            )}
            <TextField
              label="Doğrulama ve kapanış kanıtı"
              value={verification}
              onChange={(e) => setVerification(e.target.value)}
            />
            <TextField
              type="password"
              label="E-imza parolası"
              value={signaturePassword}
              onChange={(e) => setSignaturePassword(e.target.value)}
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={signatureAccepted}
                  onChange={(e) => setSignatureAccepted(e.target.checked)}
                />
              }
              label="Bulgu doğrulama ve kapanış imzasının anlamını kabul ediyorum"
            />
            <Button
              variant="contained"
              color="success"
              disabled={
                !verification ||
                !signaturePassword ||
                !signatureAccepted ||
                close.isPending
              }
              onClick={() => close.mutate()}
            >
              Bulguyu doğrula ve kapat
            </Button>
          </Stack>
        )}
      {(respond.isError || close.isError) && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {respond.error?.message ?? close.error?.message}
        </Alert>
      )}
    </Paper>
  );
}

function AuditActions({
  details,
  update,
}: {
  details: InternalAuditDetails;
  update: (d: InternalAuditDetails) => void;
}) {
  const [note, setNote] = useState("");
  const [error, setError] = useState("");
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureAccepted, setSignatureAccepted] = useState(false);
  const m = useMutation({
    mutationFn: (code: string) =>
      transitionInternalAudit(
        details.record.id,
        details.record.version,
        code,
        note,
        signaturePassword,
        signatureAccepted,
      ),
    onSuccess: (d) => {
      setError("");
      update(d);
    },
    onError: (e) => setError(e.message),
  });
  if (!details.availableTransitions.length)
    return (
      <DialogActions>
        <Chip color="success" label="Denetim yaşam döngüsü tamamlandı" />
      </DialogActions>
    );
  return (
    <DialogActions sx={{ flexWrap: "wrap" }}>
      {error && (
        <Alert severity="error" sx={{ mr: "auto" }}>
          {error}
        </Alert>
      )}
      {details.availableTransitions.some((x) => x.noteRequired) && (
        <TextField
          size="small"
          label="Kapanış gerekçesi"
          value={note}
          onChange={(e) => setNote(e.target.value)}
          sx={{ minWidth: 300 }}
        />
      )}
      {details.availableTransitions.some(
        (x) => x.code === "approve-plan" || x.code === "close",
      ) && (
        <>
          <TextField
            size="small"
            type="password"
            label="E-imza parolası"
            value={signaturePassword}
            onChange={(e) => setSignaturePassword(e.target.value)}
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={signatureAccepted}
                onChange={(e) => setSignatureAccepted(e.target.checked)}
              />
            }
            label="İmza anlamını kabul ediyorum"
          />
        </>
      )}
      {details.availableTransitions.map((t) => (
        <Button
          key={t.code}
          variant="contained"
          disabled={
            m.isPending ||
            (t.noteRequired && !note) ||
            ((t.code === "approve-plan" || t.code === "close") &&
              (!signaturePassword || !signatureAccepted))
          }
          onClick={() => m.mutate(t.code)}
        >
          {t.label}
        </Button>
      ))}
    </DialogActions>
  );
}
