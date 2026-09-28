import { lazy, Suspense, useMemo, useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  Divider,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  TextField,
  Typography,
  Switch,
  Snackbar,
} from "@mui/material";
import {
  AddRounded,
  ArrowDropDownRounded,
  CheckCircleRounded,
  MoreHorizRounded,
  OpenInNewRounded,
  SendRounded,
  SettingsRounded,
  WarningAmberRounded,
} from "@mui/icons-material";
import {
  createDeviation,
  createDeviationAssignmentRule,
  createDeviationType,
  getDeviationDetails,
  getDeviationLookups,
  listDeviationTypes,
  listDeviationAssignmentRules,
  searchDeviations,
  submitDeviation,
  updateDeviationType,
  updateDeviationAssignmentRule,
  type CreateDeviationInput,
  type DeviationListItem,
} from "../../api/deviations";
import { getAccessOverview } from "../../api/access";
import { DeviationFilters } from "./DeviationFilters";
import { NextAssigneeNotice } from "./NextAssigneeNotice";
import { taskRoleOptions } from "./taskRoles";
import {
  emptyDeviationFilters,
  toColumnFilters,
  type DeviationFilterState,
} from "./deviationFilterModel";
import {
  SearchableSelect,
  type SelectOption,
} from "../../components/SearchableSelect";
import { TableLoadingRows } from "../../components/TableLoadingRows";
import { ModalHeader } from "../../components/ModalHeader";
import { AdvancedFilterButton } from "../../components/AdvancedFilterPanel";
import {
  ModuleGuideDialog,
  ModuleInfoButton,
} from "../../components/ModuleGuideDialog";
import { Permissions, useAuth } from "../../security/AuthContext";

const DeviationDetailsDialog = lazy(() =>
  import("./DeviationDetailsDialog").then((module) => ({
    default: module.DeviationDetailsDialog,
  })),
);

type DeviationFormValues = Omit<
  CreateDeviationInput,
  "occurredAtUtc" | "detectedAtUtc"
> & {
  occurredAtUtc: string;
  detectedAtUtc: string;
};

const statusLabels: Record<string, string> = {
  Draft: "Taslak",
  Submitted: "Gönderildi",
  PreliminaryReview: "Ön inceleme",
  Investigation: "Araştırma",
  ImpactAssessment: "Etki değerlendirmesi",
  QualityAssessment: "KG değerlendirmesi",
  ActionImplementation: "Aksiyon uygulama",
  EffectivenessReview: "Etkinlik",
  ClosureApproval: "Kapanış onayı",
  Closed: "Kapalı",
  Voided: "İptal",
};

const classificationLabels = {
  Minor: "Minör",
  Major: "Majör",
  Critical: "Kritik",
};
const riskOptions: Array<SelectOption<number>> = [1, 2, 3, 4, 5].map(
  (value) => ({ value, label: String(value) }),
);

export function DeviationWorkspace() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  const [selectedDeviationId, setSelectedDeviationId] = useState<string | null>(
    null,
  );
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [typeSettingsOpen, setTypeSettingsOpen] = useState(false);
  const [assignmentSettingsOpen, setAssignmentSettingsOpen] = useState(false);
  const [submitConfirmation, setSubmitConfirmation] =
    useState<DeviationListItem | null>(null);
  const [feedback, setFeedback] = useState<{
    severity: "success" | "error";
    message: string;
  } | null>(null);
  const [filters, setFilters] = useState<DeviationFilterState>(
    emptyDeviationFilters,
  );
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25);
  const [sortBy, setSortBy] = useState("createdAtUtc");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc");
  const queryClient = useQueryClient();
  const { can } = useAuth();
  const columnFilters = useMemo(() => toColumnFilters(filters), [filters]);
  const searchRequest = useMemo(
    () => ({
      page: page + 1,
      pageSize,
      sortBy,
      sortDirection,
      filters: columnFilters,
    }),
    [page, pageSize, sortBy, sortDirection, columnFilters],
  );
  const deviations = useQuery({
    queryKey: ["deviations", searchRequest],
    queryFn: ({ signal }) => searchDeviations(searchRequest, signal),
    placeholderData: (previous) => previous,
    retry: false,
  });
  const submitPreview = useQuery({
    queryKey: ["deviation-details", submitConfirmation?.id],
    queryFn: ({ signal }) => getDeviationDetails(submitConfirmation!.id, signal),
    enabled: Boolean(submitConfirmation),
    retry: false,
  });
  const submitMutation = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) =>
      submitDeviation(id, version),
    onSuccess: async () => {
      const recordNumber = submitConfirmation?.recordNumber;
      setSubmitConfirmation(null);
      await queryClient.invalidateQueries({ queryKey: ["deviations"] });
      setFeedback({
        severity: "success",
        message: `${recordNumber ?? "Sapma kaydı"} kontrollü iş akışına gönderildi.`,
      });
    },
    onError: (error) =>
      setFeedback({ severity: "error", message: error.message }),
  });

  return (
    <Box component="section" id="deviations" className="deviation-section">
      <Stack
        className="module-page-hero"
        direction={{ xs: "column", sm: "row" }}
        sx={{
          justifyContent: "space-between",
          alignItems: { xs: "flex-start", sm: "center" },
          gap: 2,
        }}
      >
        <Box>
          <Stack direction="row" spacing={1.25} sx={{ alignItems: "center" }}>
            <Typography variant="h2">Sapma iş listesi</Typography>
            <Chip size="small" color="primary" label="M.01 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Taslakları kaydedin, risk puanını otomatik hesaplayın ve kontrollü
            iş akışına gönderin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2} sx={{ flexWrap: "wrap" }}>
          <ModuleInfoButton module="M.01" onClick={() => setGuideOpen(true)} />
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setTypeSettingsOpen(true)}
            >
              Sapma türleri
            </Button>
          )}
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setAssignmentSettingsOpen(true)}
            >
              Görev matrisi
            </Button>
          )}
          {can(Permissions.deviationCreate) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setDialogOpen(true)}
            >
              Yeni sapma
            </Button>
          )}
        </Stack>
      </Stack>

      <Stack
        className="module-list-toolbar"
        direction="row"
        spacing={1.5}
        sx={{ mt: 3, alignItems: "center" }}
      >
        <AdvancedFilterButton
          open={filtersOpen}
          activeCount={columnFilters.length}
          onClick={() => setFiltersOpen((current) => !current)}
        />
        <Typography variant="body2" color="text.secondary">
          {deviations.data
            ? `${deviations.data.totalCount} kayıt`
            : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>

      {filtersOpen && (
        <DeviationFilters
          value={filters}
          onApply={(next) => {
            setFilters(next);
            setPage(0);
          }}
        />
      )}

      <Paper className="deviation-table-card" elevation={0}>
        {deviations.isError && (
          <Alert severity="warning" sx={{ m: 2 }}>
            Sapma veritabanına ulaşılamadı. PostgreSQL ve güncel API
            çalıştığında liste otomatik yüklenecek.
          </Alert>
        )}
        <TableContainer>
          <Table aria-label="Sapma iş listesi">
            <TableHead>
              <TableRow>
                <SortableHeader
                  field="recordNumber"
                  label="Kayıt"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={handleSort}
                />
                <SortableHeader
                  field="title"
                  label="Başlık / Bölüm"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={handleSort}
                />
                <SortableHeader
                  field="riskScore"
                  label="Risk"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={handleSort}
                />
                <SortableHeader
                  field="status"
                  label="Durum"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={handleSort}
                />
                <SortableHeader
                  field="targetDateUtc"
                  label="Hedef"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={handleSort}
                />
                <TableCell align="right">İşlem</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {deviations.isLoading && <TableLoadingRows columns={6} />}
              {deviations.data?.items.map((deviation) => (
                <DeviationRow
                  deviation={deviation}
                  key={deviation.id}
                  submitting={
                    submitMutation.isPending &&
                    submitMutation.variables?.id === deviation.id
                  }
                  canSubmit={can(Permissions.deviationCreate)}
                  onSubmit={() => setSubmitConfirmation(deviation)}
                  onOpen={() => setSelectedDeviationId(deviation.id)}
                />
              ))}
              {deviations.isSuccess && deviations.data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Box className="empty-state">
                      <Typography sx={{ fontWeight: 750 }}>
                        Henüz sapma kaydı yok
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        İlk kontrollü kalite kaydını oluşturmak için “Yeni
                        sapma”yı kullanın.
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
          count={Number(deviations.data?.totalCount ?? 0)}
          page={page}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage="Sayfa boyutu"
          labelDisplayedRows={({ from, to, count }) =>
            `${from}–${to} / ${count}`
          }
          onPageChange={(_, nextPage) => setPage(nextPage)}
          onRowsPerPageChange={(event) => {
            setPageSize(Number(event.target.value) as 10 | 25 | 50 | 100);
            setPage(0);
          }}
        />
      </Paper>

      <DeviationDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
      <DeviationTypeSettingsDialog
        open={typeSettingsOpen}
        onClose={() => setTypeSettingsOpen(false)}
      />
      <DeviationAssignmentSettingsDialog
        open={assignmentSettingsOpen}
        onClose={() => setAssignmentSettingsOpen(false)}
      />
      <Dialog
        open={Boolean(submitConfirmation)}
        onClose={() => !submitMutation.isPending && setSubmitConfirmation(null)}
        maxWidth="xs"
        fullWidth
        className="action-confirmation-dialog"
      >
        <Box className="action-confirmation-heading">
          <Box className="action-confirmation-icon">
            <WarningAmberRounded />
          </Box>
          <Box>
            <Typography variant="overline">KONTROLLÜ İŞLEM</Typography>
            <Typography variant="h6">İş akışına gönderilsin mi?</Typography>
          </Box>
        </Box>
        <DialogContent>
          <Typography color="text.secondary">
            Bu işlem taslağı düzenleme aşamasından çıkarır ve yetkili
            kullanıcılara görev oluşturur.
          </Typography>
          <Box className="confirmation-record-summary">
            <Typography variant="caption">KAYIT</Typography>
            <Typography>{submitConfirmation?.recordNumber}</Typography>
            <Typography variant="body2">{submitConfirmation?.title}</Typography>
          </Box>
          {submitPreview.data?.nextAssignee && (
            <Box sx={{ mt: 2 }}>
              <NextAssigneeNotice
                nextAssignee={submitPreview.data.nextAssignee}
                when="Gönderildiğinde"
              />
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button
            disabled={submitMutation.isPending}
            onClick={() => setSubmitConfirmation(null)}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            startIcon={
              submitMutation.isPending ? (
                <CircularProgress size={17} color="inherit" />
              ) : (
                <SendRounded />
              )
            }
            disabled={!submitConfirmation || submitMutation.isPending}
            onClick={() =>
              submitConfirmation &&
              submitMutation.mutate({
                id: submitConfirmation.id,
                version: submitConfirmation.version,
              })
            }
          >
            {submitMutation.isPending ? "Gönderiliyor…" : "Onayla ve gönder"}
          </Button>
        </DialogActions>
      </Dialog>
      <Snackbar
        open={Boolean(feedback)}
        autoHideDuration={4500}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        onClose={() => setFeedback(null)}
      >
        <Alert
          className="app-result-toast"
          variant="filled"
          severity={feedback?.severity ?? "success"}
          icon={
            feedback?.severity === "success" ? (
              <CheckCircleRounded />
            ) : undefined
          }
          onClose={() => setFeedback(null)}
        >
          {feedback?.message}
        </Alert>
      </Snackbar>
      <ModuleGuideDialog
        module="M.01"
        open={guideOpen}
        onClose={() => setGuideOpen(false)}
      />
      <Suspense fallback={null}>
        <DeviationDetailsDialog
          id={selectedDeviationId}
          onClose={() => setSelectedDeviationId(null)}
        />
      </Suspense>
    </Box>
  );

  function handleSort(field: string) {
    if (field === sortBy)
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
    else {
      setSortBy(field);
      setSortDirection("asc");
    }
    setPage(0);
  }
}

function SortableHeader({
  field,
  label,
  sortBy,
  direction,
  onSort,
}: {
  field: string;
  label: string;
  sortBy: string;
  direction: "asc" | "desc";
  onSort: (field: string) => void;
}) {
  return (
    <TableCell sortDirection={sortBy === field ? direction : false}>
      <TableSortLabel
        active={sortBy === field}
        direction={sortBy === field ? direction : "asc"}
        onClick={() => onSort(field)}
      >
        {label}
      </TableSortLabel>
    </TableCell>
  );
}

function DeviationRow({
  deviation,
  submitting,
  canSubmit,
  onSubmit,
  onOpen,
}: {
  deviation: DeviationListItem;
  submitting: boolean;
  canSubmit: boolean;
  onSubmit: () => void;
  onOpen: () => void;
}) {
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const riskColor =
    deviation.classification === "Minor" ? "success" : "error";

  return (
    <TableRow hover>
      <TableCell>
        <Typography className="record-number">
          {deviation.recordNumber}
        </Typography>
        {deviation.capaRequired && (
          <Chip
            size="small"
            label="DÖF gerekli"
            color="warning"
            variant="outlined"
          />
        )}
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 700 }}>{deviation.title}</Typography>
        <Typography variant="caption" color="text.secondary">
          {deviation.detectedDepartment}
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          className={`semantic-risk-badge risk-${deviation.classification.toLowerCase()}`}
          size="small"
          color={riskColor}
          label={`${classificationLabels[deviation.classification]} · ${deviation.riskScore}`}
        />
      </TableCell>
      <TableCell>
        {statusLabels[deviation.status] ?? deviation.status}
      </TableCell>
      <TableCell>{formatDate(deviation.targetDateUtc)}</TableCell>
      <TableCell align="right">
        <Button
          className="row-action-trigger"
          size="small"
          variant="outlined"
          startIcon={<MoreHorizRounded />}
          endIcon={<ArrowDropDownRounded />}
          aria-haspopup="menu"
          aria-expanded={Boolean(menuAnchor)}
          onClick={(event) => setMenuAnchor(event.currentTarget)}
        >
          İşlemler
        </Button>
        <Menu
          anchorEl={menuAnchor}
          open={Boolean(menuAnchor)}
          onClose={() => setMenuAnchor(null)}
          className="row-action-menu"
        >
          <MenuItem
            onClick={() => {
              setMenuAnchor(null);
              onOpen();
            }}
          >
            <ListItemIcon>
              <OpenInNewRounded fontSize="small" />
            </ListItemIcon>
            <ListItemText
              primary="Aç"
              secondary="Kayıt ayrıntılarını görüntüle"
            />
          </MenuItem>
          {canSubmit && deviation.status === "Draft" && <Divider />}
          {canSubmit && deviation.status === "Draft" && (
            <MenuItem
              disabled={submitting}
              onClick={() => {
                setMenuAnchor(null);
                onSubmit();
              }}
            >
              <ListItemIcon>
                {submitting ? (
                  <CircularProgress size={18} />
                ) : (
                  <SendRounded fontSize="small" />
                )}
              </ListItemIcon>
              <ListItemText
                primary={submitting ? "Gönderiliyor…" : "İş akışına gönder"}
                secondary="Kontrollü değerlendirmeyi başlat"
              />
            </MenuItem>
          )}
        </Menu>
      </TableCell>
    </TableRow>
  );
}

function DeviationDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const defaults = useMemo(() => defaultFormValues(), []);
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<DeviationFormValues>({ defaultValues: defaults });
  const factors = useWatch({
    control,
    name: ["likelihood", "severity", "detectability"],
  });
  const lookups = useQuery({
    queryKey: ["deviation-lookups"],
    queryFn: ({ signal }) => getDeviationLookups(signal),
    enabled: open,
  });
  const deviationTypeOptions = (lookups.data?.deviationTypes ?? []).map(
    (x) => ({ value: x.name, label: x.name }),
  );
  const departmentOptions = (lookups.data?.departments ?? []).map((x) => ({
    value: x.name,
    label: `${x.name} (${x.code})`,
  }));
  const riskScore = factors.reduce(
    (total, factor) => total * Number(factor || 1),
    1,
  );
  const riskClass =
    riskScore <= 20 ? "Minör" : riskScore <= 50 ? "Majör" : "Kritik";
  const createMutation = useMutation({
    mutationFn: createDeviation,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["deviations"] });
      reset(defaultFormValues());
      onClose();
    },
  });

  const close = () => {
    if (!createMutation.isPending) {
      createMutation.reset();
      reset(defaultFormValues());
      onClose();
    }
  };

  const onSubmit = handleSubmit((values) =>
    createMutation.mutate({
      ...values,
      likelihood: Number(values.likelihood),
      severity: Number(values.severity),
      detectability: Number(values.detectability),
      occurredAtUtc: new Date(values.occurredAtUtc).toISOString(),
      detectedAtUtc: new Date(values.detectedAtUtc).toISOString(),
    }),
  );

  return (
    <Dialog open={open} onClose={close} maxWidth="md" fullWidth>
      <Box component="form" onSubmit={onSubmit}>
        <ModalHeader
          onClose={close}
          closeDisabled={createMutation.isPending}
          closeLabel="Yeni sapma penceresini kapat"
        >
          <Typography variant="h5" sx={{ fontWeight: 800 }}>
            Yeni sapma kaydı
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Taslak oluşturulduğunda kayıt numarası sunucuda atomik olarak
            atanır.
          </Typography>
        </ModalHeader>
        <DialogContent>
          <Stack spacing={2.25}>
            {createMutation.isError && (
              <Alert severity="error">{createMutation.error.message}</Alert>
            )}
            <TextField
              label="Sapma başlığı"
              required
              fullWidth
              error={Boolean(errors.title)}
              helperText={errors.title?.message}
              {...register("title", {
                required: "Başlık zorunludur.",
                maxLength: 200,
              })}
            />
            <Box className="form-grid two-columns">
              <Controller
                name="deviationType"
                control={control}
                render={({ field }) => (
                  <SearchableSelect
                    label="Sapma türü"
                    required
                    value={field.value}
                    options={deviationTypeOptions}
                    onChange={(next) => field.onChange(next ?? "")}
                    size="medium"
                    disabled={lookups.isLoading}
                  />
                )}
              />
              <Controller
                name="detectedDepartment"
                control={control}
                rules={{ required: "Tespit eden bölüm zorunludur." }}
                render={({ field }) => (
                  <SearchableSelect
                    required
                    label="Tespit eden bölüm"
                    value={field.value}
                    options={departmentOptions}
                    onChange={(next) => field.onChange(next ?? "")}
                    size="medium"
                    disabled={lookups.isLoading}
                  />
                )}
              />
              <TextField
                required
                label="Proses aşaması"
                {...register("processStage", { required: true })}
              />
              <TextField
                label="Gerçekleşme zamanı"
                required
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                {...register("occurredAtUtc", { required: true })}
              />
              <TextField
                label="Tespit zamanı"
                required
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                {...register("detectedAtUtc", { required: true })}
              />
            </Box>
            <TextField
              label="Gerçekleşen sapmanın tanımı"
              required
              multiline
              minRows={3}
              {...register("description", {
                required: "Sapma tanımı zorunludur.",
              })}
            />
            <Box className="form-grid two-columns">
              <TextField
                label="Beklenen/onaylı durum"
                required
                multiline
                minRows={2}
                {...register("expectedState", {
                  required: "Beklenen durum zorunludur.",
                })}
              />
              <TextField
                label="Acil aksiyon"
                required
                multiline
                minRows={2}
                {...register("immediateAction", {
                  required: "Acil aksiyon zorunludur.",
                })}
              />
            </Box>
            <Paper variant="outlined" className="risk-panel">
              <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={2}
                sx={{ alignItems: "center" }}
              >
                <Controller
                  name="likelihood"
                  control={control}
                  render={({ field }) => (
                    <RiskSelect
                      label="Olasılık"
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
                <Controller
                  name="severity"
                  control={control}
                  render={({ field }) => (
                    <RiskSelect
                      label="Şiddet"
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
                <Controller
                  name="detectability"
                  control={control}
                  render={({ field }) => (
                    <RiskSelect
                      label="Tespit edilebilirlik"
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
                <Box className="risk-result">
                  <Typography variant="caption" color="text.secondary">
                    O × Ş × T
                  </Typography>
                  <Typography variant="h5" sx={{ fontWeight: 850 }}>
                    {riskScore}
                  </Typography>
                  <Typography variant="caption" sx={{ fontWeight: 750 }}>
                    {riskClass}
                  </Typography>
                </Box>
              </Stack>
            </Paper>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={close} disabled={createMutation.isPending}>
            Vazgeç
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={createMutation.isPending}
          >
            {createMutation.isPending ? "Kaydediliyor…" : "Taslak oluştur"}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

function DeviationTypeSettingsDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [sortOrder, setSortOrder] = useState(10);
  const types = useQuery({
    queryKey: ["deviation-types"],
    queryFn: ({ signal }) => listDeviationTypes(signal),
    enabled: open,
  });
  const createType = useMutation({
    mutationFn: createDeviationType,
    onSuccess: async () => {
      setCode("");
      setName("");
      setSortOrder(10);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["deviation-types"] }),
        queryClient.invalidateQueries({ queryKey: ["deviation-lookups"] }),
      ]);
    },
  });
  const saveType = useMutation({
    mutationFn: ({
      id,
      nextName,
      nextSortOrder,
      isActive,
    }: {
      id: string;
      nextName: string;
      nextSortOrder: number;
      isActive: boolean;
    }) =>
      updateDeviationType(id, {
        name: nextName,
        sortOrder: nextSortOrder,
        isActive,
      }),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["deviation-types"] }),
        queryClient.invalidateQueries({ queryKey: ["deviation-lookups"] }),
      ]);
    },
  });

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose} closeLabel="Sapma türü yönetimini kapat">
        <Typography variant="h5" sx={{ fontWeight: 800 }}>
          Sapma türleri
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Pasife alınan tür yeni kayıtlarda seçilemez; geçmiş kayıtlardaki değer
          değişmez.
        </Typography>
      </ModalHeader>
      <DialogContent>
        <Stack spacing={2}>
          {(createType.isError || saveType.isError) && (
            <Alert severity="error">
              {(createType.error ?? saveType.error)?.message}
            </Alert>
          )}
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography sx={{ fontWeight: 750, mb: 1.5 }}>
              Yeni tür ekle
            </Typography>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                label="Kod"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                slotProps={{ htmlInput: { maxLength: 32 } }}
              />
              <TextField
                label="Ad"
                value={name}
                onChange={(e) => setName(e.target.value)}
                slotProps={{ htmlInput: { maxLength: 80 } }}
                fullWidth
              />
              <TextField
                label="Sıra"
                type="number"
                value={sortOrder}
                onChange={(e) => setSortOrder(Number(e.target.value))}
                sx={{ width: 120 }}
              />
              <Button
                variant="contained"
                startIcon={<AddRounded />}
                disabled={!code.trim() || !name.trim() || createType.isPending}
                onClick={() => createType.mutate({ code, name, sortOrder })}
              >
                Ekle
              </Button>
            </Stack>
          </Paper>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Kod</TableCell>
                  <TableCell>Ad</TableCell>
                  <TableCell>Sıra</TableCell>
                  <TableCell>Aktif</TableCell>
                  <TableCell align="right">İşlem</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {types.data?.map((item) => (
                  <DeviationTypeRow
                    key={item.id}
                    item={item}
                    saving={saveType.isPending}
                    onSave={(nextName, nextSortOrder, isActive) =>
                      saveType.mutate({
                        id: item.id,
                        nextName,
                        nextSortOrder,
                        isActive,
                      })
                    }
                  />
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ p: 3 }}>
        <Button onClick={onClose}>Kapat</Button>
      </DialogActions>
    </Dialog>
  );
}


function DeviationAssignmentSettingsDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const rules = useQuery({
    queryKey: ["deviation-assignment-rules"],
    queryFn: ({ signal }) => listDeviationAssignmentRules(signal),
    enabled: open,
  });
  const access = useQuery({
    queryKey: ["access-overview"],
    queryFn: ({ signal }) => getAccessOverview(signal),
    enabled: open,
  });
  const lookups = useQuery({
    queryKey: ["deviation-lookups"],
    queryFn: ({ signal }) => getDeviationLookups(signal),
    enabled: open,
  });
  const [role, setRole] = useState("ProcessAuthority"),
    [userId, setUserId] = useState(""),
    [department, setDepartment] = useState(""),
    [type, setType] = useState(""),
    [minimumRisk, setMinimumRisk] = useState(""),
    [priority, setPriority] = useState(10);
  const refresh = () =>
    qc.invalidateQueries({ queryKey: ["deviation-assignment-rules"] });
  const create = useMutation({
    mutationFn: createDeviationAssignmentRule,
    onSuccess: async () => {
      setUserId("");
      await refresh();
    },
  });
  const update = useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof updateDeviationAssignmentRule>[1];
    }) => updateDeviationAssignmentRule(id, input),
    onSuccess: refresh,
  });
  const users = (access.data?.users ?? [])
    .filter((x) => x.isActive)
    .map((x) => ({
      value: x.id,
      label: `${x.displayName} · ${x.departmentName ?? "Bölümsüz"}`,
    }));
  const departments = [
    { value: "", label: "Tüm bölümler" },
    ...(lookups.data?.departments ?? []).map((x) => ({
      value: x.name,
      label: x.name,
    })),
  ];
  const types = [
    { value: "", label: "Tüm sapma türleri" },
    ...(lookups.data?.deviationTypes ?? []).map((x) => ({
      value: x.name,
      label: x.name,
    })),
  ];
  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth>
      <ModalHeader onClose={onClose} closeLabel="Görev matrisini kapat">
        <Typography variant="h5" sx={{ fontWeight: 800 }}>
          M.01 görev-atama matrisi
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Özel bölüm/tür/RPN kuralı önce, eşitlikte yüksek öncelik önce
          uygulanır. Eşleşme yoksa iş akışı durur.
        </Typography>
      </ModalHeader>
      <DialogContent>
        <Stack spacing={2}>
          {(create.isError || update.isError) && (
            <Alert severity="error">
              {(create.error ?? update.error)?.message}
            </Alert>
          )}
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Box className="form-grid two-columns">
              <SearchableSelect
                label="Görev"
                value={role}
                options={taskRoleOptions}
                onChange={(x) => setRole(x ?? "ProcessAuthority")}
                size="medium"
              />
              <SearchableSelect
                label="Atanacak kullanıcı"
                value={userId}
                options={users}
                onChange={(x) => setUserId(x ?? "")}
                size="medium"
              />
              <SearchableSelect
                label="Tespit bölümü koşulu"
                value={department}
                options={departments}
                onChange={(x) => setDepartment(x ?? "")}
                size="medium"
              />
              <SearchableSelect
                label="Sapma türü koşulu"
                value={type}
                options={types}
                onChange={(x) => setType(x ?? "")}
                size="medium"
              />
              <TextField
                label="Minimum RPN (boş=tümü)"
                type="number"
                value={minimumRisk}
                onChange={(e) => setMinimumRisk(e.target.value)}
              />
              <TextField
                label="Öncelik"
                type="number"
                value={priority}
                onChange={(e) => setPriority(Number(e.target.value))}
              />
            </Box>
            <Button
              sx={{ mt: 2 }}
              variant="contained"
              disabled={!userId || create.isPending}
              onClick={() =>
                create.mutate({
                  taskRole: role,
                  assignedUserId: userId,
                  detectedDepartment: department || null,
                  deviationType: type || null,
                  minimumRiskScore: minimumRisk ? Number(minimumRisk) : null,
                  priority,
                  isActive: true,
                })
              }
            >
              Kural ekle
            </Button>
          </Paper>
          <TableContainer component={Paper} variant="outlined">
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Görev</TableCell>
                  <TableCell>Koşul</TableCell>
                  <TableCell>Atanan</TableCell>
                  <TableCell>Öncelik</TableCell>
                  <TableCell>Aktif</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rules.data?.map((rule) => (
                  <TableRow key={rule.id}>
                    <TableCell>
                      {taskRoleOptions.find((x) => x.value === rule.taskRole)
                        ?.label ?? rule.taskRole}
                    </TableCell>
                    <TableCell>
                      {[
                        rule.detectedDepartment,
                        rule.deviationType,
                        rule.minimumRiskScore
                          ? `RPN ≥ ${rule.minimumRiskScore}`
                          : null,
                      ]
                        .filter(Boolean)
                        .join(" · ") || "Tüm kayıtlar"}
                    </TableCell>
                    <TableCell>{rule.assignedUserName}</TableCell>
                    <TableCell>{rule.priority}</TableCell>
                    <TableCell>
                      <Switch
                        checked={rule.isActive}
                        disabled={update.isPending}
                        onChange={(_, isActive) =>
                          update.mutate({
                            id: rule.id,
                            input: {
                              taskRole: rule.taskRole,
                              assignedUserId: rule.assignedUserId,
                              detectedDepartment: rule.detectedDepartment,
                              deviationType: rule.deviationType,
                              minimumRiskScore: rule.minimumRiskScore,
                              priority: rule.priority,
                              isActive,
                            },
                          })
                        }
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ p: 3 }}>
        <Button onClick={onClose}>Kapat</Button>
      </DialogActions>
    </Dialog>
  );
}

function DeviationTypeRow({
  item,
  saving,
  onSave,
}: {
  item: { code: string; name: string; sortOrder: number; isActive: boolean };
  saving: boolean;
  onSave: (name: string, sortOrder: number, isActive: boolean) => void;
}) {
  const [name, setName] = useState(item.name);
  const [sortOrder, setSortOrder] = useState(item.sortOrder);
  const [isActive, setIsActive] = useState(item.isActive);
  return (
    <TableRow>
      <TableCell>
        <Typography sx={{ fontWeight: 700 }}>{item.code}</Typography>
      </TableCell>
      <TableCell>
        <TextField
          size="small"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </TableCell>
      <TableCell>
        <TextField
          size="small"
          type="number"
          value={sortOrder}
          onChange={(e) => setSortOrder(Number(e.target.value))}
          sx={{ width: 90 }}
        />
      </TableCell>
      <TableCell>
        <Switch
          checked={isActive}
          onChange={(_, checked) => setIsActive(checked)}
          slotProps={{ input: { "aria-label": `${item.name} aktif` } }}
        />
      </TableCell>
      <TableCell align="right">
        <Button
          size="small"
          disabled={saving || !name.trim()}
          onClick={() => onSave(name, sortOrder, isActive)}
        >
          Kaydet
        </Button>
      </TableCell>
    </TableRow>
  );
}

function RiskSelect({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <SearchableSelect
      label={label}
      value={value}
      options={riskOptions}
      onChange={(next) => onChange(next ?? 1)}
    />
  );
}

function defaultFormValues(): DeviationFormValues {
  const now = new Date();
  const detected = toLocalDateTime(now);
  const occurred = toLocalDateTime(new Date(now.getTime() - 30 * 60 * 1000));

  return {
    title: "",
    description: "",
    expectedState: "",
    immediateAction: "",
    deviationType: "",
    detectedDepartment: "",
    processStage: "Dolum",
    occurredAtUtc: occurred,
    detectedAtUtc: detected,
    likelihood: 2,
    severity: 2,
    detectability: 2,
  };
}

function toLocalDateTime(date: Date) {
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium" }).format(
    new Date(value),
  );
}
