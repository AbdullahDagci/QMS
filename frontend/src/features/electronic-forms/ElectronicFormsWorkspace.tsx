import { useEffect, useId, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AutoAwesomeRounded,
  CheckCircleRounded,
  CloseRounded,
  DescriptionRounded,
  DownloadRounded,
  EditNoteRounded,
  FactCheckRounded,
  LibraryBooksRounded,
  PublishRounded,
  SaveRounded,
  SettingsRounded,
} from '@mui/icons-material'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  FormControl,
  FormControlLabel,
  InputLabel,
  IconButton,
  MenuItem,
  Paper,
  Select,
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
  Tooltip,
  Typography,
} from '@mui/material'
import {
  approveElectronicFormRecord,
  createElectronicForm,
  createElectronicFormRecord,
  downloadElectronicFormFinalReport,
  getElectronicForm,
  getElectronicFormRecord,
  listElectronicFormRecords,
  listElectronicForms,
  publishElectronicForm,
  startElectronicFormVersion,
  submitElectronicFormForReview,
  submitElectronicFormRecord,
  updateElectronicForm,
  updateElectronicFormRecord,
  type ElectronicFormDetails,
  type ElectronicFormSchema,
  type FormCondition,
  type FormDocumentBlock,
  type FormField,
  type FormFieldType,
  type FormTextStyle,
  type OutputTemplateConfiguration,
} from '../../api/electronicForms'
import { ManagedFilesPanel } from '../../components/ManagedFilesPanel'
import { ModalHeader } from '../../components/ModalHeader'
import { Permissions, useAuth } from '../../security/AuthContext'
import { AdvancedFormDesigner, type DesignerDraft } from './AdvancedFormDesigner'
import { blocksFor, cssFont, defaultDocumentSettings, materializeSchema, normalizeBlocks } from './formDocumentModel'
import { RichTextDisplay } from './StudioRichText'
import { tableColumnWidths, visibleTableCells } from './studioTables'
import { formDraftRecoveryKey, useFormDraftRecovery } from './useFormDraftRecovery'
import { bodyMetrics, HEADER_RESERVE_MM, PIXELS_PER_MM } from './freeFormPosition'

const defaultOutput: OutputTemplateConfiguration = {
  title: '', footerText: 'Kontrollü elektronik kayıt', primaryColor: '#0F7773',
  includeEmptyFields: false, includeAuditTrail: true, includeSignatures: true,
}

const blankSchema = (): ElectronicFormSchema => ({
  engineVersion: 1,
  sections: [{
    key: 'genelBilgiler', title: 'Genel bilgiler', description: 'Formun temel bilgilerini girin.', order: 1, columns: 2,
    fields: [newField('aciklama', 'Açıklama', 'longText', 1)],
  }],
})

const equipmentSchema = (): ElectronicFormSchema => ({
  engineVersion: 1,
  sections: [
    {
      key: 'ekipmanBilgileri', title: 'Ekipman bilgileri', description: 'Kontrol edilen ekipman ve işlem bilgileri.', order: 1, columns: 2,
      fields: [
        { ...newField('ekipmanKodu', 'Ekipman kodu', 'shortText', 1), required: true },
        { ...newField('kontrolTarihi', 'Kontrol tarihi', 'date', 2), required: true },
        newField('bakimTuru', 'Bakım / kontrol türü', 'singleSelect', 3, [{ value: 'periyodik', label: 'Periyodik' }, { value: 'ariza', label: 'Arıza' }, { value: 'kalibrasyon', label: 'Kalibrasyon' }]),
        newField('uygulayan', 'İşlemi uygulayan', 'shortText', 4),
      ],
    },
    {
      key: 'sonuc', title: 'Sonuç ve kanıt', description: 'Kontrol sonucu ile gözlemleri kaydedin.', order: 2, columns: 1,
      fields: [
        { ...newField('sonuc', 'Kontrol sonucu', 'singleSelect', 1, [{ value: 'uygun', label: 'Uygun' }, { value: 'uygunDegil', label: 'Uygun değil' }]), required: true },
        newField('gozlem', 'Gözlem ve yapılan işlem', 'longText', 2),
        newField('takipGerekli', 'Takip aksiyonu gerekli', 'checkbox', 3),
      ],
    },
  ],
})

function newField(key: string, label: string, type: FormFieldType, order: number, options: FormField['options'] = []): FormField {
  return { key, label, type, required: false, helpText: '', placeholder: '', width: 12, order, maxLength: type === 'shortText' ? 500 : type === 'longText' ? 10000 : null, min: null, max: null, unit: '', options, visibilityCondition: null, requiredCondition: null }
}

export function ElectronicFormsWorkspace() {
  const [tab, setTab] = useState(0)
  const [createOpen, setCreateOpen] = useState(false)
  const [designerId, setDesignerId] = useState<string | null>(null)
  const [createRecordOpen, setCreateRecordOpen] = useState(false)
  const [recordId, setRecordId] = useState<string | null>(null)
  const [feedback, setFeedback] = useState<{ severity: 'success' | 'error'; message: string } | null>(null)
  const { can } = useAuth()
  const forms = useQuery({ queryKey: ['electronic-forms'], queryFn: ({ signal }) => listElectronicForms(signal) })
  const records = useQuery({ queryKey: ['electronic-form-records'], queryFn: ({ signal }) => listElectronicFormRecords(signal) })
  const published = forms.data?.filter(item => item.currentPublishedVersionId) ?? []
  const error = forms.error ?? records.error

  return <Box>
    <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', alignItems: { md: 'center' }, mb: 2.5 }}>
      <Box>
        <Typography variant="overline" color="primary.main" sx={{ fontWeight: 800 }}>Kodsuz kayıt platformu</Typography>
        <Typography variant="h4" sx={{ fontWeight: 850 }}>Elektronik Formlar</Typography>
        <Typography color="text.secondary">Formları kod yazmadan tasarlayın, kontrollü yayımlayın ve imzalı PDF çıktıları üretin.</Typography>
      </Box>
      <Stack direction="row" spacing={1}>
        {can(Permissions.formManage) && <Button variant="outlined" startIcon={<AutoAwesomeRounded />} onClick={() => setCreateOpen(true)}>Yeni form</Button>}
        <Button variant="contained" startIcon={<EditNoteRounded />} disabled={published.length === 0} onClick={() => setCreateRecordOpen(true)}>Form doldur</Button>
      </Stack>
    </Stack>

    <Paper variant="outlined" sx={{ mb: 2.5 }}>
      <Stack direction={{ xs: 'column', md: 'row' }} sx={{ p: 2, gap: 2, justifyContent: 'space-between' }}>
        <Stack direction="row" spacing={3}>
          <Metric value={forms.data?.length ?? 0} label="Form tanımı" />
          <Metric value={published.length} label="Yayımlanmış" />
          <Metric value={records.data?.length ?? 0} label="Kayıt" />
        </Stack>
        <Chip color="success" variant="outlined" icon={<FactCheckRounded />} label="Sürümlü · E-imzalı · Denetime hazır" />
      </Stack>
      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ px: 2 }}>
        <Tab icon={<LibraryBooksRounded />} iconPosition="start" label="Form kataloğu" />
        <Tab icon={<DescriptionRounded />} iconPosition="start" label="Doldurulan kayıtlar" />
      </Tabs>
    </Paper>

    {error && <Alert severity="error" sx={{ mb: 2 }}>{error.message}</Alert>}
    {tab === 0 ? <FormCatalog loading={forms.isLoading} forms={forms.data ?? []} canManage={can(Permissions.formManage)} onOpen={setDesignerId} />
      : <RecordCatalog loading={records.isLoading} records={records.data ?? []} onOpen={setRecordId} />}

    <CreateFormDialog open={createOpen} onClose={() => setCreateOpen(false)} onCreated={(id) => { setCreateOpen(false); setDesignerId(id); setFeedback({ severity: 'success', message: 'Form taslağı oluşturuldu.' }) }} />
    <FormDesignerDialog id={designerId} onClose={() => setDesignerId(null)} onFeedback={setFeedback} />
    <CreateRecordDialog open={createRecordOpen} forms={published} onClose={() => setCreateRecordOpen(false)} onCreated={(id) => { setCreateRecordOpen(false); setRecordId(id); setTab(1); setFeedback({ severity: 'success', message: 'Form kaydı oluşturuldu.' }) }} />
    <RecordDialog id={recordId} onClose={() => setRecordId(null)} onFeedback={setFeedback} />
    <Snackbar open={Boolean(feedback)} autoHideDuration={5000} onClose={() => setFeedback(null)}>
      <Alert severity={feedback?.severity ?? 'success'} onClose={() => setFeedback(null)}>{feedback?.message}</Alert>
    </Snackbar>
  </Box>
}

function Metric({ value, label }: { value: number; label: string }) {
  return <Box><Typography variant="h5" sx={{ fontWeight: 850 }}>{value}</Typography><Typography variant="caption" color="text.secondary">{label}</Typography></Box>
}

function FormCatalog({ forms, loading, canManage, onOpen }: { forms: Awaited<ReturnType<typeof listElectronicForms>>; loading: boolean; canManage: boolean; onOpen: (id: string) => void }) {
  if (loading) return <Alert severity="info">Form kataloğu yükleniyor…</Alert>
  if (forms.length === 0) return <Paper variant="outlined" sx={{ p: 4, textAlign: 'center' }}><AutoAwesomeRounded color="primary" sx={{ fontSize: 42 }} /><Typography variant="h6" sx={{ mt: 1 }}>İlk elektronik formunuzu oluşturun</Typography><Typography color="text.secondary">Boş formdan veya hazır ekipman kontrol şablonundan başlayabilirsiniz.</Typography></Paper>
  return <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }}>
    {forms.map(form => <Paper key={form.id} variant="outlined" sx={{ p: 2.25 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 2 }}>
        <Box><Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: .5 }}><Chip size="small" label={form.code} /><Chip size="small" color={form.currentPublishedVersionId ? 'success' : 'warning'} variant="outlined" label={form.currentPublishedVersionId ? `Yayında · v${form.currentPublishedVersionNumber}` : statusLabel(form.latestVersionStatus)} /></Stack><Typography variant="h6" sx={{ fontWeight: 800 }}>{form.name}</Typography><Typography variant="body2" color="text.secondary">{form.category} · {kindLabel(form.kind)}</Typography></Box>
        <Button size="small" onClick={() => onOpen(form.id)}>{canManage ? 'Tasarla / görüntüle' : 'Görüntüle'}</Button>
      </Stack>
      <Typography variant="body2" sx={{ mt: 1.5 }}>{form.description || 'Açıklama girilmemiş.'}</Typography>
    </Paper>)}
  </Box>
}

function RecordCatalog({ records, loading, onOpen }: { records: Awaited<ReturnType<typeof listElectronicFormRecords>>; loading: boolean; onOpen: (id: string) => void }) {
  if (loading) return <Alert severity="info">Elektronik kayıtlar yükleniyor…</Alert>
  if (records.length === 0) return <Alert severity="info">Henüz doldurulmuş elektronik form kaydı bulunmuyor.</Alert>
  return <Paper variant="outlined"><Table><TableHead><TableRow><TableCell>Kayıt</TableCell><TableCell>Form</TableCell><TableCell>Oluşturan</TableCell><TableCell>Durum</TableCell><TableCell>Güncelleme</TableCell><TableCell /></TableRow></TableHead><TableBody>
    {records.map(record => <TableRow hover key={record.id}><TableCell><Typography sx={{ fontWeight: 800 }}>{record.recordNumber}</Typography><Typography variant="caption">Form v{record.formVersionNumber}</Typography></TableCell><TableCell>{record.formName}<Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{record.formCode}</Typography></TableCell><TableCell>{record.createdBy}</TableCell><TableCell><StatusChip value={record.status} /></TableCell><TableCell>{formatDate(record.updatedAtUtc)}</TableCell><TableCell align="right"><Button size="small" onClick={() => onOpen(record.id)}>Aç</Button></TableCell></TableRow>)}
  </TableBody></Table></Paper>
}

function CreateFormDialog({ open, onClose, onCreated }: { open: boolean; onClose: () => void; onCreated: (id: string) => void }) {
  const queryClient = useQueryClient()
  const [template, setTemplate] = useState<'blank' | 'equipment'>('blank')
  const [form, setForm] = useState({ code: '', name: '', description: '', category: 'Genel', kind: 'Standard' })
  const create = useMutation({
    mutationFn: () => createElectronicForm({ ...form, schema: template === 'equipment' ? equipmentSchema() : blankSchema(), outputTemplate: defaultOutput, changeSummary: 'İlk form sürümü', workflowType: 'ReviewApprove' }),
    onSuccess: async result => { await queryClient.invalidateQueries({ queryKey: ['electronic-forms'] }); setForm({ code: '', name: '', description: '', category: 'Genel', kind: 'Standard' }); onCreated(result.definition.id) },
  })
  return <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm"><ModalHeader onClose={onClose}><Typography variant="h5" sx={{ fontWeight: 850 }}>Yeni form oluştur</Typography><Typography variant="body2" color="text.secondary">Teknik ayar gerekmeden bir başlangıç seçin.</Typography></ModalHeader><DialogContent dividers><Stack spacing={2} sx={{ pt: 1 }}>
    {create.isError && <Alert severity="error">{create.error.message}</Alert>}
    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1.5 }}>
      <Paper variant="outlined" onClick={() => setTemplate('blank')} sx={{ p: 2, cursor: 'pointer', borderColor: template === 'blank' ? 'primary.main' : undefined }}><DescriptionRounded color="primary" /><Typography sx={{ fontWeight: 800 }}>Boş form</Typography><Typography variant="caption" color="text.secondary">Alanları kendiniz ekleyin.</Typography></Paper>
      <Paper variant="outlined" onClick={() => { setTemplate('equipment'); setForm(value => ({ ...value, name: value.name || 'Ekipman bakım ve kontrol formu', category: 'Ekipman' })) }} sx={{ p: 2, cursor: 'pointer', borderColor: template === 'equipment' ? 'primary.main' : undefined }}><SettingsRounded color="primary" /><Typography sx={{ fontWeight: 800 }}>Ekipman kontrolü</Typography><Typography variant="caption" color="text.secondary">Hazır alanlarla başlayın.</Typography></Paper>
    </Box>
    <TextField required label="Form kodu" placeholder="FR-EKP-001" value={form.code} onChange={event => setForm({ ...form, code: event.target.value.toUpperCase() })} slotProps={{ htmlInput: { maxLength: 64 } }} />
    <TextField required label="Form adı" value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} />
    <TextField label="Açıklama" multiline minRows={2} value={form.description} onChange={event => setForm({ ...form, description: event.target.value })} />
    <Stack direction="row" spacing={1.5}><TextField required fullWidth label="Kategori" value={form.category} onChange={event => setForm({ ...form, category: event.target.value })} /><TextField select fullWidth label="Form türü" value={form.kind} onChange={event => setForm({ ...form, kind: event.target.value })}><MenuItem value="Standard">Standart form</MenuItem><MenuItem value="Checklist">Kontrol listesi</MenuItem><MenuItem value="Assessment">Değerlendirme</MenuItem><MenuItem value="Logbook">Kayıt defteri</MenuItem></TextField></Stack>
  </Stack></DialogContent><DialogActions><Button onClick={onClose}>Vazgeç</Button><Button variant="contained" disabled={!form.code.trim() || !form.name.trim() || !form.category.trim() || create.isPending} onClick={() => create.mutate()}>{create.isPending ? 'Oluşturuluyor…' : 'Oluştur ve tasarla'}</Button></DialogActions></Dialog>
}

function designerDraftFromDetails(details: ElectronicFormDetails): DesignerDraft {
  const latest = details.versions[0]
  return { name: details.definition.name, description: details.definition.description, category: details.definition.category, kind: details.definition.kind, schema: structuredClone(latest.schema), output: { ...latest.outputTemplate }, changeSummary: latest.changeSummary }
}

function FormDesignerDialog({ id, onClose, onFeedback }: { id: string | null; onClose: () => void; onFeedback: (value: { severity: 'success' | 'error'; message: string }) => void }) {
  const queryClient = useQueryClient()
  const { user, can } = useAuth()
  const details = useQuery({ queryKey: ['electronic-form', id], queryFn: ({ signal }) => getElectronicForm(id!, signal), enabled: Boolean(id), refetchOnWindowFocus: false })
  const latest = details.data?.versions[0]
  const editable = latest?.status === 'Draft' && latest.createdByUserId === user.id
  const recoveryKey = id && latest ? formDraftRecoveryKey(user.id, id, latest.id, latest.rowVersion) : null
  const { draft, dirty, recovered, stale, setDraft, markSaved, reloadServer } = useFormDraftRecovery({
    identity: id ? `${user.id}:${id}` : null,
    recoveryKey,
    serverDraft: details.data && latest ? designerDraftFromDetails(details.data) : null,
    enabled: editable,
  })
  const [preview, setPreview] = useState(false)
  const [designerReset, setDesignerReset] = useState(0)
  const discardDraft = () => {
    if (!window.confirm('Bu sekmedeki kaydedilmemiş değişiklikler bırakılsın ve sunucudaki son kayıt açılsın mı?')) return
    reloadServer()
    setDesignerReset(value => value + 1)
    setPreview(false)
  }
  const [previewData, setPreviewData] = useState<Record<string, unknown>>({})
  const [publishOpen, setPublishOpen] = useState(false)
  const [password, setPassword] = useState('')
  const [accepted, setAccepted] = useState(false)
  const [comment, setComment] = useState('')
  const [nextSummary, setNextSummary] = useState('Yeni kontrollü sürüm')
  useEffect(() => setPreviewData({}), [id, latest?.id])
  const refresh = async () => { await queryClient.invalidateQueries({ queryKey: ['electronic-forms'] }); await queryClient.invalidateQueries({ queryKey: ['electronic-form', id] }) }
  const save = useMutation({ mutationFn: () => updateElectronicForm(id!, { definitionExpectedVersion: details.data!.definition.definitionVersion, formVersionExpectedVersion: latest!.rowVersion, outputTemplateExpectedVersion: latest!.outputTemplateRowVersion, name: draft!.name, description: draft!.description, category: draft!.category, kind: draft!.kind, schema: normalizedSchema(draft!.schema), outputTemplate: draft!.output, changeSummary: draft!.changeSummary, workflowType: 'ReviewApprove' }), onSuccess: async saved => {
    const savedVersion = saved.versions[0]
    markSaved(designerDraftFromDetails(saved), formDraftRecoveryKey(user.id, id!, savedVersion.id, savedVersion.rowVersion))
    queryClient.setQueryData(['electronic-form', id], saved)
    await refresh()
    onFeedback({ severity: 'success', message: 'Form taslağı kaydedildi.' })
  }, onError: error => onFeedback({ severity: 'error', message: error.message }) })
  const submit = useMutation({ mutationFn: () => submitElectronicFormForReview(id!, latest!.rowVersion, draft?.changeSummary), onSuccess: async () => { await refresh(); onFeedback({ severity: 'success', message: 'Form incelemeye gönderildi.' }) }, onError: error => onFeedback({ severity: 'error', message: error.message }) })
  const publish = useMutation({ mutationFn: () => publishElectronicForm(id!, { expectedVersion: latest!.rowVersion, password, meaningAccepted: accepted, comment }), onSuccess: async () => { setPublishOpen(false); setPassword(''); setAccepted(false); setComment(''); await refresh(); onFeedback({ severity: 'success', message: 'Form sürümü yayımlandı ve kullanıma açıldı.' }) }, onError: error => onFeedback({ severity: 'error', message: error.message }) })
  const newVersion = useMutation({ mutationFn: () => startElectronicFormVersion(id!, { definitionExpectedVersion: details.data!.definition.definitionVersion, changeSummary: nextSummary }), onSuccess: async () => { await refresh(); onFeedback({ severity: 'success', message: 'Yeni form sürümü taslağı oluşturuldu.' }) }, onError: error => onFeedback({ severity: 'error', message: error.message }) })
  useEffect(() => {
    if (!dirty) return
    const warn = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [dirty])
  if (!id) return null
  const requestClose = () => { if (!dirty || window.confirm('Kaydedilmemiş form değişiklikleri var. Yine de kapatılsın mı?')) onClose() }
  return <Dialog open onClose={requestClose} fullScreen scroll="paper" className="qms-form-studio-dialog" sx={{ '& > .MuiDialog-container': { p: '0 !important' }, '& > .MuiDialog-container > .MuiDialog-paper': { width: '100vw !important', height: '100dvh !important', maxWidth: 'none !important', maxHeight: '100dvh !important', m: '0 !important', border: '0 !important', borderRadius: '0 !important' } }}>
    <Stack direction="row" sx={{ minHeight: 52, px: 2, gap: 1.5, alignItems: 'center', flexShrink: 0, bgcolor: '#fff', borderBottom: '1px solid #e2e8f0' }}>
      <Box sx={{ width: 30, height: 32, borderRadius: 1, bgcolor: '#e7f3f1', color: '#0f766e', display: 'grid', placeItems: 'center' }}><DescriptionRounded sx={{ fontSize: 20 }} /></Box>
      <Typography noWrap sx={{ minWidth: 0, fontSize: 15, fontWeight: 750 }}>{draft?.name || details.data?.definition.name || 'Form tasarımcısı'}</Typography>
      <Typography noWrap variant="caption" color="text.secondary" sx={{ flexShrink: 0 }}>{details.data?.definition.code}{latest ? ` · Sürüm ${latest.versionNumber}` : ''}</Typography>
      <Box sx={{ flex: 1 }} />
      <Tooltip title="Tasarım stüdyosunu kapat"><IconButton size="small" aria-label="Tasarım stüdyosunu kapat" onClick={requestClose}><CloseRounded sx={{ fontSize: 20 }} /></IconButton></Tooltip>
    </Stack>
    <DialogContent dividers sx={{ '&&': { p: '0 !important', overflow: 'hidden', display: 'flex', flexDirection: 'column', minHeight: 0, border: 0 } }}>
    {details.isLoading && <Alert severity="info" sx={{ m: 2 }}>Form yükleniyor…</Alert>}
    {details.isError && <Alert severity="error" sx={{ m: 2 }}>{details.error.message}</Alert>}
    {draft && latest && <Box sx={{ height: '100%', minHeight: 0, display: 'flex', flexDirection: 'column' }}>
      <Stack direction={{ xs: 'column', md: 'row' }} sx={{ px: 2, py: .6, gap: 1, flexShrink: 0, alignItems: { md: 'center' }, justifyContent: 'space-between', bgcolor: '#f8fafc', borderBottom: '1px solid #e2e8f0' }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}><StatusChip value={latest.status} /><Typography noWrap variant="caption" color="text.secondary">{latest.createdBy}</Typography>{dirty && <Chip size="small" color="warning" variant="outlined" label="Kaydedilmemiş değişiklikler" sx={{ height: 23, fontSize: 11 }} />}{details.data?.definition.currentPublishedVersionNumber && <Chip size="small" color="success" variant="outlined" label={`Yayında v${details.data.definition.currentPublishedVersionNumber}`} sx={{ height: 23 }} />}</Stack>
        <Stack direction="row" spacing={.75}>
          <Button size="small" startIcon={<FactCheckRounded />} onClick={() => setPreview(value => !value)}>{preview ? 'Tasarımı göster' : 'Formu ön izle'}</Button>
          {editable && dirty && <Button size="small" color="inherit" disabled={save.isPending} onClick={discardDraft}>Değişiklikleri bırak</Button>}
          {editable && <Button size="small" variant="outlined" startIcon={<SaveRounded />} disabled={!dirty || stale || save.isPending} onClick={() => save.mutate()}>Taslağı kaydet</Button>}
          {editable && <Tooltip title={dirty ? 'İncelemeye göndermeden önce değişiklikleri kaydedin.' : ''}><span><Button size="small" variant="contained" startIcon={<CheckCircleRounded />} disabled={dirty || stale || submit.isPending} onClick={() => submit.mutate()}>İncelemeye gönder</Button></span></Tooltip>}
          {latest.status === 'InReview' && can(Permissions.formApprove) && latest.createdByUserId !== user.id && <Button size="small" variant="contained" color="success" startIcon={<PublishRounded />} onClick={() => setPublishOpen(true)}>Yayımla</Button>}
        </Stack>
      </Stack>
      {recovered && <Alert severity="info" sx={{ py: 0, borderRadius: 0, '& .MuiAlert-message': { py: .5 } }}>Bu sekmedeki taslak geri yüklendi.</Alert>}
      {stale && <Alert severity="warning" sx={{ py: 0, borderRadius: 0 }} action={<Button size="small" onClick={discardDraft}>Sunucudaki sürümü aç</Button>}>Sunucudaki form değişti. Bu sekmedeki değişiklikleriniz korunuyor.</Alert>}
      {latest.status === 'InReview' && latest.createdByUserId === user.id && <Alert severity="info" sx={{ m: 2 }}>Görev ayrılığı gereği bu sürümü başka bir Kalite Güvence veya Sistem Yöneticisi yayımlamalıdır.</Alert>}
      {latest.status === 'Published' && can(Permissions.formManage) && <Paper variant="outlined" sx={{ m: 2, p: 2 }}><Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ alignItems: { md: 'center' } }}><TextField fullWidth size="small" label="Yeni sürüm değişiklik amacı" value={nextSummary} onChange={event => setNextSummary(event.target.value)} /><Button variant="outlined" disabled={!nextSummary.trim() || newVersion.isPending} onClick={() => newVersion.mutate()}>Yeni sürüm başlat</Button></Stack></Paper>}
      {preview && <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto', bgcolor: '#e9edf2', p: 3 }}><Paper square variant="outlined" sx={previewPageSx(draft.schema)}><Box sx={hasFreeFormPositions(draft.schema) ? { height: `${HEADER_RESERVE_MM}mm`, boxSizing: 'border-box', overflow: 'visible' } : undefined}><Typography variant="overline" sx={{ color: draft.output.primaryColor, fontWeight: 850 }}>{details.data?.definition.code} · Ön izleme</Typography><Typography variant="h4" sx={{ fontWeight: 850, mb: .5 }}>{draft.name}</Typography><Typography color="text.secondary" sx={{ mb: 3 }}>{draft.description}</Typography></Box><DynamicForm schema={draft.schema} values={previewData} onChange={setPreviewData} /></Paper></Box>}
      <Box sx={{ display: preview ? 'none' : 'flex', flex: 1, minHeight: 0, flexDirection: 'column' }}><AdvancedFormDesigner draft={draft} onChange={setDraft} editable={editable && !save.isPending} metadataEditable={editable && !save.isPending && !details.data?.definition.currentPublishedVersionId} revisionKey={`${latest.id}:${latest.rowVersion}:${designerReset}`} /></Box>
    </Box>}
  </DialogContent><Dialog open={publishOpen} onClose={() => setPublishOpen(false)} fullWidth maxWidth="xs"><ModalHeader onClose={() => setPublishOpen(false)}><Typography variant="h6" sx={{ fontWeight: 850 }}>Form sürümünü yayımla</Typography></ModalHeader><DialogContent dividers><Stack spacing={2} sx={{ pt: 1 }}><Alert severity="warning">Yayımlanan form ve çıktı şablonu değiştirilemez. Değişiklik için yeni sürüm gerekir.</Alert><TextField type="password" label="Parolanız" value={password} onChange={event => setPassword(event.target.value)} /><TextField label="Yayımlama notu" multiline value={comment} onChange={event => setComment(event.target.value)} /><FormControlLabel control={<Checkbox checked={accepted} onChange={event => setAccepted(event.target.checked)} />} label="Form içeriğini ve elektronik imzanın anlamını kabul ediyorum." /></Stack></DialogContent><DialogActions><Button onClick={() => setPublishOpen(false)}>Vazgeç</Button><Button variant="contained" color="success" disabled={!password || !accepted || publish.isPending} onClick={() => publish.mutate()}>E-imza ile yayımla</Button></DialogActions></Dialog>
  </Dialog>
}

function CreateRecordDialog({ open, forms, onClose, onCreated }: { open: boolean; forms: Awaited<ReturnType<typeof listElectronicForms>>; onClose: () => void; onCreated: (id: string) => void }) {
  const queryClient = useQueryClient()
  const [formId, setFormId] = useState('')
  const [values, setValues] = useState<Record<string, unknown>>({})
  const details = useQuery({ queryKey: ['electronic-form', formId], queryFn: ({ signal }) => getElectronicForm(formId, signal), enabled: Boolean(formId) })
  const version = currentPublished(details.data)
  useEffect(() => setValues({}), [formId])
  const create = useMutation({ mutationFn: () => createElectronicFormRecord(formId, values), onSuccess: async result => { await queryClient.invalidateQueries({ queryKey: ['electronic-form-records'] }); setFormId(''); setValues({}); onCreated(result.record.id) } })
  return <Dialog open={open} onClose={onClose} fullWidth maxWidth="md"><ModalHeader onClose={onClose}><Typography variant="h5" sx={{ fontWeight: 850 }}>Elektronik form doldur</Typography><Typography variant="body2" color="text.secondary">Kayıt, seçilen yayımlanmış form sürümüne sabitlenir.</Typography></ModalHeader><DialogContent dividers><Stack spacing={2} sx={{ pt: 1 }}>{create.isError && <Alert severity="error">{create.error.message}</Alert>}<TextField select label="Yayımlanmış form" value={formId} onChange={event => setFormId(event.target.value)}><MenuItem value="">Form seçin</MenuItem>{forms.map(form => <MenuItem key={form.id} value={form.id}>{form.code} · {form.name} · v{form.currentPublishedVersionNumber}</MenuItem>)}</TextField>{version && <Paper variant="outlined" sx={{ p: 2.5 }}><Typography variant="h6" sx={{ fontWeight: 850, mb: 2 }}>{details.data?.definition.name}</Typography><DynamicForm schema={version.schema} values={values} onChange={setValues} /></Paper>}</Stack></DialogContent><DialogActions><Button onClick={onClose}>Vazgeç</Button><Button variant="contained" disabled={!formId || !version || create.isPending} onClick={() => create.mutate()}>{create.isPending ? 'Kaydediliyor…' : 'Taslak oluştur'}</Button></DialogActions></Dialog>
}

function RecordDialog({ id, onClose, onFeedback }: { id: string | null; onClose: () => void; onFeedback: (value: { severity: 'success' | 'error'; message: string }) => void }) {
  const queryClient = useQueryClient()
  const { user, can } = useAuth()
  const details = useQuery({ queryKey: ['electronic-form-record', id], queryFn: ({ signal }) => getElectronicFormRecord(id!, signal), enabled: Boolean(id) })
  const [values, setValues] = useState<Record<string, unknown>>({})
  const [approveOpen, setApproveOpen] = useState(false)
  const [password, setPassword] = useState('')
  const [accepted, setAccepted] = useState(false)
  const [comment, setComment] = useState('')
  useEffect(() => { if (details.data) setValues(structuredClone(details.data.data)) }, [details.data])
  const refresh = async () => { await queryClient.invalidateQueries({ queryKey: ['electronic-form-records'] }); await queryClient.invalidateQueries({ queryKey: ['electronic-form-record', id] }) }
  const save = useMutation({ mutationFn: () => updateElectronicFormRecord(id!, details.data!.record.version, values), onSuccess: async () => { await refresh(); onFeedback({ severity: 'success', message: 'Form kaydı taslağı kaydedildi.' }) }, onError: error => onFeedback({ severity: 'error', message: error.message }) })
  const submit = useMutation({ mutationFn: () => submitElectronicFormRecord(id!, details.data!.record.version), onSuccess: async () => { await refresh(); onFeedback({ severity: 'success', message: 'Form kaydı onaya gönderildi.' }) }, onError: error => onFeedback({ severity: 'error', message: error.message }) })
  const approve = useMutation({ mutationFn: () => approveElectronicFormRecord(id!, { expectedVersion: details.data!.record.version, password, meaningAccepted: accepted, comment }), onSuccess: async () => { setApproveOpen(false); setPassword(''); setAccepted(false); setComment(''); await refresh(); onFeedback({ severity: 'success', message: 'Form kaydı e-imzayla onaylandı ve kapatıldı.' }) }, onError: error => onFeedback({ severity: 'error', message: error.message }) })
  if (!id) return null
  const editable = details.data?.record.status === 'Draft' && details.data.record.createdByUserId === user.id
  const canApprove = details.data?.record.status === 'Submitted' && can(Permissions.formApprove) && details.data.record.createdByUserId !== user.id
  return <Dialog open onClose={onClose} fullWidth maxWidth="lg" scroll="paper"><ModalHeader onClose={onClose}><Typography variant="h5" sx={{ fontWeight: 850 }}>{details.data?.record.formName ?? 'Elektronik form kaydı'}</Typography><Typography variant="body2" color="text.secondary">{details.data?.record.recordNumber} · {details.data?.record.formCode} v{details.data?.record.formVersionNumber}</Typography></ModalHeader><DialogContent dividers>
    {details.isLoading && <Alert severity="info">Kayıt yükleniyor…</Alert>}{details.isError && <Alert severity="error">{details.error.message}</Alert>}{details.data && <Stack spacing={2.5}><Stack direction={{ xs: 'column', md: 'row' }} sx={{ justifyContent: 'space-between', gap: 1.5 }}><Stack direction="row" spacing={1}><StatusChip value={details.data.record.status} /><Chip size="small" variant="outlined" label={`Oluşturan: ${details.data.record.createdBy}`} /></Stack><Stack direction="row" spacing={1}>{editable && <Button variant="outlined" startIcon={<SaveRounded />} disabled={save.isPending} onClick={() => save.mutate()}>Taslağı kaydet</Button>}{editable && <Button variant="contained" startIcon={<CheckCircleRounded />} disabled={submit.isPending} onClick={() => submit.mutate()}>Onaya gönder</Button>}{canApprove && <Button variant="contained" color="success" startIcon={<FactCheckRounded />} onClick={() => setApproveOpen(true)}>İncele ve onayla</Button>}{details.data.record.status === 'Closed' && <Button variant="contained" startIcon={<DownloadRounded />} onClick={() => downloadElectronicFormFinalReport(id, details.data!.record.recordNumber).catch(error => onFeedback({ severity: 'error', message: error.message }))}>Nihai PDF</Button>}</Stack></Stack><Paper variant="outlined" sx={{ p: { xs: 2, md: 3 } }}><DynamicForm schema={details.data.schema} values={values} onChange={setValues} readOnly={!editable} /></Paper><ManagedFilesPanel aggregateType="ElectronicFormRecord" aggregateId={id} /><Paper variant="outlined" sx={{ p: 2 }}><Typography variant="h6" sx={{ fontWeight: 850, mb: 1.5 }}>Elektronik imzalar ve denetim izi</Typography>{details.data.signatures.map(signature => <Alert key={signature.id} icon={<FactCheckRounded />} severity="success" sx={{ mb: 1 }}>{signature.meaning} · {signature.signer} · {formatDate(signature.signedAtUtc)} · {signature.contentHash.slice(0, 16)}…</Alert>)}{details.data.signatures.length === 0 && <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>Henüz elektronik imza bulunmuyor.</Typography>}<Table size="small"><TableHead><TableRow><TableCell>İşlem</TableCell><TableCell>Kullanıcı</TableCell><TableCell>Tarih</TableCell><TableCell>Gerekçe</TableCell></TableRow></TableHead><TableBody>{details.data.auditTrail.map((item, index) => <TableRow key={`${item.eventType}-${index}`}><TableCell>{auditLabel(item.eventType)}</TableCell><TableCell>{item.actor}</TableCell><TableCell>{formatDate(item.occurredAtUtc)}</TableCell><TableCell>{item.reason ?? '—'}</TableCell></TableRow>)}</TableBody></Table></Paper></Stack>}
  </DialogContent><Dialog open={approveOpen} onClose={() => setApproveOpen(false)} fullWidth maxWidth="xs"><ModalHeader onClose={() => setApproveOpen(false)}><Typography variant="h6" sx={{ fontWeight: 850 }}>Kaydı elektronik imzala</Typography></ModalHeader><DialogContent dividers><Stack spacing={2} sx={{ pt: 1 }}><TextField type="password" label="Parolanız" value={password} onChange={event => setPassword(event.target.value)} /><TextField multiline label="Onay notu" value={comment} onChange={event => setComment(event.target.value)} /><FormControlLabel control={<Checkbox checked={accepted} onChange={event => setAccepted(event.target.checked)} />} label="Gösterilen kayıt içeriğini inceleyip onayladım." /></Stack></DialogContent><DialogActions><Button onClick={() => setApproveOpen(false)}>Vazgeç</Button><Button color="success" variant="contained" disabled={!password || !accepted || approve.isPending} onClick={() => approve.mutate()}>E-imza ile onayla</Button></DialogActions></Dialog></Dialog>
}

export function previewPageSx(schema: ElectronicFormSchema) {
  const page = schema.document ?? defaultDocumentSettings()
  const body = bodyMetrics(schema)
  const positionedBottom = schema.sections.flatMap(blocksFor).reduce((bottom, block) => Math.max(bottom, block.position ? block.position.y + block.position.height : 0), body.height)
  return {
    width: `${body.pageWidth}mm`,
    minHeight: `${Math.max(body.pageHeight, page.marginTop + HEADER_RESERVE_MM + positionedBottom + page.marginBottom)}mm`,
    boxSizing: 'border-box', mx: 'auto', borderRadius: 0,
    overflow: 'visible',
    padding: `${page.marginTop}mm ${page.marginRight}mm ${page.marginBottom}mm ${page.marginLeft}mm`,
    fontFamily: cssFont(page.defaultFontFamily), fontSize: `${page.defaultFontSize}pt`,
  }
}

export function DynamicForm({ schema, values, onChange, readOnly = false }: { schema: ElectronicFormSchema; values: Record<string, unknown>; onChange: (value: Record<string, unknown>) => void; readOnly?: boolean }) {
  const bodyRef = useRef<HTMLDivElement>(null)
  const [measuredBottom, setMeasuredBottom] = useState(0)
  const positioned = hasFreeFormPositions(schema)
  const metrics = bodyMetrics(schema)
  const minimumHeight = schema.sections.flatMap(blocksFor).reduce((bottom, block) => Math.max(bottom, block.position ? block.position.y + block.position.height : 0), metrics.height)
  useEffect(() => {
    const body = bodyRef.current
    if (!body || !positioned || typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(() => {
      const top = body.getBoundingClientRect().top
      const bottom = [...body.querySelectorAll<HTMLElement>('[data-form-positioned]')].reduce((extent, element) => Math.max(extent, element.getBoundingClientRect().bottom - top), 0)
      setMeasuredBottom(previous => Math.abs(previous - bottom) < .5 ? previous : bottom)
    })
    body.querySelectorAll('[data-form-positioned]').forEach(element => observer.observe(element))
    return () => observer.disconnect()
  }, [schema, positioned, readOnly, values])
  const set = (key: string, value: unknown) => onChange({ ...values, [key]: value })
  const content = <Stack spacing={3}>{[...schema.sections].sort((left, right) => left.order - right.order).map(section => <Box key={section.key}><Typography variant="h6" sx={{ fontWeight: 850 }}>{section.title}</Typography>{section.description && <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>{section.description}</Typography>}<Box sx={{ display: 'grid', gridTemplateColumns: positioned ? (section.columns === 2 ? 'repeat(2, minmax(0, 1fr))' : '1fr') : { xs: '1fr', md: section.columns === 2 ? 'repeat(2, minmax(0, 1fr))' : '1fr' }, gap: 1.5 }}>{blocksFor(section).map(block => <DynamicDocumentBlock key={block.key} block={block} fields={section.fields} values={values} readOnly={readOnly} onSet={set} fixedLayout={positioned} />)}</Box></Box>)}</Stack>
  return positioned ? <Box ref={bodyRef} data-form-body="positioned" sx={{ position: 'relative', width: `${metrics.width}mm`, minHeight: `${Math.max(minimumHeight, measuredBottom / PIXELS_PER_MM)}mm`, overflow: 'visible' }}>{content}</Box> : content
}

function DynamicDocumentBlock({ block, fields, values, readOnly, onSet, fixedLayout = false }: { block: FormDocumentBlock; fields: FormField[]; values: Record<string, unknown>; readOnly: boolean; onSet: (key: string, value: unknown) => void; fixedLayout?: boolean }) {
  if (block.position) {
    const field = block.type === 'field' ? fields.find(item => item.key === block.fieldKey) : null
    if (block.type === 'field' && (!field || !conditionMatches(field.visibilityCondition, values))) return null
    const { position, ...content } = block
    return <><Box aria-hidden="true" data-form-position-placeholder={block.key} sx={{ gridColumn: block.type !== 'field' || field?.width === 12 ? '1 / -1' : undefined, minHeight: `${position.height}mm` }} /><Box data-form-positioned={block.key} sx={{ position: 'absolute', left: `${position.x}mm`, top: `${position.y}mm`, width: `${position.width}mm`, minHeight: `${position.height}mm`, boxSizing: 'border-box', overflow: 'visible' }}><DynamicDocumentBlock block={content} fields={fields} values={values} readOnly={readOnly} onSet={onSet} /></Box></>
  }
  if (block.type === 'text') return <Box sx={{ gridColumn: '1 / -1', px: .5, py: .25, bgcolor: block.style.backgroundColor }}><RichTextDisplay text={block.text} runs={block.runs} style={block.style} /></Box>
  if (block.type === 'table') return <Box sx={{ gridColumn: '1 / -1', display: 'grid', gridTemplateColumns: tableColumnWidths(block).map(width => `minmax(0, ${width}fr)`).join(' ') }}>
    {visibleTableCells(block).map(cell => {
      const field = cell.fieldKey ? fields.find(item => item.key === cell.fieldKey) : null
      return <Box key={cell.key} sx={{ minWidth: 0, p: field ? 1 : .8, gridColumn: `${cell.column + 1} / span ${cell.colSpan ?? 1}`, gridRow: `${cell.row + 1} / span ${cell.rowSpan ?? 1}`, borderStyle: 'solid', borderColor: block.borderColor, borderWidth: `${block.borderWidth}pt`, bgcolor: cell.backgroundColor }}>
        {field ? (conditionMatches(field.visibilityCondition, values) ? <DynamicField field={field} value={values[field.key]} values={values} readOnly={readOnly} style={cell.style} onChange={value => onSet(field.key, value)} /> : null) : <RichTextDisplay text={cell.text} runs={cell.runs} style={cell.style} />}
      </Box>
    })}
  </Box>
  const field = fields.find(item => item.key === block.fieldKey)
  if (!field || !conditionMatches(field.visibilityCondition, values)) return null
  return <Box sx={{ gridColumn: fixedLayout ? (field.width === 12 ? '1 / -1' : undefined) : { md: field.width === 12 ? '1 / -1' : undefined } }}><DynamicField field={field} value={values[field.key]} values={values} readOnly={readOnly} style={block.style} onChange={value => onSet(field.key, value)} /></Box>
}

function hasFreeFormPositions(schema: ElectronicFormSchema) {
  return schema.sections.some(section => blocksFor(section).some(block => Boolean(block.position)))
}

function DynamicField({ field, value, values, readOnly, style, onChange }: { field: FormField; value: unknown; values: Record<string, unknown>; readOnly: boolean; style?: FormTextStyle; onChange: (value: unknown) => void }) {
  const inputId = useId()
  const required = field.required || Boolean(field.requiredCondition && conditionMatches(field.requiredCondition, values))
  const labelStyle = style ? textStyleSx(style) : undefined
  if (readOnly) return <Box><Typography variant="caption" sx={{ color: 'text.secondary', ...labelStyle }}>{field.label}</Typography><Typography sx={{ fontWeight: 650, whiteSpace: 'pre-wrap' }}>{displayValue(field, value)}</Typography></Box>
  if (field.type === 'checkbox') return <FormControlLabel sx={labelStyle ? { '& .MuiFormControlLabel-label': labelStyle } : undefined} control={<Checkbox checked={Boolean(value)} onChange={event => onChange(event.target.checked)} />} label={`${field.label}${required ? ' *' : ''}`} />
  if (field.type === 'multiSelect') { const selected = Array.isArray(value) ? value as string[] : []; return <FormControl fullWidth required={required}><InputLabel id={`${inputId}-label`} sx={labelStyle}>{field.label}</InputLabel><Select multiple id={inputId} labelId={`${inputId}-label`} label={field.label} value={selected} onChange={event => onChange(typeof event.target.value === 'string' ? event.target.value.split(',') : event.target.value)}>{field.options.map(option => <MenuItem key={option.value} value={option.value}><Checkbox checked={selected.includes(option.value)} />{option.label}</MenuItem>)}</Select>{field.helpText && <Typography variant="caption" color="text.secondary" sx={{ mt: .5, ml: 1.5 }}>{field.helpText}</Typography>}</FormControl> }
  if (field.type === 'singleSelect') return <TextField fullWidth select required={required} label={field.label} helperText={field.helpText} value={String(value ?? '')} slotProps={{ inputLabel: { sx: labelStyle } }} onChange={event => onChange(event.target.value)}><MenuItem value="">Seçin</MenuItem>{field.options.map(option => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}</TextField>
  const inputType = field.type === 'number' ? 'number' : field.type === 'date' ? 'date' : field.type === 'dateTime' ? 'datetime-local' : 'text'
  const shownValue = field.type === 'dateTime' && typeof value === 'string' ? localDateTimeInputValue(value) : value ?? ''
  return <TextField fullWidth required={required} type={inputType} multiline={field.type === 'longText'} minRows={field.type === 'longText' ? 3 : undefined} label={field.label} placeholder={field.placeholder ?? undefined} helperText={field.helpText || field.unit || undefined} value={shownValue as string | number} slotProps={{ inputLabel: { ...(field.type === 'date' || field.type === 'dateTime' ? { shrink: true } : {}), sx: labelStyle } }} onChange={event => { if (field.type === 'number') onChange(event.target.value === '' ? '' : Number(event.target.value)); else if (field.type === 'dateTime') onChange(event.target.value ? new Date(event.target.value).toISOString() : ''); else onChange(event.target.value) }} />
}

function localDateTimeInputValue(value: string) {
  if (!value) return ''
  const date = new Date(value)
  if (!Number.isFinite(date.getTime())) return ''
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}

function currentPublished(details?: ElectronicFormDetails) { return details?.versions.find(version => version.id === details.definition.currentPublishedVersionId) }
function normalizedSchema(schema: ElectronicFormSchema): ElectronicFormSchema { const result = materializeSchema(schema); return { ...result, sections: result.sections.map((section, sectionIndex) => ({ ...section, order: sectionIndex + 1, fields: section.fields.map((field, fieldIndex) => ({ ...field, order: fieldIndex + 1 })), blocks: normalizeBlocks(blocksFor(section)) })) } }
function textStyleSx(style: FormTextStyle) { return { fontFamily: cssFont(style.fontFamily), fontSize: `${style.fontSize}pt`, fontWeight: style.bold ? 700 : 400, fontStyle: style.italic ? 'italic' : 'normal', textDecoration: style.underline ? 'underline' : 'none', textAlign: style.alignment, color: style.color } }
function conditionMatches(condition: FormCondition | null | undefined, values: Record<string, unknown>) { if (!condition) return true; const actual = values[condition.fieldKey]; if (condition.operator === 'hasValue') return actual !== undefined && actual !== null && actual !== '' && (!Array.isArray(actual) || actual.length > 0); if (condition.operator === 'notEquals') return actual !== condition.value; return actual === condition.value }
function displayValue(field: FormField, value: unknown) { if (value === undefined || value === null || value === '') return '—'; if (field.type === 'checkbox') return value ? 'Evet' : 'Hayır'; if (field.type === 'singleSelect') return field.options.find(item => item.value === value)?.label ?? String(value); if (field.type === 'multiSelect' && Array.isArray(value)) return value.map(item => field.options.find(option => option.value === item)?.label ?? item).join(', '); if (field.type === 'dateTime') return formatDate(String(value)); return `${String(value)}${field.type === 'number' && field.unit ? ` ${field.unit}` : ''}` }
function statusLabel(value: string | null) { return ({ Draft: 'Taslak', InReview: 'İncelemede', Published: 'Yayımlandı', Submitted: 'Onay bekliyor', Closed: 'Kapalı' } as Record<string, string>)[value ?? ''] ?? value ?? 'Taslak' }
function kindLabel(value: string) { return ({ Standard: 'Standart form', Logbook: 'Kayıt defteri', Checklist: 'Kontrol listesi', Assessment: 'Değerlendirme' } as Record<string, string>)[value] ?? value }
function StatusChip({ value }: { value: string | null }) { const color = value === 'Published' || value === 'Closed' ? 'success' : value === 'InReview' || value === 'Submitted' ? 'warning' : 'default'; return <Chip size="small" color={color} label={statusLabel(value)} /> }
function auditLabel(value: string) { return ({ ElectronicFormRecordCreated: 'Kayıt oluşturuldu', ElectronicFormRecordDraftUpdated: 'Taslak güncellendi', ElectronicFormRecordSubmitted: 'Onaya gönderildi', ElectronicFormRecordApproved: 'E-imzayla onaylandı' } as Record<string, string>)[value] ?? value }
function formatDate(value: string) { return new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) }
