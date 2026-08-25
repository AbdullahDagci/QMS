import { useMemo, useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AddRounded,
  AssignmentTurnedInRounded,
  FactCheckRounded,
  HistoryRounded,
  HubRounded,
  LocalShippingRounded,
  MailLockRounded,
  ReportProblemRounded,
  ShieldRounded,
  VerifiedRounded,
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
  addSupplierAuditFinding,
  answerSupplierAuditChecklist,
  closeSupplierAuditFinding,
  createSupplierAudit,
  createSupplierAuditInvitation,
  getSupplierAuditDetails,
  recordSupplierAuditResult,
  respondSupplierAuditFinding,
  searchSupplierAudits,
  submitSupplierAuditEvidence,
  transitionSupplierAudit,
  type CreateSupplierAuditInput,
  type SupplierAuditDetails,
  type SupplierAuditFinding,
  type SupplierAuditListItem,
} from "../../api/supplierAudits";
import { AdvancedFilterButton } from "../../components/AdvancedFilterPanel";
import { AuditTimeline } from "../../components/AuditTimeline";
import { ModalHeader } from "../../components/ModalHeader";
import {
  ModuleGuideDialog,
  ModuleInfoButton,
} from "../../components/ModuleGuideDialog";
import { RecordAssignments } from "../../components/RecordAssignments";
import { SearchableSelect } from "../../components/SearchableSelect";
import { TableLoadingRows } from "../../components/TableLoadingRows";
import { Permissions, useAuth } from "../../security/AuthContext";
import { SupplierAuditFilters } from "./SupplierAuditFilters";
import {
  emptySupplierAuditFilters,
  toSupplierAuditColumnFilters,
  type SupplierAuditFilterState,
} from "./supplierAuditFilterModel";

const statusLabels: Record<string, string> = {
  RiskPlan: "Risk bazlı plan",
  ScopeChecklist: "Kapsam / soru listesi",
  AuditorAssignment: "Denetçi atama",
  Execution: "Uygulama",
  Findings: "Bulgular",
  SupplierResponse: "Tedarikçi cevabı",
  EvidenceVerification: "Kanıt doğrulama",
  Capa: "DÖF / CAPA",
  AuditResult: "Denetim sonucu",
  Closed: "Kapalı",
};
const qualificationLabels: Record<string, string> = {
  Active: "Aktif",
  Conditional: "Koşullu",
  Suspended: "Askıda",
  RequalificationRequired: "Yeniden nitelendirme",
  Approved: "Onaylı",
  Rejected: "Reddedildi",
};
const classificationLabels: Record<string, string> = {
  Critical: "Kritik",
  Major: "Majör",
  Minor: "Minör",
  Observation: "Gözlem",
};
const stages = [
  "Risk planı",
  "Kapsam",
  "Denetçi",
  "Uygulama",
  "Bulgular",
  "Tedarikçi cevabı",
  "Kanıt",
  "DÖF / CAPA",
  "Sonuç",
  "Kapalı",
];
const stageStatuses = [
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
];
const eventLabels: Record<string, string> = {
  SupplierAuditCreated: "Tedarikçi denetimi oluşturuldu",
  SupplierAuditStatusChanged: "Tedarikçi denetimi aşaması değiştirildi",
  SupplierAuditChecklistAnswered: "Soru listesi kanıtıyla yanıtlandı",
  SupplierAuditFindingCreated: "Risk sınıflı tedarikçi bulgusu açıldı",
  SupplierAuditFindingResponseSubmitted: "Tedarikçi cevabı kaydedildi",
  SupplierAuditFindingEvidenceSubmitted: "Tedarikçi kanıtı sunuldu",
  SupplierAuditFindingClosed: "Tedarikçi bulgusu kapatıldı",
  SupplierAuditInvitationCreated: "Güvenli tek kullanımlık davet oluşturuldu",
  SupplierAuditInvitationResponseAccepted:
    "Güvenli tedarikçi yanıtı kabul edildi",
  SupplierAuditResultRecorded: "Tedarikçi nitelendirme sonucu kaydedildi",
};
const dt = (v: string | null) =>
  v
    ? new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(v))
    : "—";

export function SupplierAuditWorkspace() {
  const [params, setParams] = useSearchParams();
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState<SupplierAuditFilterState>(
    emptySupplierAuditFilters,
  );
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25);
  const [sortBy, setSortBy] = useState("createdAtUtc");
  const [direction, setDirection] = useState<"asc" | "desc">("desc");
  const [createOpen, setCreateOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  const { can } = useAuth();
  const columnFilters = useMemo(
    () => toSupplierAuditColumnFilters(filters),
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
    queryKey: ["supplier-audits", request],
    queryFn: ({ signal }) => searchSupplierAudits(request, signal),
    placeholderData: (p) => p,
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
    <Box component="section">
      <Stack
        direction={{ xs: "column", xl: "row" }}
        sx={{
          justifyContent: "space-between",
          alignItems: { xl: "center" },
          gap: 2,
        }}
      >
        <Box>
          <Stack direction="row" spacing={1.2} sx={{ alignItems: "center" }}>
            <Box className="section-heading-icon tone-teal">
              <LocalShippingRounded />
            </Box>
            <Typography
              variant="h2"
              sx={{ fontSize: { xs: "2.15rem", md: "2.8rem" } }}
            >
              Tedarikçi denetimleri
            </Typography>
            <Chip size="small" color="success" label="M.09 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Tedarikçi riskini, saha kanıtlarını, güvenli cevapları, M.02
            DÖF’leri ve nitelendirme kararını tek zincirde yönetin.
          </Typography>
        </Box>
        <Stack
          direction="row"
          spacing={1.2}
          sx={{ alignSelf: { xs: "flex-start", xl: "auto" }, flexWrap: "wrap" }}
        >
          <ModuleInfoButton module="M.09" onClick={() => setGuideOpen(true)} />
          {can(Permissions.supplierAuditPlan) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
              sx={{ whiteSpace: "nowrap" }}
            >
              Yeni tedarikçi denetimi
            </Button>
          )}
        </Stack>
      </Stack>
      <Box className="complaint-kpi-grid">
        <Kpi
          icon={<LocalShippingRounded />}
          label="Görünen denetim"
          value={query.data?.totalCount ?? 0}
        />
        <Kpi
          icon={<ShieldRounded />}
          label="Yüksek / kritik risk"
          value={
            items.filter((x) => ["Yüksek", "Kritik"].includes(x.riskBand))
              .length
          }
          tone="amber"
        />
        <Kpi
          icon={<ReportProblemRounded />}
          label="Açık bulgu"
          value={items.reduce((n, x) => n + x.openFindingCount, 0)}
          tone="rose"
        />
        <Kpi
          icon={<VerifiedRounded />}
          label="Askıda / yeniden nitelendirme"
          value={
            items.filter((x) =>
              ["Suspended", "RequalificationRequired"].includes(
                x.qualificationStatus,
              ),
            ).length
          }
          tone="teal"
        />
      </Box>
      <Stack
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
            ? `${query.data.totalCount} tedarikçi denetimi`
            : `Kayıtlar yükleniyor`}
        </Typography>
      </Stack>
      {filtersOpen && (
        <SupplierAuditFilters
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
          <Table aria-label="Tedarikçi denetimi iş listesi">
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
                  field="supplierName"
                  text="Tedarikçi / kapsam"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell>Malzeme / hizmet</TableCell>
                <Sort
                  field="riskScore"
                  text="Risk"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell>Nitelendirme</TableCell>
                <TableCell>Bulgular</TableCell>
                <Sort
                  field="status"
                  text="Durum"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
              </TableRow>
            </TableHead>
            <TableBody>
              {query.isFetching ? (
                <TableLoadingRows columns={7} rows={6} />
              ) : (
                items.map((x) => (
                  <AuditRow
                    key={x.id}
                    item={x}
                    onOpen={() => setParams({ open: x.id })}
                  />
                ))
              )}
              {!query.isFetching && !items.length && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Alert severity="info" sx={{ my: 2 }}>
                      Filtrelerle eşleşen tedarikçi denetimi bulunamadı.
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
      {createOpen && (
        <CreateDialog
          open
          onClose={() => setCreateOpen(false)}
          onCreated={(d) => {
            setCreateOpen(false);
            setParams({ open: d.record.id });
          }}
        />
      )}
      <DetailsDialog id={params.get("open")} onClose={() => setParams({})} />
      <ModuleGuideDialog
        open={guideOpen}
        module="M.09"
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
  onOpen,
}: {
  item: SupplierAuditListItem;
  onOpen: () => void;
}) {
  return (
    <TableRow hover onClick={onOpen} sx={{ cursor: "pointer" }}>
      <TableCell>
        <Typography color="primary" sx={{ fontWeight: 800 }}>
          {item.recordNumber}
        </Typography>
        <Typography variant="caption">{item.supplierCode}</Typography>
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 760 }}>{item.supplierName}</Typography>
        <Typography variant="caption" color="text.secondary">
          {item.supplierScope}
        </Typography>
      </TableCell>
      <TableCell>{item.materialOrService}</TableCell>
      <TableCell>
        <Chip
          size="small"
          color={
            item.riskBand === "Kritik"
              ? "error"
              : item.riskBand === "Yüksek"
                ? "warning"
                : "default"
          }
          label={`${item.riskScore} · ${item.riskBand}`}
        />
        <Typography variant="caption" sx={{ display: "block" }}>
          {item.recommendedFrequencyMonths} ay frekans
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={
            ["Suspended", "Rejected"].includes(item.qualificationStatus)
              ? "error"
              : item.qualificationStatus === "Approved"
                ? "success"
                : "warning"
          }
          label={
            qualificationLabels[item.qualificationStatus] ??
            item.qualificationStatus
          }
        />
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
    </TableRow>
  );
}

function CreateDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (d: SupplierAuditDetails) => void;
}) {
  const { user } = useAuth();
  const [start] = useState(() =>
    new Date(Date.now() + 14 * 86400000).toISOString().slice(0, 16),
  );
  const [end] = useState(() =>
    new Date(Date.now() + 16 * 86400000).toISOString().slice(0, 16),
  );
  const [form, setForm] = useState<CreateSupplierAuditInput>({
    supplierEvaluationId: null,
    supplierCode: "",
    supplierName: "",
    supplierScope: "Primer ambalaj",
    materialOrService: "",
    country: "Türkiye",
    criticality: "Yüksek",
    pastPerformanceScore: 80,
    openFindingCount: 0,
    scope: "GMP kalite sistemi, değişiklik yönetimi ve tedarik sürekliliği",
    site: "",
    leadAuditorUserId: user.id,
    leadAuditor: user.displayName,
    leadAuditorDepartment: "Kalite Güvence",
    purchasingOwner: "Satınalma Müdürü",
    plannedStartUtc: start,
    plannedEndUtc: end,
    checklistVersion: "SA-2026.1",
    checklist: [],
  });
  const [questions, setQuestions] = useState(
    "Kalite Sistemleri | Değişiklikler müşteriye zamanında bildiriliyor mu? | GMP Bölüm 5\nÜretim | Kritik prosesler valide edilmiş mi? | GMP Annex 15\nTedarik Zinciri | İzlenebilirlik ve süreklilik planı güncel mi? | ISO 9001 8.4",
  );
  const qc = useQueryClient();
  const m = useMutation({
    mutationFn: createSupplierAudit,
    onSuccess: (d) => {
      qc.invalidateQueries({ queryKey: ["supplier-audits"] });
      onCreated(d);
    },
  });
  const set = <K extends keyof CreateSupplierAuditInput>(
    k: K,
    v: CreateSupplierAuditInput[K],
  ) => setForm((x) => ({ ...x, [k]: v }));
  const checklist = questions
    .split("\n")
    .map((x) => x.trim())
    .filter(Boolean)
    .map((line) => {
      const [category, question, reference] = line
        .split("|")
        .map((x) => x.trim());
      return {
        category,
        question: question || category,
        reference: reference || "İç kriter",
      };
    });
  const valid =
    form.supplierCode &&
    form.supplierName &&
    form.materialOrService &&
    form.site &&
    form.leadAuditorUserId &&
    checklist.length > 0;
  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="md"
      slotProps={{ paper: { className: "standard-modal-paper" } }}
    >
      <ModalHeader onClose={onClose}>
        <Typography variant="overline">
          M.09 · RİSK BAZLI TEDARİKÇİ DENETİMİ
        </Typography>
        <Typography variant="h4">Yeni tedarikçi denetimi</Typography>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {m.isError && <Alert severity="error">{m.error.message}</Alert>}
          <Box className="form-grid two-column">
            <TextField
              label="Tedarikçi kodu"
              value={form.supplierCode}
              onChange={(e) => set("supplierCode", e.target.value)}
            />
            <TextField
              label="Tedarikçi adı"
              value={form.supplierName}
              onChange={(e) => set("supplierName", e.target.value)}
            />
            <TextField
              label="Onay / kullanım kapsamı"
              value={form.supplierScope}
              onChange={(e) => set("supplierScope", e.target.value)}
            />
            <TextField
              label="Malzeme / hizmet"
              value={form.materialOrService}
              onChange={(e) => set("materialOrService", e.target.value)}
            />
            <TextField
              label="Ülke"
              value={form.country}
              onChange={(e) => set("country", e.target.value)}
            />
            <TextField
              label="Denetim sahası"
              value={form.site}
              onChange={(e) => set("site", e.target.value)}
            />
            <SearchableSelect
              label="Kritiklik"
              value={form.criticality}
              options={["Kritik", "Yüksek", "Orta", "Düşük"].map((value) => ({
                value,
                label: value,
              }))}
              onChange={(v) => set("criticality", v ?? "Yüksek")}
            />
            <TextField
              type="number"
              label="Geçmiş performans (0–100)"
              value={form.pastPerformanceScore}
              onChange={(e) =>
                set("pastPerformanceScore", Number(e.target.value))
              }
            />
            <TextField
              type="number"
              label="Açık bulgu geçmişi"
              value={form.openFindingCount}
              onChange={(e) => set("openFindingCount", Number(e.target.value))}
            />
            <TextField
              label="Satınalma sorumlusu"
              value={form.purchasingOwner}
              onChange={(e) => set("purchasingOwner", e.target.value)}
            />
            <TextField
              label="Baş denetçi"
              value={form.leadAuditor}
              onChange={(e) => set("leadAuditor", e.target.value)}
            />
            <TextField
              label="Denetçi bölümü"
              value={form.leadAuditorDepartment}
              onChange={(e) => set("leadAuditorDepartment", e.target.value)}
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
            <TextField
              label="Soru listesi sürümü"
              value={form.checklistVersion}
              onChange={(e) => set("checklistVersion", e.target.value)}
            />
          </Box>
          <TextField
            multiline
            minRows={2}
            label="Denetim kapsamı"
            value={form.scope}
            onChange={(e) => set("scope", e.target.value)}
          />
          <TextField
            multiline
            minRows={5}
            label="Soru listesi — her satır: Kategori | Soru | Referans"
            value={questions}
            onChange={(e) => setQuestions(e.target.value)}
            helperText="Soru listesi denetim başladığında sürümüyle birlikte kilitlenir."
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={!valid || m.isPending}
          onClick={() =>
            m.mutate({
              ...form,
              plannedStartUtc: new Date(form.plannedStartUtc).toISOString(),
              plannedEndUtc: new Date(form.plannedEndUtc).toISOString(),
              checklist,
            })
          }
        >
          Risk planını oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function DetailsDialog({
  id,
  onClose,
}: {
  id: string | null;
  onClose: () => void;
}) {
  const [tab, setTab] = useState(0);
  const query = useQuery({
    queryKey: ["supplier-audit", id],
    queryFn: ({ signal }) => getSupplierAuditDetails(id!, signal),
    enabled: !!id,
    retry: false,
  });
  const qc = useQueryClient();
  const update = (d: SupplierAuditDetails) => {
    qc.setQueryData(["supplier-audit", id], d);
    qc.invalidateQueries({ queryKey: ["supplier-audits"] });
  };
  if (!id) return null;
  const d = query.data;
  return (
    <Dialog
      open
      onClose={onClose}
      fullScreen
      slotProps={{
        paper: { className: "standard-modal-paper audit-detail-modal" },
      }}
    >
      {query.isLoading ? (
        <>
          <ModalHeader onClose={onClose}>
            <Typography variant="h4">Tedarikçi denetimi yükleniyor</Typography>
          </ModalHeader>
          <DialogContent>
            <TableLoadingRows columns={4} rows={6} />
          </DialogContent>
        </>
      ) : query.isError || !d ? (
        <>
          <ModalHeader onClose={onClose}>
            <Typography variant="h4">Kayıt açılamadı</Typography>
          </ModalHeader>
          <DialogContent>
            <Alert severity="error">
              {query.error?.message ?? "Kayıt bulunamadı"}
            </Alert>
          </DialogContent>
        </>
      ) : (
        <>
          <ModalHeader onClose={onClose}>
            <Stack
              direction={{ xs: "column", md: "row" }}
              sx={{
                justifyContent: "space-between",
                alignItems: { md: "center" },
                gap: 1,
                width: "100%",
              }}
            >
              <Box>
                <Stack direction="row" spacing={1}>
                  <Typography variant="overline">
                    {d.record.recordNumber}
                  </Typography>
                  <Chip size="small" label="TEDARİKÇİ DENETİMİ" />
                </Stack>
                <Typography variant="h4">
                  {d.record.supplierName} · {d.record.supplierScope}
                </Typography>
              </Box>
              <Stack direction="row" spacing={1}>
                <Chip
                  label={`${d.record.riskScore} · ${d.record.riskBand}`}
                  color={d.record.riskBand === "Kritik" ? "error" : "warning"}
                />
                <Chip
                  label={
                    qualificationLabels[d.record.qualificationStatus] ??
                    d.record.qualificationStatus
                  }
                  color={
                    d.record.qualificationStatus === "Suspended"
                      ? "error"
                      : "primary"
                  }
                />
                <Chip
                  label={statusLabels[d.record.status] ?? d.record.status}
                />
              </Stack>
            </Stack>
          </ModalHeader>
          <Tabs
            value={tab}
            onChange={(_, v) => setTab(v)}
            variant="scrollable"
            scrollButtons="auto"
          >
            <Tab
              icon={<FactCheckRounded />}
              iconPosition="start"
              label="Genel bakış"
            />
            <Tab
              icon={<AssignmentTurnedInRounded />}
              iconPosition="start"
              label={`Soru listesi (${d.checklist.length})`}
            />
            <Tab
              icon={<ReportProblemRounded />}
              iconPosition="start"
              label={`Bulgular (${d.findings.length})`}
            />
            <Tab
              icon={<MailLockRounded />}
              iconPosition="start"
              label={`Tedarikçi portalı (${d.invitations.length})`}
            />
            <Tab
              icon={<VerifiedRounded />}
              iconPosition="start"
              label="Sonuç"
            />
            <Tab
              icon={<HubRounded />}
              iconPosition="start"
              label="Roller ve bağlar"
            />
            <Tab
              icon={<HistoryRounded />}
              iconPosition="start"
              label={`Geçmiş (${d.auditTrail.length})`}
            />
          </Tabs>
          <DialogContent dividers>
            {tab === 0 && <Overview d={d} />}{" "}
            {tab === 1 && <Checklist d={d} update={update} />}{" "}
            {tab === 2 && <Findings d={d} update={update} />}{" "}
            {tab === 3 && <SupplierPortal d={d} update={update} />}{" "}
            {tab === 4 && <Result d={d} update={update} />}{" "}
            {tab === 5 && (
              <Stack spacing={2}>
                <Alert severity="info">
                  M.02 DÖF ve gelecekte M.16 kapsam bazlı tedarikçi
                  değerlendirmesi bu kayıtla bağlanır.
                </Alert>
                <RecordAssignments
                  aggregateType="SupplierAudit"
                  aggregateId={d.record.id}
                />
              </Stack>
            )}{" "}
            {tab === 6 && (
              <AuditTimeline events={d.auditTrail} labels={eventLabels} />
            )}
          </DialogContent>
          <AuditActions details={d} update={update} />
        </>
      )}
    </Dialog>
  );
}

function Overview({ d }: { d: SupplierAuditDetails }) {
  const idx = Math.max(0, stageStatuses.indexOf(d.record.status));
  return (
    <Stack spacing={2.5}>
      <Paper
        variant="outlined"
        sx={{
          p: 2.5,
          borderRadius: 3,
          background: "linear-gradient(135deg,#f3f8f6,#f8f7ff)",
        }}
      >
        <Stack direction="row" spacing={1.2}>
          <Box className="section-heading-icon tone-teal">
            <LocalShippingRounded />
          </Box>
          <Box>
            <Typography sx={{ fontWeight: 850 }}>
              Risk bazlı tedarikçi denetim zinciri
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Plan frekansı, kanıt, DÖF ve kapsam bazlı nitelendirme kararı
            </Typography>
          </Box>
        </Stack>
        <Stepper activeStep={idx} alternativeLabel sx={{ mt: 2 }}>
          {stages.map((s, i) => (
            <Step key={s} completed={i < idx}>
              <StepLabel>{s}</StepLabel>
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
        <Info
          label="Tedarikçi"
          value={`${d.record.supplierCode} · ${d.record.supplierName}`}
        />
        <Info
          label="Kapsam / ürün"
          value={`${d.record.supplierScope} · ${d.record.materialOrService}`}
        />
        <Info
          label="Risk hesabı"
          value={`${d.record.riskScore}/100 · ${d.record.riskBand} · ${d.record.recommendedFrequencyMonths} ay`}
        />
        <Info
          label="Geçmiş veri"
          value={`Performans ${d.record.pastPerformanceScore} · ${d.record.openFindingSnapshot} açık bulgu`}
        />
        <Info
          label="Saha / denetçi"
          value={`${d.record.site} · ${d.record.leadAuditor}`}
        />
        <Info
          label="Takvim"
          value={`${dt(d.record.plannedStartUtc)} — ${dt(d.record.plannedEndUtc)}`}
        />
      </Box>
      {d.record.qualificationStatus === "Suspended" && (
        <Alert severity="error">
          Kritik bulgu nedeniyle bu tedarikçi kapsamı otomatik askıya alındı ve
          yeniden nitelendirme gerektiriyor.
        </Alert>
      )}
      <Alert severity="info">
        Soru listesi: {d.record.checklistVersion}
        {d.record.checklistLockedAtUtc
          ? ` · ${dt(d.record.checklistLockedAtUtc)} tarihinde kilitlendi`
          : " · henüz kilitlenmedi"}
      </Alert>
    </Stack>
  );
}
function Info({ label, value }: { label: string; value: string }) {
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.2, borderRadius: 3, borderLeft: "4px solid #6f9387" }}
    >
      <Typography variant="overline">{label}</Typography>
      <Typography sx={{ fontWeight: 750, mt: 0.4 }}>{value}</Typography>
    </Paper>
  );
}

function Checklist({
  d,
  update,
}: {
  d: SupplierAuditDetails;
  update: (x: SupplierAuditDetails) => void;
}) {
  const { can } = useAuth();
  return (
    <Stack spacing={1.5}>
      {d.checklist.map((item) => (
        <ChecklistCard
          key={item.id}
          item={item}
          audit={d}
          update={update}
          allowed={can(Permissions.supplierAuditExecute)}
        />
      ))}
    </Stack>
  );
}
function ChecklistCard({
  item,
  audit,
  update,
  allowed,
}: {
  item: SupplierAuditDetails["checklist"][number];
  audit: SupplierAuditDetails;
  update: (x: SupplierAuditDetails) => void;
  allowed: boolean;
}) {
  const [status, setStatus] = useState("Conform");
  const [evidence, setEvidence] = useState("");
  const [note, setNote] = useState("");
  const m = useMutation({
    mutationFn: () =>
      answerSupplierAuditChecklist(
        audit.record.id,
        item.id,
        audit.record.version,
        status,
        evidence,
        note,
      ),
    onSuccess: update,
  });
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        sx={{ justifyContent: "space-between", gap: 2 }}
      >
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1}>
            <Chip size="small" label={`${item.order} · ${item.category}`} />
            <Chip
              size="small"
              color={
                item.status === "Pending"
                  ? "warning"
                  : item.status === "Nonconform"
                    ? "error"
                    : "success"
              }
              label={item.status}
            />
          </Stack>
          <Typography sx={{ fontWeight: 800, mt: 1 }}>
            {item.question}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {item.reference}
          </Typography>
          {item.evidence && (
            <Alert severity="info" sx={{ mt: 1 }}>
              Kanıt: {item.evidence}
            </Alert>
          )}
        </Box>
        {allowed && audit.record.status === "Execution" && (
          <Stack spacing={1} sx={{ minWidth: { md: 420 } }}>
            <SearchableSelect
              label="Sonuç"
              value={status}
              options={["Conform", "Nonconform", "NotApplicable"].map(
                (value) => ({ value, label: value }),
              )}
              onChange={(v) => setStatus(v ?? "Conform")}
            />
            <TextField
              size="small"
              label="Objektif kanıt"
              value={evidence}
              onChange={(e) => setEvidence(e.target.value)}
            />
            <TextField
              size="small"
              label="Denetçi notu"
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
            <Button
              variant="contained"
              disabled={m.isPending || (status === "Nonconform" && !evidence)}
              onClick={() => m.mutate()}
            >
              Yanıtı kaydet
            </Button>
            {m.isError && <Alert severity="error">{m.error.message}</Alert>}
          </Stack>
        )}
      </Stack>
    </Paper>
  );
}

function Findings({
  d,
  update,
}: {
  d: SupplierAuditDetails;
  update: (x: SupplierAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [form, setForm] = useState(() => ({
    title: "",
    description: "",
    requirementReference: "",
    classification: "Major",
    capaRequired: true,
    owner: "Tedarikçi Kalite Müdürü",
    responseDueAtUtc: new Date(Date.now() + 30 * 86400000)
      .toISOString()
      .slice(0, 16),
  }));
  const m = useMutation({
    mutationFn: () =>
      addSupplierAuditFinding(d.record.id, d.record.version, {
        ...form,
        responseDueAtUtc: new Date(form.responseDueAtUtc).toISOString(),
      }),
    onSuccess: update,
  });
  return (
    <Stack spacing={2}>
      {d.findings.map((f) => (
        <FindingCard key={f.id} f={f} d={d} update={update} />
      ))}
      {!d.findings.length && (
        <Alert severity="info">Henüz tedarikçi bulgusu bulunmuyor.</Alert>
      )}
      {can(Permissions.supplierAuditExecute) &&
        ["Execution", "Findings"].includes(d.record.status) && (
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="h6" sx={{ mb: 1.5 }}>
              Risk sınıflı bulgu aç
            </Typography>
            <Box className="form-grid two-column">
              <TextField
                label="Bulgu başlığı"
                value={form.title}
                onChange={(e) =>
                  setForm((x) => ({ ...x, title: e.target.value }))
                }
              />
              <TextField
                label="Gereklilik referansı"
                value={form.requirementReference}
                onChange={(e) =>
                  setForm((x) => ({
                    ...x,
                    requirementReference: e.target.value,
                  }))
                }
              />
              <SearchableSelect
                label="Sınıf"
                value={form.classification}
                options={["Critical", "Major", "Minor", "Observation"].map(
                  (value) => ({ value, label: classificationLabels[value] }),
                )}
                onChange={(v) =>
                  setForm((x) => ({
                    ...x,
                    classification: v ?? "Major",
                    capaRequired: ["Critical", "Major"].includes(v ?? ""),
                  }))
                }
              />
              <TextField
                label="Sorumlu"
                value={form.owner}
                onChange={(e) =>
                  setForm((x) => ({ ...x, owner: e.target.value }))
                }
              />
              <TextField
                type="datetime-local"
                label="Cevap hedefi"
                value={form.responseDueAtUtc}
                onChange={(e) =>
                  setForm((x) => ({ ...x, responseDueAtUtc: e.target.value }))
                }
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </Box>
            <TextField
              fullWidth
              multiline
              minRows={2}
              sx={{ mt: 1.5 }}
              label="Bulgu açıklaması"
              value={form.description}
              onChange={(e) =>
                setForm((x) => ({ ...x, description: e.target.value }))
              }
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.capaRequired}
                  onChange={(e) =>
                    setForm((x) => ({ ...x, capaRequired: e.target.checked }))
                  }
                />
              }
              label="M.02 DÖF gerekli"
            />
            <Button
              variant="contained"
              disabled={
                !form.title ||
                !form.description ||
                !form.requirementReference ||
                m.isPending
              }
              onClick={() => m.mutate()}
            >
              Bulgu ve gerekiyorsa DÖF oluştur
            </Button>
            {m.isError && (
              <Alert severity="error" sx={{ mt: 1 }}>
                {m.error.message}
              </Alert>
            )}
          </Paper>
        )}
    </Stack>
  );
}

function FindingCard({
  f,
  d,
  update,
}: {
  f: SupplierAuditFinding;
  d: SupplierAuditDetails;
  update: (x: SupplierAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [response, setResponse] = useState("");
  const [commitment, setCommitment] = useState("");
  const [due, setDue] = useState(() =>
    new Date(Date.now() + 20 * 86400000).toISOString().slice(0, 16),
  );
  const [evidence, setEvidence] = useState("");
  const [note, setNote] = useState("");
  const respond = useMutation({
    mutationFn: () =>
      respondSupplierAuditFinding(
        d.record.id,
        f.id,
        d.record.version,
        response,
        commitment,
        new Date(due).toISOString(),
      ),
    onSuccess: update,
  });
  const submit = useMutation({
    mutationFn: () =>
      submitSupplierAuditEvidence(
        d.record.id,
        f.id,
        d.record.version,
        evidence,
      ),
    onSuccess: update,
  });
  const close = useMutation({
    mutationFn: () =>
      closeSupplierAuditFinding(d.record.id, f.id, d.record.version, note),
    onSuccess: update,
  });
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack
        direction="row"
        spacing={1}
        sx={{ alignItems: "center", flexWrap: "wrap" }}
      >
        <Typography variant="h6">
          {f.number} · {f.title}
        </Typography>
        <Chip
          size="small"
          color={
            f.classification === "Critical"
              ? "error"
              : f.classification === "Major"
                ? "warning"
                : "default"
          }
          label={classificationLabels[f.classification]}
        />
        <Chip size="small" label={f.status} />
        {f.linkedCapaNumber && (
          <Chip
            size="small"
            color="primary"
            label={`Bağlı ${f.linkedCapaNumber}`}
          />
        )}
      </Stack>
      <Typography sx={{ mt: 1 }}>{f.description}</Typography>
      <Typography variant="caption" color="text.secondary">
        Referans: {f.requirementReference} · Sorumlu: {f.owner} · Cevap:{" "}
        {dt(f.responseDueAtUtc)}
      </Typography>
      {f.linkedCapaNumber && f.status !== "Closed" && (
        <Alert severity="warning" sx={{ mt: 1 }}>
          Bu bulgu, bağlı {f.linkedCapaNumber} kapanmadan kapatılamaz.
        </Alert>
      )}
      {can(Permissions.supplierAuditRespond) &&
        d.record.status === "SupplierResponse" &&
        f.status === "Open" && (
          <Stack spacing={1} sx={{ mt: 1.5 }}>
            <TextField
              label="Tedarikçi cevabı"
              multiline
              minRows={2}
              value={response}
              onChange={(e) => setResponse(e.target.value)}
            />
            <TextField
              label="Taahhüt"
              value={commitment}
              onChange={(e) => setCommitment(e.target.value)}
            />
            <TextField
              type="datetime-local"
              label="Taahhüt hedefi"
              value={due}
              onChange={(e) => setDue(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <Button
              variant="contained"
              disabled={!response || !commitment || respond.isPending}
              onClick={() => respond.mutate()}
            >
              Cevabı kaydet
            </Button>
          </Stack>
        )}
      {can(Permissions.supplierAuditRespond) &&
        ["SupplierResponse", "EvidenceVerification"].includes(
          d.record.status,
        ) &&
        f.status === "ResponseSubmitted" && (
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={1}
            sx={{ mt: 1.5 }}
          >
            <TextField
              fullWidth
              label="Tedarikçi kanıtı / manifest"
              value={evidence}
              onChange={(e) => setEvidence(e.target.value)}
            />
            <Button
              variant="contained"
              disabled={!evidence || submit.isPending}
              onClick={() => submit.mutate()}
            >
              Kanıtı sun
            </Button>
          </Stack>
        )}
      {can(Permissions.supplierAuditApprove) &&
        d.record.status === "Capa" &&
        f.status === "EvidenceSubmitted" && (
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={1}
            sx={{ mt: 1.5 }}
          >
            <TextField
              fullWidth
              label="Doğrulama / kapanış kanıtı"
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
            <Button
              variant="contained"
              disabled={!note || close.isPending}
              onClick={() => close.mutate()}
            >
              Bulguyu doğrula ve kapat
            </Button>
          </Stack>
        )}
      {(respond.isError || submit.isError || close.isError) && (
        <Alert severity="error" sx={{ mt: 1 }}>
          {respond.error?.message ||
            submit.error?.message ||
            close.error?.message}
        </Alert>
      )}
    </Paper>
  );
}

function SupplierPortal({
  d,
  update,
}: {
  d: SupplierAuditDetails;
  update: (x: SupplierAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [email, setEmail] = useState("");
  const [expires, setExpires] = useState(() =>
    new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 16),
  );
  const [token, setToken] = useState<string | null>(null);
  const m = useMutation({
    mutationFn: () =>
      createSupplierAuditInvitation(
        d.record.id,
        d.record.version,
        email,
        new Date(expires).toISOString(),
      ),
    onSuccess: (r) => {
      setToken(r.oneTimeToken);
      update(r.details);
    },
  });
  return (
    <Stack spacing={2}>
      <Alert severity="info">
        Tedarikçi cevabı ikinci bir tenant açmadan, süreli ve tek kullanımlık
        güvenli davetle alınabilir. Token yalnız oluşturulduğu anda gösterilir;
        veritabanında özeti saklanır.
      </Alert>
      {can(Permissions.supplierAuditRespond) &&
        d.record.status === "SupplierResponse" && (
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="h6">Güvenli tedarikçi daveti</Typography>
            <Stack
              direction={{ xs: "column", md: "row" }}
              spacing={1}
              sx={{ mt: 1 }}
            >
              <TextField
                fullWidth
                label="Tedarikçi e-posta adresi"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
              <TextField
                type="datetime-local"
                label="Geçerlilik sonu"
                value={expires}
                onChange={(e) => setExpires(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <Button
                variant="contained"
                disabled={!email || m.isPending}
                onClick={() => m.mutate()}
              >
                Tek kullanımlık davet oluştur
              </Button>
            </Stack>
            {token && (
              <Alert severity="success" sx={{ mt: 1, wordBreak: "break-all" }}>
                Bir kez gösterilen token: {token}
              </Alert>
            )}
            {m.isError && (
              <Alert severity="error" sx={{ mt: 1 }}>
                {m.error.message}
              </Alert>
            )}
          </Paper>
        )}
      <Typography variant="h6">Davet geçmişi</Typography>
      {d.invitations.map((x) => (
        <Paper key={x.id} variant="outlined" sx={{ p: 1.5 }}>
          <Stack direction="row" sx={{ justifyContent: "space-between" }}>
            <Box>
              <Typography sx={{ fontWeight: 750 }}>
                {x.recipientEmail}
              </Typography>
              <Typography variant="caption">
                Oluşturuldu: {dt(x.createdAtUtc)} · Son geçerlilik:{" "}
                {dt(x.expiresAtUtc)}
              </Typography>
            </Box>
            <Chip
              color={x.usedAtUtc ? "success" : "warning"}
              label={
                x.usedAtUtc ? `Kullanıldı · ${dt(x.usedAtUtc)}` : "Kullanılmadı"
              }
            />
          </Stack>
        </Paper>
      ))}
      {!d.invitations.length && (
        <Alert severity="info">Henüz güvenli davet oluşturulmadı.</Alert>
      )}
    </Stack>
  );
}

function Result({
  d,
  update,
}: {
  d: SupplierAuditDetails;
  update: (x: SupplierAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [decision, setDecision] = useState("Conditional");
  const [rationale, setRationale] = useState("");
  const [validUntil, setValidUntil] = useState(() =>
    new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10),
  );
  const [requal, setRequal] = useState(d.record.requalificationRequired);
  const m = useMutation({
    mutationFn: () =>
      recordSupplierAuditResult(
        d.record.id,
        d.record.version,
        decision,
        rationale,
        validUntil ? new Date(validUntil).toISOString() : null,
        requal,
      ),
    onSuccess: update,
  });
  return (
    <Stack spacing={2}>
      {d.record.resultRationale ? (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction="row" spacing={1}>
            <Chip
              color={
                d.record.qualificationStatus === "Approved"
                  ? "success"
                  : d.record.qualificationStatus === "Rejected"
                    ? "error"
                    : "warning"
              }
              label={qualificationLabels[d.record.qualificationStatus]}
            />
            {d.record.requalificationRequired && (
              <Chip color="warning" label="Yeniden nitelendirme gerekli" />
            )}
          </Stack>
          <Typography sx={{ mt: 1 }}>{d.record.resultRationale}</Typography>
          <Typography variant="caption">
            Geçerlilik: {dt(d.record.qualificationValidUntilUtc)}
          </Typography>
        </Paper>
      ) : (
        <Alert severity="info">Denetim sonucu henüz kaydedilmedi.</Alert>
      )}
      {can(Permissions.supplierAuditApprove) &&
        d.record.status === "AuditResult" &&
        !d.record.resultRationale && (
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="h6">
              Kapsam bazlı nitelendirme kararı
            </Typography>
            <Box className="form-grid two-column" sx={{ mt: 1.5 }}>
              <SearchableSelect
                label="Karar"
                value={decision}
                options={[
                  "Approved",
                  "Conditional",
                  "Suspended",
                  "RequalificationRequired",
                  "Rejected",
                ].map((value) => ({
                  value,
                  label: qualificationLabels[value],
                }))}
                onChange={(v) => setDecision(v ?? "Conditional")}
              />
              <TextField
                type="date"
                label="Geçerlilik tarihi"
                value={validUntil}
                onChange={(e) => setValidUntil(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </Box>
            <TextField
              fullWidth
              multiline
              minRows={3}
              sx={{ mt: 1.5 }}
              label="Karar gerekçesi"
              value={rationale}
              onChange={(e) => setRationale(e.target.value)}
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={requal}
                  onChange={(e) => setRequal(e.target.checked)}
                />
              }
              label="M.16 yeniden nitelendirme değerlendirmesi gerekli"
            />
            <Button
              variant="contained"
              disabled={!rationale || m.isPending}
              onClick={() => m.mutate()}
            >
              Nitelendirme kararını kaydet
            </Button>
            {m.isError && (
              <Alert severity="error" sx={{ mt: 1 }}>
                {m.error.message}
              </Alert>
            )}
          </Paper>
        )}
    </Stack>
  );
}

function AuditActions({
  details,
  update,
}: {
  details: SupplierAuditDetails;
  update: (d: SupplierAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [error, setError] = useState("");
  const m = useMutation({
    mutationFn: (code: string) =>
      transitionSupplierAudit(details.record.id, details.record.version, code),
    onSuccess: (d) => {
      setError("");
      update(d);
    },
    onError: (e) => setError(e.message),
  });
  if (!details.availableTransitions.length)
    return (
      <DialogActions>
        <Chip
          color="success"
          label="Tedarikçi denetimi yaşam döngüsü tamamlandı"
        />
      </DialogActions>
    );
  const allowed =
    details.record.status === "RiskPlan" ||
    details.record.status === "ScopeChecklist"
      ? can(Permissions.supplierAuditPlan)
      : ["AuditorAssignment", "Execution", "Findings"].includes(
            details.record.status,
          )
        ? can(Permissions.supplierAuditExecute)
        : details.record.status === "SupplierResponse"
          ? can(Permissions.supplierAuditRespond)
          : can(Permissions.supplierAuditApprove);
  if (!allowed)
    return (
      <DialogActions>
        <Alert severity="info">
          Bu aşamadaki geçiş için atanmış rol veya sistem yetkisi gerekir.
        </Alert>
      </DialogActions>
    );
  const blocker = (code: string) => {
    if (
      code === "complete-audit" &&
      details.checklist.some((x) => x.status === "Pending")
    )
      return "Tüm soru listesi maddeleri cevaplanmadan uygulama tamamlanamaz.";
    if (
      code === "start-verification" &&
      details.findings.some((x) => x.status === "Open")
    )
      return "Tüm bulguların tedarikçi cevabı olmadan kanıt doğrulamaya geçilemez.";
    if (
      code === "start-capa" &&
      details.findings.some((x) =>
        ["Open", "ResponseSubmitted"].includes(x.status),
      )
    )
      return "Tüm tedarikçi kanıtları sunulmadan DÖF/CAPA aşamasına geçilemez.";
    if (
      code === "record-result" &&
      details.findings.some((x) => x.status !== "Closed")
    )
      return "Tüm bulgular ve bağlı M.02 DÖF kayıtları kapanmadan denetim sonucu oluşturulamaz.";
    if (code === "close" && !details.record.resultRationale)
      return "Nitelendirme kararı ve gerekçesi kaydedilmeden denetim kapatılamaz.";
    return null;
  };
  const blocking = details.availableTransitions
    .map((x) => blocker(x.code))
    .find(Boolean);
  return (
    <DialogActions sx={{ flexWrap: "wrap" }}>
      {error && (
        <Alert severity="error" sx={{ mr: "auto" }}>
          {error}
        </Alert>
      )}
      {blocking && (
        <Alert severity="warning" sx={{ mr: "auto" }}>
          {blocking}
        </Alert>
      )}
      {details.availableTransitions.map((t) => (
        <Button
          key={t.code}
          variant="contained"
          disabled={m.isPending || Boolean(blocker(t.code))}
          onClick={() => m.mutate(t.code)}
        >
          {t.label}
        </Button>
      ))}
    </DialogActions>
  );
}
