import { useEffect, useMemo, useState } from "react";
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
  ApartmentRounded,
  AssignmentTurnedInRounded,
  ChangeCircleRounded,
  DownloadRounded,
  GavelRounded,
  HistoryRounded,
  LinkRounded,
  PublishedWithChangesRounded,
  SettingsRounded,
} from "@mui/icons-material";
import {
  addChangeAction,
  completeChangeAction,
  completeChangeAssessment,
  createChangeLookupDefinition,
  createChangeControl,
  downloadChangeControlFinalReport,
  getChangeControlDetails,
  getChangeControlLookups,
  listChangeLookupDefinitions,
  searchChangeControls,
  setAuthorityApproval,
  transitionChangeControl,
  updateChangeLookupDefinition,
  verifyChangeAction,
  type ChangeControlDetails,
  type ChangeControlListItem,
  type ChangeLookupDefinition,
  type CreateChangeControlInput,
} from "../../api/changeControls";
import { ModalHeader } from "../../components/ModalHeader";
import {
  SearchableMultiSelect,
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
import { ChangeControlFilters } from "./ChangeControlFilters";
import {
  emptyChangeControlFilters,
  toChangeControlColumnFilters,
  type ChangeControlFilterState,
} from "./changeControlFilterModel";

const statuses: Array<SelectOption<string>> = [
  ["Draft", "Taslak"],
  ["PreliminaryReview", "Ön değerlendirme"],
  ["DepartmentReview", "Paralel bölüm değerlendirmeleri"],
  ["BoardReview", "Değişiklik kurulu"],
  ["PlanApproval", "Plan onayı"],
  ["Implementation", "Uygulama"],
  ["CommissioningApproval", "Devreye alma onayı"],
  ["PostImplementationVerification", "Uygulama sonrası doğrulama"],
  ["ClosureApproval", "Nihai kapanış onayı"],
  ["Closed", "Kapalı"],
  ["RolledBack", "Geri alındı"],
].map(([value, label]) => ({ value, label }));
const flow = statuses.slice(1, 10);
const eventLabels: Record<string, string> = {
  ChangeControlCreated: "Değişiklik kaydı oluşturuldu",
  ChangeControlStatusChanged: "Değişiklik durumu değiştirildi",
  ChangeAssessmentCompleted: "Bölüm değerlendirmesi tamamlandı",
  ChangeActionAdded: "Uygulama aksiyonu eklendi",
  ChangeActionCompletionRequested: "Aksiyon kanıtla tamamlandı",
  ChangeActionVerified: "Aksiyon doğrulandı",
  AuthorityApprovalRecorded: "Otorite onay belgesi kaydedildi",
};
const statusLabel = (value: string) =>
  statuses.find((x) => x.value === value)?.label ?? value;

export function ChangeControlWorkspace() {
  const [params, setParams] = useSearchParams();
  const [createOpen, setCreateOpen] = useState(false);
  const [guideOpen, setGuideOpen] = useState(false);
  const [lookupSettingsOpen, setLookupSettingsOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filterValues, setFilterValues] = useState<ChangeControlFilterState>(
    emptyChangeControlFilters,
  );
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25);
  const [sortBy, setSortBy] = useState("createdAtUtc");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc");
  const { can } = useAuth();
  const selectedId = params.get("open");
  const filters = useMemo(
    () => toChangeControlColumnFilters(filterValues),
    [filterValues],
  );
  const request = useMemo(
    () => ({ page: page + 1, pageSize, sortBy, sortDirection, filters }),
    [page, pageSize, sortBy, sortDirection, filters],
  );
  const changes = useQuery({
    queryKey: ["change-controls", request],
    queryFn: ({ signal }) => searchChangeControls(request, signal),
    placeholderData: (previous) => previous,
    retry: false,
  });
  const lookups = useQuery({
    queryKey: ["change-control-lookups"],
    queryFn: ({ signal }) => getChangeControlLookups(signal),
    retry: false,
  });
  const lookupOptions = (
    items: Array<{ code: string; name: string }> | undefined,
  ) => (items ?? []).map((item) => ({ value: item.code, label: item.name }));
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
            <Box className="section-heading-icon tone-violet">
              <ChangeCircleRounded />
            </Box>
            <Typography variant="h2">Değişiklik kontrol iş listesi</Typography>
            <Chip size="small" color="success" label="M.03 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Tesis, süreç, ekipman ve sistem değişikliklerini etki
            değerlendirmeleriyle kontrollü biçimde devreye alın.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2} sx={{ flexWrap: "wrap" }}>
          <ModuleInfoButton module="M.03" onClick={() => setGuideOpen(true)} />
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setLookupSettingsOpen(true)}
            >
              M.03 tanımları
            </Button>
          )}
          {can(Permissions.changeCreate) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
            >
              Yeni değişiklik
            </Button>
          )}
        </Stack>
      </Stack>
      <Stack className="module-list-toolbar" direction="row" spacing={1.5} sx={{ mt: 3, alignItems: "center" }}>
        <AdvancedFilterButton
          open={filtersOpen}
          activeCount={filters.length}
          onClick={() => setFiltersOpen((x) => !x)}
        />
        <Typography variant="body2" color="text.secondary">
          {changes.data
            ? `${changes.data.totalCount} kayıt`
            : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>
      {filtersOpen && (
        <ChangeControlFilters
          value={filterValues}
          statusOptions={statuses}
          typeOptions={lookupOptions(lookups.data?.changeTypes)}
          riskOptions={lookupOptions(lookups.data?.riskLevels)}
          regulatoryOptions={lookupOptions(lookups.data?.regulatoryImpacts)}
          onApply={(next) => {
            setFilterValues(next);
            setPage(0);
          }}
        />
      )}
      <Paper className="deviation-table-card" elevation={0} sx={{ mt: 2.5 }}>
        {changes.isError && (
          <Alert severity="error" sx={{ m: 2 }}>
            {changes.error.message}
          </Alert>
        )}
        <TableContainer>
          <Table aria-label="Değişiklik kontrol iş listesi">
            <TableHead>
              <TableRow>
                <SortCell
                  field="recordNumber"
                  label="Değişiklik / Kaynak"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={sort}
                />
                <SortCell
                  field="title"
                  label="Başlık / Tür"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={sort}
                />
                <TableCell>Değerlendirme / Aksiyon</TableCell>
                <SortCell
                  field="riskLevel"
                  label="Risk / Ruhsat"
                  sortBy={sortBy}
                  direction={sortDirection}
                  onSort={sort}
                />
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
              {changes.isLoading && <TableLoadingRows columns={7} />}
              {changes.data?.items.map((item) => (
                <ChangeRow
                  key={item.id}
                  item={item}
                  regulatoryLabel={
                    lookups.data?.regulatoryImpacts.find(
                      (x) => x.code === item.regulatoryImpact,
                    )?.name
                  }
                  onOpen={() => setParams({ open: item.id })}
                />
              ))}
              {changes.isSuccess && changes.data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Box className="empty-state">
                      <Typography sx={{ fontWeight: 750 }}>
                        Henüz değişiklik kaydı yok
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        İlk kontrollü değişiklik kaydını oluşturarak
                        değerlendirme zincirini başlatın.
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
          count={Number(changes.data?.totalCount ?? 0)}
          page={page}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage="Sayfa boyutu"
          labelDisplayedRows={({ from, to, count }) =>
            `${from}–${to} / ${count}`
          }
          onPageChange={(_, next) => setPage(next)}
          onRowsPerPageChange={(e) => {
            setPageSize(Number(e.target.value) as 10 | 25 | 50 | 100);
            setPage(0);
          }}
        />
      </Paper>
      <ChangeCreateDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => {
          setCreateOpen(false);
          setParams({ open: id });
        }}
      />
      <ChangeLookupSettingsDialog
        open={lookupSettingsOpen}
        onClose={() => setLookupSettingsOpen(false)}
      />
      <ChangeDetailsDialog id={selectedId} onClose={() => setParams({})} />
      <ModuleGuideDialog
        module="M.03"
        open={guideOpen}
        onClose={() => setGuideOpen(false)}
      />
    </Box>
  );
}

function ChangeRow({
  item,
  regulatoryLabel,
  onOpen,
}: {
  item: ChangeControlListItem;
  regulatoryLabel?: string;
  onOpen: () => void;
}) {
  const complete = item.assessmentCount
    ? Math.round((item.completedAssessmentCount / item.assessmentCount) * 100)
    : 0;
  const action = item.actionCount
    ? Math.round((item.verifiedActionCount / item.actionCount) * 100)
    : 0;
  return (
    <TableRow hover>
      <TableCell>
        <Typography sx={{ fontWeight: 800, color: "primary.main" }}>
          {item.recordNumber}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {item.sourceRecordNumber
            ? `Bağlı: ${item.sourceRecordNumber}`
            : "Bağımsız kayıt"}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 700 }}>{item.title}</Typography>
        <Typography variant="caption" color="text.secondary">
          {item.changeType} · {item.owner}
        </Typography>
      </TableCell>
      <TableCell>
        <Stack spacing={0.7} sx={{ minWidth: 145 }}>
          <Typography variant="caption">
            Bölümler {item.completedAssessmentCount}/{item.assessmentCount} · %
            {complete}
          </Typography>
          <LinearProgress
            variant="determinate"
            value={complete}
            color="success"
          />
          <Typography variant="caption">
            Aksiyonlar {item.verifiedActionCount}/{item.actionCount} · %{action}
          </Typography>
        </Stack>
      </TableCell>
      <TableCell>
        <Stack direction="row" spacing={0.7}>
          <Chip
            size="small"
            color={
              item.riskLevel === "Kritik"
                ? "error"
                : item.riskLevel === "Yüksek"
                  ? "warning"
                  : "default"
            }
            label={item.riskLevel}
          />
          <Chip
            size="small"
            variant="outlined"
            label={regulatoryLabel ?? item.regulatoryImpact}
          />
        </Stack>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={
            item.status === "Closed"
              ? "success"
              : item.status === "RolledBack"
                ? "error"
                : "primary"
          }
          variant={item.status === "Closed" ? "filled" : "outlined"}
          label={statusLabel(item.status)}
        />
      </TableCell>
      <TableCell>{date(item.targetDateUtc)}</TableCell>
      <TableCell align="right">
        <Button onClick={onOpen}>Aç</Button>
      </TableCell>
    </TableRow>
  );
}

const lookupCategoryOptions = [
  { value: "ChangeType", label: "Değişiklik türü" },
  { value: "RiskLevel", label: "Risk seviyesi" },
  { value: "RegulatoryImpact", label: "Ruhsat etkisi" },
  { value: "ActionCategory", label: "Aksiyon kategorisi" },
];

function ChangeLookupSettingsDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const [category, setCategory] = useState("ChangeType");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [sortOrder, setSortOrder] = useState(100);
  const definitions = useQuery({
    queryKey: ["change-lookup-definitions"],
    queryFn: ({ signal }) => listChangeLookupDefinitions(signal),
    enabled: open,
    retry: false,
  });
  const refresh = async () => {
    await client.invalidateQueries({ queryKey: ["change-lookup-definitions"] });
    await client.invalidateQueries({ queryKey: ["change-control-lookups"] });
  };
  const create = useMutation({
    mutationFn: () =>
      createChangeLookupDefinition({ category, code, name, sortOrder }),
    onSuccess: async () => {
      setCode("");
      setName("");
      setSortOrder(100);
      await refresh();
    },
  });
  const update = useMutation({
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
      updateChangeLookupDefinition(id, {
        name: nextName,
        sortOrder: nextSortOrder,
        isActive,
      }),
    onSuccess: refresh,
  });
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.03 · YÖNETİLEN LOOKUP</Typography>
          <Typography variant="h5" sx={{ fontWeight: 800 }}>
            Değişiklik kontrol tanımları
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Kodlar geçmiş kayıtların snapshot bütünlüğünü korur; ad ve aktiflik
            yeni seçimleri yönetir.
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        {(definitions.isError || create.isError || update.isError) && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {definitions.error?.message ??
              create.error?.message ??
              update.error?.message}
          </Alert>
        )}
        <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
            <Box sx={{ minWidth: 190 }}>
              <SearchableSelect
                label="Kategori"
                value={category}
                options={lookupCategoryOptions}
                onChange={(value) => setCategory(value ?? "ChangeType")}
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
              required
              type="number"
              label="Sıra"
              value={sortOrder}
              onChange={(e) => setSortOrder(Number(e.target.value))}
              sx={{ width: 110 }}
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
          {definitions.data?.map((item) => (
            <ChangeLookupDefinitionRow
              key={item.id}
              item={item}
              saving={update.isPending}
              onSave={(nextName, nextSortOrder, isActive) =>
                update.mutate({
                  id: item.id,
                  nextName,
                  nextSortOrder,
                  isActive,
                })
              }
            />
          ))}
          {definitions.isLoading && <LinearProgress />}
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

function ChangeLookupDefinitionRow({
  item,
  saving,
  onSave,
}: {
  item: ChangeLookupDefinition;
  saving: boolean;
  onSave: (name: string, sortOrder: number, isActive: boolean) => void;
}) {
  const [name, setName] = useState(item.name);
  const [sortOrder, setSortOrder] = useState(item.sortOrder);
  const [isActive, setIsActive] = useState(item.isActive);
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
            lookupCategoryOptions.find((x) => x.value === item.category)
              ?.label ?? item.category
          }
          sx={{ minWidth: 145 }}
        />
        <Typography
          variant="body2"
          sx={{ minWidth: 150, fontFamily: "monospace" }}
        >
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
          sx={{ width: 95 }}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
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

function ChangeCreateDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (id: string) => void;
}) {
  const client = useQueryClient();
  const [values, setValues] =
    useState<CreateChangeControlInput>(initialCreate());
  const lookups = useQuery({
    queryKey: ["change-control-lookups"],
    queryFn: ({ signal }) => getChangeControlLookups(signal),
    enabled: open,
    retry: false,
  });
  useEffect(() => {
    if (!lookups.data) return;
    setValues((current) => ({
      ...current,
      changeType: lookups.data.changeTypes.some(
        (x) => x.code === current.changeType,
      )
        ? current.changeType
        : (lookups.data.changeTypes[0]?.code ?? ""),
      riskLevel: lookups.data.riskLevels.some(
        (x) => x.code === current.riskLevel,
      )
        ? current.riskLevel
        : (lookups.data.riskLevels[0]?.code ?? ""),
      regulatoryImpact: lookups.data.regulatoryImpacts.some(
        (x) => x.code === current.regulatoryImpact,
      )
        ? current.regulatoryImpact
        : (lookups.data.regulatoryImpacts[0]?.code ?? ""),
    }));
  }, [lookups.data]);
  const set = <K extends keyof CreateChangeControlInput>(
    key: K,
    value: CreateChangeControlInput[K],
  ) => setValues((current) => ({ ...current, [key]: value }));
  const reset = () => {
    setValues(initialCreate());
    onClose();
  };
  const mutation = useMutation({
    mutationFn: () =>
      createChangeControl({
        ...values,
        targetDateUtc: new Date(values.targetDateUtc).toISOString(),
        temporaryUntilUtc:
          values.isTemporary && values.temporaryUntilUtc
            ? new Date(values.temporaryUntilUtc).toISOString()
            : null,
      }),
    onSuccess: async (data) => {
      await client.invalidateQueries({ queryKey: ["change-controls"] });
      setValues(initialCreate());
      onCreated(data.record.id);
    },
  });
  const valid =
    values.changeType &&
    values.title.trim() &&
    values.currentState.trim() &&
    values.proposedState.trim() &&
    values.justification.trim() &&
    values.scope.trim() &&
    values.ownerUserId &&
    values.riskLevel &&
    values.regulatoryImpact &&
    values.riskSummary.trim() &&
    values.rollbackPlan.trim() &&
    values.impactedDepartmentIds.length > 0;
  return (
    <Dialog open={open} onClose={reset} maxWidth="lg" fullWidth>
      <ModalHeader onClose={reset}>
        <Box>
          <Typography variant="overline">M.03 · KONTROLLÜ KAYIT</Typography>
          <Typography variant="h5" sx={{ fontWeight: 800 }}>
            Yeni değişiklik kontrolü
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Etki, risk, geri dönüş ve paralel değerlendirme kapsamını
            başlangıçta tanımlayın.
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
          {lookups.isError && (
            <Alert severity="error">
              Organizasyon ve atama listeleri alınamadı.
            </Alert>
          )}
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <Box className="change-create-select">
              <SearchableSelect
                label="Değişiklik türü"
                value={values.changeType}
                options={(lookups.data?.changeTypes ?? []).map((item) => ({
                  value: item.code,
                  label: item.name,
                }))}
                onChange={(next) => set("changeType", next ?? "")}
                size="medium"
              />
            </Box>
            <TextField
              required
              fullWidth
              label="Değişiklik başlığı"
              value={values.title}
              onChange={(e) => set("title", e.target.value)}
            />
            <Box sx={{ flex: 1 }}>
              <SearchableSelect
                required
                size="medium"
                label="Değişiklik sorumlusu"
                value={values.ownerUserId || null}
                options={(lookups.data?.owners ?? []).map((x) => ({
                  value: x.id,
                  label: x.departmentName
                    ? `${x.displayName} · ${x.departmentName}`
                    : x.displayName,
                }))}
                onChange={(next) => set("ownerUserId", next ?? "")}
              />
            </Box>
          </Stack>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              required
              fullWidth
              multiline
              minRows={3}
              label="Mevcut durum"
              value={values.currentState}
              onChange={(e) => set("currentState", e.target.value)}
            />
            <TextField
              required
              fullWidth
              multiline
              minRows={3}
              label="Önerilen yeni durum"
              value={values.proposedState}
              onChange={(e) => set("proposedState", e.target.value)}
            />
          </Stack>
          <TextField
            required
            multiline
            minRows={2}
            label="Gerekçe"
            value={values.justification}
            onChange={(e) => set("justification", e.target.value)}
          />
          <TextField
            required
            multiline
            minRows={2}
            label="Kapsam"
            value={values.scope}
            onChange={(e) => set("scope", e.target.value)}
          />
          <SearchableMultiSelect
            required
            label="Etkilenen bölümler"
            values={values.impactedDepartmentIds}
            options={(lookups.data?.departments ?? []).map((x) => ({
              value: x.id,
              label: `${x.name} · ${x.reviewerName}`,
            }))}
            onChange={(next) => set("impactedDepartmentIds", next)}
          />
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <Box className="change-create-select">
              <SearchableSelect
                label="Risk seviyesi"
                value={values.riskLevel}
                options={(lookups.data?.riskLevels ?? []).map((item) => ({
                  value: item.code,
                  label: item.name,
                }))}
                onChange={(next) => set("riskLevel", next ?? "")}
                size="medium"
              />
            </Box>
            <Box className="change-create-regulatory">
              <SearchableSelect
                label="Ruhsat / varyasyon etkisi"
                value={values.regulatoryImpact}
                options={(lookups.data?.regulatoryImpacts ?? []).map(
                  (item) => ({ value: item.code, label: item.name }),
                )}
                onChange={(next) => set("regulatoryImpact", next ?? "")}
                size="medium"
              />
            </Box>
            <TextField
              fullWidth
              type="datetime-local"
              label="Hedef tarih"
              slotProps={{ inputLabel: { shrink: true } }}
              value={values.targetDateUtc}
              onChange={(e) => set("targetDateUtc", e.target.value)}
            />
          </Stack>
          <TextField
            required
            multiline
            minRows={2}
            label="Risk değerlendirme özeti"
            value={values.riskSummary}
            onChange={(e) => set("riskSummary", e.target.value)}
          />
          <TextField
            required
            multiline
            minRows={2}
            label="Kontrollü geri dönüş planı"
            value={values.rollbackPlan}
            onChange={(e) => set("rollbackPlan", e.target.value)}
          />
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={values.productImpact}
                  onChange={(e) => set("productImpact", e.target.checked)}
                />
              }
              label="Ürün / seri etkisi var"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={values.siteImpact}
                  onChange={(e) => set("siteImpact", e.target.checked)}
                />
              }
              label="Tesis etkisi var"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={values.validationRequired}
                  onChange={(e) => set("validationRequired", e.target.checked)}
                />
              }
              label="Validasyon gerekli"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={values.isTemporary}
                  onChange={(e) => set("isTemporary", e.target.checked)}
                />
              }
              label="Geçici değişiklik"
            />
          </Stack>
          {values.isTemporary && (
            <TextField
              type="datetime-local"
              label="Geçici değişiklik bitişi"
              slotProps={{ inputLabel: { shrink: true } }}
              value={values.temporaryUntilUtc ?? ""}
              onChange={(e) => set("temporaryUntilUtc", e.target.value)}
            />
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={reset}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={!valid || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          {mutation.isPending ? "Oluşturuluyor…" : "Değişiklik kaydını oluştur"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function ChangeDetailsDialog({
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
  const [successful, setSuccessful] = useState(true);
  const [authorityReference, setAuthorityReference] = useState("");
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureMeaningAccepted, setSignatureMeaningAccepted] =
    useState(false);
  const details = useQuery({
    queryKey: ["change-control-details", id],
    queryFn: ({ signal }) => getChangeControlDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const lookups = useQuery({
    queryKey: ["change-control-lookups"],
    queryFn: ({ signal }) => getChangeControlLookups(signal),
    retry: false,
  });
  const activeTaskRoles = new Set(details.data?.actionableTaskRoles ?? []);
  const update = async (data: ChangeControlDetails) => {
    client.setQueryData(["change-control-details", id], data);
    await client.invalidateQueries({ queryKey: ["change-controls"] });
    await client.invalidateQueries({
      queryKey: ["workflow-assignments", "ChangeControl", id],
    });
    setNote("");
    setSignaturePassword("");
    setSignatureMeaningAccepted(false);
  };
  const transition = useMutation({
    mutationFn: (code: string) =>
      transitionChangeControl(
        id!,
        details.data!.record.version,
        code,
        note || undefined,
        successful,
        signaturePassword || undefined,
        signatureMeaningAccepted,
      ),
    onSuccess: update,
  });
  const authority = useMutation({
    mutationFn: () =>
      setAuthorityApproval(
        id!,
        details.data!.record.version,
        authorityReference,
      ),
    onSuccess: (data) => {
      update(data);
      setAuthorityReference("");
    },
  });
  const report = useMutation({
    mutationFn: () => downloadChangeControlFinalReport(id!),
    onSuccess: (data) => {
      const url = URL.createObjectURL(data.blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = data.fileName;
      link.click();
      URL.revokeObjectURL(url);
    },
  });
  const record = details.data?.record;
  const close = () => {
    setTab(0);
    setNote("");
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
                {record?.recordNumber ?? "DK"}
              </Typography>
              <Chip
                size="small"
                className="record-type-chip"
                label="DEĞİŞİKLİK KAYDI"
              />
            </Stack>
            <Typography variant="h5" className="record-header-title">
              {record?.title ?? "Değişiklik ayrıntısı"}
            </Typography>
          </Box>
          {record && (
            <Stack direction="row" spacing={1} className="record-header-badges">
              <Chip
                className="status-glass-chip"
                icon={<PublishedWithChangesRounded />}
                label={statusLabel(record.status)}
              />
              <Chip className="status-glass-chip" label={record.riskLevel} />
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
        {(transition.isError || authority.isError) && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {transition.error?.message ?? authority.error?.message}
          </Alert>
        )}
        {record && details.data && (
          <Box className="record-detail-workspace">
            <Paper elevation={0} square className="record-detail-tabs-shell">
              <Tabs
                value={tab}
                onChange={(_, next: number) => setTab(next)}
                orientation="vertical"
                variant="standard"
                scrollButtons="auto"
                aria-label="Değişiklik kontrol detay bölümleri"
              >
                <Tab
                  disableRipple
                  icon={<ChangeCircleRounded />}
                  iconPosition="start"
                  label={<ChangeTabLabel text="Genel Bakış" />}
                />
                <Tab
                  disableRipple
                  icon={<ApartmentRounded />}
                  iconPosition="start"
                  label={
                    <ChangeTabLabel
                      text="Bölüm Etkileri"
                      count={details.data.assessments.length}
                    />
                  }
                />
                <Tab
                  disableRipple
                  icon={<AssignmentTurnedInRounded />}
                  iconPosition="start"
                  label={
                    <ChangeTabLabel
                      text="Uygulama"
                      count={details.data.actions.length}
                    />
                  }
                />
                <Tab
                  disableRipple
                  icon={<GavelRounded />}
                  iconPosition="start"
                  label={
                    <ChangeTabLabel
                      text="Karar ve Devreye Alma"
                      count={details.data.availableTransitions.length}
                    />
                  }
                />
                <Tab
                  disableRipple
                  icon={<HistoryRounded />}
                  iconPosition="start"
                  label={
                    <ChangeTabLabel
                      text="Geçmiş"
                      count={details.data.auditTrail.length}
                    />
                  }
                />
              </Tabs>
            </Paper>
            <Box className="detail-tab-panel" role="tabpanel">
              {tab === 0 && (
                <Stack className="record-tab-canvas" spacing={3}>
                  <Paper variant="outlined" className="workflow-visual-card">
                    <Typography sx={{ fontWeight: 800, mb: 2 }}>
                      Değişiklik kontrol yaşam döngüsü
                    </Typography>
                    <Box className="change-stepper-scroll">
                      <Stepper
                        activeStep={Math.max(
                          0,
                          flow.findIndex((x) => x.value === record.status),
                        )}
                        alternativeLabel
                        className="visual-stepper change-visual-stepper"
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
                    </Box>
                  </Paper>
                  <Box className="detail-grid">
                    <Info title="Mevcut durum" value={record.currentState} />
                    <Info title="Önerilen durum" value={record.proposedState} />
                    <Info
                      title="Gerekçe ve kapsam"
                      value={`${record.justification} · ${record.scope}`}
                    />
                    <Info
                      title="Risk ve etkiler"
                      value={`${record.riskLevel} · ${record.riskSummary}`}
                    />
                    <Info
                      title="Ruhsat etkisi"
                      value={
                        lookups.data?.regulatoryImpacts.find(
                          (x) => x.code === record.regulatoryImpact,
                        )?.name ?? record.regulatoryImpact
                      }
                    />
                    <Info
                      title="Geri dönüş planı"
                      value={record.rollbackPlan}
                    />
                  </Box>
                  <RecordAssignments
                    aggregateType="ChangeControl"
                    aggregateId={record.id}
                  />
                </Stack>
              )}
              {tab === 1 && (
                <AssessmentsPanel
                  details={details.data}
                  onUpdate={update}
                  canReview={can(Permissions.changeReview)}
                  activeTaskRoles={activeTaskRoles}
                />
              )}
              {tab === 2 && (
                <ImplementationPanel
                  details={details.data}
                  onUpdate={update}
                  canReview={can(Permissions.changeReview)}
                  canExecute={can(Permissions.changeExecute)}
                  activeTaskRoles={activeTaskRoles}
                />
              )}
              {tab === 3 && (
                <Stack className="record-tab-canvas" spacing={2}>
                  <Paper variant="outlined" className="transition-panel">
                    <Stack
                      direction="row"
                      spacing={1}
                      sx={{ alignItems: "center" }}
                    >
                      <GavelRounded color="primary" />
                      <Typography variant="h6" sx={{ fontWeight: 800 }}>
                        Kontrollü karar ve devreye alma
                      </Typography>
                    </Stack>
                    <Alert severity="info" sx={{ mt: 1.5 }}>
                      Devreye alma onayı ile nihai kapanış iki ayrı imza ve
                      zaman damgasıdır. Açık doküman, eğitim, validasyon veya
                      aksiyon varken geçiş sunucu tarafından engellenir.
                    </Alert>
                    {record.regulatoryImpact === "AuthorityApproval" &&
                      !record.authorityApprovalReference &&
                      can(Permissions.changeReview) && (
                        <Stack
                          direction={{ xs: "column", sm: "row" }}
                          spacing={1}
                          sx={{ mt: 2 }}
                        >
                          <TextField
                            fullWidth
                            label="Otorite onay belge referansı"
                            value={authorityReference}
                            onChange={(e) =>
                              setAuthorityReference(e.target.value)
                            }
                          />
                          <Button
                            variant="outlined"
                            disabled={
                              !authorityReference.trim() || authority.isPending
                            }
                            onClick={() => authority.mutate()}
                          >
                            Belgeyi kaydet
                          </Button>
                        </Stack>
                      )}
                    {record.authorityApprovalReference && (
                      <Alert severity="success" sx={{ mt: 2 }}>
                        Otorite onayı: {record.authorityApprovalReference}
                      </Alert>
                    )}
                    {details.data.availableTransitions.some(
                      (x) => x.noteRequired,
                    ) && (
                      <TextField
                        sx={{ mt: 2 }}
                        fullWidth
                        multiline
                        minRows={3}
                        label="Karar gerekçesi / imza anlamı"
                        value={note}
                        onChange={(e) => setNote(e.target.value)}
                      />
                    )}
                    {record.status === "PostImplementationVerification" && (
                      <FormControlLabel
                        control={
                          <Checkbox
                            checked={successful}
                            onChange={(e) => setSuccessful(e.target.checked)}
                          />
                        }
                        label="Uygulama sonrası doğrulama başarılı"
                      />
                    )}
                    {details.data.availableTransitions.length > 0 && (
                      <Box sx={{ mt: 2 }}>
                        <TextField
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
                          label="Kararın elektronik imza anlamını kabul ediyorum."
                        />
                      </Box>
                    )}
                    <Stack
                      direction="row"
                      spacing={1.5}
                      useFlexGap
                      sx={{ mt: 2, flexWrap: "wrap" }}
                    >
                      {details.data.availableTransitions.map((item) => (
                        <Button
                          key={item.code}
                          color={item.code === "rollback" ? "error" : "primary"}
                          variant={
                            item.code === "rollback" ? "outlined" : "contained"
                          }
                          disabled={
                            transition.isPending ||
                            (item.noteRequired && !note.trim()) ||
                            !signaturePassword ||
                            !signatureMeaningAccepted
                          }
                          onClick={() => transition.mutate(item.code)}
                        >
                          {item.label}
                        </Button>
                      ))}
                      {details.data.availableTransitions.length === 0 && (
                        <Typography color="text.secondary">
                          Bu kayıt için kullanılabilir durum geçişi bulunmuyor.
                        </Typography>
                      )}
                    </Stack>
                  </Paper>
                  {record.commissionedAtUtc && (
                    <Alert severity="success">
                      Devreye alma imzası: {dateTime(record.commissionedAtUtc)}
                    </Alert>
                  )}
                  {record.status === "RolledBack" && (
                    <Alert severity="error">
                      Değişiklik kontrollü geri dönüş planına alındı. Sonuç:{" "}
                      {record.postImplementationResult}
                    </Alert>
                  )}
                </Stack>
              )}
              {tab === 4 && (
                <Box className="record-tab-canvas history-tab-panel">
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
                    <Typography variant="body2" color="text.secondary">
                      Henüz elektronik imza yok.
                    </Typography>
                  ) : (
                    details.data.signatures.map((signature) => (
                      <Alert
                        severity="success"
                        key={signature.id}
                        sx={{ mb: 1 }}
                      >
                        {signature.signerName} · {signature.meaning} ·{" "}
                        {dateTime(signature.signedAtUtc)} · v
                        {signature.recordVersion}
                      </Alert>
                    ))
                  )}
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>
                    Kronolojik değişiklik geçmişi
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mb: 2 }}
                  >
                    En yeni işlemden en eski işleme doğru değiştirilemez karar
                    ve kanıt zinciri.
                  </Typography>
                  <AuditTimeline
                    events={details.data.auditTrail}
                    labels={eventLabels}
                  />
                </Box>
              )}
            </Box>
          </Box>
        )}
      </DialogContent>
    </Dialog>
  );
}

function AssessmentsPanel({
  details,
  onUpdate,
  canReview,
  activeTaskRoles,
}: {
  details: ChangeControlDetails;
  onUpdate: (data: ChangeControlDetails) => void;
  canReview: boolean;
  activeTaskRoles: Set<string>;
}) {
  const [impact, setImpact] = useState<Record<string, string>>({});
  const [actions, setActions] = useState<Record<string, string>>({});
  const mutation = useMutation({
    mutationFn: ({
      assessmentId,
      approved,
    }: {
      assessmentId: string;
      approved: boolean;
    }) =>
      completeChangeAssessment(
        details.record.id,
        assessmentId,
        details.record.version,
        approved,
        impact[assessmentId] ?? "",
        actions[assessmentId] || "Ek aksiyon gerekmiyor.",
      ),
    onSuccess: onUpdate,
  });
  return (
    <Stack className="record-tab-canvas" spacing={2}>
      {mutation.isError && (
        <Alert severity="error">{mutation.error.message}</Alert>
      )}
      {details.assessments.map((item) => {
        const assigned = activeTaskRoles.has(`Assessment:${item.id}`);
        return (
          <Paper
            variant="outlined"
            key={item.id}
            className="change-assessment-card"
          >
            <Stack
              direction={{ xs: "column", md: "row" }}
              sx={{ justifyContent: "space-between", gap: 1.5 }}
            >
              <Box>
                <Stack direction="row" spacing={1}>
                  <Chip
                    size="small"
                    icon={<ApartmentRounded />}
                    label={item.department}
                  />
                  <Chip
                    size="small"
                    color={
                      item.status === "Approved"
                        ? "success"
                        : item.status === "Rejected"
                          ? "error"
                          : "warning"
                    }
                    label={assessmentStatus(item.status)}
                  />
                  {assigned && item.status === "Pending" && (
                    <Chip
                      size="small"
                      color="primary"
                      variant="outlined"
                      label="Size atandı"
                    />
                  )}
                </Stack>
                <Typography sx={{ mt: 1, fontWeight: 750 }}>
                  {item.reviewer}
                </Typography>
                {item.impactSummary && (
                  <Typography variant="body2" sx={{ mt: 0.7 }}>
                    {item.impactSummary}
                  </Typography>
                )}
                {item.requiredActions && (
                  <Typography variant="caption" color="text.secondary">
                    Gerekli aksiyonlar: {item.requiredActions}
                  </Typography>
                )}
              </Box>
            </Stack>
            {canReview &&
              assigned &&
              details.record.status === "DepartmentReview" &&
              item.status === "Pending" && (
                <Stack spacing={1.2} sx={{ mt: 2 }}>
                  <TextField
                    fullWidth
                    label={`${item.department} etki değerlendirmesi`}
                    value={impact[item.id] ?? ""}
                    onChange={(e) =>
                      setImpact((x) => ({ ...x, [item.id]: e.target.value }))
                    }
                  />
                  <TextField
                    fullWidth
                    label="Gerekli aksiyonlar"
                    value={actions[item.id] ?? ""}
                    onChange={(e) =>
                      setActions((x) => ({ ...x, [item.id]: e.target.value }))
                    }
                  />
                  <Stack direction="row" spacing={1}>
                    <Button
                      variant="contained"
                      color="success"
                      disabled={!impact[item.id]?.trim() || mutation.isPending}
                      onClick={() =>
                        mutation.mutate({
                          assessmentId: item.id,
                          approved: true,
                        })
                      }
                    >
                      Uygun bul
                    </Button>
                    <Button
                      variant="outlined"
                      color="error"
                      disabled={!impact[item.id]?.trim() || mutation.isPending}
                      onClick={() =>
                        mutation.mutate({
                          assessmentId: item.id,
                          approved: false,
                        })
                      }
                    >
                      Revizyona gönder
                    </Button>
                  </Stack>
                </Stack>
              )}
          </Paper>
        );
      })}
    </Stack>
  );
}

function ImplementationPanel({
  details,
  onUpdate,
  canReview,
  canExecute,
  activeTaskRoles,
}: {
  details: ChangeControlDetails;
  onUpdate: (data: ChangeControlDetails) => void;
  canReview: boolean;
  canExecute: boolean;
  activeTaskRoles: Set<string>;
}) {
  const r = details.record;
  const lookups = useQuery({
    queryKey: ["change-control-lookups"],
    queryFn: ({ signal }) => getChangeControlLookups(signal),
    retry: false,
  });
  const [category, setCategory] = useState("");
  const [description, setDescription] = useState("");
  const [ownerUserId, setOwnerUserId] = useState("");
  const [target, setTarget] = useState(localFuture(14));
  const [evidence, setEvidence] = useState<Record<string, string>>({});
  const [verification, setVerification] = useState<Record<string, string>>({});
  useEffect(() => {
    if (!lookups.data) return;
    setCategory((current) =>
      lookups.data.actionCategories.some((x) => x.code === current)
        ? current
        : (lookups.data.actionCategories[0]?.code ?? ""),
    );
  }, [lookups.data]);
  const add = useMutation({
    mutationFn: () =>
      addChangeAction(r.id, {
        expectedVersion: r.version,
        category,
        description,
        ownerUserId,
        targetDateUtc: new Date(target).toISOString(),
        isBlocking: true,
      }),
    onSuccess: (data) => {
      onUpdate(data);
      setDescription("");
      setOwnerUserId("");
    },
  });
  const complete = useMutation({
    mutationFn: (actionId: string) =>
      completeChangeAction(r.id, actionId, r.version, evidence[actionId] ?? ""),
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
      verifyChangeAction(
        r.id,
        actionId,
        r.version,
        approved,
        verification[actionId] ?? "",
      ),
    onSuccess: onUpdate,
  });
  return (
    <Stack className="record-tab-canvas" spacing={2}>
      {(add.isError || complete.isError || verify.isError) && (
        <Alert severity="error">
          {add.error?.message ??
            complete.error?.message ??
            verify.error?.message}
        </Alert>
      )}
      {canReview &&
        activeTaskRoles.has("Approver") &&
        r.status === "PlanApproval" && (
          <Paper variant="outlined" className="capa-action-planner">
            <Typography sx={{ fontWeight: 800, mb: 2 }}>
              Uygulama aksiyonu planla
            </Typography>
            <Box className="capa-action-form-grid">
              <SearchableSelect
                label="Kategori"
                value={category}
                options={(lookups.data?.actionCategories ?? []).map((item) => ({
                  value: item.code,
                  label: item.name,
                }))}
                onChange={(next) => setCategory(next ?? "")}
                size="medium"
              />
              <TextField
                fullWidth
                label="Aksiyon açıklaması"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
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
              <TextField
                fullWidth
                type="datetime-local"
                label="Hedef tarih"
                slotProps={{ inputLabel: { shrink: true } }}
                value={target}
                onChange={(e) => setTarget(e.target.value)}
              />
              <Button
                variant="contained"
                disabled={
                  !category ||
                  !description.trim() ||
                  !ownerUserId ||
                  add.isPending
                }
                onClick={() => add.mutate()}
              >
                Aksiyonu ekle
              </Button>
            </Box>
          </Paper>
        )}
      {details.actions.map((item) => (
        <Paper
          variant="outlined"
          key={item.id}
          sx={{
            p: 2,
            borderLeft: 4,
            borderLeftColor:
              item.status === "Verified"
                ? "success.main"
                : item.status === "Rejected"
                  ? "error.main"
                  : "primary.main",
          }}
        >
          <Stack direction="row" spacing={1}>
            <Chip size="small" label={item.category} />
            <Chip
              size="small"
              color={
                item.status === "Verified"
                  ? "success"
                  : item.status === "Rejected"
                    ? "error"
                    : "default"
              }
              label={actionStatus(item.status)}
            />
            {item.isBlocking && (
              <Chip
                size="small"
                variant="outlined"
                label="Devreye alma kapısı"
              />
            )}
          </Stack>
          <Typography sx={{ fontWeight: 750, mt: 1 }}>
            {item.description}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {item.owner} · {date(item.targetDateUtc)}
          </Typography>
          {item.completionEvidence && (
            <Alert severity="info" sx={{ mt: 1.5 }}>
              Kanıt: {item.completionEvidence}
            </Alert>
          )}
          {canExecute &&
            activeTaskRoles.has(`ActionOwner:${item.id}`) &&
            r.status === "Implementation" &&
            (item.status === "Planned" || item.status === "Rejected") && (
              <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={1}
                sx={{ mt: 1.5 }}
              >
                <TextField
                  fullWidth
                  size="small"
                  label="Tamamlama kanıtı / belge referansı"
                  value={evidence[item.id] ?? ""}
                  onChange={(e) =>
                    setEvidence((x) => ({ ...x, [item.id]: e.target.value }))
                  }
                />
                <Button
                  variant="contained"
                  disabled={!evidence[item.id]?.trim() || complete.isPending}
                  onClick={() => complete.mutate(item.id)}
                >
                  Tamamlandı bildir
                </Button>
              </Stack>
            )}
          {canReview &&
            activeTaskRoles.has("Evaluator") &&
            r.status === "Implementation" &&
            item.status === "CompletionRequested" && (
              <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={1}
                sx={{ mt: 1.5 }}
              >
                <TextField
                  fullWidth
                  size="small"
                  label="KG doğrulama notu"
                  value={verification[item.id] ?? ""}
                  onChange={(e) =>
                    setVerification((x) => ({
                      ...x,
                      [item.id]: e.target.value,
                    }))
                  }
                />
                <Button
                  color="success"
                  variant="contained"
                  disabled={!verification[item.id]?.trim()}
                  onClick={() =>
                    verify.mutate({ actionId: item.id, approved: true })
                  }
                >
                  Doğrula
                </Button>
                <Button
                  color="error"
                  variant="outlined"
                  disabled={!verification[item.id]?.trim()}
                  onClick={() =>
                    verify.mutate({ actionId: item.id, approved: false })
                  }
                >
                  Reddet
                </Button>
              </Stack>
            )}
        </Paper>
      ))}
      {details.actions.length === 0 && (
        <Alert severity="info">Henüz uygulama aksiyonu planlanmadı.</Alert>
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
function ChangeTabLabel({ text, count }: { text: string; count?: number }) {
  return (
    <Stack
      direction="row"
      spacing={1}
      sx={{ alignItems: "center", width: "100%" }}
    >
      <span>{text}</span>
      {count !== undefined && (
        <Chip
          size="small"
          label={count}
          sx={{ ml: "auto !important", height: 22 }}
        />
      )}
    </Stack>
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
function assessmentStatus(value: string) {
  return (
    (
      {
        Pending: "Bekliyor",
        Approved: "Uygun",
        Rejected: "Revizyon gerekli",
      } as Record<string, string>
    )[value] ?? value
  );
}
function actionStatus(value: string) {
  return (
    (
      {
        Planned: "Planlandı",
        CompletionRequested: "Doğrulama bekliyor",
        Verified: "Doğrulandı",
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
function initialCreate(): CreateChangeControlInput {
  return {
    sourceCapaId: null,
    changeType: "",
    title: "",
    currentState: "",
    proposedState: "",
    justification: "",
    scope: "",
    isTemporary: false,
    temporaryUntilUtc: null,
    ownerUserId: "",
    targetDateUtc: localFuture(45),
    riskLevel: "",
    riskSummary: "",
    productImpact: false,
    siteImpact: false,
    validationRequired: false,
    regulatoryImpact: "",
    rollbackPlan: "",
    impactedDepartmentIds: [],
  };
}
