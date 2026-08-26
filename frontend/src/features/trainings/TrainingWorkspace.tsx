import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
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
  AssignmentIndRounded,
  BadgeRounded,
  CheckCircleRounded,
  DescriptionRounded,
  DownloadRounded,
  FactCheckRounded,
  HistoryRounded,
  MenuBookRounded,
  QuizRounded,
  SchoolRounded,
  SettingsRounded,
  TaskAltRounded,
  VerifiedRounded,
  WorkspacePremiumRounded,
} from "@mui/icons-material";
import {
  acknowledgeTraining,
  createTraining,
  createTrainingMatrix,
  createTrainingLookupDefinition,
  downloadTrainingFinalReport,
  getTrainingDetails,
  getTrainingMatrix,
  getTrainingOptions,
  listTrainingLookupDefinitions,
  recordTrainingAssessment,
  searchTrainings,
  transitionTraining,
  updateTrainingLookupDefinition,
  type CreateTrainingInput,
  type TrainingDetails,
  type TrainingListItem,
  type TrainingMatrixRule,
  type TrainingLookupDefinition,
} from "../../api/trainings";
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
import { TrainingFilters } from "./TrainingFilters";
import {
  emptyTrainingFilters,
  toTrainingColumnFilters,
  type TrainingFilterState,
} from "./trainingFilterModel";

const statuses = [
  ["Planned", "Planlandı"],
  ["Assigned", "Atandı"],
  ["InProgress", "Devam ediyor"],
  ["Assessment", "Değerlendirme"],
  ["TrainerApproval", "Eğitmen onayı"],
  ["Completed", "Tamamlandı"],
  ["Failed", "Başarısız"],
  ["Expired", "Süresi doldu"],
  ["Cancelled", "İptal"],
].map(([value, label]) => ({ value, label }));
const stages = [
  "Planlandı",
  "Atandı",
  "Başladı",
  "Okuma / katılım",
  "Değerlendirme",
  "Eğitmen onayı",
  "Tamamlandı",
  "Yenileme",
];
const statusLabel = (value: string) =>
  statuses.find((x) => x.value === value)?.label ?? value;
const modeLabel = (value: string) =>
  ({
    ReadAndAcknowledge: "Oku ve anla",
    Exam: "Sınav",
    Practical: "Pratik yeterlilik",
    ExamAndPractical: "Sınav + pratik",
  })[value] ?? value;
const deliveryLabel = (value: string) =>
  ({
    Electronic: "Elektronik",
    Classroom: "Sınıf",
    OnTheJob: "İş başı",
    Hybrid: "Hibrit",
  })[value] ?? value;
const date = (value: string | null) =>
  value
    ? new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(value))
    : "—";
const auditLabels = {
  TrainingAssignmentCreated: "Eğitim görevi oluşturuldu",
  TrainingAssignmentCreatedFromDocument:
    "M.04 dokümanından eğitim görevi üretildi",
  TrainingStatusChanged: "Eğitim durumu değiştirildi",
  TrainingReadAcknowledged: "Okuma ve anlama imzalandı",
  TrainingAssessmentPassed: "Değerlendirme başarılı",
  TrainingAssessmentFailed: "Değerlendirme başarısız",
};

export function TrainingWorkspace() {
  const [params, setParams] = useSearchParams();
  const [view, setView] = useState(0);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] =
    useState<TrainingFilterState>(emptyTrainingFilters);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25);
  const [sortBy, setSortBy] = useState("createdAtUtc");
  const [direction, setDirection] = useState<"asc" | "desc">("desc");
  const [createOpen, setCreateOpen] = useState(false);
  const [matrixOpen, setMatrixOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  const [lookupOpen, setLookupOpen] = useState(false);
  const { can } = useAuth();
  const columnFilters = useMemo(
    () => toTrainingColumnFilters(filters),
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
    queryKey: ["trainings", request],
    queryFn: ({ signal }) => searchTrainings(request, signal),
    placeholderData: (p) => p,
    retry: false,
  });
  const matrix = useQuery({
    queryKey: ["training-matrix"],
    queryFn: ({ signal }) => getTrainingMatrix(signal),
    retry: false,
  });
  const sort = (field: string) => {
    if (field === sortBy)
      setDirection((value) => (value === "asc" ? "desc" : "asc"));
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
              <SchoolRounded />
            </Box>
            <Typography variant="h2">Eğitim ve yeterlilik yönetimi</Typography>
            <Chip size="small" color="success" label="M.05 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Pozisyon matrisini, dokümana bağlı görevleri, okuma imzasını, sınavı
            ve süreli yeterlilikleri tek zincirde yönetin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2}>
          <ModuleInfoButton module="M.05" onClick={() => setGuideOpen(true)} />
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setLookupOpen(true)}
            >
              Eğitim tanımları
            </Button>
          )}
          {can(Permissions.trainingManage) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
            >
              Yeni eğitim görevi
            </Button>
          )}
        </Stack>
      </Stack>
      <TrainingLookupDialog
        open={lookupOpen}
        onClose={() => setLookupOpen(false)}
      />
      <Paper variant="outlined" className="training-view-tabs">
        <Tabs value={view} onChange={(_, value) => setView(value)}>
          <Tab
            icon={<AssignmentIndRounded />}
            iconPosition="start"
            label="Eğitim iş listesi"
          />
          <Tab
            icon={<WorkspacePremiumRounded />}
            iconPosition="start"
            label={`Pozisyon matrisi (${matrix.data?.length ?? 0})`}
          />
        </Tabs>
      </Paper>
      {view === 0 ? (
        <>
          <Stack
            className="module-list-toolbar"
            direction="row"
            spacing={1.5}
            sx={{ mt: 2.5, alignItems: "center" }}
          >
            <AdvancedFilterButton
              open={filtersOpen}
              activeCount={columnFilters.length}
              onClick={() => setFiltersOpen((value) => !value)}
            />
            <Typography variant="body2" color="text.secondary">
              {query.data
                ? `${query.data.totalCount} eğitim görevi`
                : "Görevler yükleniyor"}
            </Typography>
          </Stack>
          {filtersOpen && (
            <TrainingFilters
              value={filters}
              statuses={statuses}
              onApply={(value) => {
                setFilters(value);
                setPage(0);
              }}
            />
          )}
          <Paper
            className="deviation-table-card"
            elevation={0}
            sx={{ mt: 2.5 }}
          >
            {query.isError && (
              <Alert severity="error" sx={{ m: 2 }}>
                {query.error.message}
              </Alert>
            )}
            <TableContainer>
              <Table aria-label="Eğitim yönetimi iş listesi">
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
                      field="courseCode"
                      text="Eğitim / kaynak"
                      current={sortBy}
                      direction={direction}
                      onSort={sort}
                    />
                    <Sort
                      field="employeeName"
                      text="Çalışan / pozisyon"
                      current={sortBy}
                      direction={direction}
                      onSort={sort}
                    />
                    <TableCell>Yöntem</TableCell>
                    <Sort
                      field="status"
                      text="İlerleme"
                      current={sortBy}
                      direction={direction}
                      onSort={sort}
                    />
                    <Sort
                      field="dueAtUtc"
                      text="Hedef / geçerlilik"
                      current={sortBy}
                      direction={direction}
                      onSort={sort}
                    />
                    <TableCell align="right">İşlem</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {query.isLoading && <TableLoadingRows columns={7} />}{" "}
                  {query.data?.items.map((item) => (
                    <TrainingRow
                      key={item.id}
                      item={item}
                      open={() => setParams({ open: item.id })}
                    />
                  ))}
                  {query.isSuccess && query.data.items.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={7}>
                        <Box className="empty-state">
                          <SchoolRounded />
                          <Typography sx={{ fontWeight: 800 }}>
                            Henüz eğitim görevi yok
                          </Typography>
                          <Typography variant="body2" color="text.secondary">
                            Görev oluşturun veya M.04 doküman eğitim kapısını
                            çalıştırın.
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
              onPageChange={(_, value) => setPage(value)}
              onRowsPerPageChange={(event) => {
                setPageSize(Number(event.target.value) as 10 | 25 | 50 | 100);
                setPage(0);
              }}
            />
          </Paper>
        </>
      ) : (
        <MatrixView
          rows={matrix.data ?? []}
          loading={matrix.isLoading}
          error={matrix.error?.message}
          canCreate={can(Permissions.trainingManage)}
          onCreate={() => setMatrixOpen(true)}
        />
      )}
      <CreateTrainingDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => {
          setCreateOpen(false);
          setParams({ open: id });
        }}
      />
      <CreateMatrixDialog
        open={matrixOpen}
        onClose={() => setMatrixOpen(false)}
      />
      <TrainingDetailsDialog
        id={params.get("open")}
        onClose={() => setParams({})}
      />
      <ModuleGuideDialog
        module="M.05"
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
  onSort: (field: string) => void;
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
function TrainingRow({
  item,
  open,
}: {
  item: TrainingListItem;
  open: () => void;
}) {
  return (
    <TableRow hover>
      <TableCell>
        <Typography sx={{ fontWeight: 800, color: "primary.main" }}>
          {item.recordNumber}
        </Typography>
        {item.isCriticalQualification && (
          <Chip size="small" color="warning" label="Kritik yeterlilik" />
        )}
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 750 }}>
          {item.courseCode} · {item.courseTitle}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {item.documentCode
            ? `M.04 ${item.documentCode} · v${item.documentRevision}`
            : "Bağımsız eğitim"}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2" sx={{ fontWeight: 700 }}>
          {item.employeeName}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {item.position}
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          variant="outlined"
          label={modeLabel(item.assessmentMode)}
        />
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ mt: 0.5, display: "block" }}
        >
          {deliveryLabel(item.deliveryMethod)} · {item.attemptCount} deneme
        </Typography>
      </TableCell>
      <TableCell sx={{ minWidth: 170 }}>
        <Stack direction="row" sx={{ justifyContent: "space-between" }}>
          <Typography variant="caption" sx={{ fontWeight: 750 }}>
            {statusLabel(item.status)}
          </Typography>
          <Typography variant="caption">%{item.progressPercent}</Typography>
        </Stack>
        <LinearProgress
          variant="determinate"
          value={item.progressPercent}
          className="training-progress"
        />
      </TableCell>
      <TableCell>
        <Typography variant="body2">{date(item.dueAtUtc)}</Typography>
        <Typography variant="caption" color="text.secondary">
          Bitiş: {date(item.expiresAtUtc)}
        </Typography>
      </TableCell>
      <TableCell align="right">
        <RecordActionMenu onOpen={open} />
      </TableCell>
    </TableRow>
  );
}

function MatrixView({
  rows,
  loading,
  error,
  canCreate,
  onCreate,
}: {
  rows: TrainingMatrixRule[];
  loading: boolean;
  error?: string;
  canCreate: boolean;
  onCreate: () => void;
}) {
  return (
    <Paper className="training-matrix-shell" variant="outlined">
      <Stack
        direction={{ xs: "column", sm: "row" }}
        sx={{
          justifyContent: "space-between",
          alignItems: { sm: "center" },
          gap: 2,
          p: 2.5,
        }}
      >
        <Box>
          <Typography variant="h5">Pozisyon–eğitim matrisi</Typography>
          <Typography color="text.secondary">
            Bir pozisyon için hangi eğitimin, hangi yöntemle ve hangi süreyle
            zorunlu olduğunu tanımlar.
          </Typography>
        </Box>
        {canCreate && (
          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={onCreate}
          >
            Matris kuralı ekle
          </Button>
        )}
      </Stack>
      {error && (
        <Alert severity="error" sx={{ m: 2 }}>
          {error}
        </Alert>
      )}
      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Pozisyon</TableCell>
              <TableCell>Eğitim</TableCell>
              <TableCell>Bağlı doküman</TableCell>
              <TableCell>Yöntem</TableCell>
              <TableCell>Başarı / geçerlilik</TableCell>
              <TableCell>Yeterlilik</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && <TableLoadingRows columns={6} />}{" "}
            {rows.map((row) => (
              <TableRow key={row.id}>
                <TableCell>
                  <Typography sx={{ fontWeight: 750 }}>
                    {row.position}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography sx={{ fontWeight: 750 }}>
                    {row.courseCode}
                  </Typography>
                  <Typography variant="caption">{row.courseTitle}</Typography>
                </TableCell>
                <TableCell>{row.documentCode ?? "Bağımsız eğitim"}</TableCell>
                <TableCell>
                  {modeLabel(row.assessmentMode)} ·{" "}
                  {deliveryLabel(row.deliveryMethod)}
                </TableCell>
                <TableCell>
                  ≥ %{row.passingScore} · {row.validityMonths} ay
                </TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    color={row.isCriticalQualification ? "warning" : "default"}
                    label={row.isCriticalQualification ? "Kritik" : "Standart"}
                  />
                </TableCell>
              </TableRow>
            ))}
            {!loading && rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Box className="empty-state">
                    <Typography sx={{ fontWeight: 800 }}>
                      Matris henüz tanımlanmadı
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      İlk pozisyon–eğitim kuralını ekleyin.
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  );
}

const trainingLookupCategories = [
  { value: "AssessmentMode", label: "Değerlendirme yöntemi" },
  { value: "DeliveryMethod", label: "Eğitim yöntemi" },
];
function TrainingLookupDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const [category, setCategory] = useState("AssessmentMode");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [sortOrder, setSortOrder] = useState(100);
  const q = useQuery({
    queryKey: ["training-lookup-definitions"],
    queryFn: ({ signal }) => listTrainingLookupDefinitions(signal),
    enabled: open,
    retry: false,
  });
  const refresh = async () => {
    await client.invalidateQueries({
      queryKey: ["training-lookup-definitions"],
    });
    await client.invalidateQueries({ queryKey: ["training-options"] });
  };
  const create = useMutation({
    mutationFn: () =>
      createTrainingLookupDefinition({ category, code, name, sortOrder }),
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
    }) => updateTrainingLookupDefinition(x.id, x),
    onSuccess: refresh,
  });
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.05 · YÖNETİLEN LOOKUP</Typography>
          <Typography variant="h5">Eğitim tanımları</Typography>
          <Typography color="text.secondary">
            Kodlar geçmiş kayıtlarda snapshot olarak korunur; ad ve aktiflik
            yeni seçimleri yönetir.
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        {(q.isError || create.isError || update.isError) && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {q.error?.message ?? create.error?.message ?? update.error?.message}
          </Alert>
        )}
        <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
            <Box sx={{ minWidth: 190 }}>
              <SearchableSelect
                label="Kategori"
                value={category}
                options={trainingLookupCategories}
                onChange={(v) => setCategory(v ?? "AssessmentMode")}
                size="medium"
              />
            </Box>
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
              disabled={!code.trim() || !name.trim() || create.isPending}
              onClick={() => create.mutate()}
            >
              Ekle
            </Button>
          </Stack>
        </Paper>
        <Stack spacing={1}>
          {q.data?.map((x) => (
            <TrainingLookupRow
              key={x.id}
              item={x}
              saving={update.isPending}
              onSave={(name, sortOrder, isActive) =>
                update.mutate({ id: x.id, name, sortOrder, isActive })
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
function TrainingLookupRow({
  item,
  saving,
  onSave,
}: {
  item: TrainingLookupDefinition;
  saving: boolean;
  onSave: (name: string, sortOrder: number, isActive: boolean) => void;
}) {
  const [name, setName] = useState(item.name);
  const [sortOrder, setSortOrder] = useState(item.sortOrder);
  const [isActive, setActive] = useState(item.isActive);
  return (
    <Paper variant="outlined" sx={{ p: 1.5 }}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={1.5}
        sx={{ alignItems: { md: "center" } }}
      >
        <Chip
          size="small"
          label={
            trainingLookupCategories.find((x) => x.value === item.category)
              ?.label ?? item.category
          }
        />
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
          value={sortOrder}
          onChange={(e) => setSortOrder(Number(e.target.value))}
          sx={{ width: 90 }}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={isActive}
              onChange={(e) => setActive(e.target.checked)}
            />
          }
          label="Aktif"
        />
        <Button
          variant="outlined"
          disabled={!name.trim() || saving}
          onClick={() => onSave(name, sortOrder, isActive)}
        >
          Kaydet
        </Button>
      </Stack>
    </Paper>
  );
}

function CreateTrainingDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (id: string) => void;
}) {
  const client = useQueryClient();
  const options = useQuery({
    queryKey: ["training-options"],
    queryFn: ({ signal }) => getTrainingOptions(signal),
    enabled: open,
    retry: false,
  });
  const [form, setForm] = useState<CreateTrainingInput>({
    employeeUserId: "",
    positionId: "",
    courseCode: "",
    courseTitle: "",
    assessmentMode: "ReadAndAcknowledge",
    deliveryMethod: "Electronic",
    passingScore: 80,
    validityMonths: 12,
    maxAttempts: 3,
    isCriticalQualification: false,
    dueAtUtc: new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 16),
    assignNow: true,
  });
  useEffect(() => {
    if (!options.data) return;
    setForm((x) => ({
      ...x,
      assessmentMode: options.data!.assessmentModes.some(
        (v) => v.code === x.assessmentMode,
      )
        ? x.assessmentMode
        : (options.data!.assessmentModes[0]?.code ?? ""),
      deliveryMethod: options.data!.deliveryMethods.some(
        (v) => v.code === x.deliveryMethod,
      )
        ? x.deliveryMethod
        : (options.data!.deliveryMethods[0]?.code ?? ""),
    }));
  }, [options.data]);
  const mutation = useMutation({
    mutationFn: createTraining,
    onSuccess: (data) => {
      client.invalidateQueries({ queryKey: ["trainings"] });
      onCreated(data.record.id);
    },
  });
  const employee = options.data?.employees.find(
    (x) => x.id === form.employeeUserId,
  );
  const document = options.data?.documents.find(
    (x) => x.id === form.controlledDocumentId,
  );
  const submit = () =>
    mutation.mutate({
      ...form,
      positionId: employee?.positionId || form.positionId,
      documentRevisionId: document?.revisionId ?? null,
      dueAtUtc: new Date(form.dueAtUtc).toISOString(),
    });
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.05 · KONTROLLÜ ATAMA</Typography>
          <Typography variant="h5">Yeni eğitim görevi</Typography>
        </Box>
      </ModalHeader>
      <DialogContent className="modal-scroll-content">
        <Box className="form-grid two-columns">
          <SearchableSelect
            label="Çalışan"
            value={form.employeeUserId}
            options={(options.data?.employees ?? []).map((x) => ({
              value: x.id,
              label: `${x.displayName} · ${x.position}`,
            }))}
            onChange={(value) =>
              setForm((x) => ({
                ...x,
                employeeUserId: value ?? "",
                positionId:
                  options.data?.employees.find((e) => e.id === value)
                    ?.positionId ?? "",
              }))
            }
          />
          <TextField
            label="Pozisyon"
            value={employee?.position ?? ""}
            disabled
          />
          <SearchableSelect
            label="Bağlı M.04 dokümanı (opsiyonel)"
            value={form.controlledDocumentId ?? ""}
            options={[
              { value: "", label: "Bağımsız eğitim" },
              ...(options.data?.documents ?? []).map((x) => ({
                value: x.id,
                label: `${x.documentCode} · ${x.title} · v${x.revision}`,
              })),
            ]}
            onChange={(value) => {
              const doc = options.data?.documents.find((x) => x.id === value);
              setForm((x) => ({
                ...x,
                controlledDocumentId: value || null,
                documentRevisionId: doc?.revisionId ?? null,
                courseCode: doc?.documentCode ?? x.courseCode,
                courseTitle: doc?.title ?? x.courseTitle,
              }));
            }}
          />
          <TextField
            label="Eğitim kodu"
            value={form.courseCode}
            onChange={(e) =>
              setForm((x) => ({ ...x, courseCode: e.target.value }))
            }
          />
          <TextField
            label="Eğitim adı"
            value={form.courseTitle}
            onChange={(e) =>
              setForm((x) => ({ ...x, courseTitle: e.target.value }))
            }
            sx={{ gridColumn: "1/-1" }}
          />
          <SearchableSelect
            label="Değerlendirme yöntemi"
            value={form.assessmentMode}
            options={(options.data?.assessmentModes ?? []).map((x) => ({
              value: x.code,
              label: x.name,
            }))}
            onChange={(value) =>
              setForm((x) => ({
                ...x,
                assessmentMode: value ?? "ReadAndAcknowledge",
              }))
            }
          />
          <SearchableSelect
            label="Eğitim yöntemi"
            value={form.deliveryMethod}
            options={(options.data?.deliveryMethods ?? []).map((x) => ({
              value: x.code,
              label: x.name,
            }))}
            onChange={(value) =>
              setForm((x) => ({ ...x, deliveryMethod: value ?? "Electronic" }))
            }
          />
          <TextField
            type="number"
            label="Geçme puanı"
            value={form.passingScore}
            onChange={(e) =>
              setForm((x) => ({ ...x, passingScore: Number(e.target.value) }))
            }
          />
          <TextField
            type="number"
            label="Geçerlilik (ay)"
            value={form.validityMonths}
            onChange={(e) =>
              setForm((x) => ({ ...x, validityMonths: Number(e.target.value) }))
            }
          />
          <TextField
            type="datetime-local"
            label="Hedef tarih"
            value={form.dueAtUtc}
            onChange={(e) =>
              setForm((x) => ({ ...x, dueAtUtc: e.target.value }))
            }
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label="Oturum kodu"
            value={form.sessionCode ?? ""}
            onChange={(e) =>
              setForm((x) => ({ ...x, sessionCode: e.target.value }))
            }
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={form.isCriticalQualification}
                onChange={(e) =>
                  setForm((x) => ({
                    ...x,
                    isCriticalQualification: e.target.checked,
                  }))
                }
              />
            }
            label="Kritik iş yeterliliği"
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={form.assignNow}
                onChange={(e) =>
                  setForm((x) => ({ ...x, assignNow: e.target.checked }))
                }
              />
            }
            label="Hemen çalışana ata"
          />
        </Box>
        {mutation.isError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {mutation.error.message}
          </Alert>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={
            !form.employeeUserId ||
            !form.positionId ||
            !form.courseCode ||
            !form.courseTitle ||
            mutation.isPending
          }
          onClick={submit}
        >
          Görevi oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function CreateMatrixDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const options = useQuery({
    queryKey: ["training-options"],
    queryFn: ({ signal }) => getTrainingOptions(signal),
    enabled: open,
    retry: false,
  });
  const [form, setForm] = useState({
    positionId: "",
    courseCode: "",
    courseTitle: "",
    controlledDocumentId: null as string | null,
    assessmentMode: "ReadAndAcknowledge",
    deliveryMethod: "Electronic",
    passingScore: 80,
    validityMonths: 12,
    isCriticalQualification: false,
    effectiveAtUtc: new Date().toISOString(),
  });
  useEffect(() => {
    if (!options.data) return;
    setForm((x) => ({
      ...x,
      assessmentMode: options.data!.assessmentModes.some(
        (v) => v.code === x.assessmentMode,
      )
        ? x.assessmentMode
        : (options.data!.assessmentModes[0]?.code ?? ""),
      deliveryMethod: options.data!.deliveryMethods.some(
        (v) => v.code === x.deliveryMethod,
      )
        ? x.deliveryMethod
        : (options.data!.deliveryMethods[0]?.code ?? ""),
    }));
  }, [options.data]);
  const mutation = useMutation({
    mutationFn: createTrainingMatrix,
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ["training-matrix"] });
      onClose();
    },
  });
  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.05 · YETKİNLİK MATRİSİ</Typography>
          <Typography variant="h5">Pozisyon kuralı ekle</Typography>
        </Box>
      </ModalHeader>
      <DialogContent className="modal-scroll-content">
        <Stack spacing={2}>
          <SearchableSelect
            label="Pozisyon"
            value={form.positionId}
            options={(options.data?.positions ?? []).map((value) => ({
              value: value.id,
              label: value.name,
            }))}
            onChange={(value) =>
              setForm((x) => ({ ...x, positionId: value ?? "" }))
            }
          />
          <SearchableSelect
            label="Bağlı doküman"
            value={form.controlledDocumentId ?? ""}
            options={[
              { value: "", label: "Bağımsız eğitim" },
              ...(options.data?.documents ?? []).map((x) => ({
                value: x.id,
                label: `${x.documentCode} · ${x.title}`,
              })),
            ]}
            onChange={(value) => {
              const doc = options.data?.documents.find((x) => x.id === value);
              setForm((x) => ({
                ...x,
                controlledDocumentId: value || null,
                courseCode: doc?.documentCode ?? x.courseCode,
                courseTitle: doc?.title ?? x.courseTitle,
              }));
            }}
          />
          <TextField
            label="Eğitim kodu"
            value={form.courseCode}
            onChange={(e) =>
              setForm((x) => ({ ...x, courseCode: e.target.value }))
            }
          />
          <TextField
            label="Eğitim adı"
            value={form.courseTitle}
            onChange={(e) =>
              setForm((x) => ({ ...x, courseTitle: e.target.value }))
            }
          />
          <SearchableSelect
            label="Değerlendirme"
            value={form.assessmentMode}
            options={(options.data?.assessmentModes ?? []).map((x) => ({
              value: x.code,
              label: x.name,
            }))}
            onChange={(value) =>
              setForm((x) => ({
                ...x,
                assessmentMode: value ?? "ReadAndAcknowledge",
              }))
            }
          />
          <SearchableSelect
            label="Eğitim yöntemi"
            value={form.deliveryMethod}
            options={(options.data?.deliveryMethods ?? []).map((x) => ({
              value: x.code,
              label: x.name,
            }))}
            onChange={(value) =>
              setForm((x) => ({ ...x, deliveryMethod: value ?? "" }))
            }
          />
          <Stack direction="row" spacing={2}>
            <TextField
              fullWidth
              type="number"
              label="Geçme puanı"
              value={form.passingScore}
              onChange={(e) =>
                setForm((x) => ({ ...x, passingScore: Number(e.target.value) }))
              }
            />
            <TextField
              fullWidth
              type="number"
              label="Geçerlilik (ay)"
              value={form.validityMonths}
              onChange={(e) =>
                setForm((x) => ({
                  ...x,
                  validityMonths: Number(e.target.value),
                }))
              }
            />
          </Stack>
          <FormControlLabel
            control={
              <Checkbox
                checked={form.isCriticalQualification}
                onChange={(e) =>
                  setForm((x) => ({
                    ...x,
                    isCriticalQualification: e.target.checked,
                  }))
                }
              />
            }
            label="Kritik yeterlilik"
          />
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={
            !form.positionId ||
            !form.courseCode ||
            !form.courseTitle ||
            mutation.isPending
          }
          onClick={() => mutation.mutate(form)}
        >
          Kuralı ekle
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function TrainingDetailsDialog({
  id,
  onClose,
}: {
  id: string | null;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const [tab, setTab] = useState(0);
  const [signature, setSignature] = useState(
    "Dokümanı okudum, anladım ve görevlerimde uygulayacağım.",
  );
  const [score, setScore] = useState(100);
  const [evidence, setEvidence] = useState("Elektronik değerlendirme kaydı");
  const [note, setNote] = useState(
    "Eğitim ve yeterlilik kanıtları uygun bulundu.",
  );
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureMeaningAccepted, setSignatureMeaningAccepted] =
    useState(false);
  const query = useQuery({
    queryKey: ["training", id],
    queryFn: ({ signal }) => getTrainingDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const mutate = useMutation({
    mutationFn: (action: () => Promise<TrainingDetails>) => action(),
    onSuccess: (data) => {
      client.setQueryData(["training", id], data);
      client.invalidateQueries({ queryKey: ["trainings"] });
      client.invalidateQueries({
        queryKey: ["workflow-assignments", "Training", id],
      });
    },
  });
  const report = useMutation({
    mutationFn: () => downloadTrainingFinalReport(id!),
    onSuccess: (x) => {
      const url = URL.createObjectURL(x.blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = x.fileName;
      link.click();
      URL.revokeObjectURL(url);
    },
  });
  const data = query.data;
  const record = data?.record;
  const transition = (code: string) =>
    record &&
    mutate.mutate(() =>
      transitionTraining(
        record.id,
        record.version,
        code,
        code === "approve" || code === "cancel" ? note : undefined,
        code === "approve" ? signaturePassword : undefined,
        code === "approve" ? signatureMeaningAccepted : false,
      ),
    );
  const step = record
    ? ({
        Planned: 0,
        Assigned: 1,
        InProgress: 3,
        Assessment: 4,
        TrainerApproval: 5,
        Completed: 6,
        Failed: 4,
        Expired: 7,
        Cancelled: 0,
      }[record.status] ?? 0)
    : 0;
  return (
    <Dialog
      open={Boolean(id)}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      slotProps={{ paper: { className: "record-detail-paper" } }}
    >
      <ModalHeader onClose={onClose}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          className="record-detail-header"
        >
          <Box>
            <Typography variant="overline">
              {record?.recordNumber ?? "EĞİTİM KAYDI"} · {record?.courseCode}
            </Typography>
            <Typography variant="h5">
              {record?.courseTitle ?? "Eğitim yükleniyor"}
            </Typography>
          </Box>
          {record && (
            <Stack direction="row" spacing={1} className="record-header-badges">
              <Chip
                icon={<SchoolRounded />}
                label={statusLabel(record.status)}
              />
              {record.isCriticalQualification && (
                <Chip
                  color="warning"
                  icon={<WorkspacePremiumRounded />}
                  label="Kritik yeterlilik"
                />
              )}
              {record.status === "Completed" && (
                <Button
                  color="inherit"
                  variant="outlined"
                  startIcon={<DownloadRounded />}
                  disabled={report.isPending}
                  onClick={() => report.mutate()}
                >
                  Nihai PDF
                </Button>
              )}
            </Stack>
          )}
        </Stack>
      </ModalHeader>
      {query.isLoading && <LinearProgress />}
      {query.isError && (
        <Alert severity="error" sx={{ m: 3 }}>
          {query.error.message}
        </Alert>
      )}
      {report.isError && (
        <Alert severity="error" sx={{ m: 3 }}>
          {report.error.message}
        </Alert>
      )}
      {data && record && (
        <Box className="record-detail-workspace">
          <Box className="record-detail-tabs-shell">
            <Tabs
              value={tab}
              onChange={(_, value) => setTab(value)}
              orientation="vertical"
              variant="scrollable"
            >
              <Tab
                icon={<SchoolRounded />}
                iconPosition="start"
                label="Genel bakış"
              />
              <Tab
                icon={<MenuBookRounded />}
                iconPosition="start"
                label="İçerik ve imza"
              />
              <Tab
                icon={<QuizRounded />}
                iconPosition="start"
                label={`Değerlendirme (${data.attempts.length})`}
              />
              <Tab
                icon={<WorkspacePremiumRounded />}
                iconPosition="start"
                label="Yeterlilik"
              />
              <Tab
                icon={<HistoryRounded />}
                iconPosition="start"
                label={`Geçmiş (${data.auditTrail.length})`}
              />
            </Tabs>
          </Box>
          <DialogContent className="detail-tab-panel">
            {tab === 0 && (
              <Box className="record-tab-canvas overview-canvas">
                <Paper variant="outlined" className="training-flow-card">
                  <Stack
                    direction="row"
                    sx={{
                      justifyContent: "space-between",
                      alignItems: "center",
                      mb: 2,
                    }}
                  >
                    <Box>
                      <Typography variant="h6">
                        Kontrollü eğitim akışı
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Güncel aşama: {statusLabel(record.status)}
                      </Typography>
                    </Box>
                    <Chip
                      color={
                        record.status === "Completed" ? "success" : "primary"
                      }
                      label={`%${record.progressPercent}`}
                    />
                  </Stack>
                  <Box className="training-stepper-scroll">
                    <Stepper
                      activeStep={step}
                      alternativeLabel
                      className="training-stepper"
                    >
                      {stages.map((label, index) => (
                        <Step
                          key={label}
                          completed={
                            index < step ||
                            (record.status === "Completed" && index === step)
                          }
                        >
                          <StepLabel>{label}</StepLabel>
                        </Step>
                      ))}
                    </Stepper>
                  </Box>
                </Paper>
                <Box className="training-detail-grid">
                  <InfoCard
                    icon={<BadgeRounded />}
                    label="Katılımcı"
                    value={record.employeeName}
                    helper={record.position}
                  />
                  <InfoCard
                    icon={<DescriptionRounded />}
                    label="Eğitim kaynağı"
                    value={`${record.courseCode} · ${record.courseTitle}`}
                    helper={
                      record.documentCode
                        ? `M.04 ${record.documentCode} · v${record.documentRevision}`
                        : "Bağımsız eğitim"
                    }
                  />
                  <InfoCard
                    icon={<FactCheckRounded />}
                    label="Yöntem"
                    value={modeLabel(record.assessmentMode)}
                    helper={`${deliveryLabel(record.deliveryMethod)} · Geçme ≥ %${record.passingScore}`}
                  />
                  <InfoCard
                    icon={<TaskAltRounded />}
                    label="Hedef"
                    value={date(record.dueAtUtc)}
                    helper={`${record.validityMonths} ay geçerli · ${record.maxAttempts} deneme`}
                  />
                </Box>
                <RecordAssignments
                  aggregateType="Training"
                  aggregateId={record.id}
                />
              </Box>
            )}
            {tab === 1 && (
              <Paper
                variant="outlined"
                className="record-tab-canvas training-action-card"
              >
                <Stack
                  direction="row"
                  spacing={1.3}
                  sx={{ alignItems: "center", mb: 2 }}
                >
                  <Box className="section-heading-icon tone-teal">
                    <MenuBookRounded />
                  </Box>
                  <Box>
                    <Typography variant="h6">Okuma ve anlama kanıtı</Typography>
                    <Typography color="text.secondary">
                      İmza anlamı ve zaman damgası değiştirilemez geçmişe
                      kaydedilir.
                    </Typography>
                  </Box>
                </Stack>
                {record.acknowledgedAtUtc ? (
                  <Alert severity="success" icon={<CheckCircleRounded />}>
                    “{record.acknowledgementMeaning}” ·{" "}
                    {date(record.acknowledgedAtUtc)}
                  </Alert>
                ) : (
                  <>
                    <TextField
                      fullWidth
                      multiline
                      minRows={3}
                      label="Elektronik imza anlamı"
                      value={signature}
                      onChange={(e) => setSignature(e.target.value)}
                    />
                    <TextField
                      sx={{ mt: 2 }}
                      fullWidth
                      type="password"
                      autoComplete="current-password"
                      label="E-imza parolası"
                      value={signaturePassword}
                      onChange={(e) => setSignaturePassword(e.target.value)}
                    />
                    <FormControlLabel
                      control={
                        <Checkbox
                          checked={signatureMeaningAccepted}
                          onChange={(e) =>
                            setSignatureMeaningAccepted(e.target.checked)
                          }
                        />
                      }
                      label="Bu işlemin elektronik imza anlamını kabul ediyorum."
                    />
                    <Button
                      sx={{ mt: 2 }}
                      variant="contained"
                      disabled={
                        record.status !== "InProgress" ||
                        mutate.isPending ||
                        !data.actionableTaskRoles.includes("Learner") ||
                        !signaturePassword ||
                        !signatureMeaningAccepted
                      }
                      onClick={() =>
                        mutate.mutate(() =>
                          acknowledgeTraining(
                            record.id,
                            record.version,
                            signature,
                            signaturePassword,
                            signatureMeaningAccepted,
                          ),
                        )
                      }
                    >
                      Okudum ve anladım olarak imzala
                    </Button>
                  </>
                )}
              </Paper>
            )}
            {tab === 2 && (
              <Stack className="record-tab-canvas" spacing={2}>
                <Paper variant="outlined" className="training-action-card">
                  <Typography variant="h6">
                    Değerlendirme sonucu kaydet
                  </Typography>
                  <Typography color="text.secondary" sx={{ mb: 2 }}>
                    Sınav puanı ve pratik yeterlilik eğitmen tarafından kanıtla
                    kaydedilir.
                  </Typography>
                  <TextField
                    sx={{ mb: 1.5 }}
                    fullWidth
                    type="password"
                    autoComplete="current-password"
                    label="E-imza parolası"
                    value={signaturePassword}
                    onChange={(e) => setSignaturePassword(e.target.value)}
                  />
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={signatureMeaningAccepted}
                        onChange={(e) =>
                          setSignatureMeaningAccepted(e.target.checked)
                        }
                      />
                    }
                    label="Değerlendirme kaydının elektronik imza anlamını kabul ediyorum."
                  />
                  <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                    <TextField
                      type="number"
                      label="Puan"
                      value={score}
                      onChange={(e) => setScore(Number(e.target.value))}
                    />
                    <TextField
                      fullWidth
                      label="Kanıt / değerlendirme notu"
                      value={evidence}
                      onChange={(e) => setEvidence(e.target.value)}
                    />
                    <Button
                      variant="contained"
                      disabled={
                        record.status !== "Assessment" ||
                        mutate.isPending ||
                        !data.actionableTaskRoles.includes("Trainer") ||
                        !signaturePassword ||
                        !signatureMeaningAccepted
                      }
                      onClick={() =>
                        mutate.mutate(() =>
                          recordTrainingAssessment(
                            record.id,
                            record.version,
                            score,
                            true,
                            evidence,
                            signaturePassword,
                            signatureMeaningAccepted,
                          ),
                        )
                      }
                    >
                      Sonucu kaydet
                    </Button>
                  </Stack>
                </Paper>
                {data.attempts.map((attempt) => (
                  <Paper
                    key={attempt.id}
                    variant="outlined"
                    className="training-attempt-card"
                  >
                    <Stack
                      direction="row"
                      sx={{ justifyContent: "space-between" }}
                    >
                      <Box>
                        <Typography sx={{ fontWeight: 800 }}>
                          {attempt.attemptNumber}. deneme · %{attempt.score}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {attempt.evidence} · {attempt.evaluator}
                        </Typography>
                      </Box>
                      <Chip
                        color={attempt.passed ? "success" : "error"}
                        label={attempt.passed ? "Başarılı" : "Başarısız"}
                      />
                    </Stack>
                  </Paper>
                ))}
              </Stack>
            )}
            {tab === 3 && (
              <Paper
                variant="outlined"
                className="record-tab-canvas qualification-card"
              >
                <WorkspacePremiumRounded />
                <Box>
                  <Typography variant="overline">YETERLİLİK DURUMU</Typography>
                  <Typography variant="h5">
                    {record.status === "Completed"
                      ? "Aktif yeterlilik"
                      : "Yeterlilik henüz kazanılmadı"}
                  </Typography>
                  <Typography color="text.secondary">
                    Tamamlanma: {date(record.completedAtUtc)} · Geçerlilik sonu:{" "}
                    {date(record.expiresAtUtc)}
                  </Typography>
                  {record.isCriticalQualification && (
                    <Alert severity="warning" sx={{ mt: 2 }}>
                      Bu eğitim kritik iş yeterliliğidir. Süresi dolduğunda
                      ilgili iş yetkisi yenilenene kadar askıya alınmalıdır.
                    </Alert>
                  )}
                </Box>
              </Paper>
            )}
            {tab === 4 && (
              <Box className="record-tab-canvas history-tab-panel">
                <Typography variant="h6" sx={{ fontWeight: 800, mb: 1 }}>
                  Elektronik imzalar ({data.signatures.length})
                </Typography>
                {data.signatures.map((x) => (
                  <Alert key={x.id} severity="success" sx={{ mb: 1 }}>
                    {x.signerName} · {x.meaning} · {date(x.signedAtUtc)} · v
                    {x.recordVersion}
                  </Alert>
                ))}
                <AuditTimeline events={data.auditTrail} labels={auditLabels} />
              </Box>
            )}
          </DialogContent>
          <DialogActions className="record-detail-actions">
            <Button onClick={onClose}>Kapat</Button>
            <Stack direction="row" spacing={1}>
              {data.availableTransitions.some((x) => x.code === "start") && (
                <Button variant="contained" onClick={() => transition("start")}>
                  Eğitimi başlat
                </Button>
              )}
              {data.availableTransitions.some(
                (x) => x.code === "submit-assessment",
              ) && (
                <Button
                  variant="contained"
                  disabled={
                    record.assessmentMode === "ReadAndAcknowledge" &&
                    !record.acknowledgedAtUtc
                  }
                  onClick={() => transition("submit-assessment")}
                >
                  Değerlendirmeye gönder
                </Button>
              )}
              {data.availableTransitions.some((x) => x.code === "approve") && (
                <>
                  <TextField
                    size="small"
                    label="Onay notu"
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                  />
                  <TextField
                    size="small"
                    type="password"
                    autoComplete="current-password"
                    label="E-imza parolası"
                    value={signaturePassword}
                    onChange={(e) => setSignaturePassword(e.target.value)}
                  />
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={signatureMeaningAccepted}
                        onChange={(e) =>
                          setSignatureMeaningAccepted(e.target.checked)
                        }
                      />
                    }
                    label="İmza anlamını kabul ediyorum"
                  />
                  <Button
                    variant="contained"
                    color="success"
                    startIcon={<VerifiedRounded />}
                    disabled={
                      !signaturePassword ||
                      !signatureMeaningAccepted ||
                      !note.trim() ||
                      mutate.isPending
                    }
                    onClick={() => transition("approve")}
                  >
                    Yeterliliği onayla
                  </Button>
                </>
              )}
              {data.availableTransitions.some((x) => x.code === "reassign") && (
                <Button
                  variant="contained"
                  onClick={() => transition("reassign")}
                >
                  Yeniden ata
                </Button>
              )}
              {data.availableTransitions.some((x) => x.code === "assign") && (
                <Button
                  variant="contained"
                  onClick={() => transition("assign")}
                >
                  Çalışana ata
                </Button>
              )}
              {data.availableTransitions.some((x) => x.code === "expire") && (
                <Button
                  variant="outlined"
                  color="warning"
                  onClick={() => transition("expire")}
                >
                  Süresi doldu olarak işaretle
                </Button>
              )}
              {data.availableTransitions.some((x) => x.code === "cancel") && (
                <Button
                  variant="outlined"
                  color="error"
                  disabled={!note.trim()}
                  onClick={() => transition("cancel")}
                >
                  İptal et
                </Button>
              )}
            </Stack>
          </DialogActions>
        </Box>
      )}
    </Dialog>
  );
}
function InfoCard({
  icon,
  label,
  value,
  helper,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  helper: string;
}) {
  return (
    <Paper variant="outlined" className="training-info-card">
      <Box className="training-info-icon">{icon}</Box>
      <Box>
        <Typography variant="overline">{label}</Typography>
        <Typography sx={{ fontWeight: 800 }}>{value}</Typography>
        <Typography variant="caption" color="text.secondary">
          {helper}
        </Typography>
      </Box>
    </Paper>
  );
}
