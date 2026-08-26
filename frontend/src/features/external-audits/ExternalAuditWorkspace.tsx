import { useMemo, useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AccountBalanceRounded,
  AddRounded,
  FolderZipRounded,
  GavelRounded,
  HistoryRounded,
  HubRounded,
  LockRounded,
  ReportProblemRounded,
  TravelExploreRounded,
  VerifiedRounded,
  SettingsRounded,
  DownloadRounded,
} from "@mui/icons-material";
import {
  Alert,
  Autocomplete,
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
  addExternalAuditFinding,
  closeExternalAuditFinding,
  createExternalAudit,
  exportExternalAuditDocument,
  getExternalAuditDetails,
  getExternalAuditOptions,
  getExternalAuditLookupDefinitions,
  createExternalAuditLookupDefinition,
  updateExternalAuditLookupDefinition,
  downloadExternalAuditFinalReport,
  recordExternalAuditClosureLetter,
  respondExternalAuditFinding,
  searchExternalAudits,
  transitionExternalAudit,
  type CreateExternalAuditInput,
  type ExternalAuditDetails,
  type ExternalAuditListItem,
} from "../../api/externalAudits";
import { searchDocuments, type DocumentListItem } from "../../api/documents";
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
import { ExternalAuditFilters } from "./ExternalAuditFilters";
import {
  emptyExternalAuditFilters,
  toExternalAuditColumnFilters,
  type ExternalAuditFilterState,
} from "./externalAuditFilterModel";
const statusLabels: Record<string, string> = {
  Notified: "Planlandı / bildirildi",
  Preparation: "Hazırlık",
  AuditInProgress: "Denetim",
  Findings: "Bulgular",
  ResponsePlan: "Cevap planı",
  CapaAction: "DÖF / aksiyon",
  ClosureLetter: "Kapanış mektubu",
  AuthorityClosure: "Yetkili kapanışı",
  Closed: "Kapalı",
};
const stages = [
  "Bildirildi",
  "Hazırlık",
  "Denetim",
  "Bulgular",
  "Cevap planı",
  "DÖF / aksiyon",
  "Kapanış mektubu",
  "Yetkili kapanışı",
  "Kapalı",
];
const stageStatus = [
  "Notified",
  "Preparation",
  "AuditInProgress",
  "Findings",
  "ResponsePlan",
  "CapaAction",
  "ClosureLetter",
  "AuthorityClosure",
  "Closed",
];
const classification: Record<string, string> = {
  Critical: "Kritik",
  Major: "Majör",
  Minor: "Minör",
  Observation: "Gözlem",
};
const eventLabels: Record<string, string> = {
  ExternalAuditCreated: "Dış denetim kaydı oluşturuldu",
  ExternalAuditStatusChanged: "Dış denetim aşaması değiştirildi",
  ExternalAuditDocumentExported:
    "Talep paketi dokümanı kontrollü dışa aktarıldı",
  ExternalAuditFindingCreated: "Resmi dış denetim bulgusu açıldı",
  ExternalAuditFindingResponseSubmitted: "Bulgu cevabı ve taahhüdü kaydedildi",
  ExternalAuditFindingClosed: "Dış denetim bulgusu kapatıldı",
  ExternalAuditClosureLetterRecorded:
    "Kapanış mektubu ve kabul kanıtı kaydedildi",
};
const dt = (v: string | null) =>
  v
    ? new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(v))
    : "—";
export function ExternalAuditWorkspace() {
  const [params, setParams] = useSearchParams();
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState<ExternalAuditFilterState>(
    emptyExternalAuditFilters,
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
    () => toExternalAuditColumnFilters(filters),
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
    queryKey: ["external-audits", request],
    queryFn: ({ signal }) => searchExternalAudits(request, signal),
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
            <Box className="section-heading-icon tone-indigo">
              <TravelExploreRounded />
            </Box>
            <Typography variant="h2">Dış denetimler</Typography>
            <Chip size="small" color="success" label="M.08 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Otorite ve müşteri denetimlerini kontrollü talep paketinden resmi
            cevaba, DÖF’e ve yetkili kapanışına kadar yönetin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2}>
          <ModuleInfoButton module="M.08" onClick={() => setGuideOpen(true)} />
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setLookupOpen(true)}
            >
              M.08 tanımları
            </Button>
          )}
          {can(Permissions.externalAuditCreate) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
            >
              Yeni dış denetim
            </Button>
          )}
        </Stack>
      </Stack>
      <Box className="complaint-kpi-grid">
        <Kpi
          icon={<TravelExploreRounded />}
          label="Görünen denetim"
          value={query.data?.totalCount ?? 0}
        />
        <Kpi
          icon={<AccountBalanceRounded />}
          label="Devlet kurumu"
          value={items.filter((x) => x.isGovernmentAuthority).length}
          tone="amber"
        />
        <Kpi
          icon={<ReportProblemRounded />}
          label="Açık bulgu"
          value={items.reduce((n, x) => n + x.openFindingCount, 0)}
          tone="rose"
        />
        <Kpi
          icon={<FolderZipRounded />}
          label="Aktarılan doküman"
          value={items.reduce((n, x) => n + x.exportedDocumentCount, 0)}
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
            ? `${query.data.totalCount} dış denetim kaydı`
            : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>
      {filtersOpen && (
        <ExternalAuditFilters
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
          <Table aria-label="Dış denetim iş listesi">
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
                  text="Denetim / kurum"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell>Kurum türü / ülke</TableCell>
                <Sort
                  field="plannedStartUtc"
                  text="Takvim"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell>Talep paketi</TableCell>
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
                <TableLoadingRows columns={8} rows={6} />
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
                  <TableCell colSpan={8}>
                    <Alert severity="info" sx={{ my: 2 }}>
                      Filtrelerle eşleşen dış denetim bulunamadı.
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
        <CreateAuditDialog
          open
          onClose={() => setCreateOpen(false)}
          onCreated={(d) => {
            setCreateOpen(false);
            setParams({ open: d.record.id });
          }}
        />
      )}
      <AuditDetailsDialog
        id={params.get("open")}
        onClose={() => setParams({})}
      />
      <ModuleGuideDialog
        open={guideOpen}
        module="M.08"
        onClose={() => setGuideOpen(false)}
      />
      <ExternalAuditLookupDialog
        open={lookupOpen}
        onClose={() => setLookupOpen(false)}
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
  item: ExternalAuditListItem;
  onOpen: () => void;
}) {
  return (
    <TableRow hover onClick={onOpen} sx={{ cursor: "pointer" }}>
      <TableCell>
        <Typography color="primary" sx={{ fontWeight: 800 }}>
          {item.recordNumber}
        </Typography>
        <Typography variant="caption">{item.auditKind}</Typography>
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 760 }}>{item.title}</Typography>
        <Typography variant="caption" color="text.secondary">
          {item.auditorOrganization}
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={item.isGovernmentAuthority ? "warning" : "default"}
          label={
            item.isGovernmentAuthority
              ? "Devlet kurumu"
              : "Müşteri / sertifikasyon"
          }
        />
        <Typography variant="caption" sx={{ display: "block" }}>
          {item.authorityCountry}
        </Typography>
      </TableCell>
      <TableCell>
        {dt(item.plannedStartUtc)}
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ display: "block" }}
        >
          Cevap: {dt(item.responseDueAtUtc)}
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={
            item.exportedDocumentCount === item.documentCount
              ? "success"
              : "warning"
          }
          label={`${item.exportedDocumentCount} / ${item.documentCount} aktarıldı`}
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
      <TableCell align="right">
        <RecordActionMenu onOpen={onOpen} />
      </TableCell>
    </TableRow>
  );
}
function ExternalAuditLookupDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const [category, setCategory] = useState("AuditKind");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [sortOrder, setSortOrder] = useState(100);
  const q = useQuery({
    queryKey: ["external-audit-lookups"],
    queryFn: getExternalAuditLookupDefinitions,
    enabled: open,
    retry: false,
  });
  const refresh = async () => {
    await qc.invalidateQueries({ queryKey: ["external-audit-lookups"] });
    await qc.invalidateQueries({ queryKey: ["m08-options"] });
  };
  const create = useMutation({
    mutationFn: () =>
      createExternalAuditLookupDefinition({ category, code, name, sortOrder }),
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
    }) => updateExternalAuditLookupDefinition(x.id, x),
    onSuccess: refresh,
  });
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.08 · YÖNETİLEN LOOKUP</Typography>
          <Typography variant="h5">Dış denetim tanımları</Typography>
          <Typography color="text.secondary">
            Kod geçmiş kayıtlarda değişmez snapshot olarak korunur; görünen ad
            ve aktiflik yalnız yeni seçimleri yönetir.
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.2}
          sx={{ mb: 2 }}
        >
          <SearchableSelect
            label="Kategori *"
            value={category}
            options={[
              { value: "AuditKind", label: "Denetim türü" },
              { value: "Country", label: "Ülke" },
              { value: "Confidentiality", label: "Gizlilik" },
            ]}
            onChange={(v) => setCategory(v ?? "AuditKind")}
          />
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
            <ExternalAuditLookupRow
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
function ExternalAuditLookupRow({
  item,
  saving,
  save,
}: {
  item: {
    category: string;
    code: string;
    name: string;
    sortOrder: number;
    isActive: boolean;
  };
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
        <Chip size="small" label={item.category} />
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
  onCreated: (d: ExternalAuditDetails) => void;
}) {
  const { user } = useAuth();
  const now = new Date();
  const [start] = useState(() =>
    new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 16),
  );
  const [end] = useState(() =>
    new Date(Date.now() + 9 * 86400000).toISOString().slice(0, 16),
  );
  const [due] = useState(() =>
    new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 16),
  );
  const [form, setForm] = useState<CreateExternalAuditInput>({
    title: "",
    auditKind: "",
    auditorOrganization: "",
    isGovernmentAuthority: true,
    authorityCountry: "",
    officialReference: "",
    scope: "",
    site: "",
    ownerUserId: user.id,
    owner: user.displayName,
    notifiedAtUtc: now.toISOString().slice(0, 16),
    plannedStartUtc: start,
    plannedEndUtc: end,
    responseDueAtUtc: due,
    authorizedCloserUserId: null,
    authorizedCloser: null,
    documentRequests: [],
  });
  const [manualDocuments, setManualDocuments] = useState<
    Array<{
      documentCode: string;
      title: string;
      confidentiality: string;
    }>
  >([]);
  const [selectedDocuments, setSelectedDocuments] = useState<
    DocumentListItem[]
  >([]);
  const options = useQuery({
    queryKey: ["m08-options"],
    queryFn: ({ signal }) => getExternalAuditOptions(signal),
    enabled: open,
  });
  const documentOptions = useQuery({
    queryKey: ["m08-document-options"],
    queryFn: ({ signal }) =>
      searchDocuments(
        {
          page: 1,
          pageSize: 100,
          sortBy: "documentCode",
          sortDirection: "asc",
          filters: [
            { field: "status", operator: "equals", value: "Effective" },
          ],
        },
        signal,
      ),
    enabled: open,
    retry: false,
  });
  const qc = useQueryClient();
  const mutation = useMutation({
    mutationFn: createExternalAudit,
    onSuccess: (d) => {
      qc.invalidateQueries({ queryKey: ["external-audits"] });
      onCreated(d);
    },
  });
  const set = <K extends keyof CreateExternalAuditInput>(
    k: K,
    v: CreateExternalAuditInput[K],
  ) => setForm((x) => ({ ...x, [k]: v }));
  const parsed = manualDocuments
    .filter((x) => x.documentCode.trim() && x.title.trim() && x.confidentiality)
    .map((x) => ({
      controlledDocumentId: null,
      documentCode: x.documentCode.trim(),
      title: x.title.trim(),
      confidentiality: x.confidentiality,
    }));
  const packageRequests = [
    ...selectedDocuments.map((document) => ({
      controlledDocumentId: document.id,
      documentCode: document.documentCode,
      title: `${document.title} · v${document.currentRevision}`,
      confidentiality: document.confidentiality,
    })),
    ...parsed.filter(
      (manual) =>
        !selectedDocuments.some(
          (document) => document.documentCode === manual.documentCode,
        ),
    ),
  ];
  const valid =
    form.title &&
    form.auditorOrganization &&
    form.officialReference &&
    form.scope &&
    form.site &&
    form.ownerUserId &&
    form.auditKind &&
    form.authorityCountry &&
    form.authorizedCloserUserId &&
    manualDocuments.every(
      (x) => x.documentCode.trim() && x.title.trim() && x.confidentiality,
    ) &&
    packageRequests.length > 0 &&
    true;
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
          M.08 · KONTROLLÜ DIŞ DENETİM KABULÜ
        </Typography>
        <Typography variant="h4">Yeni dış denetim</Typography>
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
              value={form.auditKind}
              options={(options.data?.auditKinds ?? []).map((x) => ({
                value: x.code,
                label: x.name,
              }))}
              onChange={(v) => set("auditKind", v ?? "")}
            />
            <TextField
              label="Denetleyen kurum / müşteri"
              value={form.auditorOrganization}
              onChange={(e) => set("auditorOrganization", e.target.value)}
            />
            <SearchableSelect
              label="Ülke *"
              value={form.authorityCountry}
              options={(options.data?.countries ?? []).map((x) => ({
                value: x.code,
                label: x.name,
              }))}
              onChange={(v) => set("authorityCountry", v ?? "")}
            />
            <TextField
              label="Resmi bildirim / referans"
              value={form.officialReference}
              onChange={(e) => set("officialReference", e.target.value)}
            />
            <TextField
              label="Denetim sahası"
              value={form.site}
              onChange={(e) => set("site", e.target.value)}
            />
            <SearchableSelect
              label="Koordinatör *"
              value={form.ownerUserId}
              options={(options.data?.owners ?? []).map((x) => ({
                value: x.id,
                label: x.department ? `${x.name} · ${x.department}` : x.name,
              }))}
              onChange={(v) => {
                const selected = options.data?.owners.find((x) => x.id === v);
                set("ownerUserId", v ?? "");
                set("owner", selected?.name ?? "");
              }}
            />
            <TextField
              type="datetime-local"
              label="Bildirim tarihi"
              value={form.notifiedAtUtc}
              onChange={(e) => set("notifiedAtUtc", e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
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
              type="datetime-local"
              label="Resmi cevap hedefi"
              value={form.responseDueAtUtc}
              onChange={(e) => set("responseDueAtUtc", e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Box>
          <TextField
            multiline
            minRows={2}
            label="Denetim kapsamı"
            value={form.scope}
            onChange={(e) => set("scope", e.target.value)}
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={form.isGovernmentAuthority}
                onChange={(e) => {
                  set("isGovernmentAuthority", e.target.checked);
                }}
              />
            }
            label="Denetleyen taraf devlet kurumu"
          />
          <Stack spacing={1}>
            <SearchableSelect
              label="Bağımsız kapanış yetkilisi *"
              value={form.authorizedCloserUserId}
              options={(options.data?.authorizedClosers ?? [])
                .filter((x) => x.id !== user.id)
                .map((x) => ({
                  value: x.id,
                  label: x.department ? `${x.name} · ${x.department}` : x.name,
                }))}
              onChange={(v) => {
                const selected = options.data?.authorizedClosers.find(
                  (x) => x.id === v,
                );
                set("authorizedCloserUserId", v);
                set("authorizedCloser", selected?.name ?? null);
              }}
            />
            <Alert severity="warning" icon={<AccountBalanceRounded />}>
              Nihai kapanış seçilen bağımsız yetkiliye yönlenecektir.
            </Alert>
          </Stack>
          <Autocomplete
            multiple
            options={documentOptions.data?.items ?? []}
            value={selectedDocuments}
            loading={documentOptions.isLoading}
            getOptionLabel={(document) =>
              `${document.documentCode} · ${document.title} · v${document.currentRevision}`
            }
            isOptionEqualToValue={(option, value) => option.id === value.id}
            onChange={(_, value) => setSelectedDocuments(value)}
            renderInput={(params) => (
              <TextField
                {...params}
                label="M.04 yürürlükteki kontrollü dokümanlardan seç"
                helperText="Aranabilir seçim gerçek M.04 teknik bağlantısını talep paketine ekler."
              />
            )}
          />
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Stack spacing={1.5}>
              <Box
                sx={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  gap: 2,
                }}
              >
                <Box>
                  <Typography variant="subtitle2">
                    Harici talep dokümanları
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    M.04'te bulunmayan dokümanları kontrollü alanlarla ekleyin.
                  </Typography>
                </Box>
                <Button
                  size="small"
                  startIcon={<AddRounded />}
                  onClick={() =>
                    setManualDocuments((items) => [
                      ...items,
                      {
                        documentCode: "",
                        title: "",
                        confidentiality:
                          options.data?.confidentialities[0]?.code ?? "",
                      },
                    ])
                  }
                >
                  Doküman ekle
                </Button>
              </Box>
              {manualDocuments.map((document, index) => (
                <Box className="form-grid two-column" key={index}>
                  <TextField
                    label="Doküman kodu *"
                    value={document.documentCode}
                    onChange={(event) =>
                      setManualDocuments((items) =>
                        items.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, documentCode: event.target.value }
                            : item,
                        ),
                      )
                    }
                  />
                  <TextField
                    label="Başlık *"
                    value={document.title}
                    onChange={(event) =>
                      setManualDocuments((items) =>
                        items.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, title: event.target.value }
                            : item,
                        ),
                      )
                    }
                  />
                  <SearchableSelect
                    label="Gizlilik *"
                    value={document.confidentiality}
                    options={(options.data?.confidentialities ?? []).map(
                      (item) => ({ value: item.code, label: item.name }),
                    )}
                    onChange={(value) =>
                      setManualDocuments((items) =>
                        items.map((item, itemIndex) =>
                          itemIndex === index
                            ? { ...item, confidentiality: value ?? "" }
                            : item,
                        ),
                      )
                    }
                  />
                  <Button
                    color="error"
                    onClick={() =>
                      setManualDocuments((items) =>
                        items.filter((_, itemIndex) => itemIndex !== index),
                      )
                    }
                  >
                    Kaldır
                  </Button>
                </Box>
              ))}
              {manualDocuments.length === 0 &&
                selectedDocuments.length === 0 && (
                  <Alert severity="info">
                    Talep paketine M.04'ten veya harici kaynaktan en az bir
                    doküman ekleyin.
                  </Alert>
                )}
            </Stack>
          </Paper>
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
              documentRequests: packageRequests,
              notifiedAtUtc: new Date(form.notifiedAtUtc).toISOString(),
              plannedStartUtc: new Date(form.plannedStartUtc).toISOString(),
              plannedEndUtc: new Date(form.plannedEndUtc).toISOString(),
              responseDueAtUtc: new Date(form.responseDueAtUtc).toISOString(),
            })
          }
        >
          Dış denetim oluştur
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
    queryKey: ["external-audit", id],
    queryFn: ({ signal }) => getExternalAuditDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const qc = useQueryClient();
  const update = (d: ExternalAuditDetails) => {
    qc.setQueryData(["external-audit", id], d);
    qc.invalidateQueries({ queryKey: ["external-audits"] });
  };
  const r = query.data?.record;
  const findings = query.data?.findings ?? [];
  const openFindings = findings.filter((x) => x.status !== "Closed").length;
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
                    color={r.isGovernmentAuthority ? "warning" : "default"}
                    label={
                      r.isGovernmentAuthority
                        ? "OTORİTE DENETİMİ"
                        : "DIŞ DENETİM"
                    }
                  />
                </Stack>
                <Typography variant="h4">{r.title}</Typography>
              </Box>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                {r.status === "Closed" && (
                  <Button
                    variant="outlined"
                    color="inherit"
                    startIcon={<DownloadRounded />}
                    onClick={() => downloadExternalAuditFinalReport(r.id)}
                  >
                    Nihai PDF
                  </Button>
                )}
                <Chip
                  icon={<GavelRounded />}
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
              icon={<TravelExploreRounded />}
              iconPosition="start"
              label="Genel bakış"
            />
            <Tab
              icon={<FolderZipRounded />}
              iconPosition="start"
              label={`Talep paketi (${query.data!.documentRequests.length})`}
            />
            <Tab
              icon={<ReportProblemRounded />}
              iconPosition="start"
              label={`Bulgular (${findings.length})`}
            />
            <Tab
              icon={<VerifiedRounded />}
              iconPosition="start"
              label="Kapanış"
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
            {tab === 1 && <Package details={query.data!} update={update} />}{" "}
            {tab === 2 && <Findings details={query.data!} update={update} />}{" "}
            {tab === 3 && <Closure details={query.data!} update={update} />}{" "}
            {tab === 4 && (
              <Stack spacing={2}>
                <RecordAssignments
                  aggregateType="ExternalAudit"
                  aggregateId={r.id}
                />
                {query.data!.signatures.length > 0 && (
                  <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}>
                    <Typography variant="h6" sx={{ mb: 1 }}>
                      Elektronik imzalar
                    </Typography>
                    {query.data!.signatures.map((s) => (
                      <Box
                        key={s.id}
                        sx={{
                          py: 1,
                          borderBottom: "1px solid",
                          borderColor: "divider",
                        }}
                      >
                        <Typography sx={{ fontWeight: 700 }}>
                          {s.signer} · v{s.recordVersion}
                        </Typography>
                        <Typography variant="body2">{s.meaning}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {dt(s.signedAtUtc)} · SHA-256{" "}
                          {s.contentHash.slice(0, 16)}…
                        </Typography>
                      </Box>
                    ))}
                  </Paper>
                )}
              </Stack>
            )}{" "}
            {tab === 5 && (
              <AuditTimeline
                events={query.data!.auditTrail}
                labels={eventLabels}
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
  details: { record: r, documentRequests, findings },
}: {
  details: ExternalAuditDetails;
}) {
  const active = Math.max(0, stageStatus.indexOf(r.status));
  const open = findings.filter((x) => x.status !== "Closed").length;
  const exported = documentRequests.filter(
    (x) => x.status === "Exported",
  ).length;
  return (
    <Stack spacing={3}>
      <Paper
        variant="outlined"
        sx={{
          p: 2.5,
          borderRadius: 3,
          background: "linear-gradient(135deg,#f4f8fb,#f8f7ff)",
        }}
      >
        <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 2 }}>
          <Box className="section-heading-icon tone-indigo">
            <GavelRounded />
          </Box>
          <Box>
            <Typography sx={{ fontWeight: 850 }}>
              Resmi dış denetim zinciri
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Kontrollü paylaşım, taahhüt, DÖF ve yetkili kapanış kapıları
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
        <Info
          title="Denetleyen kurum"
          value={`${r.auditorOrganization} · ${r.authorityCountry}`}
        />
        <Info title="Resmi referans" value={r.officialReference} />
        <Info
          title="Kurum rotası"
          value={
            r.isGovernmentAuthority
              ? `Devlet kurumu · ${r.authorizedCloser}`
              : "Müşteri / belgelendirme"
          }
        />
        <Info title="Kapsam" value={r.scope} />
        <Info title="Saha / koordinatör" value={`${r.site} · ${r.owner}`} />
        <Info
          title="Takvim / cevap"
          value={`${dt(r.plannedStartUtc)} — ${dt(r.plannedEndUtc)} · Cevap ${dt(r.responseDueAtUtc)}`}
        />
      </Box>
      <Alert
        severity={exported === documentRequests.length ? "success" : "warning"}
        icon={<LockRounded />}
      >
        Kontrollü talep paketi: {exported}/{documentRequests.length} doküman
        dışa aktarıldı. Her aktarımda alıcı, amaç, kanıt ve sürüm kaydedilir.
      </Alert>
      {findings.length > 0 && (
        <Alert severity={open ? "warning" : "success"}>
          {findings.length} resmi bulgunun {open} tanesi açık. Bağlı DÖF
          kapanmadan bulgu, bulgular kapanmadan kapanış mektubu aşaması
          tamamlanamaz.
        </Alert>
      )}
    </Stack>
  );
}
function Info({ title, value }: { title: string; value: string }) {
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.2, borderRadius: 3, borderLeft: "4px solid #849eb8" }}
    >
      <Typography variant="overline" color="text.secondary">
        {title}
      </Typography>
      <Typography sx={{ fontWeight: 700, mt: 0.4 }}>{value}</Typography>
    </Paper>
  );
}
function Package({
  details,
  update,
}: {
  details: ExternalAuditDetails;
  update: (d: ExternalAuditDetails) => void;
}) {
  return (
    <Stack spacing={2}>
      <Alert severity="info" icon={<FolderZipRounded />}>
        Talep paketi dosyaları doğrudan paylaşılmaz. Alıcı, paylaşım amacı ve
        dışa aktarım kanıtı değiştirilemez erişim kaydına yazılır.
      </Alert>
      {details.documentRequests.map((item) => (
        <DocumentCard
          key={item.id}
          item={item}
          details={details}
          update={update}
        />
      ))}
      {details.packageAccesses.length > 0 && (
        <Box>
          <Typography variant="h6" sx={{ mb: 1.5 }}>
            Erişim ve dışa aktarım günlüğü
          </Typography>
          {details.packageAccesses.map((x) => (
            <Paper
              key={x.id}
              variant="outlined"
              sx={{ p: 2, mb: 1, borderRadius: 3 }}
            >
              <Stack
                direction={{ xs: "column", md: "row" }}
                sx={{ justifyContent: "space-between", gap: 1 }}
              >
                <Box>
                  <Typography sx={{ fontWeight: 800 }}>
                    Sürüm {x.exportVersion} · {x.recipient}
                  </Typography>
                  <Typography variant="body2">{x.purpose}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Kanıt: {x.evidence}
                  </Typography>
                </Box>
                <Typography variant="caption">
                  {x.exportedBy} · {dt(x.accessedAtUtc)}
                </Typography>
              </Stack>
            </Paper>
          ))}
        </Box>
      )}
    </Stack>
  );
}
function DocumentCard({
  item,
  details,
  update,
}: {
  item: ExternalAuditDetails["documentRequests"][number];
  details: ExternalAuditDetails;
  update: (d: ExternalAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [recipient, setRecipient] = useState(
    details.record.auditorOrganization,
  );
  const [purpose, setPurpose] = useState(
    "Resmi denetim talep paketinin kontrollü karşılanması",
  );
  const [evidence, setEvidence] = useState(
    "Dışa aktarım manifesti ve SHA-256 özeti",
  );
  const m = useMutation({
    mutationFn: () =>
      exportExternalAuditDocument(
        details.record.id,
        item.id,
        details.record.version,
        recipient,
        purpose,
        evidence,
      ),
    onSuccess: update,
  });
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.2, borderRadius: 3, borderLeft: "5px solid #7d9cb6" }}
    >
      <Stack
        direction={{ xs: "column", md: "row" }}
        sx={{ justifyContent: "space-between", gap: 2 }}
      >
        <Box>
          <Typography variant="h6">
            {item.documentCode} · {item.title}
          </Typography>
          <Typography variant="caption">
            Gizlilik: {item.confidentiality}{" "}
            {item.controlledDocumentId ? "· M.04 kontrollü doküman" : ""}
          </Typography>
        </Box>
        <Chip
          color={item.status === "Exported" ? "success" : "warning"}
          label={
            item.status === "Exported"
              ? `Dışa aktarıldı · v${item.exportVersion}`
              : "Aktarım bekliyor"
          }
        />
      </Stack>
      {can(Permissions.externalAuditPrepare) &&
        details.record.status === "Preparation" &&
        item.status !== "Exported" && (
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={1.2}
            sx={{ mt: 2 }}
          >
            <TextField
              fullWidth
              label="Yetkili alıcı"
              value={recipient}
              onChange={(e) => setRecipient(e.target.value)}
            />
            <TextField
              fullWidth
              label="Paylaşım amacı"
              value={purpose}
              onChange={(e) => setPurpose(e.target.value)}
            />
            <TextField
              fullWidth
              label="Manifest / erişim kanıtı"
              value={evidence}
              onChange={(e) => setEvidence(e.target.value)}
            />
            <Button
              variant="contained"
              disabled={!recipient || !purpose || !evidence || m.isPending}
              onClick={() => m.mutate()}
            >
              Kontrollü dışa aktar
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
  details: ExternalAuditDetails;
  update: (d: ExternalAuditDetails) => void;
}) {
  const { can } = useAuth();
  return (
    <Stack spacing={2}>
      {can(Permissions.externalAuditPrepare) &&
        ["AuditInProgress", "Findings"].includes(details.record.status) && (
          <NewFinding details={details} update={update} />
        )}{" "}
      {!details.findings.length && (
        <Alert severity="info">Henüz resmi bulgu kaydedilmedi.</Alert>
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
  details: ExternalAuditDetails;
  update: (d: ExternalAuditDetails) => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [reference, setReference] = useState("");
  const [severity, setSeverity] = useState("Minor");
  const [ownerUserId, setOwnerUserId] = useState("");
  const [owner, setOwner] = useState("");
  const options = useQuery({
    queryKey: ["m08-options"],
    queryFn: ({ signal }) => getExternalAuditOptions(signal),
  });
  const [capa, setCapa] = useState(false);
  const [due, setDue] = useState(details.record.responseDueAtUtc.slice(0, 16));
  const m = useMutation({
    mutationFn: () =>
      addExternalAuditFinding(details.record.id, details.record.version, {
        title,
        description,
        officialReference: reference,
        classification: severity,
        capaRequired: capa,
        ownerUserId,
        owner,
        responseDueAtUtc: new Date(due).toISOString(),
      }),
    onSuccess: (d) => {
      update(d);
      setTitle("");
      setDescription("");
    },
  });
  const forced = ["Critical", "Major"].includes(severity);
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.5, borderRadius: 3, background: "#fbfcfd" }}
    >
      <Typography variant="h6" sx={{ mb: 2 }}>
        Yeni resmi denetim bulgusu
      </Typography>
      <Box className="form-grid two-column">
        <TextField
          label="Bulgu başlığı"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <TextField
          label="Otorite / müşteri referansı"
          value={reference}
          onChange={(e) => setReference(e.target.value)}
        />
        <TextField
          multiline
          minRows={2}
          label="Resmi bulgu açıklaması"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <SearchableSelect
          label="Sorumlu *"
          value={ownerUserId}
          options={(options.data?.owners ?? []).map((x) => ({
            value: x.id,
            label: x.department ? `${x.name} · ${x.department}` : x.name,
          }))}
          onChange={(v) => {
            const selected = options.data?.owners.find((x) => x.id === v);
            setOwnerUserId(v ?? "");
            setOwner(selected?.name ?? "");
          }}
        />
        <SearchableSelect
          label="Bulgu sınıfı"
          value={severity}
          options={["Critical", "Major", "Minor", "Observation"].map(
            (value) => ({ value, label: classification[value] }),
          )}
          onChange={(v) => setSeverity(v ?? "Minor")}
        />
        <TextField
          type="datetime-local"
          label="Resmi cevap hedefi"
          value={due}
          onChange={(e) => setDue(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={capa || forced}
              disabled={forced}
              onChange={(e) => setCapa(e.target.checked)}
            />
          }
          label={`M.02 DÖF gerekli${forced ? " (zorunlu)" : ""}`}
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
        disabled={
          !title || !description || !reference || !ownerUserId || m.isPending
        }
        onClick={() => m.mutate()}
      >
        Resmi bulgu ekle
      </Button>
    </Paper>
  );
}
function FindingCard({
  finding: f,
  details,
  update,
}: {
  finding: ExternalAuditDetails["findings"][number];
  details: ExternalAuditDetails;
  update: (d: ExternalAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [response, setResponse] = useState(f.officialResponse ?? "");
  const [commitment, setCommitment] = useState(f.commitment ?? "");
  const [commitmentDue, setCommitmentDue] = useState(() =>
    new Date(Date.now() + 20 * 86400000).toISOString().slice(0, 16),
  );
  const [verification, setVerification] = useState(f.verificationNote ?? "");
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureAccepted, setSignatureAccepted] = useState(false);
  const respond = useMutation({
    mutationFn: () =>
      respondExternalAuditFinding(
        details.record.id,
        f.id,
        details.record.version,
        response,
        commitment,
        new Date(commitmentDue).toISOString(),
      ),
    onSuccess: update,
  });
  const close = useMutation({
    mutationFn: () =>
      closeExternalAuditFinding(
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
              label={classification[f.classification]}
            />
            <Chip size="small" variant="outlined" label={f.status} />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.7 }}>
            {f.description}
          </Typography>
          <Typography variant="caption">
            Referans: {f.officialReference} · Sorumlu: {f.owner} · Cevap:{" "}
            {dt(f.responseDueAtUtc)}
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
      {can(Permissions.externalAuditRespond) &&
        details.record.status === "ResponsePlan" &&
        f.status === "Open" && (
          <Stack spacing={1.2} sx={{ mt: 2 }}>
            <TextField
              multiline
              minRows={2}
              label="Resmi cevap"
              value={response}
              onChange={(e) => setResponse(e.target.value)}
            />
            <TextField
              multiline
              minRows={2}
              label="Kurumsal taahhüt / aksiyon"
              value={commitment}
              onChange={(e) => setCommitment(e.target.value)}
            />
            <TextField
              type="datetime-local"
              label="Taahhüt hedef tarihi"
              value={commitmentDue}
              onChange={(e) => setCommitmentDue(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <Button
              variant="contained"
              disabled={!response || !commitment || respond.isPending}
              onClick={() => respond.mutate()}
            >
              Cevap ve taahhüdü kaydet
            </Button>
          </Stack>
        )}
      {can(Permissions.externalAuditApprove) &&
        details.record.status === "CapaAction" &&
        f.status !== "Closed" && (
          <Stack spacing={1.2} sx={{ mt: 2 }}>
            {f.capaRequired && (
              <Alert severity="warning">
                Bu resmi bulgu, bağlı {f.linkedCapaNumber ?? "DÖF"} kapanmadan
                kapatılamaz.
              </Alert>
            )}
            <TextField
              label="Doğrulama / kapanış kanıtı"
              value={verification}
              onChange={(e) => setVerification(e.target.value)}
            />
            <TextField
              type="password"
              label="E-imza parolası *"
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
              label="Bulgu doğrulama ve kapanışını elektronik olarak imzalıyorum"
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
function Closure({
  details,
  update,
}: {
  details: ExternalAuditDetails;
  update: (d: ExternalAuditDetails) => void;
}) {
  const { can } = useAuth();
  const r = details.record;
  const [reference, setReference] = useState(r.closureLetterReference ?? "");
  const [evidence, setEvidence] = useState(r.closureEvidence ?? "");
  const [accepted, setAccepted] = useState(r.authorityAccepted ?? false);
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureAccepted, setSignatureAccepted] = useState(false);
  const [received, setReceived] = useState(() =>
    new Date().toISOString().slice(0, 16),
  );
  const m = useMutation({
    mutationFn: () =>
      recordExternalAuditClosureLetter(
        r.id,
        r.version,
        reference,
        new Date(received).toISOString(),
        evidence,
        accepted,
        signaturePassword,
        signatureAccepted,
      ),
    onSuccess: update,
  });
  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
          <Box className="section-heading-icon tone-teal">
            <VerifiedRounded />
          </Box>
          <Box>
            <Typography variant="h6">
              Kapanış mektubu ve kabul kanıtı
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {r.isGovernmentAuthority
                ? "Otorite kabulü sonrası Mesul Müdür kapanışı gerekir."
                : "Müşteri / belgelendirme kuruluşu kabulü kaydedilir."}
            </Typography>
          </Box>
        </Stack>
        {r.closureLetterReference && (
          <Alert
            severity={r.authorityAccepted ? "success" : "warning"}
            sx={{ mt: 2 }}
          >
            Referans {r.closureLetterReference} ·{" "}
            {r.authorityAccepted ? "Kabul edildi" : "Kabul bekliyor"} ·{" "}
            {dt(r.closureLetterReceivedAtUtc)}
          </Alert>
        )}
        {can(Permissions.externalAuditApprove) &&
          r.status === "ClosureLetter" && (
            <Stack spacing={1.5} sx={{ mt: 2 }}>
              <TextField
                label="Kapanış mektubu / kabul referansı"
                value={reference}
                onChange={(e) => setReference(e.target.value)}
              />
              <TextField
                type="datetime-local"
                label="Alınma tarihi"
                value={received}
                onChange={(e) => setReceived(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <TextField
                multiline
                minRows={3}
                label="Kabul ve dosya kanıtı"
                value={evidence}
                onChange={(e) => setEvidence(e.target.value)}
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={accepted}
                    onChange={(e) => setAccepted(e.target.checked)}
                  />
                }
                label="Otorite / müşteri kapanışı kabul etti"
              />
              <TextField
                type="password"
                label="E-imza parolası *"
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
                label="Kapanış mektubu ve kabul kanıtını elektronik olarak imzalıyorum"
              />
              <Button
                variant="contained"
                disabled={
                  !reference ||
                  !evidence ||
                  !accepted ||
                  !signaturePassword ||
                  !signatureAccepted ||
                  m.isPending
                }
                onClick={() => m.mutate()}
              >
                Kapanış kanıtını kaydet
              </Button>
            </Stack>
          )}
        {m.isError && (
          <Alert severity="error" sx={{ mt: 1 }}>
            {m.error.message}
          </Alert>
        )}
      </Paper>
      <RecordAssignments aggregateType="ExternalAudit" aggregateId={r.id} />
    </Stack>
  );
}
function AuditActions({
  details,
  update,
}: {
  details: ExternalAuditDetails;
  update: (d: ExternalAuditDetails) => void;
}) {
  const { can } = useAuth();
  const [note, setNote] = useState("");
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureAccepted, setSignatureAccepted] = useState(false);
  const [error, setError] = useState("");
  const m = useMutation({
    mutationFn: (code: string) =>
      transitionExternalAudit(
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
        <Chip color="success" label="Dış denetim yaşam döngüsü tamamlandı" />
      </DialogActions>
    );
  const allowed =
    details.record.status === "Notified"
      ? can(Permissions.externalAuditCreate)
      : ["Preparation", "AuditInProgress", "Findings"].includes(
            details.record.status,
          )
        ? can(Permissions.externalAuditPrepare)
        : details.record.status === "ResponsePlan"
          ? can(Permissions.externalAuditRespond)
          : can(Permissions.externalAuditApprove);
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
      code === "start-audit" &&
      details.documentRequests.some(
        (document) => document.status !== "Exported",
      )
    )
      return "Denetim başlamadan önce talep paketindeki tüm dokümanlar kontrollü dışa aktarılmalıdır.";
    if (
      code === "start-capa-actions" &&
      details.findings.some((finding) => finding.status === "Open")
    )
      return "DÖF/aksiyon aşamasına geçmeden önce tüm resmi bulgular cevaplanmalıdır.";
    if (
      code === "complete-capa-actions" &&
      details.findings.some((finding) => finding.status !== "Closed")
    )
      return "Kapanış mektubu aşamasına geçmeden önce tüm bulgular ve bağlı M.02 DÖF kayıtları kapatılmalıdır.";
    if (
      code === "submit-authority-closure" &&
      (!details.record.closureLetterReference ||
        details.record.authorityAccepted !== true)
    )
      return "Yetkili kapanışına göndermek için kabul edilmiş kapanış mektubu ve kanıtı kaydedilmelidir.";
    return null;
  };
  const blockingMessage = details.availableTransitions
    .map((transition) => blocker(transition.code))
    .find(Boolean);
  return (
    <DialogActions sx={{ flexWrap: "wrap" }}>
      {error && (
        <Alert severity="error" sx={{ mr: "auto" }}>
          {error}
        </Alert>
      )}
      {blockingMessage && (
        <Alert severity="warning" sx={{ mr: "auto" }}>
          {blockingMessage}
        </Alert>
      )}
      {details.availableTransitions.some((x) => x.noteRequired) && (
        <TextField
          size="small"
          label="Yetkili kapanış gerekçesi"
          value={note}
          onChange={(e) => setNote(e.target.value)}
          sx={{ minWidth: 320 }}
        />
      )}
      {details.availableTransitions.some((x) => x.code === "close") && (
        <Stack sx={{ minWidth: 320 }}>
          <TextField
            size="small"
            type="password"
            label="E-imza parolası *"
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
            label="Nihai kapanışı elektronik olarak imzalıyorum"
          />
        </Stack>
      )}
      {details.availableTransitions.map((t) => (
        <Button
          key={t.code}
          variant="contained"
          disabled={
            m.isPending ||
            Boolean(blocker(t.code)) ||
            (t.noteRequired && !note) ||
            (t.code === "close" && (!signaturePassword || !signatureAccepted))
          }
          onClick={() => m.mutate(t.code)}
        >
          {t.label}
        </Button>
      ))}
    </DialogActions>
  );
}
