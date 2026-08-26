import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AddRounded,
  AssignmentTurnedInRounded,
  DownloadRounded,
  HistoryRounded,
  SettingsRounded,
  TaskAltRounded,
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
  MenuItem,
  Paper,
  Snackbar,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { ModalHeader } from "../../components/ModalHeader";
import {
  createWorkItem,
  createWorkItemLookup,
  downloadWorkItemFinalReport,
  getWorkItem,
  getWorkItemLookups,
  getWorkItemOptions,
  searchWorkItems,
  transitionWorkItem,
  updateWorkItemLookup,
  type Lookup,
  type WorkItemDetails,
} from "../../api/workItems";
import { SearchableSelect } from "../../components/SearchableSelect";
import { RecordAssignments } from "../../components/RecordAssignments";
import { RecordActionMenu } from "../../components/RecordActionMenu";
import { Permissions, useAuth } from "../../security/AuthContext";

const status: Record<string, string> = {
  Draft: "Taslak",
  Assigned: "Atandı",
  InProgress: "Çalışılıyor",
  PendingVerification: "Doğrulama bekliyor",
  Completed: "Tamamlandı",
  Cancelled: "İptal",
};
const dt = (v: string | null) =>
  v
    ? new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(new Date(v))
    : "—";
const empty = {
  category: "",
  priority: "",
  title: "",
  description: "",
  ownerUserId: "",
  verifierUserId: "",
  dueAtUtc: "",
  sourceModule: "",
  sourceRecordNumber: "",
};
export function WorkItemWorkspace() {
  const { can } = useAuth();
  const qc = useQueryClient();
  const [params, setParams] = useSearchParams();
  const open = params.get("open");
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState(empty);
  const [tab, setTab] = useState(0);
  const [transition, setTransition] = useState<{
    code: string;
    label: string;
    requiresSignature: boolean;
  } | null>(null);
  const [evidence, setEvidence] = useState("");
  const [note, setNote] = useState("");
  const [approved, setApproved] = useState(true);
  const [password, setPassword] = useState("");
  const [accepted, setAccepted] = useState(false);
  const [feedback, setFeedback] = useState<{
    severity: "success" | "error";
    message: string;
  } | null>(null);
  const [lookupOpen, setLookupOpen] = useState(false);
  const [lookupDraft, setLookupDraft] = useState({
    category: "Category",
    code: "",
    name: "",
    sortOrder: 50,
  });
  const list = useQuery({
    queryKey: ["work-items"],
    queryFn: ({ signal }) => searchWorkItems(signal),
  });
  const options = useQuery({
    queryKey: ["work-item-options"],
    queryFn: ({ signal }) => getWorkItemOptions(signal),
  });
  const details = useQuery({
    queryKey: ["work-item", open],
    queryFn: ({ signal }) => getWorkItem(open!, signal),
    enabled: Boolean(open),
  });
  const lookups = useQuery({
    queryKey: ["work-item-lookups"],
    queryFn: getWorkItemLookups,
    enabled: lookupOpen,
  });
  const addLookup = useMutation({
    mutationFn: () => createWorkItemLookup(lookupDraft),
    onSuccess: () => {
      setLookupDraft({
        category: "Category",
        code: "",
        name: "",
        sortOrder: 50,
      });
      void qc.invalidateQueries({ queryKey: ["work-item-lookups"] });
      void qc.invalidateQueries({ queryKey: ["work-item-options"] });
      setFeedback({ severity: "success", message: "M.10 tanımı eklendi." });
    },
    onError: (e) => setFeedback({ severity: "error", message: e.message }),
  });
  const create = useMutation({
    mutationFn: () =>
      createWorkItem({
        ...form,
        sourceModule: form.sourceModule || null,
        sourceRecordId: null,
        sourceRecordNumber: form.sourceRecordNumber || null,
        dueAtUtc: new Date(form.dueAtUtc).toISOString(),
      }),
    onSuccess: (x) => {
      setCreateOpen(false);
      setForm(empty);
      setParams({ open: x.record.id });
      void qc.invalidateQueries({ queryKey: ["work-items"] });
      setFeedback({ severity: "success", message: "İş kaydı oluşturuldu." });
    },
    onError: (e) => setFeedback({ severity: "error", message: e.message }),
  });
  const move = useMutation({
    mutationFn: () =>
      transitionWorkItem(open!, {
        expectedVersion: details.data!.record.version,
        transition: transition!.code,
        evidence: evidence || null,
        approved,
        note: note || null,
        signaturePassword: password || null,
        signatureMeaningAccepted: accepted,
      }),
    onSuccess: () => {
      setTransition(null);
      setEvidence("");
      setNote("");
      setPassword("");
      setAccepted(false);
      void qc.invalidateQueries({ queryKey: ["work-item", open] });
      void qc.invalidateQueries({ queryKey: ["work-items"] });
      setFeedback({
        severity: "success",
        message: "İş akışı başarıyla güncellendi.",
      });
    },
    onError: (e) => setFeedback({ severity: "error", message: e.message }),
  });
  return (
    <Box className="module-workspace">
      <Paper className="module-hero" elevation={0}>
        <Box>
          <Chip label="M.10 aktif" size="small" />
          <Typography variant="h2">İş takip ve aksiyonlar</Typography>
          <Typography>
            Kalite kayıtlarından doğan işleri gerçek sorumlulara atayın, kanıtla
            tamamlayın ve bağımsız olarak doğrulayın.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setLookupOpen(true)}
            >
              M.10 tanımları
            </Button>
          )}
          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={() => setCreateOpen(true)}
          >
            Yeni iş
          </Button>
        </Stack>
      </Paper>
      <Paper className="module-content-card" elevation={0}>
        <Stack
          direction="row"
          sx={{
            p: 2,
            borderBottom: "1px solid",
            borderColor: "divider",
            alignItems: "center",
            justifyContent: "space-between",
          }}
        >
          <Box>
            <Typography variant="h6">İş kayıtları</Typography>
            <Typography variant="body2" color="text.secondary">
              {list.data?.totalCount ?? 0} kayıt · geciken işler kırmızıyla
              işaretlenir
            </Typography>
          </Box>
        </Stack>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Kayıt</TableCell>
              <TableCell>Başlık / sorumlu</TableCell>
              <TableCell>Kategori</TableCell>
              <TableCell>Öncelik</TableCell>
              <TableCell>Hedef</TableCell>
              <TableCell>Durum</TableCell>
              <TableCell align="right">İşlem</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {list.data?.items.map((x) => (
              <TableRow key={x.id} hover>
                <TableCell>
                  <b>{x.recordNumber}</b>
                </TableCell>
                <TableCell>
                  <Typography sx={{ fontWeight: 700 }}>{x.title}</Typography>
                  <Typography variant="caption">{x.owner}</Typography>
                </TableCell>
                <TableCell>
                  {options.data?.categories.find((o) => o.code === x.category)
                    ?.name ?? x.category}
                </TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={
                      options.data?.priorities.find(
                        (o) => o.code === x.priority,
                      )?.name ?? x.priority
                    }
                    color={
                      x.priority === "Critical"
                        ? "error"
                        : x.priority === "High"
                          ? "warning"
                          : "default"
                    }
                  />
                </TableCell>
                <TableCell
                  sx={{
                    color: x.isOverdue ? "error.main" : undefined,
                    fontWeight: x.isOverdue ? 700 : 400,
                  }}
                >
                  {dt(x.dueAtUtc)}
                </TableCell>
                <TableCell>{status[x.status] ?? x.status}</TableCell>
                <TableCell align="right">
                  <RecordActionMenu onOpen={() => setParams({ open: x.id })} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      <Dialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        fullWidth
        maxWidth="md"
      >
        <Header title="Yeni iş kaydı" close={() => setCreateOpen(false)} />
        <DialogContent sx={{ p: 3 }}>
          <Stack spacing={2}>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <TextField
                required
                select
                fullWidth
                label="Kategori"
                value={form.category}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
              >
                {options.data?.categories.map((x) => (
                  <MenuItem key={x.code} value={x.code}>
                    {x.name}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                required
                select
                fullWidth
                label="Öncelik"
                value={form.priority}
                onChange={(e) => setForm({ ...form, priority: e.target.value })}
              >
                {options.data?.priorities.map((x) => (
                  <MenuItem key={x.code} value={x.code}>
                    {x.name}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>
            <TextField
              required
              label="İş başlığı"
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
            <TextField
              required
              multiline
              minRows={3}
              label="İş tanımı ve kabul kriteri"
              value={form.description}
              onChange={(e) =>
                setForm({ ...form, description: e.target.value })
              }
            />
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <SearchableSelect
                required
                label="İş sorumlusu"
                value={form.ownerUserId}
                onChange={(v) => setForm({ ...form, ownerUserId: v ?? "" })}
                options={(options.data?.users ?? []).map((x) => ({
                  value: x.id,
                  label: x.name,
                  secondary: x.department ?? "Bölüm atanmamış",
                }))}
              />
              <SearchableSelect
                required
                label="Bağımsız doğrulayıcı"
                value={form.verifierUserId}
                onChange={(v) => setForm({ ...form, verifierUserId: v ?? "" })}
                options={(options.data?.users ?? []).map((x) => ({
                  value: x.id,
                  label: x.name,
                  secondary: x.department ?? "Bölüm atanmamış",
                }))}
              />
            </Stack>
            <TextField
              required
              type="datetime-local"
              slotProps={{ inputLabel: { shrink: true } }}
              label="Hedef tarih"
              value={form.dueAtUtc}
              onChange={(e) => setForm({ ...form, dueAtUtc: e.target.value })}
            />
            <Stack direction="row" spacing={2}>
              <TextField
                label="Kaynak modül (isteğe bağlı)"
                placeholder="M.01"
                value={form.sourceModule}
                onChange={(e) =>
                  setForm({ ...form, sourceModule: e.target.value })
                }
              />
              <TextField
                fullWidth
                label="Kaynak kayıt numarası (isteğe bağlı)"
                value={form.sourceRecordNumber}
                onChange={(e) =>
                  setForm({ ...form, sourceRecordNumber: e.target.value })
                }
              />
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button color="inherit" onClick={() => setCreateOpen(false)}>
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={
              !form.category ||
              !form.priority ||
              !form.title ||
              !form.description ||
              !form.ownerUserId ||
              !form.verifierUserId ||
              !form.dueAtUtc ||
              create.isPending
            }
            onClick={() => create.mutate()}
          >
            Kaydı oluştur
          </Button>
        </DialogActions>
      </Dialog>
      <Dialog
        open={lookupOpen}
        onClose={() => setLookupOpen(false)}
        fullWidth
        maxWidth="md"
      >
        <Header
          title="M.10 kategori ve öncelik tanımları"
          close={() => setLookupOpen(false)}
        />
        <DialogContent sx={{ p: 3 }}>
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={1.5}
            sx={{ mb: 3 }}
          >
            <TextField
              select
              label="Tür"
              value={lookupDraft.category}
              onChange={(e) =>
                setLookupDraft({ ...lookupDraft, category: e.target.value })
              }
            >
              <MenuItem value="Category">Kategori</MenuItem>
              <MenuItem value="Priority">Öncelik</MenuItem>
            </TextField>
            <TextField
              required
              label="Kod"
              value={lookupDraft.code}
              onChange={(e) =>
                setLookupDraft({ ...lookupDraft, code: e.target.value })
              }
            />
            <TextField
              required
              fullWidth
              label="Görünen ad"
              value={lookupDraft.name}
              onChange={(e) =>
                setLookupDraft({ ...lookupDraft, name: e.target.value })
              }
            />
            <TextField
              type="number"
              label="Sıra"
              value={lookupDraft.sortOrder}
              onChange={(e) =>
                setLookupDraft({
                  ...lookupDraft,
                  sortOrder: Number(e.target.value),
                })
              }
            />
            <Button
              variant="contained"
              disabled={
                !lookupDraft.code || !lookupDraft.name || addLookup.isPending
              }
              onClick={() => addLookup.mutate()}
            >
              Ekle
            </Button>
          </Stack>
          <Stack spacing={1}>
            {lookups.data?.map((x) => (
              <LookupRow
                key={x.id}
                item={x}
                saved={() => {
                  void qc.invalidateQueries({
                    queryKey: ["work-item-lookups"],
                  });
                  void qc.invalidateQueries({
                    queryKey: ["work-item-options"],
                  });
                  setFeedback({
                    severity: "success",
                    message: "M.10 tanımı güncellendi.",
                  });
                }}
                failed={(message) =>
                  setFeedback({ severity: "error", message })
                }
              />
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLookupOpen(false)}>Kapat</Button>
        </DialogActions>
      </Dialog>
      <Details
        open={Boolean(open)}
        data={details.data}
        close={() => setParams({})}
        tab={tab}
        setTab={setTab}
        act={setTransition}
      />
      <Dialog
        open={Boolean(transition)}
        onClose={() => setTransition(null)}
        fullWidth
        maxWidth="sm"
      >
        <Header
          title={transition?.label ?? ""}
          close={() => setTransition(null)}
        />
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            {transition?.code === "submit" && (
              <TextField
                required
                multiline
                minRows={4}
                label="Tamamlama kanıtı / referansı"
                value={evidence}
                onChange={(e) => setEvidence(e.target.value)}
              />
            )}{" "}
            {transition?.code === "verify" && (
              <FormControlLabel
                control={
                  <Checkbox
                    checked={approved}
                    onChange={(e) => setApproved(e.target.checked)}
                  />
                }
                label="Kanıtı yeterli buldum; işi tamamla"
              />
            )}
            {(transition?.code === "verify" ||
              transition?.code === "cancel") && (
              <TextField
                required
                multiline
                minRows={3}
                label="Karar gerekçesi"
                value={note}
                onChange={(e) => setNote(e.target.value)}
              />
            )}{" "}
            {transition?.requiresSignature && (
              <>
                <TextField
                  required
                  type="password"
                  label="E-imza parolası"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={accepted}
                      onChange={(e) => setAccepted(e.target.checked)}
                    />
                  }
                  label="İşlemin e-imza anlamını ve sorumluluğunu kabul ediyorum"
                />
              </>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button color="inherit" onClick={() => setTransition(null)}>
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={
              move.isPending ||
              (transition?.code === "submit" && !evidence) ||
              (transition?.requiresSignature &&
                (!password || !accepted || !note))
            }
            onClick={() => move.mutate()}
          >
            Onayla ve ilerlet
          </Button>
        </DialogActions>
      </Dialog>
      <Snackbar
        open={Boolean(feedback)}
        autoHideDuration={4500}
        onClose={() => setFeedback(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      >
        <Alert
          className="app-result-toast"
          variant="filled"
          severity={feedback?.severity ?? "success"}
          onClose={() => setFeedback(null)}
        >
          {feedback?.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}

function Header({ title, close }: { title: string; close: () => void }) {
  return (
    <ModalHeader onClose={close}>
      <Typography variant="h5" sx={{ fontWeight: 800 }}>
        {title}
      </Typography>
    </ModalHeader>
  );
}
function Details({
  open,
  data,
  close,
  tab,
  setTab,
  act,
}: {
  open: boolean;
  data: WorkItemDetails | undefined;
  close: () => void;
  tab: number;
  setTab: (x: number) => void;
  act: (x: { code: string; label: string; requiresSignature: boolean }) => void;
}) {
  useEffect(() => {
    if (!open) setTab(0);
  }, [open, setTab]);
  return (
    <Dialog
      open={open}
      onClose={close}
      fullWidth
      maxWidth="xl"
      slotProps={{ paper: { className: "record-details-paper" } }}
    >
      <Box sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
        <Header
          title={
            data
              ? `${data.record.recordNumber} · ${data.record.title}`
              : "İş kaydı yükleniyor"
          }
          close={close}
        />
        {data && (
          <>
            <Tabs value={tab} onChange={(_, v) => setTab(v)}>
              <Tab
                icon={<AssignmentTurnedInRounded />}
                iconPosition="start"
                label="Genel bakış"
              />
              <Tab
                icon={<TaskAltRounded />}
                iconPosition="start"
                label={`Görev ve karar (${data.availableTransitions.length})`}
              />
              <Tab
                icon={<HistoryRounded />}
                iconPosition="start"
                label={`Geçmiş (${data.auditTrail.length})`}
              />
            </Tabs>
            <Box
              sx={{ p: 3, overflow: "auto", flex: 1, background: "#f5f8f7" }}
            >
              {tab === 0 && (
                <Stack spacing={2}>
                  <Paper sx={{ p: 3 }}>
                    <Stack
                      direction="row"
                      sx={{ justifyContent: "space-between" }}
                    >
                      <Box>
                        <Typography variant="overline">
                          {status[data.record.status]}
                        </Typography>
                        <Typography variant="h4">
                          {data.record.title}
                        </Typography>
                        <Typography sx={{ mt: 1 }}>
                          {data.record.description}
                        </Typography>
                      </Box>
                      <Stack spacing={1} sx={{ alignItems: "flex-end" }}>
                        <Chip
                          label={
                            data.record.isOverdue
                              ? "Gecikti"
                              : dt(data.record.dueAtUtc)
                          }
                          color={data.record.isOverdue ? "error" : "success"}
                        />
                        {data.record.status === "Completed" && (
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<DownloadRounded />}
                            onClick={() =>
                              void downloadWorkItemFinalReport(data.record.id)
                            }
                          >
                            Nihai PDF
                          </Button>
                        )}
                      </Stack>
                    </Stack>
                  </Paper>
                  <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                    <Info
                      title="Sorumlu"
                      value={`${data.record.owner} · ${data.record.ownerDepartment ?? "Bölüm yok"}`}
                    />
                    <Info
                      title="Bağımsız doğrulayıcı"
                      value={data.record.verifier}
                    />
                    <Info
                      title="Kaynak"
                      value={
                        data.record.sourceRecordNumber
                          ? `${data.record.sourceModule} · ${data.record.sourceRecordNumber}`
                          : "Bağımsız iş kaydı"
                      }
                    />
                  </Stack>
                  {data.record.completionEvidence && (
                    <Info
                      title="Tamamlama kanıtı"
                      value={data.record.completionEvidence}
                    />
                  )}{" "}
                  {data.signatures.length > 0 && (
                    <Paper sx={{ p: 3 }}>
                      <Typography variant="h6">Elektronik imzalar</Typography>
                      {data.signatures.map((s) => (
                        <Box
                          key={s.id}
                          sx={{ py: 1.5, borderBottom: "1px solid #e3ebe8" }}
                        >
                          <b>{s.signer}</b> · {s.meaning}
                          <Typography
                            variant="caption"
                            sx={{ display: "block" }}
                          >
                            {dt(s.signedAtUtc)} · {s.contentHash.slice(0, 20)}…
                          </Typography>
                        </Box>
                      ))}
                    </Paper>
                  )}
                </Stack>
              )}
              {tab === 1 && (
                <Stack spacing={2}>
                  <Paper sx={{ p: 3 }}>
                    <Typography variant="h6">
                      Sıradaki kontrollü adım
                    </Typography>
                    <Typography color="text.secondary" sx={{ mb: 2 }}>
                      Yalnızca size atanmış veya etkin delegasyon kapsamındaki
                      işlemler gösterilir.
                    </Typography>
                    <Stack
                      direction="row"
                      spacing={1}
                      sx={{ flexWrap: "wrap" }}
                    >
                      {data.availableTransitions.length ? (
                        data.availableTransitions.map((t) => (
                          <Button
                            key={t.code}
                            variant="contained"
                            onClick={() => act(t)}
                          >
                            {t.label}
                          </Button>
                        ))
                      ) : (
                        <Alert severity="info">
                          Bu kullanıcı için kullanılabilir geçiş bulunmuyor.
                        </Alert>
                      )}
                    </Stack>
                  </Paper>
                  <RecordAssignments
                    aggregateType="WorkItem"
                    aggregateId={data.record.id}
                  />
                </Stack>
              )}
              {tab === 2 && (
                <Paper sx={{ p: 3 }}>
                  {data.auditTrail.map((e) => (
                    <Box
                      key={e.id}
                      sx={{
                        display: "grid",
                        gridTemplateColumns: "170px 1fr",
                        gap: 2,
                        py: 1.5,
                        borderBottom: "1px solid #e3ebe8",
                      }}
                    >
                      <Typography variant="caption">
                        {dt(e.occurredAtUtc)}
                      </Typography>
                      <Box>
                        <b>{e.eventType}</b>
                        <Typography variant="body2">
                          {e.actor}
                          {e.reason ? ` · ${e.reason}` : ""}
                        </Typography>
                      </Box>
                    </Box>
                  ))}
                </Paper>
              )}
            </Box>
          </>
        )}
      </Box>
    </Dialog>
  );
}
function Info({ title, value }: { title: string; value: string }) {
  return (
    <Paper sx={{ p: 2.5, flex: 1, borderLeft: "3px solid #0b8f87" }}>
      <Typography variant="overline" color="text.secondary">
        {title}
      </Typography>
      <Typography sx={{ fontWeight: 700 }}>{value}</Typography>
    </Paper>
  );
}
function LookupRow({
  item,
  saved,
  failed,
}: {
  item: Lookup;
  saved: () => void;
  failed: (message: string) => void;
}) {
  const [name, setName] = useState(item.name);
  const [sortOrder, setSortOrder] = useState(item.sortOrder);
  const [isActive, setIsActive] = useState(item.isActive);
  const mutation = useMutation({
    mutationFn: () =>
      updateWorkItemLookup(item.id, { name, sortOrder, isActive }),
    onSuccess: saved,
    onError: (e) => failed(e.message),
  });
  return (
    <Paper variant="outlined" sx={{ p: 1.5 }}>
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={1.5}
        sx={{ alignItems: "center" }}
      >
        <Chip label={item.category === "Category" ? "Kategori" : "Öncelik"} />
        <Typography sx={{ minWidth: 130, fontWeight: 700 }}>
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
          disabled={!name || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          Kaydet
        </Button>
      </Stack>
    </Paper>
  );
}
