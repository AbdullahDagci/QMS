import { useMemo, useState } from "react";
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
  DownloadRounded,
  FactCheckRounded,
  HistoryRounded,
  LinkRounded,
  PlaylistAddCheckRounded,
  RuleRounded,
  TaskAltRounded,
} from "@mui/icons-material";
import {
  addCapaAction,
  completeCapaAction,
  createCapa,
  downloadCapaFinalReport,
  getCapaDetails,
  getCapaLookups,
  searchCapas,
  transitionCapa,
  verifyCapaAction,
  type CapaDetails,
  type CapaListItem,
  type CreateCapaInput,
} from "../../api/capas";
import { ModalHeader } from "../../components/ModalHeader";
import { RecordActionMenu } from "../../components/RecordActionMenu";
import {
  SearchableSelect,
  type SelectOption,
} from "../../components/SearchableSelect";
import { TableLoadingRows } from "../../components/TableLoadingRows";
import { AuditTimeline } from "../../components/AuditTimeline";
import { RecordAssignments } from "../../components/RecordAssignments";
import { AdvancedFilterButton } from "../../components/AdvancedFilterPanel";
import {
  ModuleGuideDialog,
  ModuleInfoButton,
} from "../../components/ModuleGuideDialog";
import { Permissions, useAuth } from "../../security/AuthContext";
import { CapaFilters } from "./CapaFilters";
import {
  emptyCapaFilters,
  toCapaColumnFilters,
  type CapaFilterState,
} from "./capaFilterModel";

const statuses: Array<SelectOption<string>> = [
  ["Draft", "Taslak"],
  ["ScopeApproval", "Kapsam onayı"],
  ["RootCauseApproval", "Kök neden onayı"],
  ["ActionPlanning", "Aksiyon planlama"],
  ["PlanApproval", "Plan onayı"],
  ["Implementation", "Uygulama"],
  ["ActionVerification", "KG aksiyon doğrulaması"],
  ["EffectivenessWaiting", "Etkinlik bekleme"],
  ["EffectivenessReview", "Etkinlik değerlendirmesi"],
  ["ClosureApproval", "Kapanış onayı"],
  ["Closed", "Kapalı"],
].map(([value, label]) => ({ value, label }));
const statusLabel = (value: string) =>
  statuses.find((x) => x.value === value)?.label ?? value;
const flow = statuses.slice(1, 11);
const eventLabels: Record<string, string> = {
  CapaCreated: "DÖF kaydı oluşturuldu",
  CapaStatusChanged: "DÖF durumu değiştirildi",
  CapaActionAdded: "Aksiyon plana eklendi",
  CapaActionCompletionRequested: "Aksiyon kanıtla tamamlandı",
  CapaActionVerified: "Aksiyon KG tarafından doğrulandı",
};

export function CapaWorkspace() {
  const [params, setParams] = useSearchParams();
  const [createOpen, setCreateOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  const { can } = useAuth();
  const selectedId = params.get("open");
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filterValues, setFilterValues] =
    useState<CapaFilterState>(emptyCapaFilters);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25);
  const [sortBy, setSortBy] = useState("createdAtUtc");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc");
  const filters = useMemo(
    () => toCapaColumnFilters(filterValues),
    [filterValues],
  );
  const request = useMemo(
    () => ({ page: page + 1, pageSize, sortBy, sortDirection, filters }),
    [page, pageSize, sortBy, sortDirection, filters],
  );
  const capas = useQuery({
    queryKey: ["capas", request],
    queryFn: ({ signal }) => searchCapas(request, signal),
    placeholderData: (previous) => previous,
    retry: false,
  });

  const openDetails = (id: string) => setParams({ open: id });
  const closeDetails = () => setParams({});
  const sort = (field: string) => {
    if (field === sortBy)
      setSortDirection((x) => (x === "asc" ? "desc" : "asc"));
    else {
      setSortBy(field);
      setSortDirection("asc");
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
              <TaskAltRounded />
            </Box>
            <Typography variant="h2">DÖF iş listesi</Typography>
            <Chip size="small" color="success" label="M.02 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Kök nedenleri aksiyon planına dönüştürün, kanıtları KG
            doğrulamasından geçirip etkinliği izleyin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2} sx={{ flexWrap: "wrap" }}>
          <ModuleInfoButton module="M.02" onClick={() => setGuideOpen(true)} />
          {can(Permissions.capaPlan) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
            >
              Yeni DÖF
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
          activeCount={filters.length}
          onClick={() => setFiltersOpen((x) => !x)}
        />
        <Typography variant="body2" color="text.secondary">
          {capas.data
            ? `${capas.data.totalCount} kayıt`
            : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>
      {filtersOpen && (
        <CapaFilters
          value={filterValues}
          statusOptions={statuses}
          onApply={(next) => {
            setFilterValues(next);
            setPage(0);
          }}
        />
      )}
      <Paper className="deviation-table-card" elevation={0} sx={{ mt: 2.5 }}>
        {capas.isError && (
          <Alert severity="error" sx={{ m: 2 }}>
            {capas.error.message}
          </Alert>
        )}
        <TableContainer>
          <Table aria-label="DÖF iş listesi">
            <TableHead>
              <TableRow>
                <SortCell
                  field="recordNumber"
                  label="DÖF / Kaynak"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={sort}
                />
                <SortCell
                  field="title"
                  label="Başlık / Sorumlu"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={sort}
                />
                <TableCell>Aksiyon ilerlemesi</TableCell>
                <SortCell
                  field="status"
                  label="Durum"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={sort}
                />
                <SortCell
                  field="targetDateUtc"
                  label="Hedef"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={sort}
                />
                <TableCell align="right">İşlem</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {capas.isLoading && <TableLoadingRows columns={6} />}
              {capas.data?.items.map((c) => (
                <CapaRow key={c.id} capa={c} onOpen={() => openDetails(c.id)} />
              ))}
              {capas.isSuccess && capas.data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Box className="empty-state">
                      <Typography sx={{ fontWeight: 750 }}>
                        Henüz DÖF kaydı yok
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Bağımsız bir DÖF oluşturabilir veya M.01 içindeki
                        sapmadan ilişkilendirebilirsiniz.
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
          count={Number(capas.data?.totalCount ?? 0)}
          page={page}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage="Sayfa boyutu"
          labelDisplayedRows={({ from, to, count }) =>
            `${from}–${to} / ${count}`
          }
          onPageChange={(_, p) => setPage(p)}
          onRowsPerPageChange={(e) => {
            setPageSize(Number(e.target.value) as 10 | 25 | 50 | 100);
            setPage(0);
          }}
        />
      </Paper>
      <CapaCreateDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => {
          setCreateOpen(false);
          openDetails(id);
        }}
      />
      <CapaDetailsDialog id={selectedId} onClose={closeDetails} />
      <ModuleGuideDialog
        module="M.02"
        open={guideOpen}
        onClose={() => setGuideOpen(false)}
      />
    </Box>
  );
}

function CapaRow({ capa, onOpen }: { capa: CapaListItem; onOpen: () => void }) {
  const progress = capa.actionCount
    ? Math.round((capa.verifiedActionCount / capa.actionCount) * 100)
    : 0;
  return (
    <TableRow hover>
      <TableCell>
        <Typography sx={{ fontWeight: 800, color: "primary.main" }}>
          {capa.recordNumber}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {capa.sourceRecordNumber
            ? `Bağlı: ${capa.sourceRecordNumber}`
            : "Bağımsız kayıt"}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 700 }}>{capa.title}</Typography>
        <Typography variant="caption" color="text.secondary">
          {capa.owner}
        </Typography>
      </TableCell>
      <TableCell>
        <Stack spacing={0.5}>
          <Typography variant="caption">
            {capa.verifiedActionCount}/{capa.actionCount} doğrulandı · %
            {progress}
          </Typography>
          <LinearProgress
            variant="determinate"
            color="success"
            value={progress}
            sx={{ width: 140, borderRadius: 5 }}
          />
        </Stack>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={capa.status === "Closed" ? "success" : "primary"}
          variant={capa.status === "Closed" ? "filled" : "outlined"}
          label={statusLabel(capa.status)}
        />
      </TableCell>
      <TableCell>{date(capa.targetDateUtc)}</TableCell>
      <TableCell align="right">
        <RecordActionMenu onOpen={onOpen} />
      </TableCell>
    </TableRow>
  );
}

export function CapaCreateDialog({
  open,
  onClose,
  onCreated,
  sourceDeviation,
}: {
  open: boolean;
  onClose: () => void;
  onCreated?: (id: string) => void;
  sourceDeviation?: {
    id: string;
    recordNumber: string;
    title: string;
    description: string;
  };
}) {
  const queryClient = useQueryClient();
  const [values, setValues] = useState<CreateCapaInput>(
    initialCreate(sourceDeviation),
  );
  const lookups = useQuery({
    queryKey: ["capa-lookups"],
    queryFn: ({ signal }) => getCapaLookups(signal),
    enabled: open,
    retry: false,
  });
  const resetAndClose = () => {
    setValues(initialCreate(sourceDeviation));
    onClose();
  };
  const mutation = useMutation({
    mutationFn: () =>
      createCapa({
        ...values,
        targetDateUtc: new Date(values.targetDateUtc).toISOString(),
      }),
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: ["capas"] });
      await queryClient.invalidateQueries({
        queryKey: ["deviation-details", sourceDeviation?.id],
      });
      setValues(initialCreate(sourceDeviation));
      onCreated?.(data.record.id);
      if (!onCreated) onClose();
    },
  });
  const set = <K extends keyof CreateCapaInput>(
    key: K,
    value: CreateCapaInput[K],
  ) => setValues((x) => ({ ...x, [key]: value }));
  const valid =
    values.title.trim() &&
    values.description.trim() &&
    values.rootCause.trim() &&
    values.immediateActions.trim() &&
    values.ownerUserId &&
    values.targetDateUtc &&
    (!values.effectivenessRequired ||
      Boolean(values.effectivenessEvaluatorUserId));
  return (
    <Dialog open={open} onClose={resetAndClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={resetAndClose}>
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 800 }}>
            {sourceDeviation ? "Sapmadan DÖF oluştur" : "Yeni DÖF kaydı"}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {sourceDeviation ? (
              <>
                <LinkRounded sx={{ fontSize: 16, verticalAlign: "middle" }} />{" "}
                {sourceDeviation.recordNumber} ile çift yönlü bağlantı
                kurulacak.
              </>
            ) : (
              "Düzeltici ve önleyici faaliyet kaydı kontrollü iş akışında oluşturulur."
            )}
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
          {lookups.isError && (
            <Alert severity="error">Atama listeleri alınamadı.</Alert>
          )}
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <TextField
              required
              fullWidth
              label="DÖF başlığı"
              value={values.title}
              onChange={(e) => set("title", e.target.value)}
            />
            <Box sx={{ flex: 1 }}>
              <SearchableSelect
                required
                size="medium"
                label="DÖF sorumlusu"
                value={values.ownerUserId || null}
                options={(lookups.data?.owners ?? []).map((x) => ({
                  value: x.id,
                  label: x.departmentName
                    ? `${x.displayName} · ${x.departmentName}`
                    : x.displayName,
                }))}
                onChange={(value) => set("ownerUserId", value ?? "")}
              />
            </Box>
          </Stack>
          <TextField
            required
            label="Problem / uygunsuzluk tanımı"
            multiline
            minRows={2}
            value={values.description}
            onChange={(e) => set("description", e.target.value)}
          />
          <TextField
            required
            label="Doğrulanmış kök neden"
            multiline
            minRows={2}
            value={values.rootCause}
            onChange={(e) => set("rootCause", e.target.value)}
          />
          <TextField
            required
            label="Acil düzeltmeler"
            multiline
            minRows={2}
            value={values.immediateActions}
            onChange={(e) => set("immediateActions", e.target.value)}
          />
          <TextField
            required
            label="DÖF hedef tarihi"
            type="datetime-local"
            slotProps={{ inputLabel: { shrink: true } }}
            value={values.targetDateUtc}
            onChange={(e) => set("targetDateUtc", e.target.value)}
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={values.effectivenessRequired}
                onChange={(e) => set("effectivenessRequired", e.target.checked)}
              />
            }
            label="Etkinlik değerlendirmesi gerekli"
          />
          {values.effectivenessRequired && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Stack spacing={2}>
                <Typography sx={{ fontWeight: 800 }}>
                  Onayla sabitlenecek etkinlik planı
                </Typography>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                  <TextField
                    required
                    fullWidth
                    label="Yöntem"
                    value={values.effectivenessMethod}
                    onChange={(e) => set("effectivenessMethod", e.target.value)}
                  />
                  <TextField
                    required
                    fullWidth
                    label="Örneklem"
                    value={values.effectivenessSample}
                    onChange={(e) => set("effectivenessSample", e.target.value)}
                  />
                </Stack>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                  <TextField
                    fullWidth
                    type="number"
                    label="Gözlem süresi (gün)"
                    value={values.observationPeriodDays}
                    onChange={(e) =>
                      set("observationPeriodDays", Number(e.target.value))
                    }
                  />
                  <Box sx={{ flex: 1 }}>
                    <SearchableSelect
                      required
                      size="medium"
                      label="Etkinlik değerlendiricisi"
                      value={values.effectivenessEvaluatorUserId ?? null}
                      options={(
                        lookups.data?.effectivenessEvaluators ?? []
                      ).map((x) => ({
                        value: x.id,
                        label: x.departmentName
                          ? `${x.displayName} · ${x.departmentName}`
                          : x.displayName,
                      }))}
                      onChange={(value) =>
                        set("effectivenessEvaluatorUserId", value)
                      }
                    />
                  </Box>
                </Stack>
                <TextField
                  required
                  label="Başarı kriterleri"
                  value={values.successCriteria}
                  onChange={(e) => set("successCriteria", e.target.value)}
                />
              </Stack>
            </Paper>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={resetAndClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={!valid || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          {mutation.isPending ? "Oluşturuluyor…" : "DÖF oluştur"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function CapaDetailsDialog({
  id,
  onClose,
}: {
  id: string | null;
  onClose: () => void;
}) {
  const { can } = useAuth();
  const client = useQueryClient();
  const [tab, setTab] = useState(0);
  const [note, setNote] = useState("");
  const [isEffective, setIsEffective] = useState(true);
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureMeaningAccepted, setSignatureMeaningAccepted] =
    useState(false);
  const details = useQuery({
    queryKey: ["capa-details", id],
    queryFn: ({ signal }) => getCapaDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const update = async (data: CapaDetails) => {
    client.setQueryData(["capa-details", id], data);
    await client.invalidateQueries({ queryKey: ["capas"] });
    await client.invalidateQueries({ queryKey: ["deviation-details"] });
    await client.invalidateQueries({
      queryKey: ["workflow-assignments", "Capa", id],
    });
    setNote("");
    setSignaturePassword("");
    setSignatureMeaningAccepted(false);
  };
  const transition = useMutation({
    mutationFn: (code: string) =>
      transitionCapa(
        id!,
        details.data!.record.version,
        code,
        note || undefined,
        isEffective,
        signaturePassword || undefined,
        signatureMeaningAccepted,
      ),
    onSuccess: update,
  });
  const report = useMutation({
    mutationFn: () => downloadCapaFinalReport(id!),
    onSuccess: (data) => {
      const url = URL.createObjectURL(data.blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = data.fileName;
      link.click();
      URL.revokeObjectURL(url);
    },
  });
  const needsSignature =
    details.data?.availableTransitions.some((x) =>
      [
        "submit-scope",
        "approve-scope",
        "approve-root-cause",
        "approve-plan",
        "record-effectiveness",
        "approve-closure",
      ].includes(x.code),
    ) ?? false;
  const record = details.data?.record;
  const close = () => {
    setTab(0);
    onClose();
  };
  return (
    <Dialog
      open={Boolean(id)}
      onClose={close}
      maxWidth="lg"
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
                {record?.recordNumber ?? "DÖF"}
              </Typography>
              <Chip
                size="small"
                className="record-type-chip"
                label="DÖF KAYDI"
              />
            </Stack>
            <Typography variant="h5" className="record-header-title">
              {record?.title ?? "DÖF ayrıntısı"}
            </Typography>
          </Box>
          {record && (
            <Stack direction="row" spacing={1} className="record-header-badges">
              <Chip
                className="status-glass-chip"
                icon={<TaskAltRounded />}
                label={statusLabel(record.status)}
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
      {details.isLoading && <LinearProgress />}
      <DialogContent className="record-details-content">
        {details.isError && (
          <Alert severity="error">{details.error.message}</Alert>
        )}
        {transition.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {transition.error.message}
          </Alert>
        )}
        {record && details.data && (
          <>
            <Paper elevation={0} square className="record-detail-tabs-shell">
              <Tabs
                value={tab}
                onChange={(_, x: number) => setTab(x)}
                variant="scrollable"
              >
                <Tab
                  icon={<FactCheckRounded />}
                  iconPosition="start"
                  label="Genel Bakış"
                />
                <Tab
                  icon={<PlaylistAddCheckRounded />}
                  iconPosition="start"
                  label={`Aksiyonlar (${details.data.actions.length})`}
                />
                <Tab
                  icon={<RuleRounded />}
                  iconPosition="start"
                  label="Karar ve Etkinlik"
                />
                <Tab
                  icon={<HistoryRounded />}
                  iconPosition="start"
                  label={`Geçmiş (${details.data.auditTrail.length})`}
                />
              </Tabs>
            </Paper>
            <Box className="detail-tab-panel">
              {tab === 0 && (
                <Stack spacing={3}>
                  <Paper variant="outlined" className="workflow-visual-card">
                    <Typography sx={{ fontWeight: 800, mb: 2 }}>
                      DÖF kontrollü iş akışı
                    </Typography>
                    <Stepper
                      activeStep={Math.max(
                        0,
                        flow.findIndex((x) => x.value === record.status),
                      )}
                      alternativeLabel
                      className="visual-stepper"
                    >
                      {flow.map((x) => (
                        <Step
                          key={x.value}
                          completed={
                            flow.findIndex((s) => s.value === record.status) >
                              flow.indexOf(x) || record.status === "Closed"
                          }
                        >
                          <StepLabel>{x.label}</StepLabel>
                        </Step>
                      ))}
                    </Stepper>
                  </Paper>
                  <Box className="detail-grid">
                    <Info title="Problem tanımı" value={record.description} />
                    <Info title="Kök neden" value={record.rootCause} />
                    <Info
                      title="Acil düzeltmeler"
                      value={record.immediateActions}
                    />
                    <Info
                      title="Sorumluluk ve hedef"
                      value={`${record.owner} · ${date(record.targetDateUtc)}`}
                    />
                  </Box>
                  <RecordAssignments
                    aggregateType="Capa"
                    aggregateId={record.id}
                  />
                </Stack>
              )}
              {tab === 1 && (
                <ActionsPanel
                  details={details.data}
                  onUpdate={update}
                  canPlan={can(Permissions.capaPlan)}
                  canComplete={can(Permissions.capaCompleteAction)}
                  canVerify={can(Permissions.capaVerify)}
                />
              )}
              {tab === 2 && (
                <Stack spacing={2}>
                  <Paper variant="outlined" className="transition-panel">
                    <Typography variant="h6" sx={{ fontWeight: 800 }}>
                      Kontrollü karar
                    </Typography>
                    {record.effectivenessRequired && (
                      <Alert severity="info" sx={{ mt: 1.5 }}>
                        Yöntem: {record.effectivenessMethod} · Örneklem:{" "}
                        {record.effectivenessSample} ·{" "}
                        {record.observationPeriodDays} gün · Kriter:{" "}
                        {record.successCriteria}
                      </Alert>
                    )}
                    {details.data.availableTransitions.some(
                      (x) => x.noteRequired,
                    ) && (
                      <TextField
                        sx={{ mt: 2 }}
                        label="Karar notu / kanıt"
                        multiline
                        minRows={3}
                        fullWidth
                        value={note}
                        onChange={(e) => setNote(e.target.value)}
                      />
                    )}
                    {record.status === "EffectivenessReview" && (
                      <FormControlLabel
                        control={
                          <Checkbox
                            checked={isEffective}
                            onChange={(e) => setIsEffective(e.target.checked)}
                          />
                        }
                        label="DÖF etkili bulundu"
                      />
                    )}
                    {needsSignature && (
                      <Box sx={{ mt: 2 }}>
                        <Typography sx={{ fontWeight: 800 }}>
                          Elektronik imza doğrulaması
                        </Typography>
                        <TextField
                          label="Parolanızı yeniden girin"
                          type="password"
                          autoComplete="current-password"
                          value={signaturePassword}
                          onChange={(e) => setSignaturePassword(e.target.value)}
                          fullWidth
                          sx={{ mt: 1.5 }}
                        />
                        <FormControlLabel
                          sx={{ mt: 1 }}
                          control={
                            <Checkbox
                              checked={signatureMeaningAccepted}
                              onChange={(e) =>
                                setSignatureMeaningAccepted(e.target.checked)
                              }
                            />
                          }
                          label="Bu kararın elektronik imza anlamını okudum ve onaylıyorum."
                        />
                      </Box>
                    )}
                    <Stack direction="row" spacing={1.5} sx={{ mt: 2 }}>
                      {details.data.availableTransitions.map((x) => (
                        <Button
                          key={x.code}
                          variant="contained"
                          disabled={
                            transition.isPending ||
                            (x.noteRequired && !note.trim()) ||
                            (needsSignature &&
                              (!signaturePassword || !signatureMeaningAccepted))
                          }
                          onClick={() => transition.mutate(x.code)}
                        >
                          {x.label}
                        </Button>
                      ))}
                      {details.data.availableTransitions.length === 0 && (
                        <Typography color="text.secondary">
                          Bu kullanıcıya atanmış aktif bir karar görevi
                          bulunmuyor.
                        </Typography>
                      )}
                    </Stack>
                  </Paper>
                  {record.isEffective === false && (
                    <Alert severity="warning">
                      Etkinlik başarısız bulundu. Kayıt aksiyon planlamaya geri
                      döndü; yeni aksiyon ve revizyon gerekir.
                    </Alert>
                  )}
                </Stack>
              )}
              {tab === 3 && (
                <Box className="history-tab-panel">
                  <Stack
                    direction="row"
                    sx={{
                      justifyContent: "space-between",
                      alignItems: "center",
                      mb: 1,
                    }}
                  >
                    <Typography variant="h6" sx={{ fontWeight: 800 }}>
                      Elektronik imzalar
                    </Typography>
                    {record.status === "Closed" && (
                      <Button
                        variant="contained"
                        startIcon={<DownloadRounded />}
                        disabled={report.isPending}
                        onClick={() => report.mutate()}
                      >
                        {report.isPending ? "Hazırlanıyor…" : "Nihai PDF"}
                      </Button>
                    )}
                  </Stack>
                  {report.isError && (
                    <Alert severity="error" sx={{ mb: 1 }}>
                      {report.error.message}
                    </Alert>
                  )}
                  {details.data.signatures.length === 0 ? (
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ mb: 2 }}
                    >
                      Henüz elektronik imza yok.
                    </Typography>
                  ) : (
                    details.data.signatures.map((s) => (
                      <Alert key={s.id} severity="success" sx={{ mb: 1 }}>
                        {s.signerName} · {s.meaning} · {dateTime(s.signedAtUtc)}{" "}
                        · v{s.recordVersion}
                      </Alert>
                    ))
                  )}
                  <Typography variant="h6" sx={{ fontWeight: 800, mt: 3 }}>
                    Kronolojik DÖF geçmişi
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mb: 2 }}
                  >
                    En yeni işlemden en eski işleme doğru değiştirilemez kayıt
                    zinciri.
                  </Typography>
                  <AuditTimeline
                    events={details.data.auditTrail}
                    labels={eventLabels}
                  />
                </Box>
              )}
            </Box>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}

function ActionsPanel({
  details,
  onUpdate,
  canPlan,
  canComplete,
  canVerify,
}: {
  details: CapaDetails;
  onUpdate: (data: CapaDetails) => void;
  canPlan: boolean;
  canComplete: boolean;
  canVerify: boolean;
}) {
  const r = details.record;
  const lookups = useQuery({
    queryKey: ["capa-lookups"],
    queryFn: ({ signal }) => getCapaLookups(signal),
    retry: false,
  });
  const [type, setType] = useState("Düzeltici");
  const [description, setDescription] = useState("");
  const [ownerUserId, setOwnerUserId] = useState("");
  const [target, setTarget] = useState(localFuture(14));
  const [evidence, setEvidence] = useState<Record<string, string>>({});
  const [verification, setVerification] = useState<Record<string, string>>({});
  const add = useMutation({
    mutationFn: () =>
      addCapaAction(r.id, {
        expectedVersion: r.version,
        actionType: type,
        description,
        ownerUserId,
        targetDateUtc: new Date(target).toISOString(),
      }),
    onSuccess: (data) => {
      onUpdate(data);
      setDescription("");
      setOwnerUserId("");
    },
  });
  const complete = useMutation({
    mutationFn: (actionId: string) =>
      completeCapaAction(r.id, actionId, r.version, evidence[actionId] ?? ""),
    onSuccess: onUpdate,
  });
  const verify = useMutation({
    mutationFn: ({
      actionId,
      approved,
    }: {
      actionId: string;
      approved: boolean;
    }) =>
      verifyCapaAction(
        r.id,
        actionId,
        r.version,
        approved,
        verification[actionId] ?? "",
      ),
    onSuccess: onUpdate,
  });
  return (
    <Stack spacing={2}>
      {(add.isError || complete.isError || verify.isError) && (
        <Alert severity="error">
          {add.error?.message ??
            complete.error?.message ??
            verify.error?.message}
        </Alert>
      )}
      {canPlan && (r.status === "Draft" || r.status === "ActionPlanning") && (
        <Paper variant="outlined" className="capa-action-planner">
          <Stack
            direction="row"
            spacing={1.2}
            className="capa-action-planner-heading"
          >
            <Box className="section-heading-icon tone-violet">
              <PlaylistAddCheckRounded />
            </Box>
            <Box>
              <Typography sx={{ fontWeight: 800 }}>Aksiyon planla</Typography>
              <Typography variant="caption" color="text.secondary">
                Sorumluyu, hedefi ve uygulanacak faaliyeti tanımlayın.
              </Typography>
            </Box>
          </Stack>
          <Box className="capa-action-form-grid">
            <Box className="capa-action-type-field">
              <SearchableSelect
                label="Aksiyon türü"
                value={type}
                options={(
                  lookups.data?.actionTypes ?? ["Düzeltici", "Önleyici"]
                ).map((value) => ({ value, label: value }))}
                onChange={(value) => setType(value ?? "Düzeltici")}
                size="medium"
              />
            </Box>
            <TextField
              className="capa-action-description"
              fullWidth
              label="Aksiyon açıklaması"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
            <Box className="capa-action-owner">
              <SearchableSelect
                required
                size="medium"
                label="Aksiyon sorumlusu"
                value={ownerUserId || null}
                options={(lookups.data?.owners ?? []).map((x) => ({
                  value: x.id,
                  label: x.departmentName
                    ? `${x.displayName} · ${x.departmentName}`
                    : x.displayName,
                }))}
                onChange={(value) => setOwnerUserId(value ?? "")}
              />
            </Box>
            <TextField
              className="capa-action-target"
              fullWidth
              type="datetime-local"
              label="Hedef tarih"
              slotProps={{ inputLabel: { shrink: true } }}
              value={target}
              onChange={(e) => setTarget(e.target.value)}
            />
            <Button
              className="capa-action-add-button"
              variant="contained"
              disabled={!description.trim() || !ownerUserId || add.isPending}
              onClick={() => add.mutate()}
            >
              Aksiyonu ekle
            </Button>
          </Box>
        </Paper>
      )}
      {details.actions.map((a) => (
        <Paper
          variant="outlined"
          key={a.id}
          sx={{
            p: 2,
            borderLeft: 4,
            borderLeftColor:
              a.status === "Verified"
                ? "success.main"
                : a.status === "Rejected"
                  ? "error.main"
                  : "primary.main",
          }}
        >
          <Stack
            direction={{ xs: "column", sm: "row" }}
            sx={{ justifyContent: "space-between", gap: 1 }}
          >
            <Box>
              <Stack direction="row" spacing={1}>
                <Chip size="small" label={a.actionType} />
                <Chip
                  size="small"
                  color={
                    a.status === "Verified"
                      ? "success"
                      : a.status === "Rejected"
                        ? "error"
                        : "default"
                  }
                  label={actionStatus(a.status)}
                />
              </Stack>
              <Typography sx={{ fontWeight: 750, mt: 1 }}>
                {a.description}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {a.owner} · {date(a.targetDateUtc)}
              </Typography>
            </Box>
          </Stack>
          {a.completionEvidence && (
            <Alert severity="info" sx={{ mt: 1.5 }}>
              Tamamlama kanıtı: {a.completionEvidence}
            </Alert>
          )}
          {canComplete &&
            r.status === "Implementation" &&
            (a.status === "Planned" || a.status === "Rejected") && (
              <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                <TextField
                  fullWidth
                  size="small"
                  label="Tamamlama kanıtı"
                  value={evidence[a.id] ?? ""}
                  onChange={(e) =>
                    setEvidence((x) => ({ ...x, [a.id]: e.target.value }))
                  }
                />
                <Button
                  variant="contained"
                  disabled={!evidence[a.id]?.trim() || complete.isPending}
                  onClick={() => complete.mutate(a.id)}
                >
                  Tamamlandı bildir
                </Button>
              </Stack>
            )}
          {canVerify &&
            r.status === "ActionVerification" &&
            a.status === "CompletionRequested" && (
              <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
                <TextField
                  fullWidth
                  size="small"
                  label="KG doğrulama notu"
                  value={verification[a.id] ?? ""}
                  onChange={(e) =>
                    setVerification((x) => ({ ...x, [a.id]: e.target.value }))
                  }
                />
                <Button
                  color="success"
                  variant="contained"
                  disabled={!verification[a.id]?.trim()}
                  onClick={() =>
                    verify.mutate({ actionId: a.id, approved: true })
                  }
                >
                  Doğrula
                </Button>
                <Button
                  color="error"
                  variant="outlined"
                  disabled={!verification[a.id]?.trim()}
                  onClick={() =>
                    verify.mutate({ actionId: a.id, approved: false })
                  }
                >
                  Reddet
                </Button>
              </Stack>
            )}
        </Paper>
      ))}
      {details.actions.length === 0 && (
        <Alert severity="info">Henüz aksiyon planlanmadı.</Alert>
      )}
    </Stack>
  );
}

function SortCell({
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
function Info({ title, value }: { title: string; value: string }) {
  return (
    <Paper variant="outlined" className="detail-list-card">
      <Typography variant="overline" color="text.secondary">
        {title}
      </Typography>
      <Typography sx={{ fontWeight: 650 }}>{value}</Typography>
    </Paper>
  );
}
function actionStatus(value: string) {
  return (
    (
      {
        Planned: "Planlandı",
        CompletionRequested: "Doğrulama bekliyor",
        Verified: "KG doğruladı",
        Rejected: "Reddedildi",
      } as Record<string, string>
    )[value] ?? value
  );
}
function date(value: string) {
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium" }).format(
    new Date(value),
  );
}
function dateTime(value: string) {
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
function localFuture(days: number) {
  const d = new Date(Date.now() + days * 86400000);
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 16);
}
function initialCreate(source?: {
  id: string;
  recordNumber: string;
  title: string;
  description: string;
}): CreateCapaInput {
  return {
    sourceDeviationId: source?.id ?? null,
    sourceType: source ? "Deviation" : "Manual",
    title: source ? `${source.recordNumber} · ${source.title}` : "",
    description: source?.description ?? "",
    rootCause: "",
    immediateActions: "",
    ownerUserId: "",
    targetDateUtc: localFuture(30),
    effectivenessRequired: false,
    effectivenessMethod: "",
    effectivenessSample: "",
    observationPeriodDays: 30,
    successCriteria: "",
    effectivenessEvaluatorUserId: null,
  };
}
