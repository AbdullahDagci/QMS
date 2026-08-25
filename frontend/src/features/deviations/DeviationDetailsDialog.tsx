import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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
} from '@mui/material'
import {
  AccountTreeRounded,
  BoltRounded,
  DescriptionRounded,
  FactCheckRounded,
  GppMaybeRounded,
  HistoryRounded,
  Inventory2Rounded,
  ManageSearchRounded,
  TimelineRounded,
  TaskAltRounded,
  WarningAmberRounded,
} from '@mui/icons-material'
import type { SvgIconComponent } from '@mui/icons-material'
import {
  addDeviationBatchImpact,
  addDeviationInvestigation,
  getDeviationDetails,
  transitionDeviation,
  type DeviationDetails,
} from '../../api/deviations'
import { SearchableSelect, type SelectOption } from '../../components/SearchableSelect'
import { ModalHeader } from '../../components/ModalHeader'
import { AuditTimeline } from '../../components/AuditTimeline'
import { RecordAssignments } from '../../components/RecordAssignments'
import { CapaCreateDialog } from '../capas/CapaWorkspace'
import { Permissions, useAuth } from '../../security/AuthContext'

const flow = [
  ['Submitted', 'Gönderildi'],
  ['PreliminaryReview', 'Ön inceleme'],
  ['Investigation', 'Araştırma'],
  ['ImpactAssessment', 'Etki'],
  ['QualityAssessment', 'KG değerlendirmesi'],
  ['ActionImplementation', 'Aksiyon'],
  ['EffectivenessReview', 'Etkinlik'],
  ['ClosureApproval', 'Kapanış onayı'],
  ['Closed', 'Kapalı'],
] as const

const eventLabels: Record<string, string> = {
  DeviationCreated: 'Sapma taslağı oluşturuldu',
  DeviationSubmitted: 'Sapma iş akışına gönderildi',
  DeviationStatusChanged: 'Durum değiştirildi',
  DeviationInvestigationCompleted: 'Kök neden araştırması eklendi',
  DeviationBatchImpactAssessed: 'Batch/seri etkisi değerlendirildi',
}

const dispositionOptions: Array<SelectOption<string>> = [
  { value: 'Pending', label: 'Karar bekliyor' },
  { value: 'Release', label: 'Serbest bırak' },
  { value: 'Hold', label: 'Beklet' },
  { value: 'Reject', label: 'Reddet' },
  { value: 'NotApplicable', label: 'Uygulanamaz' },
]

export function DeviationDetailsDialog({ id, onClose }: { id: string | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const { can } = useAuth()
  const [note, setNote] = useState('')
  const [effectivenessRequired, setEffectivenessRequired] = useState(false)
  const [isEffective, setIsEffective] = useState(true)
  const [activeTab, setActiveTab] = useState(0)
  const [capaDialogOpen, setCapaDialogOpen] = useState(false)
  const close = () => {
    setActiveTab(0)
    onClose()
  }
  const details = useQuery({
    queryKey: ['deviation-details', id],
    queryFn: ({ signal }) => getDeviationDetails(id!, signal),
    enabled: Boolean(id),
    retry: false,
  })
  const updateDetails = async (data: DeviationDetails) => {
    queryClient.setQueryData(['deviation-details', id], data)
    await queryClient.invalidateQueries({ queryKey: ['deviations'] })
    setNote('')
  }
  const transition = useMutation({
    mutationFn: (code: string) => transitionDeviation(id!, {
      transition: code,
      expectedVersion: details.data!.record.version,
      note: note || undefined,
      effectivenessRequired,
      isEffective,
    }),
    onSuccess: updateDetails,
  })

  const record = details.data?.record
  const linkedCapas = details.data?.linkedCapas ?? []
  const activeStep = record
    ? record.status === 'Closed' ? flow.length : Math.max(0, flow.findIndex(([status]) => status === record.status))
    : 0
  const mutationError = transition.error

  return (
    <Dialog
      open={Boolean(id)}
      onClose={close}
      maxWidth="lg"
      fullWidth
      className="record-details-dialog"
      slotProps={{ paper: { className: 'record-details-paper' } }}
    >
      <ModalHeader onClose={close} closeLabel="Sapma ayrıntısını kapat">
        <Stack direction={{ xs: 'column', sm: 'row' }} className="record-detail-header">
          <Stack direction="row" spacing={1.7} sx={{ alignItems: 'center', minWidth: 0 }}>
            <Box sx={{ minWidth: 0 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.4 }}>
                <Typography className="record-header-number">{record?.recordNumber ?? 'SAPMA'}</Typography>
                <Chip size="small" className="record-type-chip" label="SAPMA KAYDI" />
              </Stack>
              <Typography variant="h5" className="record-header-title">{record?.title ?? 'Sapma ayrıntısı'}</Typography>
            </Box>
          </Stack>
          {record && (
            <Stack direction="row" spacing={1} className="record-header-badges">
              <Chip className="status-glass-chip" icon={<TimelineRounded />} label={statusLabel(record.status)} />
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
        {details.isError && <Alert severity="error">Sapma ayrıntısı alınamadı.</Alert>}
        {mutationError && <Alert severity="error" sx={{ mb: 2 }}>{mutationError.message}</Alert>}
        {record && details.data && (
          <>
            <Paper elevation={0} square className="record-detail-tabs-shell">
              <Tabs
                value={activeTab}
                onChange={(_, nextTab: number) => setActiveTab(nextTab)}
                variant="scrollable"
                scrollButtons="auto"
                aria-label="Sapma detay bölümleri"
              >
                <Tab icon={<DescriptionRounded />} iconPosition="start" label={<TabLabel text="Genel Bakış" />} />
                <Tab
                  icon={<ManageSearchRounded />}
                  iconPosition="start"
                  label={<TabLabel text="Araştırma ve Etki" count={details.data.investigations.length + details.data.batchImpacts.length} />}
                />
                <Tab
                  icon={<TaskAltRounded />}
                  iconPosition="start"
                  label={<TabLabel text="Karar ve Aksiyon" count={details.data.availableTransitions.length} />}
                />
                <Tab icon={<HistoryRounded />} iconPosition="start" label={<TabLabel text="Geçmiş" count={details.data.auditTrail.length} />} />
              </Tabs>
            </Paper>

            <Box className="detail-tab-panel" role="tabpanel" aria-label={tabPanelLabel(activeTab)}>
              {activeTab === 0 && (
                <Stack spacing={3}>
                  <Paper elevation={0} className="workflow-visual-card">
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2, mb: 2.5 }}>
                      <Stack direction="row" spacing={1.2} sx={{ alignItems: 'center' }}>
                        <Box className="section-heading-icon tone-indigo"><AccountTreeRounded /></Box>
                        <Box>
                          <Typography sx={{ fontWeight: 780 }}>Kontrollü iş akışı</Typography>
                          <Typography variant="caption" color="text.secondary">Kayıt yaşam döngüsündeki güncel konum</Typography>
                        </Box>
                      </Stack>
                      <Chip size="small" color="primary" label={statusLabel(record.status)} />
                    </Stack>
                    <Stepper activeStep={activeStep} alternativeLabel className="deviation-stepper visual-stepper">
                      {flow.map(([, label]) => <Step key={label}><StepLabel>{label}</StepLabel></Step>)}
                    </Stepper>
                  </Paper>

                  <Box className="detail-grid">
                    <DetailCard title="Sapma tanımı" value={record.description} icon={DescriptionRounded} tone="indigo" />
                    <DetailCard title="Beklenen durum" value={record.expectedState} icon={FactCheckRounded} tone="teal" />
                    <DetailCard title="Acil aksiyon" value={record.immediateAction} icon={BoltRounded} tone="amber" />
                    <DetailCard
                      title="Risk kararı"
                      value={`${record.likelihood} × ${record.severity} × ${record.detectability} = ${record.riskScore}. ${record.capaRequired ? 'DÖF zorunlu.' : 'DÖF zorunlu değil.'}`}
                      icon={WarningAmberRounded}
                      tone="rose"
                    />
                  </Box>
                  <RecordAssignments aggregateType="Deviation" aggregateId={record.id} />
                </Stack>
              )}

              {activeTab === 1 && (
                <Stack spacing={2.5}>
                  {can(Permissions.deviationInvestigate) && record.status === 'Investigation' && (
                    <InvestigationForm id={record.id} version={record.version} onSuccess={updateDetails} />
                  )}
                  {can(Permissions.deviationInvestigate) && record.status === 'ImpactAssessment' && (
                    <BatchImpactForm id={record.id} version={record.version} onSuccess={updateDetails} />
                  )}
                  <Box className="detail-columns visual-detail-columns">
                    <Paper variant="outlined" className="record-section-panel">
                      <Stack direction="row" className="record-section-heading">
                        <Stack direction="row" spacing={1.1} sx={{ alignItems: 'center' }}>
                          <Box className="section-heading-icon tone-violet"><ManageSearchRounded /></Box>
                          <Box>
                            <Typography variant="h6" sx={{ fontWeight: 800 }}>Araştırmalar</Typography>
                            <Typography variant="caption" color="text.secondary">Kök neden ve sonuç kayıtları</Typography>
                          </Box>
                        </Stack>
                        <Chip size="small" label={details.data.investigations.length} />
                      </Stack>
                      <Stack spacing={1.25}>
                        {details.data.investigations.map((item) => (
                          <Paper variant="outlined" className="detail-list-card" key={item.id}>
                            <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 2 }}>
                              <Typography sx={{ fontWeight: 750 }}>{item.method}</Typography>
                              <Chip size="small" label={item.rootCauseCategory} />
                            </Stack>
                            <Typography variant="body2" sx={{ mt: 1 }}>{item.rootCauseDescription}</Typography>
                            <Typography variant="caption" color="text.secondary">Sonuç: {item.conclusion}</Typography>
                          </Paper>
                        ))}
                        {details.data.investigations.length === 0 && (
                          <VisualEmptyState icon={ManageSearchRounded} text="Henüz araştırma eklenmedi." />
                        )}
                      </Stack>
                    </Paper>
                    <Paper variant="outlined" className="record-section-panel">
                      <Stack direction="row" className="record-section-heading">
                        <Stack direction="row" spacing={1.1} sx={{ alignItems: 'center' }}>
                          <Box className="section-heading-icon tone-cyan"><Inventory2Rounded /></Box>
                          <Box>
                            <Typography variant="h6" sx={{ fontWeight: 800 }}>Batch / seri etkisi</Typography>
                            <Typography variant="caption" color="text.secondary">Etkilenen ürün ve kararlar</Typography>
                          </Box>
                        </Stack>
                        <Chip size="small" label={details.data.batchImpacts.length} />
                      </Stack>
                      <Stack spacing={1.25}>
                        {details.data.batchImpacts.map((item) => (
                          <Paper variant="outlined" className="detail-list-card" key={item.id}>
                            <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 2 }}>
                              <Typography sx={{ fontWeight: 750 }}>{item.batchNumber}</Typography>
                              <Chip size="small" label={item.disposition} color={item.disposition === 'Pending' ? 'warning' : 'default'} />
                            </Stack>
                            <Typography variant="body2" sx={{ mt: 1 }}>{item.rationale}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {item.isAffected ? 'Etkilendi' : 'Etkilenmedi'} · {item.isLocked ? 'Kilitli' : 'Kilitli değil'}
                            </Typography>
                          </Paper>
                        ))}
                        {details.data.batchImpacts.length === 0 && (
                          <VisualEmptyState icon={Inventory2Rounded} text="Batch/seri etkisi bulunmuyor." />
                        )}
                      </Stack>
                    </Paper>
                  </Box>
                </Stack>
              )}

              {activeTab === 2 && (
                <Paper variant="outlined" className="transition-panel tab-transition-panel">
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Sıradaki kontrollü adım</Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4 }}>
                    Kayıt yalnızca yetkili karar ve gerekli kanıtlarla bir sonraki aşamaya geçirilebilir.
                  </Typography>
                  {record.status === 'QualityAssessment' && record.capaRequired && (
                    linkedCapas.length === 0 ? <Alert severity="info" sx={{ mt: 1.5 }} action={can(Permissions.capaPlan) ? <Button color="inherit" size="small" onClick={() => setCapaDialogOpen(true)}>DÖF oluştur</Button> : undefined}>
                      Bu kayıt DÖF gerektiriyor. M.02 bağlantısı oluşturulmadan KG değerlendirmesi tamamlanamaz.
                    </Alert> : <Alert severity="success" sx={{ mt: 1.5 }}>İlişkili DÖF: {linkedCapas[0].recordNumber} · {linkedCapas[0].status}</Alert>
                  )}
                  {linkedCapas.length > 0 && (
                    <Paper variant="outlined" sx={{ mt: 2, p: 2 }}>
                      <Typography sx={{ fontWeight: 800, mb: 1 }}>M.02 bağlantıları</Typography>
                      {linkedCapas.map((capa) => <Stack key={capa.id} direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, gap: 1 }}><Box><Typography sx={{ fontWeight: 750 }}>{capa.recordNumber} · {capa.title}</Typography><Typography variant="caption" color="text.secondary">{capa.owner} · {capa.status}</Typography></Box><Button href={`/modules/m02?open=${capa.id}`}>DÖF kaydını aç</Button></Stack>)}
                    </Paper>
                  )}
                  {can(Permissions.deviationManage) && details.data.availableTransitions.some((item) => item.noteRequired) && (
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
                  {can(Permissions.deviationManage) && record.status === 'QualityAssessment' && (
                    <FormControlLabel
                      control={<Checkbox checked={effectivenessRequired} onChange={(event) => setEffectivenessRequired(event.target.checked)} />}
                      label="Etkinlik değerlendirmesi gerekli"
                    />
                  )}
                  {can(Permissions.deviationManage) && record.status === 'EffectivenessReview' && (
                    <FormControlLabel
                      control={<Checkbox checked={isEffective} onChange={(event) => setIsEffective(event.target.checked)} />}
                      label="Aksiyon etkili bulundu"
                    />
                  )}
                  <Stack direction="row" spacing={1.5} sx={{ mt: 2, flexWrap: 'wrap' }}>
                    {!can(Permissions.deviationManage) && details.data.availableTransitions.length > 0 && (
                      <Alert severity="warning">Bu aşamadaki durum kararını vermek için Kalite Güvence veya Onaylayan rolü gerekir.</Alert>
                    )}
                    {can(Permissions.deviationManage) && details.data.availableTransitions.map((item) => (
                      <Button
                        variant="contained"
                        key={item.code}
                        disabled={
                          transition.isPending ||
                          (item.noteRequired && !note.trim()) ||
                          (record.status === 'QualityAssessment' && record.capaRequired && linkedCapas.length === 0)
                        }
                        onClick={() => transition.mutate(item.code)}
                      >
                        {item.label}
                      </Button>
                    ))}
                    {details.data.availableTransitions.length === 0 && (
                      <Typography color="text.secondary">Bu aşama için kullanılabilir geçiş bulunmuyor.</Typography>
                    )}
                  </Stack>
                </Paper>
              )}

              {activeTab === 3 && (
                <Box className="history-tab-panel">
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Kronolojik durum geçmişi</Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4, mb: 2 }}>
                    Tüm işlemler en yeni kayıttan en eski kayda doğru tarih bazında sıralanır.
                  </Typography>
                  <AuditTimeline events={details.data.auditTrail} labels={eventLabels} />
                </Box>
              )}
            </Box>
          </>
        )}
      </DialogContent>
      {record && can(Permissions.capaPlan) && <CapaCreateDialog open={capaDialogOpen} onClose={() => setCapaDialogOpen(false)} sourceDeviation={{ id: record.id, recordNumber: record.recordNumber, title: record.title, description: record.description }} onCreated={() => setCapaDialogOpen(false)} />}
    </Dialog>
  )
}

function InvestigationForm({ id, version, onSuccess }: { id: string; version: number; onSuccess: (data: DeviationDetails) => void }) {
  const [method, setMethod] = useState('5 Neden')
  const [category, setCategory] = useState('Proses')
  const [rootCause, setRootCause] = useState('')
  const [conclusion, setConclusion] = useState('')
  const mutation = useMutation({
    mutationFn: () => addDeviationInvestigation(id, {
      expectedVersion: version,
      method,
      rootCauseCategory: category,
      rootCauseDescription: rootCause,
      conclusion,
    }),
    onSuccess,
  })

  return (
    <Paper variant="outlined" className="stage-form">
      <Typography variant="h6" sx={{ fontWeight: 800 }}>Kök neden araştırması ekle</Typography>
      {mutation.isError && <Alert severity="error" sx={{ mt: 1.5 }}>{mutation.error.message}</Alert>}
      <Box className="form-grid two-columns" sx={{ mt: 2 }}>
        <TextField label="Yöntem" value={method} onChange={(event) => setMethod(event.target.value)} />
        <TextField label="Kök neden kategorisi" value={category} onChange={(event) => setCategory(event.target.value)} />
        <TextField label="Kök neden" value={rootCause} onChange={(event) => setRootCause(event.target.value)} multiline minRows={2} />
        <TextField label="Araştırma sonucu" value={conclusion} onChange={(event) => setConclusion(event.target.value)} multiline minRows={2} />
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
  )
}

function BatchImpactForm({ id, version, onSuccess }: { id: string; version: number; onSuccess: (data: DeviationDetails) => void }) {
  const [batchNumber, setBatchNumber] = useState('')
  const [isAffected, setIsAffected] = useState(true)
  const [isLocked, setIsLocked] = useState(true)
  const [disposition, setDisposition] = useState('Pending')
  const [rationale, setRationale] = useState('')
  const mutation = useMutation({
    mutationFn: () => addDeviationBatchImpact(id, {
      expectedVersion: version,
      batchNumber,
      isAffected,
      isLocked,
      disposition,
      rationale,
    }),
    onSuccess,
  })

  return (
    <Paper variant="outlined" className="stage-form">
      <Typography variant="h6" sx={{ fontWeight: 800 }}>Batch / seri etkisi ekle</Typography>
      {mutation.isError && <Alert severity="error" sx={{ mt: 1.5 }}>{mutation.error.message}</Alert>}
      <Box className="form-grid two-columns" sx={{ mt: 2 }}>
        <TextField label="Batch / seri numarası" value={batchNumber} onChange={(event) => setBatchNumber(event.target.value)} />
        <SearchableSelect label="Karar" value={disposition} options={dispositionOptions} onChange={(next) => setDisposition(next ?? 'Pending')} size="medium" />
        <FormControlLabel control={<Checkbox checked={isAffected} onChange={(event) => setIsAffected(event.target.checked)} />} label="Batch/seri etkilendi" />
        <FormControlLabel control={<Checkbox checked={isLocked} onChange={(event) => setIsLocked(event.target.checked)} />} label="Batch/seri kilitli" />
      </Box>
      <TextField label="Karar gerekçesi" value={rationale} onChange={(event) => setRationale(event.target.value)} fullWidth multiline minRows={2} sx={{ mt: 2 }} />
      <Button
        variant="outlined"
        sx={{ mt: 2 }}
        disabled={mutation.isPending || !batchNumber.trim() || !rationale.trim()}
        onClick={() => mutation.mutate()}
      >
        Etki değerlendirmesini ekle
      </Button>
    </Paper>
  )
}

function DetailCard({ title, value, icon: Icon, tone }: {
  title: string
  value: string
  icon: SvgIconComponent
  tone: 'indigo' | 'teal' | 'amber' | 'rose'
}) {
  return (
    <Paper variant="outlined" className={`detail-card visual-detail-card detail-tone-${tone}`}>
      <Box className="detail-card-icon"><Icon /></Box>
      <Box>
        <Typography className="detail-card-label">{title}</Typography>
        <Typography className="detail-card-value">{value}</Typography>
      </Box>
    </Paper>
  )
}

function VisualEmptyState({ text, icon: Icon }: { text: string; icon: SvgIconComponent }) {
  return (
    <Box className="visual-empty-state">
      <Icon />
      <Typography variant="body2" color="text.secondary">{text}</Typography>
    </Box>
  )
}

function TabLabel({ text, count }: { text: string; count?: number }) {
  return (
    <span className="record-tab-label">
      <span>{text}</span>
      {count !== undefined && <span className="record-tab-count">{count}</span>}
    </span>
  )
}

function tabPanelLabel(tab: number) {
  return ['Genel Bakış', 'Araştırma ve Etki', 'Karar ve Aksiyon', 'Geçmiş'][tab] ?? 'Detay'
}

function classificationLabel(classification: string) {
  return classification === 'Critical' ? 'Kritik' : classification === 'Major' ? 'Majör' : 'Minör'
}

function statusLabel(status: string) {
  if (status === 'Draft') return 'Taslak'
  if (status === 'Voided') return 'İptal'
  return flow.find(([value]) => value === status)?.[1] ?? status
}
