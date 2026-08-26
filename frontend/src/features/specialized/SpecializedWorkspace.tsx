import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AddRounded,
  DescriptionRounded,
  FilterAltRounded,
  HistoryRounded,
  MoreHorizRounded,
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
import {
  createSpecialized,
  createSpecializedLookup,
  downloadSpecializedFinalReport,
  searchSpecialized,
  specializedDetails,
  specializedLookups,
  specializedOptions,
  transitionSpecialized,
  updateSpecializedLookup,
  type SpecializedDetails,
  type SpecializedOption,
  type SpecializedOptions,
  type SpecializedLookup,
} from "../../api/specializedRecords";
import { RecordAssignments } from "../../components/RecordAssignments";
import { SearchableSelect } from "../../components/SearchableSelect";
import { ModalHeader } from "../../components/ModalHeader";
import { Permissions, useAuth } from "../../security/AuthContext";

type Module = "m13" | "m14" | "m15" | "m16";
const configs = {
  m13: {
    code: "M.13",
    title: "Artwork yönetimi",
    intro:
      "Ambalaj tasarımlarını proof bütünlüğü, pazar/dil snapshot'ı ve üçlü e-imza ile kontrollü yayınlayın.",
    reference: "Artwork kodu",
    fields: [
      ["proofDocumentNumber", "Proof doküman numarası"],
      ["proofVersion", "Proof sürümü"],
      ["proofSha256", "Proof SHA-256 (64 karakter)"],
      ["barcode", "Barkod / GTIN"],
      ["regulatoryText", "Ruhsat metni referansı"],
    ],
  },
  m14: {
    code: "M.14",
    title: "Limit dışı durum yönetimi",
    intro:
      "OOS/OOT sonuçlarını hipotez, yetkili retest kontrolü ve bilimsel dispozisyonla yönetin.",
    reference: "Numune / batch referansı",
    fields: [
      ["specification", "Onaylı spesifikasyon"],
      ["observedResult", "Gözlenen sonuç"],
      ["unit", "Birim"],
      ["laboratoryInvestigation", "Laboratuvar araştırması"],
      ["hypothesis", "Bilimsel hipotez"],
      ["rootCause", "Doğrulanmış kök neden"],
      ["disposition", "Nihai batch / sonuç dispozisyonu"],
    ],
  },
  m15: {
    code: "M.15",
    title: "Farmakovijilans vaka yönetimi",
    intro:
      "Gizlilik kontrollü güvenlilik vakalarını tıbbi inceleme ve QPPV e-imzalarıyla yönetin.",
    reference: "Kaynak vaka referansı",
    fields: [
      ["source", "Vaka kaynağı"],
      ["patientCode", "Anonim hasta kodu"],
      ["eventTerm", "Advers olay / MedDRA terimi"],
      ["seriousness", "Ciddiyet"],
      ["expectedness", "Beklenirlik"],
      ["initialReceiptAtUtc", "İlk bilgi alınma zamanı"],
      ["regulatoryDueAtUtc", "Düzenleyici rapor son tarihi"],
    ],
  },
  m16: {
    code: "M.16",
    title: "Tedarikçi değerlendirme",
    intro:
      "Nitelendirme kararlarını dönem, kanıt ve puan snapshot'larıyla kontrollü olarak yönetin.",
    reference: "Tedarikçi kodu / adı",
    fields: [
      ["evaluationPeriod", "Değerlendirme dönemi"],
      ["score", "Toplam puan (0-100)"],
      ["qualityScore", "Kalite puanı"],
      ["deliveryScore", "Teslimat puanı"],
      ["evidence", "Değerlendirme kanıtı"],
      ["qualificationDecision", "Nitelendirme kararı"],
    ],
  },
} as const;
const empty = {
  title: "",
  typeCode: "",
  subjectCode: "",
  scopeCode: "",
  reference: "",
  description: "",
  dueAtUtc: "",
  ownerUserId: "",
  reviewerUserId: "",
  approverUserId: "",
};
const labels: Record<string, string> = {
  Draft: "Taslak",
  InReview: "İncelemede",
  Reviewed: "İncelendi",
  Approved: "Onaylandı",
  Closed: "Kapalı",
  Cancelled: "İptal",
};

export function SpecializedWorkspace({ module }: { module: Module }) {
  const { can } = useAuth();
  const cfg = configs[module];
  const qc = useQueryClient();
  const [params, setParams] = useSearchParams();
  const id = params.get("open");
  const [createOpen, setCreateOpen] = useState(false);
  const [lookupOpen, setLookupOpen] = useState(false);
  const [lookupDraft, setLookupDraft] = useState({
    category: "Type",
    code: "",
    name: "",
    sortOrder: 50,
  });
  const [form, setForm] = useState(empty);
  const [structured, setStructured] = useState<
    Record<string, string | boolean>
  >({});
  const [tab, setTab] = useState(0);
  const [transition, setTransition] = useState<{
    code: string;
    label: string;
    requiresSignature: boolean;
  } | null>(null);
  const [password, setPassword] = useState("");
  const [accepted, setAccepted] = useState(false);
  const [note, setNote] = useState("");
  const [feedback, setFeedback] = useState<{
    severity: "success" | "error";
    message: string;
  } | null>(null);
  const list = useQuery({
    queryKey: ["specialized", module],
    queryFn: ({ signal }) => searchSpecialized(module, signal),
  });
  const options = useQuery({
    queryKey: ["specialized-options", module],
    queryFn: ({ signal }) => specializedOptions(module, signal),
  });
  const details = useQuery({
    queryKey: ["specialized-details", module, id],
    queryFn: ({ signal }) => specializedDetails(module, id!, signal),
    enabled: Boolean(id),
  });
  const lookups = useQuery({
    queryKey: ["specialized-lookups", module],
    queryFn: ({ signal }) => specializedLookups(module, signal),
    enabled: lookupOpen,
  });
  const done = (message: string) => {
    void qc.invalidateQueries({ queryKey: ["specialized", module] });
    void qc.invalidateQueries({
      queryKey: ["specialized-details", module, id],
    });
    setFeedback({ severity: "success", message });
  };
  const fail = (e: Error) =>
    setFeedback({ severity: "error", message: e.message });
  const create = useMutation({
    mutationFn: () =>
      createSpecialized(module, {
        ...form,
        structuredData: normalizedData(module, structured),
        dueAtUtc: new Date(form.dueAtUtc).toISOString(),
      }),
    onSuccess: (x) => {
      setCreateOpen(false);
      setForm(empty);
      setStructured({});
      setParams({ open: x.record.id });
      done(`${cfg.code} kaydı oluşturuldu.`);
    },
    onError: fail,
  });
  const move = useMutation({
    mutationFn: () =>
      transitionSpecialized(module, id!, {
        expectedVersion: details.data!.record.version,
        transition: transition!.code,
        signaturePassword: password || null,
        signatureMeaningAccepted: accepted,
        note: note || null,
      }),
    onSuccess: () => {
      setTransition(null);
      setPassword("");
      setAccepted(false);
      setNote("");
      done("Kontrollü iş akışı güncellendi.");
    },
    onError: fail,
  });
  const addLookup = useMutation({
    mutationFn: () => createSpecializedLookup(module, lookupDraft),
    onSuccess: () => {
      setLookupDraft({ category: "Type", code: "", name: "", sortOrder: 50 });
      void qc.invalidateQueries({ queryKey: ["specialized-lookups", module] });
      void qc.invalidateQueries({ queryKey: ["specialized-options", module] });
      done("Yönetilebilir tanım eklendi.");
    },
    onError: fail,
  });
  const toggleLookup = useMutation({
    mutationFn: (x: SpecializedLookup) =>
      updateSpecializedLookup(module, x.id, {
        name: x.name,
        sortOrder: x.sortOrder,
        isActive: !x.isActive,
      }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["specialized-lookups", module] });
      void qc.invalidateQueries({ queryKey: ["specialized-options", module] });
      done("Tanım durumu güncellendi.");
    },
    onError: fail,
  });
  return (
    <Box className="module-workspace">
      <Paper className="module-hero">
        <Box>
          <Chip label={`${cfg.code} aktif`} />
          <Typography variant="h2">{cfg.title}</Typography>
          <Typography>{cfg.intro}</Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          {can(Permissions.administrationManage) && (
            <Button
              variant="outlined"
              startIcon={<SettingsRounded />}
              onClick={() => setLookupOpen(true)}
            >
              Tanımlar
            </Button>
          )}
          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={() => setCreateOpen(true)}
          >
            Yeni kayıt
          </Button>
        </Stack>
      </Paper>
      <Stack className="module-list-toolbar" direction="row" spacing={1.5}>
        <Button component="span" variant="outlined" startIcon={<FilterAltRounded />}>
          Tüm kayıtlar
        </Button>
        <Typography variant="body2">
          {list.data ? `${list.data.totalCount} kayıt` : "Kayıtlar yükleniyor"}
        </Typography>
      </Stack>
      <Paper className="module-content-card">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Kayıt</TableCell>
              <TableCell>Başlık / konu</TableCell>
              <TableCell>Tür / kapsam</TableCell>
              <TableCell>Sorumlu</TableCell>
              <TableCell>Durum</TableCell>
              <TableCell>Hedef</TableCell>
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
                  <b>{x.title}</b>
                  <Typography variant="caption" sx={{ display: "block" }}>
                    {x.subjectName}
                  </Typography>
                </TableCell>
                <TableCell>
                  {x.typeName} · {x.scopeName}
                </TableCell>
                <TableCell>{x.owner}</TableCell>
                <TableCell>
                  <Chip
                    label={labels[x.status] ?? x.status}
                    color={x.status === "Closed" ? "success" : "default"}
                  />
                </TableCell>
                <TableCell>
                  {new Date(x.dueAtUtc).toLocaleDateString("tr-TR")}
                </TableCell>
                <TableCell align="right">
                  <Button
                    className="module-row-actions"
                    variant="outlined"
                    startIcon={<MoreHorizRounded />}
                    onClick={() => setParams({ open: x.id })}
                  >
                    İşlemler
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      <CreateDialog
        open={createOpen}
        close={() => setCreateOpen(false)}
        cfg={cfg}
        form={form}
        setForm={setForm}
        structured={structured}
        setStructured={setStructured}
        options={options.data}
        save={() => create.mutate()}
      />
      <LookupDialog
        open={lookupOpen}
        close={() => setLookupOpen(false)}
        cfg={cfg}
        rows={lookups.data ?? []}
        draft={lookupDraft}
        setDraft={setLookupDraft}
        add={() => addLookup.mutate()}
        toggle={(x) => toggleLookup.mutate(x)}
      />
      <DetailsDialog
        open={Boolean(id)}
        close={() => setParams({})}
        data={details.data}
        cfg={cfg}
        tab={tab}
        setTab={setTab}
        act={setTransition}
        download={() => downloadSpecializedFinalReport(module, id!).catch(fail)}
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
            <TextField
              label="Karar ve gerekçe notu"
              multiline
              minRows={3}
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
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
                  label="E-imza anlamını kabul ediyorum"
                />
              </>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTransition(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={transition?.requiresSignature && (!password || !accepted)}
            onClick={() => move.mutate()}
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
function normalizedData(
  module: Module,
  data: Record<string, string | boolean>,
) {
  if (module === "m16")
    return {
      ...data,
      score: Number(data.score ?? 0),
      qualityScore: Number(data.qualityScore ?? 0),
      deliveryScore: Number(data.deliveryScore ?? 0),
    };
  if (module === "m14")
    return {
      ...data,
      retestPerformed: Boolean(data.retestPerformed),
      retestAuthorized: Boolean(data.retestAuthorized),
    };
  return data;
}
function CreateDialog({
  open,
  close,
  cfg,
  form,
  setForm,
  structured,
  setStructured,
  options,
  save,
}: {
  open: boolean;
  close: () => void;
  cfg: (typeof configs)[Module];
  form: typeof empty;
  setForm: (x: typeof empty) => void;
  structured: Record<string, string | boolean>;
  setStructured: (x: Record<string, string | boolean>) => void;
  options?: SpecializedOptions;
  save: () => void;
}) {
  const users =
    options?.users.map((x) => ({
      value: x.id,
      label: `${x.name}${x.department ? ` · ${x.department}` : ""}`,
    })) ?? [];
  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="lg">
      <Header title={`Yeni ${cfg.code} kaydı`} close={close} />
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 2 }}>
          <TextField
            required
            label="Kayıt başlığı"
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
          />
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <Select
              label="Kayıt türü"
              value={form.typeCode}
              options={options?.types ?? []}
              change={(v) => setForm({ ...form, typeCode: v })}
            />
            <Select
              label="Konu / ürün"
              value={form.subjectCode}
              options={options?.subjects ?? []}
              change={(v) => setForm({ ...form, subjectCode: v })}
            />
            <Select
              label="Kapsam"
              value={form.scopeCode}
              options={options?.scopes ?? []}
              change={(v) => setForm({ ...form, scopeCode: v })}
            />
          </Stack>
          <TextField
            required
            label={cfg.reference}
            value={form.reference}
            onChange={(e) => setForm({ ...form, reference: e.target.value })}
          />
          <TextField
            required
            multiline
            minRows={3}
            label="Tanım ve değerlendirme kapsamı"
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "1fr",
                md: "repeat(2, minmax(0, 1fr))",
              },
              gap: 2,
            }}
          >
            {cfg.fields.map(([key, label]) =>
              key === "seriousness" ? (
                <TextField
                  key={key}
                  required
                  select
                  label={label}
                  value={String(structured[key] ?? "")}
                  onChange={(e) =>
                    setStructured({ ...structured, [key]: e.target.value })
                  }
                >
                  <MenuItem value="FatalOrLifeThreatening">
                    Ölümcül / yaşamı tehdit eden · 7 gün
                  </MenuItem>
                  <MenuItem value="Serious">Ciddi · 15 gün</MenuItem>
                  <MenuItem value="NonSerious">Ciddi olmayan · 90 gün</MenuItem>
                </TextField>
              ) : key === "qualificationDecision" ? (
                <TextField
                  key={key}
                  required
                  select
                  label={label}
                  value={String(structured[key] ?? "")}
                  onChange={(e) =>
                    setStructured({ ...structured, [key]: e.target.value })
                  }
                >
                  <MenuItem value="Approved">Onaylı · 80-100</MenuItem>
                  <MenuItem value="Conditional">Koşullu · 60-79</MenuItem>
                  <MenuItem value="Disqualified">Uygun değil · 0-59</MenuItem>
                </TextField>
              ) : (
                <TextField
                  key={key}
                  required
                  fullWidth
                  type={
                    key.endsWith("AtUtc")
                      ? "datetime-local"
                      : key.toLowerCase().includes("score")
                        ? "number"
                        : "text"
                  }
                  slotProps={
                    key.endsWith("AtUtc")
                      ? { inputLabel: { shrink: true } }
                      : undefined
                  }
                  label={label}
                  value={String(structured[key] ?? "")}
                  onChange={(e) =>
                    setStructured({ ...structured, [key]: e.target.value })
                  }
                />
              ),
            )}
          </Box>
          {cfg.code === "M.14" && (
            <Stack direction="row">
              <FormControlLabel
                control={
                  <Checkbox
                    checked={Boolean(structured.retestAuthorized)}
                    onChange={(e) =>
                      setStructured({
                        ...structured,
                        retestAuthorized: e.target.checked,
                      })
                    }
                  />
                }
                label="Tekrar test bilimsel hipotezle yetkilendirildi"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={Boolean(structured.retestPerformed)}
                    onChange={(e) =>
                      setStructured({
                        ...structured,
                        retestPerformed: e.target.checked,
                      })
                    }
                  />
                }
                label="Tekrar test yapıldı"
              />
            </Stack>
          )}
          <TextField
            required
            type="datetime-local"
            slotProps={{ inputLabel: { shrink: true } }}
            label="Hedef tarih"
            value={form.dueAtUtc}
            onChange={(e) => setForm({ ...form, dueAtUtc: e.target.value })}
          />
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <SearchableSelect
              label="Sorumlu"
              value={form.ownerUserId}
              options={users}
              onChange={(v) => setForm({ ...form, ownerUserId: v ?? "" })}
            />
            <SearchableSelect
              label="Bağımsız inceleyen"
              value={form.reviewerUserId}
              options={users}
              onChange={(v) => setForm({ ...form, reviewerUserId: v ?? "" })}
            />
            <SearchableSelect
              label="Onaylayan"
              value={form.approverUserId}
              options={users}
              onChange={(v) => setForm({ ...form, approverUserId: v ?? "" })}
            />
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>Vazgeç</Button>
        <Button variant="contained" onClick={save}>
          Kaydı oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}
function DetailsDialog({
  open,
  close,
  data,
  cfg,
  tab,
  setTab,
  act,
  download,
}: {
  open: boolean;
  close: () => void;
  data?: SpecializedDetails;
  cfg: (typeof configs)[Module];
  tab: number;
  setTab: (x: number) => void;
  act: (x: { code: string; label: string; requiresSignature: boolean }) => void;
  download: () => void;
}) {
  return (
    <Dialog
      open={open}
      onClose={close}
      maxWidth="lg"
      fullWidth
      className="record-details-dialog"
      slotProps={{ paper: { className: "record-details-paper" } }}
    >
      <Header
        title={
          data
            ? `${data.record.recordNumber} · ${data.record.title}`
            : "Yükleniyor"
        }
        close={close}
      />
      {data && (
        <Box className="record-detail-workspace">
          <Paper elevation={0} square className="record-detail-tabs-shell">
            <Tabs
              value={tab}
              onChange={(_, v) => setTab(v)}
              orientation="vertical"
            >
              <Tab
                icon={<DescriptionRounded />}
                iconPosition="start"
                label="Kayıt özeti"
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
          </Paper>
          <Box className="detail-tab-panel">
            <Box
              sx={{
                p: 0,
                overflow: "auto",
                background: "#f5f8f7",
                minHeight: "calc(100vh - 130px)",
              }}
            >
              {tab === 0 && (
                <Stack spacing={2}>
                  <Paper sx={{ p: 3 }}>
                    <Chip
                      label={labels[data.record.status] ?? data.record.status}
                    />
                    <Typography variant="h4" sx={{ mt: 1 }}>
                      {data.record.title}
                    </Typography>
                    <Typography color="text.secondary">
                      {data.record.typeName} · {data.record.subjectName} ·{" "}
                      {data.record.scopeName}
                    </Typography>
                    <Typography sx={{ mt: 2 }}>
                      {data.record.description}
                    </Typography>
                    {data.record.status === "Closed" && (
                      <Button
                        sx={{ mt: 2 }}
                        variant="outlined"
                        onClick={download}
                      >
                        İmzalı nihai PDF
                      </Button>
                    )}
                  </Paper>
                  <Paper sx={{ p: 3 }}>
                    <Typography variant="h6">
                      Kontrollü veri snapshot'ı
                    </Typography>
                    <Table>
                      <TableBody>
                        {Object.entries(data.record.structuredData).map(
                          ([key, value]) => (
                            <TableRow key={key}>
                              <TableCell>
                                <b>
                                  {cfg.fields.find((x) => x[0] === key)?.[1] ??
                                    key}
                                </b>
                              </TableCell>
                              <TableCell>{String(value)}</TableCell>
                            </TableRow>
                          ),
                        )}
                      </TableBody>
                    </Table>
                  </Paper>
                  <Paper sx={{ p: 3 }}>
                    <Typography variant="h6">Elektronik imzalar</Typography>
                    {data.signatures.map((x) => (
                      <Typography key={x.id} sx={{ py: 1 }}>
                        {x.meaning} · <b>{x.signer}</b> ·{" "}
                        {new Date(x.signedAtUtc).toLocaleString("tr-TR")}
                      </Typography>
                    ))}
                  </Paper>
                </Stack>
              )}
              {tab === 1 && (
                <Stack spacing={2}>
                  <Paper sx={{ p: 3 }}>
                    <Typography variant="h6">
                      Sıradaki kontrollü adım
                    </Typography>
                    <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
                      {data.availableTransitions.map((x) => (
                        <Button
                          variant="contained"
                          key={x.code}
                          onClick={() => act(x)}
                        >
                          {x.label}
                        </Button>
                      ))}
                    </Stack>
                  </Paper>
                  <RecordAssignments
                    aggregateType="SpecializedRecord"
                    aggregateId={data.record.id}
                  />
                </Stack>
              )}
              {tab === 2 && (
                <Paper sx={{ p: 3 }}>
                  {data.auditTrail.map((x) => (
                    <Box
                      key={x.id}
                      sx={{ py: 1.5, borderBottom: "1px solid #ddd" }}
                    >
                      <b>
                        v{x.version} · {x.eventType}
                      </b>{" "}
                      · {x.actor}
                      <Typography variant="caption" sx={{ display: "block" }}>
                        {new Date(x.occurredAtUtc).toLocaleString("tr-TR")}{" "}
                        {x.reason}
                      </Typography>
                    </Box>
                  ))}
                </Paper>
              )}
            </Box>
          </Box>
        </Box>
      )}
    </Dialog>
  );
}
function LookupDialog({
  open,
  close,
  cfg,
  rows,
  draft,
  setDraft,
  add,
  toggle,
}: {
  open: boolean;
  close: () => void;
  cfg: (typeof configs)[Module];
  rows: SpecializedLookup[];
  draft: { category: string; code: string; name: string; sortOrder: number };
  setDraft: (x: {
    category: string;
    code: string;
    name: string;
    sortOrder: number;
  }) => void;
  add: () => void;
  toggle: (x: SpecializedLookup) => void;
}) {
  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="md">
      <Header title={`${cfg.code} yönetilebilir tanımlar`} close={close} />
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 2 }}>
          <Alert severity="info">
            Kod kayıt snapshot'larında değişmez; ad ve aktiflik yeni kayıtların
            seçim listesini etkiler.
          </Alert>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
            <TextField
              select
              label="Kategori"
              value={draft.category}
              onChange={(e) => setDraft({ ...draft, category: e.target.value })}
            >
              {["Type", "Subject", "Scope"].map((x) => (
                <MenuItem key={x} value={x}>
                  {x === "Type"
                    ? "Tür"
                    : x === "Subject"
                      ? "Konu / ürün"
                      : "Kapsam"}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              required
              label="Kod"
              value={draft.code}
              onChange={(e) => setDraft({ ...draft, code: e.target.value })}
            />
            <TextField
              required
              fullWidth
              label="Görünen ad"
              value={draft.name}
              onChange={(e) => setDraft({ ...draft, name: e.target.value })}
            />
            <TextField
              type="number"
              label="Sıra"
              value={draft.sortOrder}
              onChange={(e) =>
                setDraft({ ...draft, sortOrder: Number(e.target.value) })
              }
            />
            <Button
              variant="contained"
              disabled={!draft.code.trim() || !draft.name.trim()}
              onClick={add}
            >
              Ekle
            </Button>
          </Stack>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Kategori</TableCell>
                <TableCell>Kod</TableCell>
                <TableCell>Ad</TableCell>
                <TableCell>Durum</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((x) => (
                <TableRow key={x.id}>
                  <TableCell>{x.category}</TableCell>
                  <TableCell>
                    <b>{x.code}</b>
                  </TableCell>
                  <TableCell>{x.name}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      color={x.isActive ? "success" : "default"}
                      label={x.isActive ? "Aktif" : "Pasif"}
                    />
                  </TableCell>
                  <TableCell>
                    <Button onClick={() => toggle(x)}>
                      {x.isActive ? "Pasifleştir" : "Etkinleştir"}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button variant="contained" onClick={close}>
          Kapat
        </Button>
      </DialogActions>
    </Dialog>
  );
}
function Select({
  label,
  value,
  options,
  change,
}: {
  label: string;
  value: string;
  options: readonly SpecializedOption[];
  change: (x: string) => void;
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
function Header({ title, close }: { title: string; close: () => void }) {
  return (
    <ModalHeader onClose={close}>
      <Typography variant="h4" className="record-header-title">
        {title}
      </Typography>
    </ModalHeader>
  );
}
