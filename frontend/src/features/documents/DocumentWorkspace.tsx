import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  LinearProgress,
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
  AddRounded,
  ApprovalRounded,
  AssignmentIndRounded,
  ContentCopyRounded,
  DescriptionRounded,
  HistoryRounded,
  LinkRounded,
  MenuBookRounded,
  PublishedWithChangesRounded,
  SchoolRounded,
  TaskAltRounded,
  VisibilityRounded,
} from "@mui/icons-material";
import {
  acknowledgeDocument,
  closeControlledCopy,
  completeDocumentReview,
  createDocument,
  getDocumentDetails,
  issueControlledCopy,
  searchDocuments,
  startDocumentRevision,
  transitionDocument,
  updateDocumentDraft,
  type CreateDocumentInput,
  type DocumentDetails,
  type DocumentListItem,
} from "../../api/documents";
import { searchChangeControls } from "../../api/changeControls";
import { ModalHeader } from "../../components/ModalHeader";
import {
  SearchableSelect,
  type SelectOption,
} from "../../components/SearchableSelect";
import { TableLoadingRows } from "../../components/TableLoadingRows";
import { AdvancedFilterButton } from "../../components/AdvancedFilterPanel";
import { AuditTimeline } from "../../components/AuditTimeline";
import { RecordAssignments } from "../../components/RecordAssignments";
import {
  ModuleGuideDialog,
  ModuleInfoButton,
} from "../../components/ModuleGuideDialog";
import { Permissions, useAuth } from "../../security/AuthContext";
import { DocumentFilters } from "./DocumentFilters";
import {
  emptyDocumentFilters,
  toDocumentColumnFilters,
  type DocumentFilterState,
} from "./documentFilterModel";

const statuses: Array<SelectOption<string>> = [
  ["Draft", "Taslak"],
  ["Writing", "Yazım"],
  ["Review", "İnceleme"],
  ["Approval", "Onay"],
  ["Approved", "Onaylandı"],
  ["TrainingWaiting", "Eğitim bekliyor"],
  ["Effective", "Yürürlükte"],
  ["RevisionPending", "Revizyon bekliyor"],
  ["PeriodicReview", "Periyodik gözden geçirme"],
  ["Withdrawn", "Yürürlükten kaldırıldı"],
  ["Archived", "Arşivlendi"],
].map(([value, label]) => ({ value, label }));
const types = [
  "SOP",
  "Talimat",
  "Spesifikasyon",
  "Politika",
  "Form / Şablon",
  "Prosedür",
].map((value) => ({ value, label: value }));
const departments = [
  "Üretim",
  "Kalite Kontrol",
  "Kalite Güvence",
  "Ruhsatlandırma",
  "Validasyon",
  "Mühendislik",
  "Tedarik Zinciri",
  "Bilgi Teknolojileri",
];
const positions = [
  "Üretim Operatörü",
  "Hat Lideri",
  "Kalite Kontrol Analisti",
  "Kalite Güvence Uzmanı",
  "Bakım Teknisyeni",
];
const flow = statuses.slice(0, 7);
const label = (v: string) => statuses.find((x) => x.value === v)?.label ?? v;
const dt = (v: string | null) =>
  v
    ? new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(v))
    : "—";
const events: Record<string, string> = {
  DocumentCreated: "Doküman kaydı oluşturuldu",
  DocumentDraftUpdated: "Doküman taslağı güncellendi",
  DocumentReviewApproved: "Doküman incelemesi onaylandı",
  DocumentChangesRequested: "Doküman için değişiklik istendi",
  DocumentTrainingCompleted: "Zorunlu eğitim tamamlandı",
  DocumentRevisionStarted: "Yeni doküman revizyonu başlatıldı",
  ControlledCopyIssued: "Kontrollü kopya dağıtıldı",
  ControlledCopyReturned: "Kontrollü kopya iade edildi",
  ControlledCopyDestroyed: "Kontrollü kopya imha edildi",
  DocumentReadAcknowledged: "Okuma kanıtı imzalandı",
  DocumentStatusChanged: "Doküman durumu değiştirildi",
};

export function DocumentWorkspace() {
  const [params, setParams] = useSearchParams();
  const [createOpen, setCreateOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filterValue, setFilterValue] =
    useState<DocumentFilterState>(emptyDocumentFilters);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25);
  const [sortBy, setSortBy] = useState("createdAtUtc");
  const [direction, setDirection] = useState<"asc" | "desc">("desc");
  const { can } = useAuth();
  const filters = useMemo(
    () => toDocumentColumnFilters(filterValue),
    [filterValue],
  );
  const request = useMemo(
    () => ({
      page: page + 1,
      pageSize,
      sortBy,
      sortDirection: direction,
      filters,
    }),
    [page, pageSize, sortBy, direction, filters],
  );
  const query = useQuery({
    queryKey: ["documents", request],
    queryFn: ({ signal }) => searchDocuments(request, signal),
    placeholderData: (p) => p,
    retry: false,
  });
  const sort = (f: string) => {
    if (f === sortBy) setDirection((x) => (x === "asc" ? "desc" : "asc"));
    else {
      setSortBy(f);
      setDirection("asc");
    }
    setPage(0);
  };
  return (
    <Box component="section">
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
            <Box className="section-heading-icon tone-violet">
              <DescriptionRounded />
            </Box>
            <Typography variant="h2">Doküman yönetimi iş listesi</Typography>
            <Chip size="small" color="success" label="M.04 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            SOP, talimat, spesifikasyon ve formları sürüm, inceleme, eğitim,
            dağıtım ve okuma kanıtlarıyla yönetin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2}>
          <ModuleInfoButton module="M.04" onClick={() => setGuideOpen(true)} />
          {can(Permissions.documentCreate) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
            >
              Yeni doküman
            </Button>
          )}
        </Stack>
      </Stack>
      <Stack direction="row" spacing={1.5} sx={{ mt: 3, alignItems: "center" }}>
        <AdvancedFilterButton
          open={filtersOpen}
          activeCount={filters.length}
          onClick={() => setFiltersOpen((x) => !x)}
        />
        <Typography variant="body2" color="text.secondary">
          {query.data
            ? `${query.data.totalCount} kayıt`
            : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>
      {filtersOpen && (
        <DocumentFilters
          value={filterValue}
          statuses={statuses}
          types={types}
          onApply={(v) => {
            setFilterValue(v);
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
          <Table aria-label="Doküman yönetimi iş listesi">
            <TableHead>
              <TableRow>
                <Sort
                  field="recordNumber"
                  text="Kayıt / Kaynak"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <Sort
                  field="documentCode"
                  text="Doküman / Sürüm"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell>İnceleme / Eğitim</TableCell>
                <TableCell>Bölüm / Gizlilik</TableCell>
                <Sort
                  field="status"
                  text="Durum"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <Sort
                  field="plannedEffectiveDateUtc"
                  text="Yürürlük"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell align="right">İşlem</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {query.isLoading && <TableLoadingRows columns={7} />}{" "}
              {query.data?.items.map((x) => (
                <Row
                  key={x.id}
                  item={x}
                  open={() => setParams({ open: x.id })}
                />
              ))}
              {query.isSuccess && query.data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Box className="empty-state">
                      <Typography sx={{ fontWeight: 800 }}>
                        Henüz kontrollü doküman yok
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        İlk sürümlü dokümanı oluşturarak M.04 yaşam döngüsünü
                        başlatın.
                      </Typography>
                    </Box>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={Number(query.data?.totalCount ?? 0)}
          page={page}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage="Sayfa boyutu"
          onPageChange={(_, v) => setPage(v)}
          onRowsPerPageChange={(e) => {
            setPageSize(Number(e.target.value) as 10 | 25 | 50 | 100);
            setPage(0);
          }}
        />
      </Paper>
      <CreateDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => {
          setCreateOpen(false);
          setParams({ open: id });
        }}
      />
      <DetailsDialog id={params.get("open")} onClose={() => setParams({})} />
      <ModuleGuideDialog
        module="M.04"
        open={guideOpen}
        onClose={() => setGuideOpen(false)}
      />
    </Box>
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
function Row({ item, open }: { item: DocumentListItem; open: () => void }) {
  return (
    <TableRow hover>
      <TableCell>
        <Typography sx={{ fontWeight: 800, color: "primary.main" }}>
          {item.recordNumber}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {item.sourceRecordNumber
            ? `M.03: ${item.sourceRecordNumber}`
            : "Bağımsız doküman"}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 750 }}>
          {item.documentCode} · {item.title}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {item.documentType} · v{item.currentRevision}
        </Typography>
      </TableCell>
      <TableCell>
        <Stack direction="row" spacing={0.7}>
          <Chip
            size="small"
            color={item.pendingReviews ? "warning" : "success"}
            label={`${item.pendingReviews} inceleme`}
          />
          <Chip
            size="small"
            color={item.pendingTrainings ? "warning" : "success"}
            label={`${item.pendingTrainings} eğitim`}
          />
        </Stack>
      </TableCell>
      <TableCell>
        <Typography variant="body2">{item.department}</Typography>
        <Typography variant="caption" color="text.secondary">
          {item.confidentiality}
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={
            item.status === "Effective"
              ? "success"
              : item.status === "Withdrawn"
                ? "error"
                : "primary"
          }
          variant={item.status === "Effective" ? "filled" : "outlined"}
          label={label(item.status)}
        />
      </TableCell>
      <TableCell>{dt(item.plannedEffectiveDateUtc)}</TableCell>
      <TableCell align="right">
        <Button onClick={open}>Aç</Button>
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
  onCreated: (id: string) => void;
}) {
  const client = useQueryClient();
  const [v, setV] = useState<CreateDocumentInput>(initial());
  const changes = useQuery({
    queryKey: ["m04-source-changes"],
    queryFn: ({ signal }) =>
      searchChangeControls(
        {
          page: 1,
          pageSize: 100,
          sortBy: "createdAtUtc",
          sortDirection: "desc",
          filters: [],
        },
        signal,
      ),
    enabled: open,
    retry: false,
  });
  const set = <K extends keyof CreateDocumentInput>(
    k: K,
    value: CreateDocumentInput[K],
  ) => setV((x) => ({ ...x, [k]: value }));
  const mutation = useMutation({
    mutationFn: () =>
      createDocument({
        ...v,
        plannedEffectiveDateUtc: new Date(
          v.plannedEffectiveDateUtc,
        ).toISOString(),
      }),
    onSuccess: async (d) => {
      await client.invalidateQueries({ queryKey: ["documents"] });
      setV(initial());
      onCreated(d.record.id);
    },
  });
  const close = () => {
    setV(initial());
    onClose();
  };
  const valid =
    v.documentCode.trim() &&
    v.title.trim() &&
    v.owner.trim() &&
    v.department &&
    v.content.trim() &&
    v.changeSummary.trim() &&
    v.reviewDepartments.length > 0;
  return (
    <Dialog open={open} onClose={close} maxWidth="lg" fullWidth>
      <ModalHeader onClose={close}>
        <Box>
          <Typography variant="overline">M.04 · KONTROLLÜ DOKÜMAN</Typography>
          <Typography variant="h5" sx={{ fontWeight: 800 }}>
            Yeni doküman kaydı
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Sürüm, inceleme ve eğitim kapılarını ilk yayından önce tanımlayın.
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              fullWidth
              label="Doküman kodu"
              value={v.documentCode}
              onChange={(e) => set("documentCode", e.target.value)}
            />
            <TextField
              fullWidth
              label="Doküman başlığı"
              value={v.title}
              onChange={(e) => set("title", e.target.value)}
            />
            <Box sx={{ minWidth: 210 }}>
              <SearchableSelect
                size="medium"
                label="Doküman türü"
                value={v.documentType}
                options={types}
                onChange={(x) => set("documentType", x ?? "SOP")}
              />
            </Box>
          </Stack>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              fullWidth
              label="Doküman sahibi"
              value={v.owner}
              onChange={(e) => set("owner", e.target.value)}
            />
            <Autocomplete
              fullWidth
              options={departments}
              value={v.department}
              onChange={(_, x) => set("department", x ?? "")}
              renderInput={(p) => <TextField {...p} label="Sorumlu bölüm" />}
            />
            <Box sx={{ minWidth: 210 }}>
              <SearchableSelect
                size="medium"
                label="Gizlilik"
                value={v.confidentiality}
                options={["Kurum İçi", "Gizli", "Halka Açık"].map((value) => ({
                  value,
                  label: value,
                }))}
                onChange={(x) => set("confidentiality", x ?? "Kurum İçi")}
              />
            </Box>
          </Stack>
          <Autocomplete
            options={changes.data?.items ?? []}
            getOptionLabel={(x) => `${x.recordNumber} · ${x.title}`}
            value={
              changes.data?.items.find(
                (x) => x.id === v.sourceChangeControlId,
              ) ?? null
            }
            onChange={(_, x) => set("sourceChangeControlId", x?.id ?? null)}
            renderInput={(p) => (
              <TextField
                {...p}
                label="Kaynak M.03 değişiklik kontrolü (opsiyonel)"
                helperText="Doküman revizyonunu değişiklik kaydının kanıt zincirine bağlar."
              />
            )}
          />
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              fullWidth
              type="number"
              label="Gözden geçirme periyodu (ay)"
              value={v.reviewPeriodMonths}
              onChange={(e) =>
                set("reviewPeriodMonths", Number(e.target.value))
              }
            />
            <TextField
              fullWidth
              type="datetime-local"
              label="Planlanan yürürlük"
              value={v.plannedEffectiveDateUtc}
              onChange={(e) => set("plannedEffectiveDateUtc", e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
          <TextField
            multiline
            minRows={5}
            label="Doküman içeriği"
            value={v.content}
            onChange={(e) => set("content", e.target.value)}
          />
          <TextField
            multiline
            minRows={2}
            label="Değişiklik / ilk yayın özeti"
            value={v.changeSummary}
            onChange={(e) => set("changeSummary", e.target.value)}
          />
          <Autocomplete
            multiple
            options={departments}
            value={v.reviewDepartments}
            onChange={(_, x) => set("reviewDepartments", x)}
            renderInput={(p) => (
              <TextField
                {...p}
                label="İnceleme bölümleri"
                helperText="Kalite Güvence incelemesi sunucu tarafından otomatik eklenir."
              />
            )}
          />
          <Autocomplete
            multiple
            options={positions}
            value={v.trainingPositions}
            onChange={(_, x) => set("trainingPositions", x)}
            renderInput={(p) => (
              <TextField
                {...p}
                label="Yürürlük öncesi zorunlu eğitim pozisyonları"
              />
            )}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={!valid || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          {mutation.isPending ? "Oluşturuluyor…" : "Dokümanı oluştur"}
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
  const { can, user } = useAuth();
  const client = useQueryClient();
  const [tab, setTab] = useState(0);
  const [note, setNote] = useState("");
  const q = useQuery({
    queryKey: ["document-details", id],
    queryFn: ({ signal }) => getDocumentDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const update = async (d: DocumentDetails) => {
    client.setQueryData(["document-details", id], d);
    await client.invalidateQueries({ queryKey: ["documents"] });
    setNote("");
  };
  const transition = useMutation({
    mutationFn: (code: string) =>
      transitionDocument(
        id!,
        q.data!.record.version,
        code,
        note || undefined,
        false,
      ),
    onSuccess: update,
  });
  const record = q.data?.record;
  const close = () => {
    setTab(0);
    setNote("");
    onClose();
  };
  return (
    <Dialog
      open={Boolean(id)}
      onClose={close}
      maxWidth="xl"
      fullWidth
      className="record-details-dialog"
      slotProps={{ paper: { className: "record-details-paper" } }}
    >
      <ModalHeader onClose={close}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          className="record-detail-header"
        >
          <Box>
            <Stack direction="row" spacing={1}>
              <Typography className="record-header-number">
                {record?.recordNumber ?? "DOC"}
              </Typography>
              <Chip
                size="small"
                className="record-type-chip"
                label="DOKÜMAN KAYDI"
              />
            </Stack>
            <Typography variant="h5" className="record-header-title">
              {record
                ? `${record.documentCode} · ${record.title}`
                : "Doküman ayrıntısı"}
            </Typography>
          </Box>
          {record && (
            <Stack direction="row" spacing={1} className="record-header-badges">
              <Chip
                className="status-glass-chip"
                icon={<PublishedWithChangesRounded />}
                label={label(record.status)}
              />
              <Chip
                className="status-glass-chip"
                label={`Sürüm ${record.currentRevision}`}
              />
              {record.sourceRecordNumber && (
                <Chip
                  className="status-glass-chip"
                  icon={<LinkRounded />}
                  label={record.sourceRecordNumber}
                />
              )}
            </Stack>
          )}
        </Stack>
      </ModalHeader>
      {q.isLoading && <LinearProgress />}
      <DialogContent className="record-details-content">
        {q.isError && <Alert severity="error">{q.error.message}</Alert>}
        {transition.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {transition.error.message}
          </Alert>
        )}
        {record && q.data && (
          <>
            <Paper elevation={0} square className="record-detail-tabs-shell">
              <Tabs
                value={tab}
                onChange={(_, x: number) => setTab(x)}
                variant="scrollable"
              >
                <Tab
                  icon={<DescriptionRounded />}
                  iconPosition="start"
                  label="Genel Bakış"
                />
                <Tab
                  icon={<MenuBookRounded />}
                  iconPosition="start"
                  label={`Sürümler (${q.data.revisions.length})`}
                />
                <Tab
                  icon={<ApprovalRounded />}
                  iconPosition="start"
                  label={`İncelemeler (${q.data.reviews.length})`}
                />
                <Tab
                  icon={<SchoolRounded />}
                  iconPosition="start"
                  label={`Eğitim ve Okuma (${q.data.trainingRequirements.length})`}
                />
                <Tab
                  icon={<ContentCopyRounded />}
                  iconPosition="start"
                  label={`Kontrollü Kopyalar (${q.data.controlledCopies.length})`}
                />
                <Tab
                  icon={<TaskAltRounded />}
                  iconPosition="start"
                  label="Karar ve Akış"
                />
                <Tab
                  icon={<HistoryRounded />}
                  iconPosition="start"
                  label={`Geçmiş (${q.data.auditTrail.length})`}
                />
              </Tabs>
            </Paper>
            <Box className="detail-tab-panel">
              {tab === 0 && (
                <Stack spacing={3}>
                  <Paper variant="outlined" className="workflow-visual-card">
                    <Typography sx={{ fontWeight: 800, mb: 2 }}>
                      Kontrollü doküman yaşam döngüsü
                    </Typography>
                    <Box sx={{ overflowX: "auto" }}>
                      <Stepper
                        alternativeLabel
                        activeStep={Math.max(
                          0,
                          flow.findIndex((x) => x.value === record.status),
                        )}
                        className="visual-stepper"
                      >
                        {flow.map((x) => (
                          <Step
                            key={x.value}
                            completed={
                              flow.findIndex((s) => s.value === record.status) >
                                flow.indexOf(x) || record.status === "Effective"
                            }
                          >
                            <StepLabel>{x.label}</StepLabel>
                          </Step>
                        ))}
                      </Stepper>
                    </Box>
                  </Paper>
                  <Box className="detail-grid">
                    <Info
                      title="Doküman sınıfı"
                      value={`${record.documentType} · ${record.confidentiality}`}
                    />
                    <Info
                      title="Sahiplik"
                      value={`${record.owner} · ${record.department}`}
                    />
                    <Info
                      title="Yürürlük ve gözden geçirme"
                      value={`${dt(record.plannedEffectiveDateUtc)} · ${record.reviewPeriodMonths} ay`}
                    />
                    <Info
                      title="Kaynak değişiklik"
                      value={record.sourceRecordNumber ?? "Bağımsız doküman"}
                    />
                  </Box>
                  <RecordAssignments
                    aggregateType="Document"
                    aggregateId={record.id}
                  />
                </Stack>
              )}
              {tab === 1 && (
                <Versions
                  details={q.data}
                  canWrite={can(Permissions.documentWrite)}
                  onUpdate={update}
                />
              )}{" "}
              {tab === 2 && (
                <Reviews
                  details={q.data}
                  canReview={can(Permissions.documentReview)}
                  onUpdate={update}
                />
              )}{" "}
              {tab === 3 && (
                <Training
                  details={q.data}
                  canManage={hasAnyRole(user.roles, "Administrator", "QualityAssurance", "TrainingCoordinator")}
                  canRead={can(Permissions.documentRead)}
                  onUpdate={update}
                />
              )}{" "}
              {tab === 4 && (
                <Copies
                  details={q.data}
                  canDistribute={can(Permissions.documentDistribute)}
                  onUpdate={update}
                />
              )}{" "}
              {tab === 5 && (
                <Paper variant="outlined" className="transition-panel">
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>
                    Kontrollü karar ve yürürlük kapıları
                  </Typography>
                  <Alert severity="info" sx={{ my: 2 }}>
                    Tüm incelemeler ve zorunlu eğitimler tamamlanmadan yürürlük
                    butonu sunucuda reddedilir. Onay ve yürürlük kayıtları
                    değiştirilemez zaman damgası taşır.
                  </Alert>
                  {q.data.availableTransitions.some((x) => x.noteRequired && canUseTransition(x.code, user.roles)) && (
                      <TextField
                        fullWidth
                        multiline
                        minRows={2}
                        label="Karar gerekçesi / imza anlamı"
                        value={note}
                        onChange={(e) => setNote(e.target.value)}
                        sx={{ mb: 2 }}
                      />
                    )}
                  <Stack
                    direction="row"
                    spacing={1}
                    useFlexGap
                    sx={{ flexWrap: "wrap" }}
                  >
                    {q.data.availableTransitions.filter((x) => canUseTransition(x.code, user.roles)).map((x) => (
                        <Button
                          key={x.code}
                          variant="contained"
                          color={x.code === "withdraw" ? "error" : "primary"}
                          disabled={
                            transition.isPending ||
                            (x.noteRequired && !note.trim())
                          }
                          onClick={() => transition.mutate(x.code)}
                        >
                          {x.label}
                        </Button>
                      ))}
                    {q.data.availableTransitions.length === 0 &&
                      record.status === "RevisionPending" &&
                      can(Permissions.documentWrite) && (
                        <RevisionButton details={q.data} onUpdate={update} />
                      )}
                  </Stack>
                </Paper>
              )}
              {tab === 6 && (
                <Box className="history-tab-panel">
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>
                    Kronolojik doküman geçmişi
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mb: 2 }}
                  >
                    En yeni işlemden en eski işleme değiştirilemez sürüm, karar,
                    eğitim ve dağıtım zinciri.
                  </Typography>
                  <AuditTimeline events={q.data.auditTrail} labels={events} />
                </Box>
              )}
            </Box>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}

function Versions({
  details,
  canWrite,
  onUpdate,
}: {
  details: DocumentDetails;
  canWrite: boolean;
  onUpdate: (d: DocumentDetails) => void;
}) {
  const current = details.revisions.find((x) => x.isCurrent)!;
  const [content, setContent] = useState(current.content);
  const [summary, setSummary] = useState(current.changeSummary);
  const mutation = useMutation({
    mutationFn: () =>
      updateDocumentDraft(
        details.record.id,
        details.record.version,
        content,
        summary,
      ),
    onSuccess: onUpdate,
  });
  return (
    <Stack spacing={2}>
      {mutation.isError && (
        <Alert severity="error">{mutation.error.message}</Alert>
      )}
      {canWrite && ["Draft", "Writing"].includes(details.record.status) && (
        <Paper variant="outlined" className="capa-action-planner">
          <Typography sx={{ fontWeight: 800, mb: 1.5 }}>
            Sürüm {current.versionLabel} içeriğini düzenle
          </Typography>
          <TextField
            fullWidth
            multiline
            minRows={6}
            label="Kontrollü içerik"
            value={content}
            onChange={(e) => setContent(e.target.value)}
          />
          <TextField
            fullWidth
            multiline
            minRows={2}
            label="Değişiklik özeti"
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
            sx={{ mt: 1.5 }}
          />
          <Button
            sx={{ mt: 1.5 }}
            variant="contained"
            disabled={!content.trim() || !summary.trim() || mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            Taslağı kaydet
          </Button>
        </Paper>
      )}
      {details.revisions.map((x) => (
        <Paper
          key={x.id}
          variant="outlined"
          sx={{
            p: 2,
            borderLeft: 4,
            borderLeftColor: x.isCurrent ? "success.main" : "divider",
          }}
        >
          <Stack direction="row" sx={{ justifyContent: "space-between" }}>
            <Typography sx={{ fontWeight: 800 }}>
              Sürüm {x.versionLabel}
              {x.isCurrent ? " · Güncel" : ""}
            </Typography>
            <Chip
              size="small"
              color={x.status === "Effective" ? "success" : "default"}
              label={x.status}
            />
          </Stack>
          <Typography variant="body2" sx={{ mt: 1, whiteSpace: "pre-wrap" }}>
            {x.content}
          </Typography>
          <Alert severity="info" sx={{ mt: 1.5 }}>
            Değişiklik özeti: {x.changeSummary}
          </Alert>
        </Paper>
      ))}
    </Stack>
  );
}
function Reviews({
  details,
  canReview,
  onUpdate,
}: {
  details: DocumentDetails;
  canReview: boolean;
  onUpdate: (d: DocumentDetails) => void;
}) {
  const [comments, setComments] = useState<Record<string, string>>({});
  const mutation = useMutation({
    mutationFn: ({ id, approved }: { id: string; approved: boolean }) =>
      completeDocumentReview(
        details.record.id,
        id,
        details.record.version,
        approved,
        comments[id] ?? "",
      ),
    onSuccess: onUpdate,
  });
  return (
    <Stack spacing={2}>
      {mutation.isError && (
        <Alert severity="error">{mutation.error.message}</Alert>
      )}
      {details.reviews
        .filter((x) => x.revisionId === details.record.currentRevisionId)
        .map((x) => (
          <Paper
            key={x.id}
            variant="outlined"
            sx={{
              p: 2,
              borderLeft: 4,
              borderLeftColor:
                x.status === "Approved"
                  ? "success.main"
                  : x.status === "ChangesRequested"
                    ? "error.main"
                    : "warning.main",
            }}
          >
            <Stack direction="row" sx={{ justifyContent: "space-between" }}>
              <Box>
                <Typography sx={{ fontWeight: 800 }}>{x.department}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {x.reviewer}
                </Typography>
              </Box>
              <Chip
                size="small"
                color={
                  x.status === "Approved"
                    ? "success"
                    : x.status === "ChangesRequested"
                      ? "error"
                      : "warning"
                }
                label={
                  x.status === "Pending"
                    ? "Bekliyor"
                    : x.status === "Approved"
                      ? "Uygun"
                      : "Revizyon istendi"
                }
              />
            </Stack>
            {x.comment && (
              <Alert severity="info" sx={{ mt: 1.5 }}>
                {x.comment}
              </Alert>
            )}
            {canReview &&
              details.record.status === "Review" &&
              x.status === "Pending" && (
                <Stack spacing={1} sx={{ mt: 1.5 }}>
                  <TextField
                    fullWidth
                    label="İnceleme görüşü"
                    value={comments[x.id] ?? ""}
                    onChange={(e) =>
                      setComments((v) => ({ ...v, [x.id]: e.target.value }))
                    }
                  />
                  <Stack direction="row" spacing={1}>
                    <Button
                      color="success"
                      variant="contained"
                      disabled={!comments[x.id]?.trim()}
                      onClick={() =>
                        mutation.mutate({ id: x.id, approved: true })
                      }
                    >
                      Uygun bul
                    </Button>
                    <Button
                      color="error"
                      variant="outlined"
                      disabled={!comments[x.id]?.trim()}
                      onClick={() =>
                        mutation.mutate({ id: x.id, approved: false })
                      }
                    >
                      Revizyon iste
                    </Button>
                  </Stack>
                </Stack>
              )}
          </Paper>
        ))}
    </Stack>
  );
}
function Training({
  details,
  canManage,
  canRead,
  onUpdate,
}: {
  details: DocumentDetails;
  canManage: boolean;
  canRead: boolean;
  onUpdate: (d: DocumentDetails) => void;
}) {
  const [meaning, setMeaning] = useState(
    "Bu dokümanın güncel sürümünü okudum ve anladım.",
  );
  const read = useMutation({
    mutationFn: () =>
      acknowledgeDocument(details.record.id, details.record.version, meaning),
    onSuccess: onUpdate,
  });
  return (
    <Stack spacing={2}>
      {read.isError && (
        <Alert severity="error">
          {read.error?.message}
        </Alert>
      )}
      {details.trainingRequirements
        .filter((x) => x.revisionId === details.record.currentRevisionId)
        .map((x) => (
          <Paper key={x.id} variant="outlined" sx={{ p: 2 }}>
            <Stack direction="row" sx={{ justifyContent: "space-between" }}>
              <Box>
                <Typography sx={{ fontWeight: 800 }}>{x.position}</Typography>
                <Typography variant="caption">{x.assignedUser}</Typography>
              </Box>
              <Chip
                size="small"
                color={x.status === "Completed" ? "success" : "warning"}
                label={x.status === "Completed" ? "Tamamlandı" : "Bekliyor"}
              />
            </Stack>
            {x.evidence && (
              <Alert severity="success" sx={{ mt: 1 }}>
                {x.evidence}
              </Alert>
            )}
            {canManage &&
              details.record.status === "TrainingWaiting" &&
              x.status === "Pending" && (
                <Alert severity="info" sx={{ mt: 1.5 }} action={<Button href="/modules/m05" color="inherit">M.05 kaydını aç</Button>}>
                  Bu gereksinim M.05 eğitim görevi, okuma imzası, değerlendirme ve eğitmen onayı tamamlanınca otomatik kapanır.
                </Alert>
              )}
          </Paper>
        ))}
      {details.trainingRequirements.filter(
        (x) => x.revisionId === details.record.currentRevisionId,
      ).length === 0 && (
        <Alert severity="info">
          Bu sürüm için zorunlu pozisyon eğitimi tanımlanmadı.
        </Alert>
      )}
      {canRead && details.record.status === "Effective" && (
        <Paper variant="outlined" className="transition-panel">
          <Typography sx={{ fontWeight: 800, mb: 1 }}>
            Elektronik okuma kanıtı
          </Typography>
          <TextField
            fullWidth
            label="İmza anlamı"
            value={meaning}
            onChange={(e) => setMeaning(e.target.value)}
          />
          <Button
            sx={{ mt: 1.5 }}
            startIcon={<VisibilityRounded />}
            variant="contained"
            disabled={!meaning.trim() || read.isPending}
            onClick={() => read.mutate()}
          >
            Okudum ve anladım
          </Button>
        </Paper>
      )}
      <Typography sx={{ fontWeight: 800 }}>
        Okuma kayıtları ({details.readReceipts.length})
      </Typography>
      {details.readReceipts.map((x) => (
        <Paper key={x.id} variant="outlined" sx={{ p: 1.5 }}>
          <Typography sx={{ fontWeight: 700 }}>{x.userDisplayName}</Typography>
          <Typography variant="body2">{x.signatureMeaning}</Typography>
          <Typography variant="caption" color="text.secondary">
            {dt(x.acknowledgedAtUtc)}
          </Typography>
        </Paper>
      ))}
    </Stack>
  );
}
function Copies({
  details,
  canDistribute,
  onUpdate,
}: {
  details: DocumentDetails;
  canDistribute: boolean;
  onUpdate: (d: DocumentDetails) => void;
}) {
  const [number, setNumber] = useState("");
  const [recipient, setRecipient] = useState("");
  const [purpose, setPurpose] = useState("");
  const issue = useMutation({
    mutationFn: () =>
      issueControlledCopy(details.record.id, {
        expectedVersion: details.record.version,
        copyNumber: number,
        recipient,
        purpose,
        dueBackAtUtc: null,
      }),
    onSuccess: (d) => {
      setNumber("");
      setRecipient("");
      setPurpose("");
      onUpdate(d);
    },
  });
  const close = useMutation({
    mutationFn: ({ id, destroyed }: { id: string; destroyed: boolean }) =>
      closeControlledCopy(
        details.record.id,
        id,
        details.record.version,
        destroyed,
      ),
    onSuccess: onUpdate,
  });
  return (
    <Stack spacing={2}>
      {(issue.isError || close.isError) && (
        <Alert severity="error">
          {issue.error?.message ?? close.error?.message}
        </Alert>
      )}
      {canDistribute && details.record.status === "Effective" && (
        <Paper variant="outlined" className="capa-action-planner">
          <Typography sx={{ fontWeight: 800, mb: 1.5 }}>
            Numaralı kontrollü kopya dağıt
          </Typography>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            <TextField
              label="Kopya no"
              value={number}
              onChange={(e) => setNumber(e.target.value)}
            />
            <TextField
              fullWidth
              label="Teslim alan"
              value={recipient}
              onChange={(e) => setRecipient(e.target.value)}
            />
            <TextField
              fullWidth
              label="Kullanım amacı"
              value={purpose}
              onChange={(e) => setPurpose(e.target.value)}
            />
            <Button
              variant="contained"
              disabled={!number.trim() || !recipient.trim() || !purpose.trim()}
              onClick={() => issue.mutate()}
            >
              Dağıt
            </Button>
          </Stack>
        </Paper>
      )}
      {details.controlledCopies.map((x) => (
        <Paper key={x.id} variant="outlined" sx={{ p: 2 }}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            sx={{ justifyContent: "space-between", gap: 1 }}
          >
            <Box>
              <Typography sx={{ fontWeight: 800 }}>
                {x.copyNumber} · {x.recipient}
              </Typography>
              <Typography variant="body2">{x.purpose}</Typography>
              <Typography variant="caption" color="text.secondary">
                Dağıtım: {dt(x.issuedAtUtc)}
              </Typography>
            </Box>
            <Stack direction="row" spacing={1}>
              <Chip
                color={x.status === "Issued" ? "warning" : "success"}
                label={
                  x.status === "Issued"
                    ? "Dağıtımda"
                    : x.status === "Returned"
                      ? "İade"
                      : "İmha"
                }
              />
              {canDistribute && x.status === "Issued" && (
                <>
                  <Button
                    variant="outlined"
                    onClick={() => close.mutate({ id: x.id, destroyed: false })}
                  >
                    İade al
                  </Button>
                  <Button
                    color="error"
                    variant="outlined"
                    onClick={() => close.mutate({ id: x.id, destroyed: true })}
                  >
                    İmha et
                  </Button>
                </>
              )}
            </Stack>
          </Stack>
        </Paper>
      ))}
      {details.controlledCopies.length === 0 && (
        <Alert severity="info">Kontrollü basılı kopya dağıtılmadı.</Alert>
      )}
    </Stack>
  );
}
function RevisionButton({
  details,
  onUpdate,
}: {
  details: DocumentDetails;
  onUpdate: (d: DocumentDetails) => void;
}) {
  const [open, setOpen] = useState(false);
  const [summary, setSummary] = useState("");
  const [major, setMajor] = useState(false);
  const mutation = useMutation({
    mutationFn: () =>
      startDocumentRevision(details.record.id, {
        expectedVersion: details.record.version,
        major,
        changeSummary: summary,
        reviewDepartments: [details.record.department],
        trainingPositions: [],
      }),
    onSuccess: (d) => {
      setOpen(false);
      onUpdate(d);
    },
  });
  return (
    <>
      {
        <Button
          variant="contained"
          startIcon={<AddRounded />}
          onClick={() => setOpen(true)}
        >
          Yeni revizyonu başlat
        </Button>
      }
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <ModalHeader onClose={() => setOpen(false)}>
          <Box>
            <Typography variant="overline">M.04 · REVİZYON</Typography>
            <Typography variant="h5">Yeni doküman sürümü</Typography>
          </Box>
        </ModalHeader>
        <DialogContent dividers>
          <Stack spacing={2}>
            <SearchableSelect
              size="medium"
              label="Revizyon türü"
              value={major ? "major" : "minor"}
              options={[
                { value: "minor", label: "Minör revizyon" },
                { value: "major", label: "Majör revizyon" },
              ]}
              onChange={(x) => setMajor(x === "major")}
            />
            <TextField
              multiline
              minRows={3}
              label="Revizyon gerekçesi ve özeti"
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
            />
            {mutation.isError && (
              <Alert severity="error">{mutation.error.message}</Alert>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={!summary.trim()}
            onClick={() => mutation.mutate()}
          >
            Revizyonu aç
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
function Info({ title, value }: { title: string; value: string }) {
  return (
    <Paper variant="outlined" className="visual-detail-card detail-tone-teal">
      <Box className="detail-card-icon">
        <AssignmentIndRounded />
      </Box>
      <Box>
        <Typography className="detail-card-label">{title}</Typography>
        <Typography className="detail-card-value">{value}</Typography>
      </Box>
    </Paper>
  );
}
function initial(): CreateDocumentInput {
  const d = new Date(Date.now() - 60_000);
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return {
    sourceChangeControlId: null,
    documentCode: "",
    title: "",
    documentType: "SOP",
    owner: "",
    department: "",
    confidentiality: "Kurum İçi",
    reviewPeriodMonths: 12,
    plannedEffectiveDateUtc: d.toISOString().slice(0, 16),
    content: "",
    changeSummary: "İlk yayın",
    reviewDepartments: [],
    trainingPositions: [],
  };
}

function hasAnyRole(roles: string[], ...expected: string[]) {
  return expected.some((role) => roles.includes(role));
}

function canUseTransition(code: string, roles: string[]) {
  if (hasAnyRole(roles, "Administrator")) return true;
  if (code === "approve") return hasAnyRole(roles, "Approver");
  if (code === "release") return hasAnyRole(roles, "DocumentController", "QualityAssurance");
  return hasAnyRole(roles, "DocumentController", "QualityAssurance");
}
