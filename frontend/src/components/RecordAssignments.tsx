import { useQuery } from "@tanstack/react-query";
import {
  AssignmentIndRounded,
  BusinessRounded,
  ScheduleRounded,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { getWorkflowAssignments } from "../api/access";

const taskLabels: Record<string, string> = {
  Initiator: "Başlatan",
  ProcessAuthority: "İşlem yetkilisi",
  Investigator: "Araştırmacı",
  ActionOwner: "Aksiyon sorumlusu",
  Evaluator: "Değerlendiren",
  Approver: "Onaylayan",
  QualifiedPerson: "Mesul Müdür",
  ChangeBoard: "Değişiklik kurulu",
  DocumentAuthor: "Doküman yazarı",
  DocumentApprover: "Doküman onaylayanı",
  TrainingCoordinator: "Eğitim koordinatörü",
  Learner: "Eğitim katılımcısı",
  Trainer: "Eğitmen / değerlendirici",
  ComplaintCoordinator: "Şikâyet koordinatörü",
  ComplaintInvestigator: "Şikâyet araştırmacısı",
  ResponseApprover: "Müşteri yanıtı onaylayanı",
  PharmacovigilanceReviewer: "Farmakovijilans değerlendiricisi",
  AuditPlanner: "Denetim planlayıcısı",
  LeadAuditor: "Baş denetçi",
  AuditeeResponder: "Denetlenen bölüm yanıtlayanı",
  ExternalAuditCoordinator: "Dış denetim koordinatörü",
  DocumentPackageController: "Talep paketi kontrolörü",
  ExternalAuditeeResponder: "Resmi bulgu yanıtlayanı",
  ExternalAuditAuthorizedCloser: "Yetkili kapanış sorumlusu",
  SupplierAuditPlanner: "Tedarikçi denetimi planlayıcısı",
  SupplierAuditLeadAuditor: "Tedarikçi baş denetçisi",
  SupplierResponder: "Tedarikçi yanıt sorumlusu",
  SupplierAuditVerifier: "Tedarikçi kanıt doğrulayıcısı",
  SupplierQualityApprover: "Tedarikçi kalite onaylayanı",
  WorkItemOwner: "İş sorumlusu",
  WorkItemVerifier: "Bağımsız iş doğrulayıcısı",
  RiskOwner: "Risk sahibi",
  RiskApprover: "Bağımsız risk onaylayanı",
  MbrAuthor: "MBR yazarı",
  MbrReviewer: "MBR teknik inceleyeni",
  MbrApprover: "MBR kalite onaylayanı",
  SpecializedOwner: "Kayıt sorumlusu",
  SpecializedReviewer: "Bağımsız inceleyen",
  SpecializedApprover: "Kalite / yetkili onaylayan",
};

const statusLabels: Record<string, string> = {
  Active: "Aktif görev",
  Completed: "Tamamlandı",
  Cancelled: "İptal edildi",
};

export function RecordAssignments({
  aggregateType,
  aggregateId,
}: {
  aggregateType:
    | "Deviation"
    | "Capa"
    | "ChangeControl"
    | "Document"
    | "Training"
    | "Complaint"
    | "InternalAudit"
    | "ExternalAudit"
    | "SupplierAudit"
    | "WorkItem"
    | "RiskAssessment"
    | "MasterBatchRecord"
    | "SpecializedRecord";
  aggregateId: string;
}) {
  const assignments = useQuery({
    queryKey: ["workflow-assignments", aggregateType, aggregateId],
    queryFn: ({ signal }) =>
      getWorkflowAssignments(aggregateType, aggregateId, signal),
    retry: false,
  });

  return (
    <Paper variant="outlined" className="record-assignment-panel">
      <Stack
        direction={{ xs: "column", sm: "row" }}
        className="record-assignment-heading"
      >
        <Stack direction="row" spacing={1.2} sx={{ alignItems: "center" }}>
          <Box className="section-heading-icon tone-teal">
            <AssignmentIndRounded />
          </Box>
          <Box>
            <Typography sx={{ fontWeight: 800 }}>
              Kayıt görevleri ve yetkililer
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Kimin hangi sıfatla işlem yapabileceği kayıt bazında izlenir.
            </Typography>
          </Box>
        </Stack>
        {assignments.isFetching && <CircularProgress size={22} />}
      </Stack>
      {assignments.isError && (
        <Alert severity="error">Kayıt görevleri alınamadı.</Alert>
      )}
      {assignments.isSuccess && assignments.data.length === 0 && (
        <Alert severity="info">
          Bu kayıt için görev ataması bulunmuyor. Eski kayıtlar rol yetkileriyle
          çalışmaya devam eder.
        </Alert>
      )}
      <Box className="record-assignment-grid">
        {assignments.data?.map((assignment) => (
          <Paper
            key={assignment.id}
            variant="outlined"
            className={`record-assignment-card status-${assignment.status.toLowerCase()}`}
          >
            <Box className="assignment-identity">
              <Typography variant="overline" color="text.secondary">
                {taskLabel(assignment.taskRole)}
              </Typography>
              <Typography sx={{ fontWeight: 780 }}>
                {assignment.assignedUserName}
              </Typography>
            </Box>
            <Stack
              direction="row"
              spacing={1.5}
              className="assignment-meta"
              sx={{ flexWrap: "wrap", color: "text.secondary" }}
            >
              {assignment.departmentName && (
                <Typography variant="caption">
                  <BusinessRounded /> {assignment.departmentName}
                </Typography>
              )}
              <Typography variant="caption">
                <ScheduleRounded /> {formatDate(assignment.assignedAtUtc)}
              </Typography>
            </Stack>
            <Chip
              className="assignment-status"
              size="small"
              color={assignment.status === "Active" ? "success" : "default"}
              label={statusLabels[assignment.status] ?? assignment.status}
            />
          </Paper>
        ))}
      </Box>
    </Paper>
  );
}

function taskLabel(value: string) {
  if (value.startsWith("ActionOwner:")) return "Aksiyon sorumlusu";
  if (value.startsWith("Assessment:")) return "Bölüm değerlendirmesi";
  if (value.startsWith("DocumentReview:")) return "Doküman incelemesi";
  if (value.startsWith("AuditFinding:")) return "İç denetim bulgu sorumlusu";
  if (value.startsWith("ExternalAuditFinding:"))
    return "Dış denetim bulgu sorumlusu";
  if (value.startsWith("SupplierAuditFinding:"))
    return "Tedarikçi denetimi bulgu sorumlusu";
  if (value.startsWith("RiskAction:")) return "Risk azaltma aksiyonu sorumlusu";
  return taskLabels[value] ?? value;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
