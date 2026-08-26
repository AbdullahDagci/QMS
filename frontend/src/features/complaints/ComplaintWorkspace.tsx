import { useEffect, useMemo, useState, type ReactElement } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AddRounded,
  ChatBubbleOutlineRounded,
  HistoryRounded,
  HubRounded,
  MedicalServicesRounded,
  ManageSearchRounded,
  MarkEmailReadRounded,
  ScienceRounded,
  SettingsRounded,
  WarningAmberRounded,
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
  addComplaintResponse,
  approveComplaintResponse,
  completeComplaintImpact,
  completeComplaintInvestigation,
  createComplaint,
  createComplaintLookupDefinition,
  decideComplaintCapa,
  downloadComplaintFinalReport,
  getComplaintDetails,
  getComplaintOptions,
  getComplaintLookupDefinitions,
  searchComplaints,
  transitionComplaint,
  updateComplaintLookupDefinition,
  type ComplaintDetails,
  type ComplaintListItem,
  type ComplaintLookupDefinition,
  type CreateComplaintInput,
} from "../../api/complaints";
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
import { ComplaintFilters } from "./ComplaintFilters";
import {
  emptyComplaintFilters,
  toComplaintColumnFilters,
  type ComplaintFilterState,
} from "./complaintFilterModel";

const statusLabels: Record<string, string> = {
  Received: "Alındı",
  Triage: "Triyaj",
  PreliminaryResponse: "Ön yanıt",
  Investigation: "Paralel araştırma",
  ImpactAssessment: "Etki / kök neden",
  CapaDecision: "DÖF kararı",
  FinalResponseApproval: "Nihai yanıt onayı",
  Closed: "Kapalı",
  Cancelled: "İptal",
};
const severityLabels: Record<string, string> = {
  Minor: "Minör",
  Major: "Majör",
  Critical: "Kritik",
};
const stages = [
  "Alındı",
  "Triyaj",
  "Ön yanıt",
  "Araştırma",
  "Etki / kök neden",
  "DÖF kararı",
  "Nihai yanıt",
  "Kapalı",
];
const stageStatus = [
  "Received",
  "Triage",
  "PreliminaryResponse",
  "Investigation",
  "ImpactAssessment",
  "CapaDecision",
  "FinalResponseApproval",
  "Closed",
];
const dt = (v: string | null) =>
  v
    ? new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(v))
    : "—";

export function ComplaintWorkspace() {
  const [params, setParams] = useSearchParams();
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState<ComplaintFilterState>(
    emptyComplaintFilters,
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
    () => toComplaintColumnFilters(filters),
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
    queryKey: ["complaints", request],
    queryFn: ({ signal }) => searchComplaints(request, signal),
    placeholderData: (p) => p,
    retry: false,
  });
  const optionsQuery = useQuery({
    queryKey: ["complaint-options"],
    queryFn: ({ signal }) => getComplaintOptions(signal),
    retry: false,
  });
  const lookupNames = useMemo(
    () => ({
      complaintTypes: new Map(
        (optionsQuery.data?.complaintTypes ?? []).map((x) => [x.code, x.name]),
      ),
      countries: new Map(
        (optionsQuery.data?.countries ?? []).map((x) => [x.code, x.name]),
      ),
    }),
    [optionsQuery.data],
  );
  const sort = (field: string) => {
    if (field === sortBy) setDirection((x) => (x === "asc" ? "desc" : "asc"));
    else {
      setSortBy(field);
      setDirection("asc");
    }
    setPage(0);
  };
  const items = query.data?.items ?? [];
  const critical = items.filter((x) => x.severity === "Critical").length;
  const pv = items.filter((x) => x.suspectedAdverseEvent).length;
  const trend = items.filter((x) => x.trendFlagged).length;
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
            <Box className="section-heading-icon tone-rose">
              <ChatBubbleOutlineRounded />
            </Box>
            <Typography variant="h2">Müşteri şikâyetleri</Typography>
            <Chip size="small" color="success" label="M.06 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Şikâyeti alımdan nihai müşteri yanıtına; ürün kalitesi,
            farmakovijilans ve DÖF bağlantılarıyla izleyin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2}>
          <ModuleInfoButton module="M.06" onClick={() => setGuideOpen(true)} />
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setLookupOpen(true)}
            >
              Şikâyet tanımları
            </Button>
          )}
          {can(Permissions.complaintCreate) && (
            <Button
              variant="contained"
              size="large"
              startIcon={<AddRounded />}
              onClick={() => setCreateOpen(true)}
            >
              Yeni şikâyet
            </Button>
          )}
        </Stack>
      </Stack>
      <Box className="complaint-kpi-grid">
        <Kpi
          icon={<ChatBubbleOutlineRounded />}
          label="Görünen kayıt"
          value={query.data?.totalCount ?? 0}
          tone="teal"
        />
        <Kpi
          icon={<WarningAmberRounded />}
          label="Kritik"
          value={critical}
          tone="rose"
        />
        <Kpi
          icon={<MedicalServicesRounded />}
          label="FV yönlendirmesi"
          value={pv}
          tone="amber"
        />
        <Kpi
          icon={<HubRounded />}
          label="Trend sinyali"
          value={trend}
          tone="indigo"
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
            ? `${query.data.totalCount} şikâyet kaydı`
            : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>
      {filtersOpen && (
        <ComplaintFilters
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
          <Table aria-label="Müşteri şikâyeti iş listesi">
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
                  field="customerName"
                  text="Müşteri / ülke"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <Sort
                  field="product"
                  text="Ürün / batch"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <Sort
                  field="severity"
                  text="Risk sinyali"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <Sort
                  field="status"
                  text="Aşama"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <Sort
                  field="finalResponseDueAtUtc"
                  text="Yanıt hedefi"
                  current={sortBy}
                  direction={direction}
                  onSort={sort}
                />
                <TableCell align="right">İşlem</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {query.isLoading && <TableLoadingRows columns={7} />}{" "}
              {items.map((item) => (
                <ComplaintRow
                  key={item.id}
                  item={item}
                  complaintTypeName={
                    lookupNames.complaintTypes.get(item.complaintType) ??
                    item.complaintType
                  }
                  countryName={
                    lookupNames.countries.get(item.country) ?? item.country
                  }
                  open={() => setParams({ open: item.id })}
                />
              ))}
              {query.isSuccess && !items.length && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Box className="empty-state">
                      <ChatBubbleOutlineRounded />
                      <Typography sx={{ fontWeight: 800 }}>
                        Henüz şikâyet kaydı yok
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        İlk müşteri bildirimini kontrollü iş akışına alın.
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
      <CreateComplaintDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => {
          setCreateOpen(false);
          setParams({ open: id });
        }}
      />
      <ComplaintDetailsDialog
        id={params.get("open")}
        onClose={() => setParams({})}
      />
      <ModuleGuideDialog
        module="M.06"
        open={guideOpen}
        onClose={() => setGuideOpen(false)}
      />
      <ComplaintLookupDialog
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
  tone,
}: {
  icon: ReactElement;
  label: string;
  value: number | bigint;
  tone: string;
}) {
  return (
    <Paper variant="outlined" className={`complaint-kpi tone-${tone}`}>
      <Box className="complaint-kpi-icon">{icon}</Box>
      <Box>
        <Typography variant="overline">{label}</Typography>
        <Typography variant="h4">{String(value)}</Typography>
      </Box>
    </Paper>
  );
}

const complaintLookupCategories = [
  { value: "Channel", label: "Bildirim kanalı" },
  { value: "Country", label: "Ülke" },
  { value: "ComplaintType", label: "Şikâyet türü" },
];
function ComplaintLookupDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const [category, setCategory] = useState("Channel");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [sortOrder, setSortOrder] = useState(100);
  const query = useQuery({
    queryKey: ["complaint-lookup-definitions"],
    queryFn: getComplaintLookupDefinitions,
    enabled: open,
    retry: false,
  });
  const refresh = async () => {
    await client.invalidateQueries({
      queryKey: ["complaint-lookup-definitions"],
    });
    await client.invalidateQueries({ queryKey: ["complaint-options"] });
  };
  const create = useMutation({
    mutationFn: () =>
      createComplaintLookupDefinition({ category, code, name, sortOrder }),
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
    }) => updateComplaintLookupDefinition(x.id, x),
    onSuccess: refresh,
  });
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.06 · YÖNETİLEN LOOKUP</Typography>
          <Typography variant="h5">Şikâyet tanımları</Typography>
          <Typography color="text.secondary">
            Değişmez kodlar geçmiş kayıtlarda snapshot olarak korunur; görünen
            ad ve aktiflik yalnız yeni seçimleri yönetir.
          </Typography>
        </Box>
      </ModalHeader>
      <DialogContent dividers>
        {(query.isError || create.isError || update.isError) && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {query.error?.message ??
              create.error?.message ??
              update.error?.message}
          </Alert>
        )}
        <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
            <Box sx={{ minWidth: 180 }}>
              <SearchableSelect
                label="Kategori"
                value={category}
                options={complaintLookupCategories}
                onChange={(v) => setCategory(v ?? "Channel")}
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
          {query.data?.map((item) => (
            <ComplaintLookupRow
              key={item.id}
              item={item}
              saving={update.isPending}
              onSave={(nextName, nextSort, active) =>
                update.mutate({
                  id: item.id,
                  name: nextName,
                  sortOrder: nextSort,
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
function ComplaintLookupRow({
  item,
  saving,
  onSave,
}: {
  item: ComplaintLookupDefinition;
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
            complaintLookupCategories.find((x) => x.value === item.category)
              ?.label ?? item.category
          }
        />
        <Typography sx={{ minWidth: 145, fontFamily: "monospace" }}>
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
function ComplaintRow({
  item,
  complaintTypeName,
  countryName,
  open,
}: {
  item: ComplaintListItem;
  complaintTypeName: string;
  countryName: string;
  open: () => void;
}) {
  const overdue =
    new Date(item.finalResponseDueAtUtc) < new Date() &&
    item.status !== "Closed";
  return (
    <TableRow hover>
      <TableCell>
        <Typography sx={{ fontWeight: 800, color: "primary.main" }}>
          {item.recordNumber}
        </Typography>
        <Typography variant="caption">{complaintTypeName}</Typography>
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 730 }}>{item.customerName}</Typography>
        <Typography variant="caption" color="text.secondary">
          {countryName}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 730 }}>{item.product}</Typography>
        <Typography variant="caption" color="text.secondary">
          {item.batchNumber ?? "Batch belirtilmedi"}
        </Typography>
      </TableCell>
      <TableCell>
        <Stack direction="row" spacing={0.7} sx={{ flexWrap: "wrap" }}>
          <Chip
            className={`semantic-risk-badge risk-${item.severity.toLowerCase()}`}
            size="small"
            color={item.severity === "Minor" ? "success" : "error"}
            label={severityLabels[item.severity]}
          />
          {item.suspectedAdverseEvent && (
            <Chip size="small" color="info" label="FV" />
          )}
          {item.trendFlagged && (
            <Chip
              size="small"
              color="secondary"
              label={`Trend · ${item.similarComplaintCount}`}
            />
          )}
        </Stack>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          variant="outlined"
          label={statusLabels[item.status] ?? item.status}
        />
      </TableCell>
      <TableCell>
        <Typography
          color={overdue ? "error.main" : "text.primary"}
          sx={{ fontWeight: overdue ? 750 : 500 }}
        >
          {dt(item.finalResponseDueAtUtc)}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          Ön: {dt(item.preliminaryResponseDueAtUtc)}
        </Typography>
      </TableCell>
      <TableCell align="right">
        <RecordActionMenu onOpen={open} />
      </TableCell>
    </TableRow>
  );
}

function CreateComplaintDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (id: string) => void;
}) {
  const client = useQueryClient();
  const now = new Date();
  const local = (d: Date) =>
    new Date(d.getTime() - d.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);
  const [form, setForm] = useState<CreateComplaintInput>({
    channel: "",
    customerName: "",
    country: "",
    product: "",
    batchNumber: "",
    eventAtUtc: local(new Date(now.getTime() - 86400000)),
    receivedAtUtc: local(now),
    complaintType: "",
    description: "",
    severity: "Minor",
    hasHealthImpact: false,
    suspectedAdverseEvent: false,
    sampleExpected: false,
    returnExpected: false,
    attachmentSummary: "",
    ownerUserId: "",
    preliminaryResponseDueAtUtc: local(new Date(now.getTime() + 3 * 86400000)),
    finalResponseDueAtUtc: local(new Date(now.getTime() + 30 * 86400000)),
    investigationDepartmentIds: [],
  });
  const optionsQuery = useQuery({
    queryKey: ["complaint-options"],
    queryFn: ({ signal }) => getComplaintOptions(signal),
    enabled: open,
  });
  const options = optionsQuery.data;
  useEffect(() => {
    if (!options) return;
    setForm((current) => ({
      ...current,
      channel: current.channel || options.channels[0]?.code || "",
      country: current.country || options.countries[0]?.code || "",
      complaintType:
        current.complaintType || options.complaintTypes[0]?.code || "",
      ownerUserId: current.ownerUserId || options.owners[0]?.id || "",
      investigationDepartmentIds: current.investigationDepartmentIds.length
        ? current.investigationDepartmentIds
        : options.departments.slice(0, 1).map((x) => x.id),
    }));
  }, [options]);
  const mutation = useMutation({
    mutationFn: createComplaint,
    onSuccess: (d) => {
      client.invalidateQueries({ queryKey: ["complaints"] });
      onCreated(d.record.id);
    },
  });
  const set = <K extends keyof CreateComplaintInput>(
    k: K,
    v: CreateComplaintInput[K],
  ) => setForm((x) => ({ ...x, [k]: v }));
  const valid =
    form.customerName.trim() &&
    form.product.trim() &&
    form.description.trim() &&
    form.ownerUserId &&
    form.investigationDepartmentIds.length > 0;
  const submit = () =>
    mutation.mutate({
      ...form,
      batchNumber: form.batchNumber || null,
      eventAtUtc: new Date(form.eventAtUtc).toISOString(),
      receivedAtUtc: new Date(form.receivedAtUtc).toISOString(),
      preliminaryResponseDueAtUtc: new Date(
        form.preliminaryResponseDueAtUtc,
      ).toISOString(),
      finalResponseDueAtUtc: new Date(form.finalResponseDueAtUtc).toISOString(),
    });
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Box>
          <Typography variant="overline">M.06 · KONTROLLÜ KABUL</Typography>
          <Typography variant="h5">Yeni müşteri şikâyeti</Typography>
        </Box>
      </ModalHeader>
      <DialogContent className="modal-scroll-content">
        {mutation.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {mutation.error.message}
          </Alert>
        )}
        <Box className="form-grid two-columns">
          <SearchableSelect
            label="Kanal"
            required
            value={form.channel}
            options={(options?.channels ?? []).map((x) => ({
              value: x.code,
              label: x.name,
            }))}
            onChange={(v) => set("channel", v ?? "")}
          />
          <TextField
            label="Müşteri"
            required
            value={form.customerName}
            onChange={(e) => set("customerName", e.target.value)}
          />
          <SearchableSelect
            label="Ülke"
            required
            value={form.country}
            options={(options?.countries ?? []).map((x) => ({
              value: x.code,
              label: x.name,
            }))}
            onChange={(v) => set("country", v ?? "")}
          />
          <TextField
            label="Ürün"
            required
            value={form.product}
            onChange={(e) => set("product", e.target.value)}
          />
          <TextField
            label="Batch / seri"
            value={form.batchNumber ?? ""}
            onChange={(e) => set("batchNumber", e.target.value)}
          />
          <SearchableSelect
            label="Şikâyet türü"
            required
            value={form.complaintType}
            options={(options?.complaintTypes ?? []).map((x) => ({
              value: x.code,
              label: x.name,
            }))}
            onChange={(v) => set("complaintType", v ?? "")}
          />
          <SearchableSelect
            label="Önem derecesi"
            required
            value={form.severity}
            options={[
              { value: "Minor", label: "Minör" },
              { value: "Major", label: "Majör" },
              { value: "Critical", label: "Kritik" },
            ]}
            onChange={(v) => set("severity", v ?? "Minor")}
          />
          <SearchableSelect
            label="Sorumlu"
            required
            value={form.ownerUserId}
            options={(options?.owners ?? []).map((x) => ({
              value: x.id,
              label: `${x.name}${x.department ? ` · ${x.department}` : ""}`,
            }))}
            onChange={(v) => set("ownerUserId", v ?? "")}
          />
          <TextField
            type="datetime-local"
            label="Olay tarihi"
            required
            value={form.eventAtUtc}
            onChange={(e) => set("eventAtUtc", e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            type="datetime-local"
            label="Alınma tarihi"
            required
            value={form.receivedAtUtc}
            onChange={(e) => set("receivedAtUtc", e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            type="datetime-local"
            label="Ön yanıt hedefi"
            required
            value={form.preliminaryResponseDueAtUtc}
            onChange={(e) => set("preliminaryResponseDueAtUtc", e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            type="datetime-local"
            label="Nihai yanıt hedefi"
            required
            value={form.finalResponseDueAtUtc}
            onChange={(e) => set("finalResponseDueAtUtc", e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            multiline
            minRows={3}
            label="Şikâyet açıklaması"
            required
            value={form.description}
            onChange={(e) => set("description", e.target.value)}
            sx={{ gridColumn: "1/-1" }}
          />
          <Autocomplete
            multiple
            options={options?.departments ?? []}
            getOptionLabel={(x) => `${x.name} · ${x.investigator}`}
            value={(options?.departments ?? []).filter((x) =>
              form.investigationDepartmentIds.includes(x.id),
            )}
            onChange={(_, v) =>
              set(
                "investigationDepartmentIds",
                v.map((x) => x.id),
              )
            }
            renderInput={(p) => (
              <TextField {...p} label="Paralel araştırma bölümleri" required />
            )}
            sx={{ gridColumn: "1/-1" }}
          />
          <TextField
            label="Ek / numune özeti"
            value={form.attachmentSummary}
            onChange={(e) => set("attachmentSummary", e.target.value)}
            sx={{ gridColumn: "1/-1" }}
          />
          <Box sx={{ gridColumn: "1/-1" }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.hasHealthImpact}
                  onChange={(e) => set("hasHealthImpact", e.target.checked)}
                />
              }
              label="Sağlık etkisi bildirildi"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.suspectedAdverseEvent}
                  onChange={(e) =>
                    set("suspectedAdverseEvent", e.target.checked)
                  }
                />
              }
              label="Advers olay şüphesi"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.sampleExpected}
                  onChange={(e) => set("sampleExpected", e.target.checked)}
                />
              }
              label="Numune bekleniyor"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.returnExpected}
                  onChange={(e) => set("returnExpected", e.target.checked)}
                />
              }
              label="İade bekleniyor"
            />
          </Box>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={!valid || mutation.isPending}
          onClick={submit}
        >
          Şikâyeti oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function ComplaintDetailsDialog({
  id,
  onClose,
}: {
  id: string | null;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const { can } = useAuth();
  const [tab, setTab] = useState(0);
  const query = useQuery({
    queryKey: ["complaint", id],
    queryFn: ({ signal }) => getComplaintDetails(id!, signal),
    enabled: !!id,
    retry: false,
  });
  const update = (data: ComplaintDetails) => {
    client.setQueryData(["complaint", id], data);
    client.invalidateQueries({ queryKey: ["complaints"] });
  };
  return (
    <Dialog
      open={!!id}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      slotProps={{ paper: { className: "record-details-paper" } }}
    >
      <ModalHeader onClose={onClose}>
        {query.data ? (
          <Stack
            direction={{ xs: "column", sm: "row" }}
            className="record-detail-header"
          >
            <Box>
              <Typography variant="overline">
                {query.data.record.recordNumber} · MÜŞTERİ ŞİKÂYETİ
              </Typography>
              <Typography variant="h5">
                {query.data.record.product} · {query.data.record.customerName}
              </Typography>
            </Box>
            <Stack direction="row" spacing={1}>
              {query.data.record.status === "Closed" && (
                <Button
                  variant="outlined"
                  onClick={() =>
                    void downloadComplaintFinalReport(
                      query.data!.record.id,
                      query.data!.record.recordNumber,
                    )
                  }
                >
                  Nihai PDF
                </Button>
              )}
              <Chip
                className={`semantic-risk-badge risk-${query.data.record.severity.toLowerCase()}`}
                color={query.data.record.severity === "Minor" ? "success" : "error"}
                label={severityLabels[query.data.record.severity]}
              />
              <Chip
                variant="outlined"
                label={statusLabels[query.data.record.status]}
              />
            </Stack>
          </Stack>
        ) : (
          <Typography variant="h5">Şikâyet yükleniyor</Typography>
        )}
      </ModalHeader>
      {query.isLoading && (
        <Box sx={{ p: 4 }}>
          <TableLoadingRows columns={1} />
        </Box>
      )}
      {query.isError && (
        <Alert severity="error" sx={{ m: 3 }}>
          {query.error.message}
        </Alert>
      )}
      {query.data && (
        <>
          <Box className="record-detail-tabs-shell">
            <Tabs
              value={tab}
              onChange={(_, v) => setTab(v)}
              variant="scrollable"
            >
              <Tab
                icon={<ChatBubbleOutlineRounded />}
                iconPosition="start"
                label="Genel bakış"
              />
              <Tab
                icon={<ManageSearchRounded />}
                iconPosition="start"
                label={`Araştırmalar (${query.data.investigations.length})`}
              />
              <Tab
                icon={<MarkEmailReadRounded />}
                iconPosition="start"
                label={`Müşteri yanıtları (${query.data.responses.length})`}
              />
              <Tab
                icon={<HubRounded />}
                iconPosition="start"
                label="Bağlantılar ve görevler"
              />
              <Tab
                icon={<HistoryRounded />}
                iconPosition="start"
                label={`Geçmiş (${query.data.auditTrail.length})`}
              />
            </Tabs>
          </Box>
          <DialogContent className="record-details-content">
            <Box className="detail-tab-panel">
              {tab === 0 && <Overview details={query.data} />}{" "}
              {tab === 1 && (
                <Investigations
                  details={query.data}
                  update={update}
                  canAct={can(Permissions.complaintInvestigate)}
                />
              )}{" "}
              {tab === 2 && (
                <Responses
                  details={query.data}
                  update={update}
                  canWrite={can(Permissions.complaintManage)}
                  canApprove={can(Permissions.complaintApprove)}
                />
              )}{" "}
              {tab === 3 && <Links details={query.data} />}{" "}
              {tab === 4 && (
                <AuditTimeline
                  events={query.data.auditTrail}
                  labels={auditLabels}
                />
              )}
            </Box>
          </DialogContent>
          <ComplaintActions
            details={query.data}
            update={update}
            canManage={
              can(Permissions.complaintManage) ||
              can(Permissions.complaintApprove)
            }
          />
        </>
      )}
    </Dialog>
  );
}

function Overview({ details }: { details: ComplaintDetails }) {
  const r = details.record;
  const active = Math.max(0, stageStatus.indexOf(r.status));
  return (
    <Stack spacing={2.4}>
      <Paper variant="outlined" className="complaint-flow-card">
        <Stack
          direction="row"
          sx={{ justifyContent: "space-between", alignItems: "center" }}
        >
          <Box>
            <Typography variant="h6">
              Kontrollü şikâyet yaşam döngüsü
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Ürün kalitesi ve hasta güvenliği aynı kaynaktan, ayrıştırılmış
              sorumluluklarla ilerler.
            </Typography>
          </Box>
          {r.trendFlagged && (
            <Chip
              color="secondary"
              label={`Trend sinyali · ${r.similarComplaintCount} benzer kayıt`}
            />
          )}
        </Stack>
        <Box className="complaint-stepper-scroll">
          <Stepper
            activeStep={active}
            alternativeLabel
            className="complaint-stepper"
          >
            {stages.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>
        </Box>
      </Paper>
      <Box className="complaint-detail-grid">
        <Info
          icon={<ChatBubbleOutlineRounded />}
          label="Bildirim"
          value={`${r.channel} · ${r.customerName} · ${r.country}`}
        />
        <Info
          icon={<ScienceRounded />}
          label="Ürün / batch"
          value={`${r.product} · ${r.batchNumber ?? "Batch yok"}`}
        />
        <Info
          icon={<WarningAmberRounded />}
          label="Olay ve risk"
          value={`${severityLabels[r.severity]} · ${r.complaintType} · ${dt(r.eventAtUtc)}`}
        />
        <Info
          icon={<MarkEmailReadRounded />}
          label="Yanıt SLA"
          value={`Ön: ${dt(r.preliminaryResponseDueAtUtc)} · Nihai: ${dt(r.finalResponseDueAtUtc)}`}
        />
      </Box>
      <Paper variant="outlined" className="complaint-narrative">
        <Typography variant="overline">ŞİKÂYET TANIMI</Typography>
        <Typography>{r.description}</Typography>
      </Paper>
      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        {r.suspectedAdverseEvent && (
          <Alert severity="info" sx={{ flex: 1 }}>
            Advers olay şüphesi işaretli. Triyaj tamamlandığında M.15
            yönlendirmesi, müşteri ve sağlık anlatısı aktarılmadan oluşturulur.
          </Alert>
        )}
        {r.sampleExpected && (
          <Alert severity="warning" sx={{ flex: 1 }}>
            Numune bekleniyor.{" "}
            {r.attachmentSummary || "Numune / ek açıklaması girilmedi."}
          </Alert>
        )}
      </Stack>
    </Stack>
  );
}
function Info({
  icon,
  label,
  value,
}: {
  icon: ReactElement;
  label: string;
  value: string;
}) {
  return (
    <Paper variant="outlined" className="complaint-info-card">
      <Box className="complaint-info-icon">{icon}</Box>
      <Box>
        <Typography variant="overline">{label}</Typography>
        <Typography sx={{ fontWeight: 720 }}>{value}</Typography>
      </Box>
    </Paper>
  );
}

function Investigations({
  details,
  update,
  canAct,
}: {
  details: ComplaintDetails;
  update: (d: ComplaintDetails) => void;
  canAct: boolean;
}) {
  return (
    <Stack spacing={2}>
      {details.investigations.map((x) => (
        <InvestigationCard
          key={x.id}
          item={x}
          details={details}
          update={update}
          canAct={canAct}
        />
      ))}
    </Stack>
  );
}
function InvestigationCard({
  item,
  details,
  update,
  canAct,
}: {
  item: ComplaintDetails["investigations"][number];
  details: ComplaintDetails;
  update: (d: ComplaintDetails) => void;
  canAct: boolean;
}) {
  const [findings, setFindings] = useState("");
  const [root, setRoot] = useState("");
  const mutation = useMutation({
    mutationFn: () =>
      completeComplaintInvestigation(
        details.record.id,
        item.id,
        details.record.version,
        findings,
        root,
      ),
    onSuccess: update,
  });
  return (
    <Paper variant="outlined" className="complaint-investigation-card">
      <Stack
        direction={{ xs: "column", sm: "row" }}
        sx={{ justifyContent: "space-between", gap: 1 }}
      >
        <Box>
          <Typography variant="overline">PARALEL BÖLÜM ARAŞTIRMASI</Typography>
          <Typography variant="h6">{item.department}</Typography>
        </Box>
        <Chip
          color={item.status === "Completed" ? "success" : "warning"}
          label={item.status === "Completed" ? "Tamamlandı" : "Bekliyor"}
        />
      </Stack>
      {item.status === "Completed" ? (
        <Box sx={{ mt: 2 }}>
          <Typography sx={{ fontWeight: 750 }}>Bulgular</Typography>
          <Typography>{item.findings}</Typography>
          <Typography sx={{ fontWeight: 750, mt: 1.5 }}>
            Kök neden katkısı
          </Typography>
          <Typography>{item.rootCauseContribution}</Typography>
        </Box>
      ) : (
        canAct &&
        details.record.status === "Investigation" && (
          <Stack spacing={1.2} sx={{ mt: 2 }}>
            {mutation.isError && (
              <Alert severity="error">{mutation.error.message}</Alert>
            )}
            <TextField
              multiline
              minRows={2}
              label="Araştırma bulguları"
              value={findings}
              onChange={(e) => setFindings(e.target.value)}
            />
            <TextField
              label="Kök neden katkısı"
              value={root}
              onChange={(e) => setRoot(e.target.value)}
            />
            <Button
              variant="contained"
              sx={{ alignSelf: "flex-end" }}
              disabled={!findings.trim() || !root.trim() || mutation.isPending}
              onClick={() => mutation.mutate()}
            >
              Araştırmayı tamamla
            </Button>
          </Stack>
        )
      )}
    </Paper>
  );
}

function Responses({
  details,
  update,
  canWrite,
  canApprove,
}: {
  details: ComplaintDetails;
  update: (d: ComplaintDetails) => void;
  canWrite: boolean;
  canApprove: boolean;
}) {
  const type =
    details.record.status === "PreliminaryResponse"
      ? "Preliminary"
      : details.record.status === "FinalResponseApproval"
        ? "Final"
        : null;
  const [content, setContent] = useState("");
  const create = useMutation({
    mutationFn: () =>
      addComplaintResponse(
        details.record.id,
        details.record.version,
        type!,
        content,
      ),
    onSuccess: (d) => {
      update(d);
      setContent("");
    },
  });
  return (
    <Stack spacing={2}>
      {type && canWrite && (
        <Paper variant="outlined" className="complaint-response-editor">
          <Typography variant="h6">
            {type === "Preliminary" ? "Ön yanıt" : "Nihai yanıt"} yeni sürümü
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Onaylanan sürümler değiştirilemez; düzeltme yeni sürüm olarak
            eklenir.
          </Typography>
          {create.isError && (
            <Alert severity="error" sx={{ mt: 1 }}>
              {create.error.message}
            </Alert>
          )}
          <TextField
            fullWidth
            multiline
            minRows={4}
            sx={{ mt: 2 }}
            label="Müşteriye iletilecek yanıt"
            value={content}
            onChange={(e) => setContent(e.target.value)}
          />
          <Button
            variant="contained"
            sx={{ mt: 1.5 }}
            disabled={!content.trim() || create.isPending}
            onClick={() => create.mutate()}
          >
            Taslak sürümü kaydet
          </Button>
        </Paper>
      )}
      {details.responses.map((r) => (
        <ResponseCard
          key={r.id}
          response={r}
          details={details}
          update={update}
          canApprove={canApprove}
        />
      ))}
      {!details.responses.length && (
        <Alert severity="info">Henüz müşteri yanıtı hazırlanmadı.</Alert>
      )}
    </Stack>
  );
}
function ResponseCard({
  response,
  details,
  update,
  canApprove,
}: {
  response: ComplaintDetails["responses"][number];
  details: ComplaintDetails;
  update: (d: ComplaintDetails) => void;
  canApprove: boolean;
}) {
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureAccepted, setSignatureAccepted] = useState(false);
  const mutation = useMutation({
    mutationFn: () =>
      approveComplaintResponse(
        details.record.id,
        response.id,
        details.record.version,
        signaturePassword,
        signatureAccepted,
      ),
    onSuccess: update,
  });
  return (
    <Paper variant="outlined" className="complaint-response-card">
      <Stack
        direction="row"
        sx={{ justifyContent: "space-between", gap: 1, alignItems: "center" }}
      >
        <Box>
          <Typography variant="overline">
            {response.responseType === "Preliminary"
              ? "ÖN YANIT"
              : "NİHAİ YANIT"}{" "}
            · SÜRÜM {response.versionNumber}
          </Typography>
          <Typography variant="caption" sx={{ display: "block" }}>
            {response.preparedBy} · {dt(response.createdAtUtc)}
          </Typography>
        </Box>
        <Chip
          color={response.status === "Approved" ? "success" : "warning"}
          label={response.status === "Approved" ? "Onaylı" : "Taslak"}
        />
      </Stack>
      <Typography sx={{ mt: 2, whiteSpace: "pre-wrap" }}>
        {response.content}
      </Typography>
      {response.approvedBy && (
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ mt: 1.5, display: "block" }}
        >
          Onay: {response.approvedBy} · {dt(response.approvedAtUtc)}
        </Typography>
      )}
      {response.status === "Draft" && canApprove && (
        <Stack spacing={1.2} sx={{ mt: 2 }}>
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
            label="Yanıt içeriğini elektronik imzamla onaylıyorum"
          />
          <Button
            variant="outlined"
            color="success"
            disabled={
              mutation.isPending || !signaturePassword || !signatureAccepted
            }
            onClick={() => mutation.mutate()}
          >
            Bu sürümü e-imzala ve onayla
          </Button>
        </Stack>
      )}
    </Paper>
  );
}

function Links({ details }: { details: ComplaintDetails }) {
  const r = details.record;
  return (
    <Stack spacing={2}>
      <Box className="complaint-link-grid">
        <LinkCard
          title="M.01 Sapma"
          number={r.linkedDeviationNumber}
          text={
            r.linkedDeviationNumber
              ? "Ürün kalitesi araştırması M.01 üzerinden izleniyor."
              : "Triyaj kriteri oluşursa otomatik açılır."
          }
        />
        <LinkCard
          title="M.02 DÖF"
          number={r.linkedCapaNumber}
          text={
            r.linkedCapaNumber
              ? "Kök neden aksiyonları M.02 üzerinden izleniyor."
              : "DÖF kararı gerekli ise otomatik açılır."
          }
        />
        <LinkCard
          title="M.15 Farmakovijilans"
          number={r.pharmacovigilanceRecordNumber}
          text={
            r.pharmacovigilanceRecordNumber
              ? "Gizlilik sınırıyla güvenli yönlendirme tamamlandı."
              : "Advers olay şüphesi varsa triyajda açılır."
          }
        />
      </Box>
      <RecordAssignments aggregateType="Complaint" aggregateId={r.id} />
    </Stack>
  );
}
function LinkCard({
  title,
  number,
  text,
}: {
  title: string;
  number: string | null;
  text: string;
}) {
  return (
    <Paper variant="outlined" className="complaint-link-card">
      <HubRounded />
      <Typography variant="overline">{title}</Typography>
      <Typography variant="h6">{number ?? "Henüz bağlı kayıt yok"}</Typography>
      <Typography variant="body2" color="text.secondary">
        {text}
      </Typography>
    </Paper>
  );
}

function ComplaintActions({
  details,
  update,
  canManage,
}: {
  details: ComplaintDetails;
  update: (d: ComplaintDetails) => void;
  canManage: boolean;
}) {
  const [note, setNote] = useState("");
  const [impact, setImpact] = useState("");
  const [root, setRoot] = useState("");
  const [capa, setCapa] = useState(true);
  const [owner, setOwner] = useState(details.record.ownerUserId);
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureAccepted, setSignatureAccepted] = useState(false);
  const optionsQuery = useQuery({
    queryKey: ["complaint-options"],
    queryFn: ({ signal }) => getComplaintOptions(signal),
  });
  const [target, setTarget] = useState(
    new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10),
  );
  const mutation = useMutation({
    mutationFn: async (action: string) => {
      if (action === "impact")
        return completeComplaintImpact(
          details.record.id,
          details.record.version,
          impact,
          root,
        );
      if (action === "capa")
        return decideComplaintCapa(
          details.record.id,
          details.record.version,
          capa,
          owner,
          new Date(`${target}T12:00:00`).toISOString(),
          true,
        );
      return transitionComplaint(
        details.record.id,
        details.record.version,
        action,
        note,
        action === "close" ? signaturePassword : undefined,
        action === "close" ? signatureAccepted : false,
      );
    },
    onSuccess: (d) => {
      update(d);
      setNote("");
    },
  });
  if (!canManage || details.record.status === "Closed") return null;
  const transition = details.availableTransitions[0];
  return (
    <DialogActions className="record-detail-actions">
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={1.2}
        sx={{
          width: "100%",
          alignItems: { md: "center" },
          justifyContent: "space-between",
        }}
      >
        {details.record.status === "ImpactAssessment" ? (
          <>
            <Stack
              direction={{ xs: "column", md: "row" }}
              spacing={1}
              sx={{ flex: 1 }}
            >
              <TextField
                size="small"
                label="Etki değerlendirmesi"
                value={impact}
                onChange={(e) => setImpact(e.target.value)}
                sx={{ flex: 1 }}
              />
              <TextField
                size="small"
                label="Doğrulanmış kök neden"
                value={root}
                onChange={(e) => setRoot(e.target.value)}
                sx={{ flex: 1 }}
              />
            </Stack>
            <Button
              variant="contained"
              disabled={!impact.trim() || !root.trim() || mutation.isPending}
              onClick={() => mutation.mutate("impact")}
            >
              DÖF kararına geç
            </Button>
          </>
        ) : details.record.status === "CapaDecision" ? (
          <>
            <Stack
              direction={{ xs: "column", md: "row" }}
              spacing={1}
              sx={{ flex: 1, alignItems: { md: "center" } }}
            >
              <FormControlLabel
                control={
                  <Checkbox
                    checked={capa}
                    onChange={(e) => setCapa(e.target.checked)}
                  />
                }
                label="DÖF gerekli"
              />
              {capa && (
                <>
                  <SearchableSelect
                    label="DÖF sorumlusu"
                    value={owner}
                    options={(optionsQuery.data?.owners ?? []).map((x) => ({
                      value: x.id,
                      label: `${x.name}${x.department ? ` · ${x.department}` : ""}`,
                    }))}
                    onChange={(v) => setOwner(v ?? "")}
                  />
                  <TextField
                    size="small"
                    type="date"
                    label="DÖF hedefi"
                    value={target}
                    onChange={(e) => setTarget(e.target.value)}
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                </>
              )}
            </Stack>
            <Button
              variant="contained"
              disabled={(capa && !owner.trim()) || mutation.isPending}
              onClick={() => mutation.mutate("capa")}
            >
              Kararı kaydet
            </Button>
          </>
        ) : transition ? (
          <>
            <Box>
              <Typography sx={{ fontWeight: 800 }}>
                Sıradaki kontrollü adım
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {transition.label}
              </Typography>
            </Box>
            <Stack direction="row" spacing={1}>
              {transition.noteRequired && (
                <Stack spacing={1}>
                  <TextField
                    size="small"
                    label="Kapanış gerekçesi"
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                  />
                  {transition.code === "close" && (
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
                            onChange={(e) =>
                              setSignatureAccepted(e.target.checked)
                            }
                          />
                        }
                        label="Kapanışı elektronik imzamla onaylıyorum"
                      />
                    </>
                  )}
                </Stack>
              )}
              <Button
                variant="contained"
                disabled={
                  (transition.noteRequired && !note.trim()) ||
                  (transition.code === "close" &&
                    (!signaturePassword || !signatureAccepted)) ||
                  mutation.isPending
                }
                onClick={() => mutation.mutate(transition.code)}
              >
                {transition.label}
              </Button>
            </Stack>
          </>
        ) : (
          <Alert severity="info" sx={{ width: "100%" }}>
            Bu aşamanın tamamlanması için ilgili sekmedeki zorunlu kayıtları
            tamamlayın.
          </Alert>
        )}
      </Stack>
    </DialogActions>
  );
}
const auditLabels: Record<string, string> = {
  ComplaintCreated: "Şikâyet kaydı oluşturuldu",
  ComplaintStatusChanged: "Şikâyet aşaması değiştirildi",
  ComplaintResponseDrafted: "Müşteri yanıtı sürümü hazırlandı",
  ComplaintResponseApproved: "Müşteri yanıtı onaylandı",
  ComplaintInvestigationCompleted: "Bölüm araştırması tamamlandı",
  ComplaintImpactAssessed: "Etki ve kök neden değerlendirildi",
  ComplaintCapaDecisionRecorded: "DÖF kararı kaydedildi",
};
