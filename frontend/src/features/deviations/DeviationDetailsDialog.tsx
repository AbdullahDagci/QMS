import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogContent,
  FormControlLabel,
  LinearProgress,
  Paper,
  Stack,
  Step,
  StepLabel,
  Stepper,
  Tab,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import {
  AssignmentIndRounded,
  AccountTreeRounded,
  BusinessRounded,
  BoltRounded,
  CategoryRounded,
  DescriptionRounded,
  DownloadRounded,
  EventRounded,
  GppMaybeRounded,
  HistoryRounded,
  Inventory2Rounded,
  ManageSearchRounded,
  PrecisionManufacturingRounded,
  TimelineRounded,
  TaskAltRounded,
  WarningAmberRounded,
} from "@mui/icons-material";
import type { SvgIconComponent } from "@mui/icons-material";
import {
  addDeviationBatchImpact,
  addDeviationInvestigation,
  downloadDeviationFinalReport,
  getDeviationDetails,
  transitionDeviation,
  type DeviationDetails,
} from "../../api/deviations";
import {
  SearchableSelect,
  type SelectOption,
} from "../../components/SearchableSelect";
import { ModalHeader } from "../../components/ModalHeader";
import { ElectronicSignaturePanel } from "../../components/ElectronicSignaturePanel";
import { ElectronicSignatureVerificationBadge } from "../../components/ElectronicSignatureVerificationBadge";
import { AuditTimeline } from "../../components/AuditTimeline";
import { RecordAssignments } from "../../components/RecordAssignments";
import { CapaCreateDialog } from "../capas/CapaWorkspace";
import { Permissions, useAuth } from "../../security/AuthContext";
import { getWorkflowAssignments } from "../../api/access";

const flow = [
  ["Submitted", "Gönderildi"],
  ["PreliminaryReview", "Ön inceleme"],
  ["Investigation", "Araştırma"],
  ["ImpactAssessment", "Etki"],
  ["QualityAssessment", "KG değerlendirmesi"],
  ["ActionImplementation", "Aksiyon"],
  ["EffectivenessReview", "Etkinlik"],
  ["ClosureApproval", "Kapanış onayı"],
  ["Closed", "Kapalı"],
] as const;

const eventLabels: Record<string, string> = {
  DeviationCreated: "Sapma taslağı oluşturuldu",
  DeviationSubmitted: "Sapma iş akışına gönderildi",
  DeviationStatusChanged: "Durum değiştirildi",
  DeviationInvestigationCompleted: "Kök neden araştırması eklendi",
  DeviationBatchImpactAssessed: "Batch/seri etkisi değerlendirildi",
};

const dispositionOptions: Array<SelectOption<string>> = [
  { value: "Pending", label: "Karar bekliyor" },
  { value: "Release", label: "Serbest bırak" },
  { value: "Hold", label: "Beklet" },
  { value: "Reject", label: "Reddet" },
  { value: "NotApplicable", label: "Uygulanamaz" },
];

export function DeviationDetailsDialog({
  id,
  onClose,
}: {
  id: string | null;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const { can, user } = useAuth();
  const [note, setNote] = useState("");
  const [effectivenessRequired, setEffectivenessRequired] = useState(false);
  const [isEffective, setIsEffective] = useState(true);
  const [signaturePassword, setSignaturePassword] = useState("");
  const [signatureMeaningAccepted, setSignatureMeaningAccepted] =
    useState(false);
  const [activeTab, setActiveTab] = useState(0);
  const [capaDialogOpen, setCapaDialogOpen] = useState(false);
  const close = () => {
    setActiveTab(0);
    onClose();
  };
  const details = useQuery({
    queryKey: ["deviation-details", id],
    queryFn: ({ signal }) => getDeviationDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const assignments = useQuery({
    queryKey: ["workflow-assignments", "Deviation", id],
    queryFn: ({ signal }) => getWorkflowAssignments("Deviation", id!, signal),
    enabled: Boolean(id),
    retry: false,
  });
  const updateDetails = async (data: DeviationDetails) => {
    queryClient.setQueryData(["deviation-details", id], data);
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["deviations"] }),
      queryClient.invalidateQueries({
        queryKey: ["workflow-assignments", "Deviation", id],
      }),
    ]);
    setNote("");
    setSignaturePassword("");
    setSignatureMeaningAccepted(false);
  };
  const transition = useMutation({
    mutationFn: (code: string) =>
      transitionDeviation(id!, {
        transition: code,
        expectedVersion: details.data!.record.version,
        note: note || undefined,
        effectivenessRequired,
        isEffective,
        signaturePassword: signaturePassword || undefined,
        signatureMeaningAccepted,
      }),
    onSuccess: updateDetails,
  });
  const finalReport = useMutation({
    mutationFn: () => downloadDeviationFinalReport(id!),
    onSuccess: ({ blob, fileName }) => {
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    },
  });

  const record = details.data?.record;
  const linkedCapas = details.data?.linkedCapas ?? [];
  const activeAssignment = assignments.data?.find(
    (item) => item.status === "Active",
  );
  const activeStep = record
    ? record.status === "Closed"
      ? flow.length
      : Math.max(
          0,
          flow.findIndex(([status]) => status === record.status),
        )
    : 0;
  const mutationError = transition.error;

  return (
    <Dialog
      open={Boolean(id)}
      onClose={close}
      maxWidth="lg"
      fullWidth
      className="record-details-dialog"
      slotProps={{ paper: { className: "record-details-paper" } }}
    >
      <ModalHeader onClose={close} closeLabel="Sapma ayrıntısını kapat">
        <Stack
          direction={{ xs: "column", sm: "row" }}
          className="record-detail-header"
        >
          <Stack
            direction="row"
            spacing={1.7}
            sx={{ alignItems: "center", minWidth: 0 }}
          >
            <Box sx={{ minWidth: 0 }}>
              <Stack
                direction="row"
                spacing={1}
                sx={{ alignItems: "center", flexWrap: "wrap", mb: 0.4 }}
              >
                <Typography className="record-header-number">
                  {record?.recordNumber ?? "SAPMA"}
                </Typography>
                <Chip
                  size="small"
                  className="record-type-chip"
                  label="SAPMA KAYDI"
                />
              </Stack>
              <Typography variant="h5" className="record-header-title">
                {record?.title ?? "Sapma ayrıntısı"}
              </Typography>
            </Box>
          </Stack>
          {record && (
            <Stack direction="row" spacing={1} className="record-header-badges">
              {record.status === "Closed" && (
                <Button
                  variant="contained"
                  color="inherit"
                  size="small"
                  startIcon={<DownloadRounded />}
                  disabled={finalReport.isPending}
                  onClick={() => finalReport.mutate()}
                  sx={{
                    bgcolor: "rgba(255,255,255,.96)",
                    color: "#0f5f61",
                    "&:hover": { bgcolor: "#fff" },
                  }}
                >
                  {finalReport.isPending ? "PDF hazırlanıyor" : "Nihai PDF"}
                </Button>
              )}
              <Chip
                className="status-glass-chip"
                icon={<TimelineRounded />}
                label={statusLabel(record.status)}
              />
              <Chip
                className={`risk-glass-chip risk-${record.classification.toLowerCase()}`}
                icon={<GppMaybeRounded />}
                label={`${classificationLabel(record.classification)} · RPN ${record.riskScore}`}
              />
            </Stack>
          )}
        </Stack>
      </ModalHeader>
      {details.isLoading && <LinearProgress />}
      <DialogContent className="record-details-content">
        {details.isError && (
          <Alert severity="error">Sapma ayrıntısı alınamadı.</Alert>
        )}
        {mutationError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {mutationError.message}
          </Alert>
        )}
        {finalReport.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {finalReport.error.message}
          </Alert>
        )}
        {record && details.data && (
          <Box className="record-detail-workspace">
            <Paper elevation={0} square className="record-detail-tabs-shell">
              <Tabs
                value={activeTab}
                onChange={(_, nextTab: number) => setActiveTab(nextTab)}
                orientation="vertical"
                variant="standard"
                scrollButtons="auto"
                aria-label="Sapma detay bölümleri"
              >
                <Tab
                  disableRipple
                  icon={<DescriptionRounded />}
                  iconPosition="start"
                  label={<TabLabel text="Genel Bakış" />}
                />
                <Tab
                  disableRipple
                  icon={<ManageSearchRounded />}
                  iconPosition="start"
                  label={
                    <TabLabel
                      text="Araştırma ve Etki"
                      count={
                        details.data.investigations.length +
                        details.data.batchImpacts.length
                      }
                    />
                  }
                />
                <Tab
                  disableRipple
                  icon={<TaskAltRounded />}
                  iconPosition="start"
                  label={
                    <TabLabel
                      text="Karar ve Aksiyon"
                      count={details.data.availableTransitions.length}
                    />
                  }
                />
                <Tab
                  disableRipple
                  icon={<HistoryRounded />}
                  iconPosition="start"
                  label={
                    <TabLabel
                      text="Geçmiş"
                      count={details.data.auditTrail.length}
                    />
                  }
                />
              </Tabs>
            </Paper>

            <Box
              className="detail-tab-panel"
              role="tabpanel"
              aria-label={tabPanelLabel(activeTab)}
            >
              {activeTab === 0 && (
                <Stack
                  className="record-tab-canvas overview-canvas"
                  spacing={0}
                >
                  <Box className="overview-intro">
                    <Stack
                      direction="row"
                      spacing={1.2}
                      sx={{ alignItems: "center" }}
                    >
                      <Box className="section-heading-icon tone-indigo">
                        <DescriptionRounded />
                      </Box>
                      <Box>
                        <Typography variant="overline">Olay özeti</Typography>
                        <Typography variant="h6">
                          Ne oldu, ne olması gerekiyordu?
                        </Typography>
                      </Box>
                    </Stack>
                    <Box className="overview-compare">
                      <Box className="overview-statement is-actual">
                        <Typography variant="caption">
                          GERÇEKLEŞEN SAPMA
                        </Typography>
                        <Typography>{record.description}</Typography>
                      </Box>
                      <Box className="overview-statement is-expected">
                        <Typography variant="caption">
                          BEKLENEN / ONAYLI DURUM
                        </Typography>
                        <Typography>{record.expectedState}</Typography>
                      </Box>
                    </Box>
                  </Box>

                  <Box className="overview-decision-band">
                    <Box className="overview-decision-item is-action">
                      <Box className="overview-decision-icon">
                        <BoltRounded />
                      </Box>
                      <Box>
                        <Typography variant="overline">İLK KONTROL</Typography>
                        <Typography>{record.immediateAction}</Typography>
                      </Box>
                    </Box>
                    <Box className="overview-decision-item is-risk">
                      <Box className="overview-decision-icon">
                        <WarningAmberRounded />
                      </Box>
                      <Box>
                        <Typography variant="overline">
                          RİSK KARARI · RPN {record.riskScore}
                        </Typography>
                        <Typography>
                          {record.likelihood} × {record.severity} ×{" "}
                          {record.detectability} ·{" "}
                          {record.capaRequired
                            ? "DÖF zorunlu"
                            : "DÖF zorunlu değil"}
                        </Typography>
                      </Box>
                    </Box>
                  </Box>

                  <Box className="overview-metadata">
                    <Stack direction="row" className="overview-section-title">
                      <Box>
                        <Typography sx={{ fontWeight: 800 }}>
                          Kayıt bağlamı
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Sınıflandırma, kaynak ve zaman bilgileri
                        </Typography>
                      </Box>
                      <Chip
                        size="small"
                        variant="outlined"
                        label={record.deviationType}
                      />
                    </Stack>
                    <Box className="deviation-facts-grid">
                      <RecordFact
                        icon={CategoryRounded}
                        label="Sapma türü"
                        value={record.deviationType}
                      />
                      <RecordFact
                        icon={BusinessRounded}
                        label="Tespit eden bölüm"
                        value={record.detectedDepartment}
                      />
                      <RecordFact
                        icon={PrecisionManufacturingRounded}
                        label="Proses aşaması"
                        value={record.processStage}
                      />
                      <RecordFact
                        icon={EventRounded}
                        label="Gerçekleşme zamanı"
                        value={formatDateTime(record.occurredAtUtc)}
                      />
                      <RecordFact
                        icon={EventRounded}
                        label="Tespit zamanı"
                        value={formatDateTime(record.detectedAtUtc)}
                      />
                      <RecordFact
                        icon={EventRounded}
                        label="Hedef kapanış"
                        value={formatDateTime(record.targetDateUtc)}
                      />
                    </Box>
                  </Box>

                  <Box className="overview-secondary-tools">
                    <Paper
                      component="details"
                      elevation={0}
                      className="workflow-visual-card overview-disclosure"
                    >
                      <Box
                        component="summary"
                        className="overview-disclosure-summary"
                      >
                        <Stack
                          direction="row"
                          spacing={1.2}
                          sx={{ alignItems: "center" }}
                        >
                          <Box className="section-heading-icon tone-indigo">
                            <AccountTreeRounded />
                          </Box>
                          <Box>
                            <Typography sx={{ fontWeight: 800 }}>
                              Süreç durumu
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Tüm yaşam döngüsü adımlarını göster
                            </Typography>
                          </Box>
                        </Stack>
                        <Chip
                          size="small"
                          color="primary"
                          label={statusLabel(record.status)}
                        />
                      </Box>
                      <Box className="overview-disclosure-content">
                        <Stepper
                          activeStep={activeStep}
                          alternativeLabel
                          className="deviation-stepper visual-stepper"
                        >
                          {flow.map(([, label]) => (
                            <Step key={label}>
                              <StepLabel>{label}</StepLabel>
                            </Step>
                          ))}
                        </Stepper>
                      </Box>
                    </Paper>
                    <Box component="details" className="assignments-disclosure">
                      <Box
                        component="summary"
                        className="assignments-disclosure-summary"
                      >
                        <Stack
                          direction="row"
                          spacing={1.2}
                          sx={{ alignItems: "center" }}
                        >
                          <Box className="section-heading-icon tone-teal">
                            <TaskAltRounded />
                          </Box>
                          <Box>
                            <Typography sx={{ fontWeight: 800 }}>
                              Görevler ve yetkililer
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Atamaları ve tamamlanan görevleri gerektiğinde
                              görüntüleyin
                            </Typography>
                          </Box>
                        </Stack>
                      </Box>
                      <Box className="assignments-disclosure-content">
                        <RecordAssignments
                          aggregateType="Deviation"
                          aggregateId={record.id}
                        />
                      </Box>
                    </Box>
                  </Box>
                </Stack>
              )}

              {activeTab === 1 && (
                <Stack
                  className="record-tab-canvas investigation-tab-canvas"
                  spacing={2.5}
                >
                  {can(Permissions.deviationInvestigate) &&
                    details.data.canAddInvestigation && (
                      <InvestigationForm
                        id={record.id}
                        version={record.version}
                        onSuccess={updateDetails}
                      />
                    )}
                  {can(Permissions.deviationInvestigate) &&
                    details.data.canAddBatchImpact && (
                      <BatchImpactForm
                        id={record.id}
                        version={record.version}
                        onSuccess={updateDetails}
                      />
                    )}
                  <Box className="detail-columns visual-detail-columns">
                    <Paper variant="outlined" className="record-section-panel">
                      <Stack direction="row" className="record-section-heading">
                        <Stack
                          direction="row"
                          spacing={1.1}
                          sx={{ alignItems: "center" }}
                        >
                          <Box className="section-heading-icon tone-violet">
                            <ManageSearchRounded />
                          </Box>
                          <Box>
                            <Typography variant="h6" sx={{ fontWeight: 800 }}>
                              Araştırmalar
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Kök neden ve sonuç kayıtları
                            </Typography>
                          </Box>
                        </Stack>
                        <Chip
                          size="small"
                          label={details.data.investigations.length}
                        />
                      </Stack>
                      <Stack spacing={1.25}>
                        {details.data.investigations.map((item) => (
                          <Paper
                            variant="outlined"
                            className="detail-list-card"
                            key={item.id}
                          >
                            <Stack
                              direction="row"
                              sx={{ justifyContent: "space-between", gap: 2 }}
                            >
                              <Typography sx={{ fontWeight: 750 }}>
                                {item.method}
                              </Typography>
                              <Chip
                                size="small"
                                label={item.rootCauseCategory}
                              />
                            </Stack>
                            <Typography variant="body2" sx={{ mt: 1 }}>
                              {item.rootCauseDescription}
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Sonuç: {item.conclusion}
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                              sx={{ display: "block", mt: 0.5 }}
                            >
                              Araştırmacı (kayıt anı): {item.investigatorName} ·{" "}
                              {item.investigatorDepartment}
                            </Typography>
                          </Paper>
                        ))}
                        {details.data.investigations.length === 0 && (
                          <VisualEmptyState
                            icon={ManageSearchRounded}
                            text="Henüz araştırma eklenmedi."
                          />
                        )}
                      </Stack>
                    </Paper>
                    <Paper variant="outlined" className="record-section-panel">
                      <Stack direction="row" className="record-section-heading">
                        <Stack
                          direction="row"
                          spacing={1.1}
                          sx={{ alignItems: "center" }}
                        >
                          <Box className="section-heading-icon tone-cyan">
                            <Inventory2Rounded />
                          </Box>
                          <Box>
                            <Typography variant="h6" sx={{ fontWeight: 800 }}>
                              Batch / seri etkisi
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              Etkilenen ürün ve kararlar
                            </Typography>
                          </Box>
                        </Stack>
                        <Chip
                          size="small"
                          label={details.data.batchImpacts.length}
                        />
                      </Stack>
                      <Stack spacing={1.25}>
                        {details.data.batchImpacts.map((item) => (
                          <Paper
                            variant="outlined"
                            className="detail-list-card"
                            key={item.id}
                          >
                            <Stack
                              direction="row"
                              sx={{ justifyContent: "space-between", gap: 2 }}
                            >
                              <Typography sx={{ fontWeight: 750 }}>
                                {item.batchNumber}
                              </Typography>
                              <Chip
                                size="small"
                                label={item.disposition}
                                color={
                                  item.disposition === "Pending"
                                    ? "warning"
                                    : "default"
                                }
                              />
                            </Stack>
                            <Typography variant="body2" sx={{ mt: 1 }}>
                              {item.rationale}
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              {item.isAffected ? "Etkilendi" : "Etkilenmedi"} ·{" "}
                              {item.isLocked ? "Kilitli" : "Kilitli değil"}
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                              sx={{ display: "block", mt: 0.5 }}
                            >
                              Değerlendiren (kayıt anı): {item.assessedByName} ·{" "}
                              {item.assessedByDepartment}
                            </Typography>
                          </Paper>
                        ))}
                        {details.data.batchImpacts.length === 0 && (
                          <VisualEmptyState
                            icon={Inventory2Rounded}
                            text="Batch/seri etkisi bulunmuyor."
                          />
                        )}
                      </Stack>
                    </Paper>
                  </Box>
                </Stack>
              )}

              {activeTab === 2 && (
                <Paper
                  variant="outlined"
                  className="record-tab-canvas transition-panel tab-transition-panel"
                >
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>
                    Sıradaki kontrollü adım
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mt: 0.4 }}
                  >
                    Kayıt yalnızca yetkili karar ve gerekli kanıtlarla bir
                    sonraki aşamaya geçirilebilir.
                  </Typography>
                  {record.status === "QualityAssessment" &&
                    record.capaRequired &&
                    (linkedCapas.length === 0 ? (
                      <Alert
                        severity="info"
                        sx={{ mt: 1.5 }}
                        action={
                          can(Permissions.capaPlan) ? (
                            <Button
                              color="inherit"
                              size="small"
                              onClick={() => setCapaDialogOpen(true)}
                            >
                              DÖF oluştur
                            </Button>
                          ) : undefined
                        }
                      >
                        Bu kayıt DÖF gerektiriyor. M.02 bağlantısı
                        oluşturulmadan KG değerlendirmesi tamamlanamaz.
                      </Alert>
                    ) : (
                      <Alert severity="success" sx={{ mt: 1.5 }}>
                        İlişkili DÖF: {linkedCapas[0].recordNumber} ·{" "}
                        {linkedCapas[0].status}
                      </Alert>
                    ))}
                  {linkedCapas.length > 0 && (
                    <Paper variant="outlined" sx={{ mt: 2, p: 2 }}>
                      <Typography sx={{ fontWeight: 800, mb: 1 }}>
                        M.02 bağlantıları
                      </Typography>
                      {linkedCapas.map((capa) => (
                        <Stack
                          key={capa.id}
                          direction={{ xs: "column", sm: "row" }}
                          sx={{
                            justifyContent: "space-between",
                            alignItems: { sm: "center" },
                            gap: 1,
                          }}
                        >
                          <Box>
                            <Typography sx={{ fontWeight: 750 }}>
                              {capa.recordNumber} · {capa.title}
                            </Typography>
                            <Typography
                              variant="caption"
                              color="text.secondary"
                            >
                              {capa.owner} · {capa.status}
                            </Typography>
                          </Box>
                          <Button href={`/modules/m02?open=${capa.id}`}>
                            DÖF kaydını aç
                          </Button>
                        </Stack>
                      ))}
                    </Paper>
                  )}
                  {can(Permissions.deviationManage) &&
                    details.data.availableTransitions.some(
                      (item) => item.noteRequired,
                    ) && (
                      <TextField
                        label="Karar / işlem notu"
                        value={note}
                        onChange={(event) => setNote(event.target.value)}
                        multiline
                        minRows={3}
                        fullWidth
                        sx={{ mt: 2 }}
                      />
                    )}
                  {can(Permissions.deviationManage) &&
                    record.status === "QualityAssessment" && (
                      <FormControlLabel
                        control={
                          <Checkbox
                            checked={effectivenessRequired}
                            onChange={(event) =>
                              setEffectivenessRequired(event.target.checked)
                            }
                          />
                        }
                        label="Etkinlik değerlendirmesi gerekli"
                      />
                    )}
                  {can(Permissions.deviationManage) &&
                    record.status === "EffectivenessReview" && (
                      <FormControlLabel
                        control={
                          <Checkbox
                            checked={isEffective}
                            onChange={(event) =>
                              setIsEffective(event.target.checked)
                            }
                          />
                        }
                        label="Aksiyon etkili bulundu"
                      />
                    )}
                  {can(Permissions.deviationManage) &&
                    details.data.availableTransitions.some((item) =>
                      [
                        "start-preliminary-review",
                        "complete-quality-assessment",
                        "complete-effectiveness-review",
                        "close",
                      ].includes(item.code),
                    ) && (
                      <ElectronicSignaturePanel
                        meaning={details.data.availableTransitions
                          .filter((item) =>
                            [
                              "start-preliminary-review",
                              "complete-quality-assessment",
                              "complete-effectiveness-review",
                              "close",
                            ].includes(item.code),
                          )
                          .map((item) => item.label)
                          .join(" / ")}
                        password={signaturePassword}
                        accepted={signatureMeaningAccepted}
                        onPasswordChange={setSignaturePassword}
                        onAcceptedChange={setSignatureMeaningAccepted}
                        disabled={transition.isPending}
                      />
                    )}
                  <Stack
                    direction="row"
                    spacing={1.5}
                    sx={{ mt: 2, flexWrap: "wrap" }}
                  >
                    {!can(Permissions.deviationManage) &&
                      details.data.availableTransitions.length > 0 && (
                        <Alert severity="warning">
                          Bu aşamadaki durum kararını vermek için Kalite Güvence
                          veya Onaylayan rolü gerekir.
                        </Alert>
                      )}
                    {can(Permissions.deviationManage) &&
                      details.data.availableTransitions.map((item) => (
                        <Button
                          variant="contained"
                          key={item.code}
                          disabled={
                            transition.isPending ||
                            (item.noteRequired && !note.trim()) ||
                            ([
                              "start-preliminary-review",
                              "complete-quality-assessment",
                              "complete-effectiveness-review",
                              "close",
                            ].includes(item.code) &&
                              (!signaturePassword ||
                                !signatureMeaningAccepted)) ||
                            (record.status === "QualityAssessment" &&
                              record.capaRequired &&
                              linkedCapas.length === 0)
                          }
                          onClick={() => transition.mutate(item.code)}
                        >
                          {item.label}
                        </Button>
                      ))}
                    {details.data.availableTransitions.length === 0 &&
                      activeAssignment &&
                      activeAssignment.assignedUserId !== user.id && (
                        <Alert
                          severity="info"
                          icon={<AssignmentIndRounded />}
                          sx={{ width: "100%" }}
                        >
                          <Typography sx={{ fontWeight: 800 }}>
                            Sıradaki görev {activeAssignment.assignedUserName}{" "}
                            kullanıcısında
                          </Typography>
                          <Typography variant="body2">
                            {activeAssignment.departmentName
                              ? `${activeAssignment.departmentName} · `
                              : ""}
                            Bu aşamadaki işlemler yalnızca atanan kullanıcı veya
                            etkin delegesi tarafından tamamlanabilir.
                          </Typography>
                        </Alert>
                      )}
                    {details.data.availableTransitions.length === 0 &&
                      !activeAssignment &&
                      record.status !== "Closed" && (
                        <Alert severity="warning" sx={{ width: "100%" }}>
                          Bu aşama için etkin görev ataması bulunmuyor. Sistem
                          yöneticisi kayıt görevini kontrol etmelidir.
                        </Alert>
                      )}
                    {details.data.availableTransitions.length === 0 &&
                      record.status === "Closed" && (
                        <Typography color="text.secondary">
                          Kayıt kapalı; bekleyen işlem bulunmuyor.
                        </Typography>
                      )}
                  </Stack>
                </Paper>
              )}

              {activeTab === 3 && (
                <Box className="record-tab-canvas history-tab-panel">
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>
                    Kronolojik durum geçmişi
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mt: 0.4, mb: 2 }}
                  >
                    Tüm işlemler en yeni kayıttan en eski kayda doğru tarih
                    bazında sıralanır.
                  </Typography>
                  {details.data.signatures.length > 0 && (
                    <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
                      <Typography sx={{ fontWeight: 800, mb: 1 }}>
                        Elektronik imzalar
                      </Typography>
                      <Stack spacing={1}>
                        {details.data.signatures.map((signature) => (
                          <Stack
                            key={signature.id}
                            direction={{ xs: "column", sm: "row" }}
                            sx={{
                              justifyContent: "space-between",
                              alignItems: { sm: "center" },
                              gap: 1,
                            }}
                          >
                            <Box>
                              <Typography
                                variant="body2"
                                sx={{ fontWeight: 700 }}
                              >
                                {signature.meaning} · {signature.signerName}
                              </Typography>
                              <Typography
                                variant="caption"
                                color="text.secondary"
                              >
                                Sürüm {signature.recordVersion} ·{" "}
                                {formatDateTime(signature.signedAtUtc)} · Hash{" "}
                                {signature.contentHash.slice(0, 12)}…
                              </Typography>
                            </Box>
                            <ElectronicSignatureVerificationBadge
                              signatureId={signature.id}
                            />
                          </Stack>
                        ))}
                      </Stack>
                    </Paper>
                  )}
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
      {record && can(Permissions.capaPlan) && (
        <CapaCreateDialog
          open={capaDialogOpen}
          onClose={() => setCapaDialogOpen(false)}
          sourceDeviation={{
            id: record.id,
            recordNumber: record.recordNumber,
            title: record.title,
            description: record.description,
          }}
          onCreated={() => setCapaDialogOpen(false)}
        />
      )}
    </Dialog>
  );
}

function InvestigationForm({
  id,
  version,
  onSuccess,
}: {
  id: string;
  version: number;
  onSuccess: (data: DeviationDetails) => void;
}) {
  const [method, setMethod] = useState("5 Neden");
  const [category, setCategory] = useState("Proses");
  const [rootCause, setRootCause] = useState("");
  const [conclusion, setConclusion] = useState("");
  const mutation = useMutation({
    mutationFn: () =>
      addDeviationInvestigation(id, {
        expectedVersion: version,
        method,
        rootCauseCategory: category,
        rootCauseDescription: rootCause,
        conclusion,
      }),
    onSuccess,
  });

  return (
    <Paper variant="outlined" className="stage-form">
      <Typography variant="h6" sx={{ fontWeight: 800 }}>
        Kök neden araştırması ekle
      </Typography>
      {mutation.isError && (
        <Alert severity="error" sx={{ mt: 1.5 }}>
          {mutation.error.message}
        </Alert>
      )}
      <Box className="form-grid two-columns" sx={{ mt: 2 }}>
        <TextField
          label="Yöntem"
          value={method}
          onChange={(event) => setMethod(event.target.value)}
        />
        <TextField
          label="Kök neden kategorisi"
          value={category}
          onChange={(event) => setCategory(event.target.value)}
        />
        <TextField
          label="Kök neden"
          value={rootCause}
          onChange={(event) => setRootCause(event.target.value)}
          multiline
          minRows={2}
        />
        <TextField
          label="Araştırma sonucu"
          value={conclusion}
          onChange={(event) => setConclusion(event.target.value)}
          multiline
          minRows={2}
        />
      </Box>
      <Button
        variant="outlined"
        sx={{ mt: 2 }}
        disabled={mutation.isPending || !rootCause.trim() || !conclusion.trim()}
        onClick={() => mutation.mutate()}
      >
        Tamamlanmış araştırmayı ekle
      </Button>
    </Paper>
  );
}

function BatchImpactForm({
  id,
  version,
  onSuccess,
}: {
  id: string;
  version: number;
  onSuccess: (data: DeviationDetails) => void;
}) {
  const [batchNumber, setBatchNumber] = useState("");
  const [isAffected, setIsAffected] = useState(true);
  const [isLocked, setIsLocked] = useState(true);
  const [disposition, setDisposition] = useState("Pending");
  const [rationale, setRationale] = useState("");
  const mutation = useMutation({
    mutationFn: () =>
      addDeviationBatchImpact(id, {
        expectedVersion: version,
        batchNumber,
        isAffected,
        isLocked,
        disposition,
        rationale,
      }),
    onSuccess,
  });

  return (
    <Paper variant="outlined" className="stage-form">
      <Typography variant="h6" sx={{ fontWeight: 800 }}>
        Batch / seri etkisi ekle
      </Typography>
      {mutation.isError && (
        <Alert severity="error" sx={{ mt: 1.5 }}>
          {mutation.error.message}
        </Alert>
      )}
      <Box className="form-grid two-columns" sx={{ mt: 2 }}>
        <TextField
          label="Batch / seri numarası"
          value={batchNumber}
          onChange={(event) => setBatchNumber(event.target.value)}
        />
        <SearchableSelect
          label="Karar"
          value={disposition}
          options={dispositionOptions}
          onChange={(next) => setDisposition(next ?? "Pending")}
          size="medium"
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={isAffected}
              onChange={(event) => setIsAffected(event.target.checked)}
            />
          }
          label="Batch/seri etkilendi"
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={isLocked}
              onChange={(event) => setIsLocked(event.target.checked)}
            />
          }
          label="Batch/seri kilitli"
        />
      </Box>
      <TextField
        label="Karar gerekçesi"
        value={rationale}
        onChange={(event) => setRationale(event.target.value)}
        fullWidth
        multiline
        minRows={2}
        sx={{ mt: 2 }}
      />
      <Button
        variant="outlined"
        sx={{ mt: 2 }}
        disabled={
          mutation.isPending || !batchNumber.trim() || !rationale.trim()
        }
        onClick={() => mutation.mutate()}
      >
        Etki değerlendirmesini ekle
      </Button>
    </Paper>
  );
}

function VisualEmptyState({
  text,
  icon: Icon,
}: {
  text: string;
  icon: SvgIconComponent;
}) {
  return (
    <Box className="visual-empty-state">
      <Icon />
      <Typography variant="body2" color="text.secondary">
        {text}
      </Typography>
    </Box>
  );
}

function TabLabel({ text, count }: { text: string; count?: number }) {
  return (
    <span className="record-tab-label">
      <span>{text}</span>
      {count !== undefined && <span className="record-tab-count">{count}</span>}
    </span>
  );
}

function tabPanelLabel(tab: number) {
  return (
    ["Genel Bakış", "Araştırma ve Etki", "Karar ve Aksiyon", "Geçmiş"][tab] ??
    "Detay"
  );
}

function classificationLabel(classification: string) {
  return classification === "Critical"
    ? "Kritik"
    : classification === "Major"
      ? "Majör"
      : "Minör";
}

function statusLabel(status: string) {
  if (status === "Draft") return "Taslak";
  if (status === "Voided") return "İptal";
  return flow.find(([value]) => value === status)?.[1] ?? status;
}

function RecordFact({
  icon: Icon,
  label,
  value,
}: {
  icon: SvgIconComponent;
  label: string;
  value: string;
}) {
  return (
    <Box className="deviation-record-fact">
      <Box className="deviation-record-fact-icon">
        <Icon fontSize="small" />
      </Box>
      <Box>
        <Typography variant="caption" className="deviation-record-fact-label">
          {label}
        </Typography>
        <Typography variant="body2" className="deviation-record-fact-value">
          {value}
        </Typography>
      </Box>
    </Box>
  );
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
