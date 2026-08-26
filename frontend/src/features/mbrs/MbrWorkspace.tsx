import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import {
  AddRounded,
  CloseRounded,
  DescriptionRounded,
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
  addMbrStep,
  createMbr,
  downloadMbrFinalReport,
  mbrDetails,
  mbrOptions,
  searchMbrs,
  transitionMbr,
  type MbrDetails,
} from "../../api/mbrs";
import { SearchableSelect } from "../../components/SearchableSelect";
import { RecordAssignments } from "../../components/RecordAssignments";
import { RecordActionMenu } from "../../components/RecordActionMenu";
const status: Record<string, string> = {
  Draft: "Taslak",
  InReview: "İncelemede",
  Reviewed: "İncelendi",
  Approved: "Onaylandı",
  Effective: "Yürürlükte",
  Superseded: "Yerine yenisi yayınlandı",
  Archived: "Arşiv",
};
const empty = {
  previousVersionId: null,
  productCode: "",
  dosageFormCode: "",
  strength: "",
  batchSize: 0,
  batchUnitCode: "",
  siteCode: "",
  lineCode: "",
  documentVersion: "",
  changeReason: "",
  authorUserId: "",
  reviewerUserId: "",
  approverUserId: "",
};
const stepEmpty = {
  order: 1,
  phaseCode: "",
  instruction: "",
  materialOrEquipmentReference: "",
  isCritical: false,
  parameter: "",
  lowerLimit: "",
  upperLimit: "",
  unitCode: "",
};
export function MbrWorkspace() {
  const qc = useQueryClient();
  const [params, setParams] = useSearchParams();
  const id = params.get("open");
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState(empty);
  const [stepOpen, setStepOpen] = useState(false);
  const [step, setStep] = useState(stepEmpty);
  const [tab, setTab] = useState(0);
  const [transition, setTransition] = useState<{
    code: string;
    label: string;
    requiresSignature: boolean;
  } | null>(null);
  const [note, setNote] = useState("");
  const [effective, setEffective] = useState("");
  const [password, setPassword] = useState("");
  const [accepted, setAccepted] = useState(false);
  const [feedback, setFeedback] = useState<{
    severity: "success" | "error";
    message: string;
  } | null>(null);
  const list = useQuery({
    queryKey: ["mbrs"],
    queryFn: ({ signal }) => searchMbrs(signal),
  });
  const opts = useQuery({
    queryKey: ["mbr-options"],
    queryFn: ({ signal }) => mbrOptions(signal),
  });
  const details = useQuery({
    queryKey: ["mbr", id],
    queryFn: ({ signal }) => mbrDetails(id!, signal),
    enabled: Boolean(id),
  });
  const done = (m: string) => {
    void qc.invalidateQueries({ queryKey: ["mbrs"] });
    void qc.invalidateQueries({ queryKey: ["mbr", id] });
    setFeedback({ severity: "success", message: m });
  };
  const fail = (e: Error) =>
    setFeedback({ severity: "error", message: e.message });
  const create = useMutation({
    mutationFn: () => createMbr(form),
    onSuccess: (x) => {
      setCreateOpen(false);
      setForm(empty);
      setParams({ open: x.record.id });
      done("MBR kaydı oluşturuldu.");
    },
    onError: fail,
  });
  const add = useMutation({
    mutationFn: () =>
      addMbrStep(id!, {
        ...step,
        expectedVersion: details.data!.record.version,
        materialOrEquipmentReference: step.materialOrEquipmentReference || null,
        parameter: step.parameter || null,
        lowerLimit: step.lowerLimit === "" ? null : Number(step.lowerLimit),
        upperLimit: step.upperLimit === "" ? null : Number(step.upperLimit),
        unitCode: step.unitCode || null,
      }),
    onSuccess: () => {
      setStepOpen(false);
      setStep({ ...stepEmpty, order: (details.data?.steps.length ?? 0) + 2 });
      done("Kontrollü işlem adımı eklendi.");
    },
    onError: fail,
  });
  const move = useMutation({
    mutationFn: () =>
      transitionMbr(id!, {
        expectedVersion: details.data!.record.version,
        transition: transition!.code,
        effectiveAtUtc: effective ? new Date(effective).toISOString() : null,
        signaturePassword: password || null,
        signatureMeaningAccepted: accepted,
        note: note || null,
      }),
    onSuccess: () => {
      setTransition(null);
      setNote("");
      setPassword("");
      setAccepted(false);
      done("MBR iş akışı güncellendi.");
    },
    onError: fail,
  });
  return (
    <Box className="module-workspace">
      <Paper className="module-hero">
        <Box>
          <Chip label="M.12 aktif" />
          <Typography variant="h2">Master Batch Record yönetimi</Typography>
          <Typography>
            Üretim ana talimatlarını kontrollü adımlar, kritik proses limitleri,
            görev ayrılığı ve sürüm zinciriyle yönetin.
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddRounded />}
          onClick={() => setCreateOpen(true)}
        >
          Yeni MBR
        </Button>
      </Paper>
      <Paper className="module-content-card">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Kayıt</TableCell>
              <TableCell>Ürün / sürüm</TableCell>
              <TableCell>Tesis / hat</TableCell>
              <TableCell>Yazar</TableCell>
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
                  <b>{x.productName}</b>
                  <Typography variant="caption" sx={{ display: "block" }}>
                    {x.documentVersion}
                  </Typography>
                </TableCell>
                <TableCell>
                  {x.siteName} · {x.lineName}
                </TableCell>
                <TableCell>{x.author}</TableCell>
                <TableCell>
                  <Chip
                    label={status[x.status] ?? x.status}
                    color={x.status === "Effective" ? "success" : "default"}
                  />
                </TableCell>
                <TableCell>
                  <RecordActionMenu onOpen={() => setParams({ open: x.id })} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      <Create
        open={createOpen}
        close={() => setCreateOpen(false)}
        form={form}
        set={setForm}
        opts={opts.data}
        save={() => create.mutate()}
      />
      <Details
        open={Boolean(id)}
        close={() => setParams({})}
        data={details.data}
        tab={tab}
        setTab={setTab}
        add={() => setStepOpen(true)}
        act={setTransition}
        download={() =>
          downloadMbrFinalReport(id!).catch((e: Error) => fail(e))
        }
      />
      <Dialog
        open={stepOpen}
        onClose={() => setStepOpen(false)}
        fullWidth
        maxWidth="md"
      >
        <Head
          title="Kontrollü MBR işlem adımı"
          close={() => setStepOpen(false)}
        />
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <Stack direction="row" spacing={2}>
              <TextField
                type="number"
                label="Sıra"
                value={step.order}
                onChange={(e) =>
                  setStep({ ...step, order: Number(e.target.value) })
                }
              />
              <Select
                label="Faz"
                value={step.phaseCode}
                options={opts.data?.phases ?? []}
                change={(v) => setStep({ ...step, phaseCode: v })}
              />
            </Stack>
            <TextField
              required
              multiline
              minRows={4}
              label="Uygulama talimatı"
              value={step.instruction}
              onChange={(e) =>
                setStep({ ...step, instruction: e.target.value })
              }
            />
            <TextField
              label="Malzeme / ekipman / SOP referansı"
              value={step.materialOrEquipmentReference}
              onChange={(e) =>
                setStep({
                  ...step,
                  materialOrEquipmentReference: e.target.value,
                })
              }
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={step.isCritical}
                  onChange={(e) =>
                    setStep({ ...step, isCritical: e.target.checked })
                  }
                />
              }
              label="Kritik proses parametresi"
            />
            {step.isCritical && (
              <Stack direction="row" spacing={2}>
                <TextField
                  fullWidth
                  label="Parametre"
                  value={step.parameter}
                  onChange={(e) =>
                    setStep({ ...step, parameter: e.target.value })
                  }
                />
                <TextField
                  type="number"
                  label="Alt limit"
                  value={step.lowerLimit}
                  onChange={(e) =>
                    setStep({ ...step, lowerLimit: e.target.value })
                  }
                />
                <TextField
                  type="number"
                  label="Üst limit"
                  value={step.upperLimit}
                  onChange={(e) =>
                    setStep({ ...step, upperLimit: e.target.value })
                  }
                />
                <Select
                  label="Birim"
                  value={step.unitCode}
                  options={opts.data?.parameterUnits ?? []}
                  change={(v) => setStep({ ...step, unitCode: v })}
                />
              </Stack>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setStepOpen(false)}>Vazgeç</Button>
          <Button variant="contained" onClick={() => add.mutate()}>
            Adımı ekle
          </Button>
        </DialogActions>
      </Dialog>
      <Dialog
        open={Boolean(transition)}
        onClose={() => setTransition(null)}
        fullWidth
        maxWidth="sm"
      >
        <Head
          title={transition?.label ?? ""}
          close={() => setTransition(null)}
        />
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 2 }}>
            {transition?.code === "make-effective" && (
              <TextField
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                label="Yürürlük tarihi"
                value={effective}
                onChange={(e) => setEffective(e.target.value)}
              />
            )}
            <TextField
              multiline
              minRows={3}
              label="Karar notu"
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
            {transition?.requiresSignature && (
              <>
                <TextField
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
function Create({
  open,
  close,
  form,
  set,
  opts,
  save,
}: {
  open: boolean;
  close: () => void;
  form: typeof empty;
  set: (x: typeof empty) => void;
  opts: ReturnType<typeof useQuery>["data"] | any;
  save: () => void;
}) {
  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="md">
      <Head title="Yeni Master Batch Record" close={close} />
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 2 }}>
          <Stack direction="row" spacing={2}>
            <Select
              label="Ürün"
              value={form.productCode}
              options={opts?.products ?? []}
              change={(v) => set({ ...form, productCode: v })}
            />
            <Select
              label="Dozaj şekli"
              value={form.dosageFormCode}
              options={opts?.dosageForms ?? []}
              change={(v) => set({ ...form, dosageFormCode: v })}
            />
            <TextField
              fullWidth
              label="Doz / güç"
              value={form.strength}
              onChange={(e) => set({ ...form, strength: e.target.value })}
            />
          </Stack>
          <Stack direction="row" spacing={2}>
            <TextField
              type="number"
              label="Standart batch miktarı"
              value={form.batchSize}
              onChange={(e) =>
                set({ ...form, batchSize: Number(e.target.value) })
              }
            />
            <Select
              label="Batch birimi"
              value={form.batchUnitCode}
              options={opts?.batchUnits ?? []}
              change={(v) => set({ ...form, batchUnitCode: v })}
            />
            <Select
              label="Tesis"
              value={form.siteCode}
              options={opts?.sites ?? []}
              change={(v) => set({ ...form, siteCode: v })}
            />
            <Select
              label="Hat"
              value={form.lineCode}
              options={opts?.lines ?? []}
              change={(v) => set({ ...form, lineCode: v })}
            />
          </Stack>
          <TextField
            label="Doküman sürümü"
            value={form.documentVersion}
            onChange={(e) => set({ ...form, documentVersion: e.target.value })}
          />
          <TextField
            multiline
            label="Oluşturma / değişiklik gerekçesi"
            value={form.changeReason}
            onChange={(e) => set({ ...form, changeReason: e.target.value })}
          />
          <Stack direction="row" spacing={2}>
            <Person
              label="Yazar"
              value={form.authorUserId}
              users={opts?.users ?? []}
              change={(v) => set({ ...form, authorUserId: v })}
            />
            <Person
              label="Teknik inceleyen"
              value={form.reviewerUserId}
              users={opts?.users ?? []}
              change={(v) => set({ ...form, reviewerUserId: v })}
            />
            <Person
              label="Kalite onaylayanı"
              value={form.approverUserId}
              users={opts?.users ?? []}
              change={(v) => set({ ...form, approverUserId: v })}
            />
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>Vazgeç</Button>
        <Button variant="contained" onClick={save}>
          Oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}
function Details({
  open,
  close,
  data,
  tab,
  setTab,
  add,
  act,
  download,
}: {
  open: boolean;
  close: () => void;
  data: MbrDetails | undefined;
  tab: number;
  setTab: (n: number) => void;
  add: () => void;
  act: (t: { code: string; label: string; requiresSignature: boolean }) => void;
  download: () => void;
}) {
  return (
    <Dialog
      open={open}
      onClose={close}
      fullWidth
      maxWidth="xl"
      slotProps={{ paper: { className: "record-details-paper m01-aligned-details" } }}
    >
      <Head
        title={
          data
            ? `${data.record.recordNumber} · ${data.record.productName} · ${data.record.documentVersion}`
            : "Yükleniyor"
        }
        close={close}
      />
      {data && (
        <>
          <Tabs value={tab} onChange={(_, v) => setTab(v)}>
            <Tab
              icon={<DescriptionRounded />}
              iconPosition="start"
              label={`Ana talimat (${data.steps.length})`}
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
                <Paper sx={{ p: 3 }}>
                  <Typography variant="h5">
                    {data.record.productName} · {data.record.strength}
                  </Typography>
                  <Typography>
                    {data.record.batchSize} {data.record.batchUnitName} ·{" "}
                    {data.record.siteName} / {data.record.lineName}
                  </Typography>
                  <Typography color="text.secondary">
                    Değişiklik gerekçesi: {data.record.changeReason}
                  </Typography>
                  {["Effective", "Superseded", "Archived"].includes(
                    data.record.status,
                  ) && (
                    <Button
                      sx={{ mt: 2 }}
                      variant="outlined"
                      onClick={download}
                    >
                      İmzalı nihai PDF
                    </Button>
                  )}
                </Paper>
                {data.record.status === "Draft" && (
                  <Button variant="contained" onClick={add}>
                    Kontrollü adım ekle
                  </Button>
                )}
                <Table component={Paper}>
                  <TableHead>
                    <TableRow>
                      <TableCell>Sıra / faz</TableCell>
                      <TableCell>Talimat</TableCell>
                      <TableCell>Referans</TableCell>
                      <TableCell>Kritik parametre / limit</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {data.steps.map((s) => (
                      <TableRow key={s.id}>
                        <TableCell>
                          {s.order} · {s.phaseName}
                        </TableCell>
                        <TableCell>{s.instruction}</TableCell>
                        <TableCell>
                          {s.materialOrEquipmentReference ?? "—"}
                        </TableCell>
                        <TableCell>
                          {s.isCritical
                            ? `${s.parameter}: ${s.lowerLimit}–${s.upperLimit} ${s.unitName}`
                            : "Kritik değil"}
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
                    {data.availableTransitions.map((t) => (
                      <Button
                        variant="contained"
                        key={t.code}
                        onClick={() => act(t)}
                      >
                        {t.label}
                      </Button>
                    ))}
                  </Stack>
                </Paper>
                <RecordAssignments
                  aggregateType="MasterBatchRecord"
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
function Select({
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
      label={label}
      required
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
