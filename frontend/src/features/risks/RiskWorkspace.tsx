import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AddRounded,
  AnalyticsRounded,
  CloseRounded,
  HistoryRounded,
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
  IconButton,
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
import {
  addRiskItem,
  completeRiskAction,
  createRisk,
  createRiskLookup,
  downloadRiskFinalReport,
  riskDetails,
  riskOptions,
  riskLookups,
  searchRisks,
  setResidualRisk,
  transitionRisk,
  updateRiskLookup,
  type RiskLookup,
  type RiskDetails,
  type RiskItem,
} from "../../api/risks";
import { SearchableSelect } from "../../components/SearchableSelect";
import { RecordAssignments } from "../../components/RecordAssignments";
import { Permissions, useAuth } from "../../security/AuthContext";
const labels: Record<string, string> = {
  Draft: "Taslak",
  Scoring: "Puanlama",
  ActionPlan: "Aksiyon planı",
  ResidualReview: "Kalıntı risk",
  Approved: "Onaylandı",
  Closed: "Kapalı",
};
const empty = {
  process: "",
  scope: "",
  category: "",
  methodology: "",
  matrixVersion: "",
  actionThreshold: 40,
  ownerUserId: "",
  approverUserId: "",
};
const itemEmpty = {
  failureMode: "",
  effect: "",
  cause: "",
  existingControls: "",
  severity: 3,
  occurrence: 3,
  detectability: 3,
  action: "",
  actionOwnerUserId: "",
  actionDueAtUtc: "",
};
export function RiskWorkspace() {
  const { can } = useAuth();
  const qc = useQueryClient();
  const [params, setParams] = useSearchParams();
  const id = params.get("open");
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState(empty);
  const [itemOpen, setItemOpen] = useState(false);
  const [item, setItem] = useState(itemEmpty);
  const [tab, setTab] = useState(0);
  const [decision, setDecision] = useState<{
    kind: "transition" | "complete" | "residual";
    code?: string;
    item?: RiskItem;
    label: string;
    signature?: boolean;
  } | null>(null);
  const [note, setNote] = useState("");
  const [password, setPassword] = useState("");
  const [accepted, setAccepted] = useState(false);
  const [scores, setScores] = useState({
    severity: 2,
    occurrence: 2,
    detectability: 2,
  });
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
    queryKey: ["risks"],
    queryFn: ({ signal }) => searchRisks(signal),
  });
  const opts = useQuery({
    queryKey: ["risk-options"],
    queryFn: ({ signal }) => riskOptions(signal),
  });
  const details = useQuery({
    queryKey: ["risk", id],
    queryFn: ({ signal }) => riskDetails(id!, signal),
    enabled: Boolean(id),
  });
  const lookupQuery = useQuery({
    queryKey: ["risk-lookups"],
    queryFn: riskLookups,
    enabled: lookupOpen,
  });
  const done = (msg: string) => {
    void qc.invalidateQueries({ queryKey: ["risks"] });
    void qc.invalidateQueries({ queryKey: ["risk", id] });
    setFeedback({ severity: "success", message: msg });
  };
  const fail = (e: Error) =>
    setFeedback({ severity: "error", message: e.message });
  const addLookup = useMutation({
    mutationFn: () => createRiskLookup(lookupDraft),
    onSuccess: () => {
      setLookupDraft({
        category: "Category",
        code: "",
        name: "",
        sortOrder: 50,
      });
      void qc.invalidateQueries({ queryKey: ["risk-lookups"] });
      void qc.invalidateQueries({ queryKey: ["risk-options"] });
      done("M.11 tanımı eklendi.");
    },
    onError: fail,
  });
  const create = useMutation({
    mutationFn: () => createRisk(form),
    onSuccess: (x) => {
      setCreateOpen(false);
      setForm(empty);
      setParams({ open: x.record.id });
      done("FMEA kaydı oluşturuldu.");
    },
    onError: fail,
  });
  const add = useMutation({
    mutationFn: () =>
      addRiskItem(id!, {
        ...item,
        expectedVersion: details.data!.record.version,
        actionOwnerUserId: item.actionOwnerUserId || null,
        actionDueAtUtc: item.actionDueAtUtc
          ? new Date(item.actionDueAtUtc).toISOString()
          : null,
      }),
    onSuccess: () => {
      setItemOpen(false);
      setItem(itemEmpty);
      done("Risk satırı eklendi.");
    },
    onError: fail,
  });
  const act = useMutation({
    mutationFn: () =>
      decision!.kind === "transition"
        ? transitionRisk(id!, {
            expectedVersion: details.data!.record.version,
            transition: decision!.code,
            note: note || null,
            signaturePassword: password || null,
            signatureMeaningAccepted: accepted,
          })
        : decision!.kind === "complete"
          ? completeRiskAction(id!, decision!.item!.id, {
              expectedVersion: details.data!.record.version,
              evidence: note,
            })
          : setResidualRisk(id!, decision!.item!.id, {
              expectedVersion: details.data!.record.version,
              ...scores,
              rationale: note,
            }),
    onSuccess: () => {
      setDecision(null);
      setNote("");
      setPassword("");
      setAccepted(false);
      done("Risk kaydı güncellendi.");
    },
    onError: fail,
  });
  return (
    <Box className="module-workspace">
      <Paper className="module-hero" elevation={0}>
        <Box>
          <Chip label="M.11 aktif" size="small" />
          <Typography variant="h2">Risk yönetimi · FMEA</Typography>
          <Typography>
            Başlangıç ve kalıntı riskleri aynı kayıt üzerinde puanlayın; eşik
            üstü riskleri gerçek aksiyon sahipleriyle azaltın.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          {can(Permissions.administrationManage) && (
            <Button variant="outlined" onClick={() => setLookupOpen(true)}>
              M.11 tanımları
            </Button>
          )}
          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={() => setCreateOpen(true)}
          >
            Yeni FMEA
          </Button>
        </Stack>
      </Paper>
      <Paper className="module-content-card">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Kayıt</TableCell>
              <TableCell>Proses / sahibi</TableCell>
              <TableCell>Metodoloji</TableCell>
              <TableCell>En yüksek RPN</TableCell>
              <TableCell>Açık aksiyon</TableCell>
              <TableCell>Durum</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {list.data?.items.map((x) => (
              <TableRow key={x.id} hover>
                <TableCell>
                  <b>{x.recordNumber}</b>
                </TableCell>
                <TableCell>
                  <b>{x.process}</b>
                  <Typography variant="caption" sx={{ display: "block" }}>
                    {x.owner}
                  </Typography>
                </TableCell>
                <TableCell>
                  {opts.data?.methodologies.find(
                    (o) => o.code === x.methodology,
                  )?.name ?? x.methodology}
                </TableCell>
                <TableCell>
                  <Chip
                    label={x.maxInitialRpn}
                    color={x.maxInitialRpn >= 40 ? "error" : "success"}
                  />
                </TableCell>
                <TableCell>{x.openActionCount}</TableCell>
                <TableCell>{labels[x.status]}</TableCell>
                <TableCell>
                  <Button onClick={() => setParams({ open: x.id })}>Aç</Button>
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
        <Head title="Yeni FMEA kaydı" close={() => setCreateOpen(false)} />
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <TextField
              required
              label="Proses / sistem"
              value={form.process}
              onChange={(e) => setForm({ ...form, process: e.target.value })}
            />
            <TextField
              required
              multiline
              minRows={3}
              label="Kapsam ve sınırlar"
              value={form.scope}
              onChange={(e) => setForm({ ...form, scope: e.target.value })}
            />
            <Stack direction="row" spacing={2}>
              <Sel
                label="Risk kategorisi"
                value={form.category}
                options={opts.data?.categories ?? []}
                change={(v) => setForm({ ...form, category: v })}
              />
              <Sel
                label="Metodoloji"
                value={form.methodology}
                options={opts.data?.methodologies ?? []}
                change={(v) => setForm({ ...form, methodology: v })}
              />
              <Sel
                label="Matris sürümü"
                value={form.matrixVersion}
                options={opts.data?.matrixVersions ?? []}
                change={(v) => setForm({ ...form, matrixVersion: v })}
              />
            </Stack>
            <TextField
              required
              type="number"
              label="Aksiyon eşiği (RPN)"
              value={form.actionThreshold}
              onChange={(e) =>
                setForm({ ...form, actionThreshold: Number(e.target.value) })
              }
            />
            <Stack direction="row" spacing={2}>
              <Person
                label="Risk sahibi"
                value={form.ownerUserId}
                change={(v) => setForm({ ...form, ownerUserId: v })}
                users={opts.data?.users ?? []}
              />
              <Person
                label="Bağımsız onaylayan"
                value={form.approverUserId}
                change={(v) => setForm({ ...form, approverUserId: v })}
                users={opts.data?.users ?? []}
              />
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateOpen(false)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={
              Object.entries(form).some(
                ([k, v]) => k !== "actionThreshold" && !v,
              ) || create.isPending
            }
            onClick={() => create.mutate()}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>
      <Dialog
        open={lookupOpen}
        onClose={() => setLookupOpen(false)}
        fullWidth
        maxWidth="md"
      >
        <Head title="M.11 risk tanımları" close={() => setLookupOpen(false)} />
        <DialogContent sx={{ p: 3 }}>
          <Stack direction="row" spacing={1} sx={{ mb: 3 }}>
            <TextField
              select
              label="Tür"
              value={lookupDraft.category}
              onChange={(e) =>
                setLookupDraft({ ...lookupDraft, category: e.target.value })
              }
            >
              <MenuItem value="Category">Kategori</MenuItem>
              <MenuItem value="Methodology">Metodoloji</MenuItem>
              <MenuItem value="MatrixVersion">Matris sürümü</MenuItem>
            </TextField>
            <TextField
              label="Kod"
              value={lookupDraft.code}
              onChange={(e) =>
                setLookupDraft({ ...lookupDraft, code: e.target.value })
              }
            />
            <TextField
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
              disabled={!lookupDraft.code || !lookupDraft.name}
              onClick={() => addLookup.mutate()}
            >
              Ekle
            </Button>
          </Stack>
          <Stack spacing={1}>
            {lookupQuery.data?.map((x) => (
              <RiskLookupRow
                key={x.id}
                item={x}
                saved={() => {
                  void qc.invalidateQueries({ queryKey: ["risk-lookups"] });
                  void qc.invalidateQueries({ queryKey: ["risk-options"] });
                  done("M.11 tanımı güncellendi.");
                }}
                failed={(m) => setFeedback({ severity: "error", message: m })}
              />
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLookupOpen(false)}>Kapat</Button>
        </DialogActions>
      </Dialog>
      <RiskDialog
        data={details.data}
        open={Boolean(id)}
        close={() => setParams({})}
        tab={tab}
        setTab={setTab}
        add={() => setItemOpen(true)}
        decide={setDecision}
      />
      <Dialog
        open={itemOpen}
        onClose={() => setItemOpen(false)}
        fullWidth
        maxWidth="md"
      >
        <Head title="FMEA risk satırı" close={() => setItemOpen(false)} />
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <TextField
              required
              label="Hata türü"
              value={item.failureMode}
              onChange={(e) =>
                setItem({ ...item, failureMode: e.target.value })
              }
            />
            <Stack direction="row" spacing={2}>
              <TextField
                required
                fullWidth
                multiline
                label="Etkisi"
                value={item.effect}
                onChange={(e) => setItem({ ...item, effect: e.target.value })}
              />
              <TextField
                required
                fullWidth
                multiline
                label="Nedeni"
                value={item.cause}
                onChange={(e) => setItem({ ...item, cause: e.target.value })}
              />
            </Stack>
            <TextField
              required
              multiline
              label="Mevcut kontroller"
              value={item.existingControls}
              onChange={(e) =>
                setItem({ ...item, existingControls: e.target.value })
              }
            />
            <Scores value={item} change={(v) => setItem({ ...item, ...v })} />
            <Alert
              severity={
                item.severity * item.occurrence * item.detectability >=
                form.actionThreshold
                  ? "warning"
                  : "success"
              }
            >
              Başlangıç RPN:{" "}
              {item.severity * item.occurrence * item.detectability} · Eşik:{" "}
              {details.data?.record.actionThreshold ?? form.actionThreshold}
            </Alert>
            <TextField
              multiline
              label="Risk azaltma aksiyonu (eşik üstünde zorunlu)"
              value={item.action}
              onChange={(e) => setItem({ ...item, action: e.target.value })}
            />
            <Stack direction="row" spacing={2}>
              <Person
                label="Aksiyon sorumlusu"
                value={item.actionOwnerUserId}
                change={(v) => setItem({ ...item, actionOwnerUserId: v })}
                users={opts.data?.users ?? []}
              />
              <TextField
                fullWidth
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                label="Hedef tarih"
                value={item.actionDueAtUtc}
                onChange={(e) =>
                  setItem({ ...item, actionDueAtUtc: e.target.value })
                }
              />
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setItemOpen(false)}>Vazgeç</Button>
          <Button variant="contained" onClick={() => add.mutate()}>
            Risk satırını ekle
          </Button>
        </DialogActions>
      </Dialog>
      <Dialog
        open={Boolean(decision)}
        onClose={() => setDecision(null)}
        fullWidth
        maxWidth="sm"
      >
        <Head title={decision?.label ?? ""} close={() => setDecision(null)} />
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            {decision?.kind === "residual" && (
              <Scores value={scores} change={setScores} />
            )}
            <TextField
              required
              multiline
              minRows={3}
              label={
                decision?.kind === "complete"
                  ? "Tamamlama kanıtı"
                  : "Karar / değerlendirme gerekçesi"
              }
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
            {decision?.signature && (
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
                  label="E-imza anlamını kabul ediyorum"
                />
              </>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDecision(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={
              !note || (decision?.signature && (!password || !accepted))
            }
            onClick={() => act.mutate()}
          >
            Onayla
          </Button>
        </DialogActions>
      </Dialog>
      <Snackbar
        open={Boolean(feedback)}
        autoHideDuration={4500}
        onClose={() => setFeedback(null)}
      >
        <Alert variant="filled" severity={feedback?.severity ?? "success"}>
          {feedback?.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
function RiskDialog({
  data,
  open,
  close,
  tab,
  setTab,
  add,
  decide,
}: {
  data: RiskDetails | undefined;
  open: boolean;
  close: () => void;
  tab: number;
  setTab: (v: number) => void;
  add: () => void;
  decide: (v: {
    kind: "transition" | "complete" | "residual";
    code?: string;
    item?: RiskItem;
    label: string;
    signature?: boolean;
  }) => void;
}) {
  return (
    <Dialog open={open} fullScreen onClose={close}>
      <Head
        title={
          data
            ? `${data.record.recordNumber} · ${data.record.process}`
            : "Yükleniyor"
        }
        close={close}
      />
      {data && (
        <>
          <Tabs value={tab} onChange={(_, v) => setTab(v)}>
            <Tab
              icon={<AnalyticsRounded />}
              iconPosition="start"
              label={`Riskler (${data.items.length})`}
            />
            <Tab
              icon={<TaskAltRounded />}
              iconPosition="start"
              label="Görev ve karar"
            />
            <Tab
              icon={<HistoryRounded />}
              iconPosition="start"
              label={`Geçmiş (${data.auditTrail.length})`}
            />
          </Tabs>
          <Box
            sx={{
              p: 3,
              overflow: "auto",
              background: "#f5f8f7",
              minHeight: "calc(100vh - 130px)",
            }}
          >
            {tab === 0 && (
              <Stack spacing={2}>
                <Paper sx={{ p: 2 }}>
                  {data.record.status === "Closed" && (
                    <Button
                      variant="outlined"
                      sx={{ float: "right" }}
                      onClick={() =>
                        void downloadRiskFinalReport(data.record.id)
                      }
                    >
                      Nihai PDF
                    </Button>
                  )}
                  <Typography variant="h5">{data.record.scope}</Typography>
                  <Typography>
                    Matris: {data.record.matrixVersion} · Aksiyon eşiği:{" "}
                    {data.record.actionThreshold} · Sahip: {data.record.owner} ·
                    Onaylayan: {data.record.approver}
                  </Typography>
                </Paper>
                {data.record.status === "Draft" && (
                  <Button
                    variant="contained"
                    startIcon={<AddRounded />}
                    onClick={add}
                  >
                    Risk satırı ekle
                  </Button>
                )}
                <Table component={Paper}>
                  <TableHead>
                    <TableRow>
                      <TableCell>Hata türü / neden / etki</TableCell>
                      <TableCell>S×O×D</TableCell>
                      <TableCell>Aksiyon / sorumlu</TableCell>
                      <TableCell>Kalıntı RPN</TableCell>
                      <TableCell>İşlem</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {data.items.map((i) => (
                      <TableRow key={i.id}>
                        <TableCell>
                          <b>{i.failureMode}</b>
                          <Typography variant="body2">
                            {i.cause} → {i.effect}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={`${i.severity}×${i.occurrence}×${i.detectability} = ${i.initialRpn}`}
                            color={
                              i.initialRpn >= data.record.actionThreshold
                                ? "error"
                                : "success"
                            }
                          />
                        </TableCell>
                        <TableCell>
                          {i.action ?? "Aksiyon gerekmiyor"}
                          <Typography
                            variant="caption"
                            sx={{ display: "block" }}
                          >
                            {i.actionOwner}
                          </Typography>
                        </TableCell>
                        <TableCell>{i.residualRpn ?? "—"}</TableCell>
                        <TableCell>
                          {data.record.status === "ActionPlan" &&
                            i.status === "ActionRequired" && (
                              <Button
                                onClick={() =>
                                  decide({
                                    kind: "complete",
                                    item: i,
                                    label: "Risk aksiyonunu tamamla",
                                  })
                                }
                              >
                                Kanıt gir
                              </Button>
                            )}
                          {data.record.status === "ResidualReview" &&
                            !i.residualRpn && (
                              <Button
                                onClick={() =>
                                  decide({
                                    kind: "residual",
                                    item: i,
                                    label: "Kalıntı riski değerlendir",
                                  })
                                }
                              >
                                Puanla
                              </Button>
                            )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </Stack>
            )}
            {tab === 1 && (
              <Stack spacing={2}>
                <Paper sx={{ p: 3 }}>
                  <Typography variant="h6">Sıradaki kontrollü adım</Typography>
                  <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
                    {data.availableTransitions.length ? (
                      data.availableTransitions.map((t) => (
                        <Button
                          variant="contained"
                          key={t.code}
                          onClick={() =>
                            decide({
                              kind: "transition",
                              code: t.code,
                              label: t.label,
                              signature: t.requiresSignature,
                            })
                          }
                        >
                          {t.label}
                        </Button>
                      ))
                    ) : (
                      <Alert severity="info">
                        Bu kullanıcı için kullanılabilir geçiş yok.
                      </Alert>
                    )}
                  </Stack>
                </Paper>
                <RecordAssignments
                  aggregateType="RiskAssessment"
                  aggregateId={data.record.id}
                />
              </Stack>
            )}
            {tab === 2 && (
              <Paper sx={{ p: 3 }}>
                {data.auditTrail.map((e) => (
                  <Box
                    key={e.id}
                    sx={{ py: 1.5, borderBottom: "1px solid #ddd" }}
                  >
                    <b>{e.eventType}</b> · {e.actor}
                    <Typography variant="caption" sx={{ display: "block" }}>
                      {new Date(e.occurredAtUtc).toLocaleString("tr-TR")}{" "}
                      {e.reason}
                    </Typography>
                  </Box>
                ))}
              </Paper>
            )}
          </Box>
        </>
      )}
    </Dialog>
  );
}
function Head({ title, close }: { title: string; close: () => void }) {
  return (
    <Box
      className="modal-header"
      sx={{
        p: 2,
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
      }}
    >
      <Typography variant="h5" sx={{ fontWeight: 800 }}>
        {title}
      </Typography>
      <IconButton onClick={close}>
        <CloseRounded />
      </IconButton>
    </Box>
  );
}
function Sel({
  label,
  value,
  options,
  change,
}: {
  label: string;
  value: string;
  options: Array<{ code: string; name: string }>;
  change: (v: string) => void;
}) {
  return (
    <TextField
      required
      select
      fullWidth
      label={label}
      value={value}
      onChange={(e) => change(e.target.value)}
    >
      {options.map((x) => (
        <MenuItem key={x.code} value={x.code}>
          {x.name}
        </MenuItem>
      ))}
    </TextField>
  );
}
function Person({
  label,
  value,
  users,
  change,
}: {
  label: string;
  value: string;
  users: Array<{ id: string; name: string; department: string | null }>;
  change: (v: string) => void;
}) {
  return (
    <SearchableSelect
      required
      label={label}
      value={value}
      onChange={(v) => change(v ?? "")}
      options={users.map((x) => ({
        value: x.id,
        label: x.name,
        secondary: x.department ?? "Bölüm yok",
      }))}
    />
  );
}
function Scores({
  value,
  change,
}: {
  value: { severity: number; occurrence: number; detectability: number };
  change: (v: {
    severity: number;
    occurrence: number;
    detectability: number;
  }) => void;
}) {
  return (
    <Stack direction="row" spacing={2}>
      {(["severity", "occurrence", "detectability"] as const).map((k, i) => (
        <TextField
          key={k}
          select
          fullWidth
          label={["Şiddet", "Olasılık", "Tespit edilebilirlik"][i]}
          value={value[k]}
          onChange={(e) => change({ ...value, [k]: Number(e.target.value) })}
        >
          {[1, 2, 3, 4, 5].map((n) => (
            <MenuItem key={n} value={n}>
              {n}
            </MenuItem>
          ))}
        </TextField>
      ))}
    </Stack>
  );
}
function RiskLookupRow({
  item,
  saved,
  failed,
}: {
  item: RiskLookup;
  saved: () => void;
  failed: (m: string) => void;
}) {
  const [name, setName] = useState(item.name);
  const [sortOrder, setSortOrder] = useState(item.sortOrder);
  const [isActive, setIsActive] = useState(item.isActive);
  const save = useMutation({
    mutationFn: () => updateRiskLookup(item.id, { name, sortOrder, isActive }),
    onSuccess: saved,
    onError: (e) => failed(e.message),
  });
  return (
    <Paper variant="outlined" sx={{ p: 1.5 }}>
      <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
        <Chip label={item.category} />
        <Typography sx={{ minWidth: 140, fontWeight: 700 }}>
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
        <Button onClick={() => save.mutate()}>Kaydet</Button>
      </Stack>
    </Paper>
  );
}
