import { lazy, Suspense, useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  TextField,
  Typography,
} from '@mui/material'
import { AddRounded } from '@mui/icons-material'
import {
  createDeviation,
  searchDeviations,
  submitDeviation,
  type CreateDeviationInput,
  type DeviationListItem,
} from '../../api/deviations'
import {
  DeviationFilters,
} from './DeviationFilters'
import { emptyDeviationFilters, toColumnFilters, type DeviationFilterState } from './deviationFilterModel'
import { SearchableSelect, type SelectOption } from '../../components/SearchableSelect'
import { TableLoadingRows } from '../../components/TableLoadingRows'
import { ModalHeader } from '../../components/ModalHeader'
import { AdvancedFilterButton } from '../../components/AdvancedFilterPanel'
import { ModuleGuideDialog, ModuleInfoButton } from '../../components/ModuleGuideDialog'
import { Permissions, useAuth } from '../../security/AuthContext'

const DeviationDetailsDialog = lazy(() =>
  import('./DeviationDetailsDialog').then((module) => ({ default: module.DeviationDetailsDialog })))

type DeviationFormValues = Omit<CreateDeviationInput, 'occurredAtUtc' | 'detectedAtUtc'> & {
  occurredAtUtc: string
  detectedAtUtc: string
}

const statusLabels: Record<string, string> = {
  Draft: 'Taslak',
  Submitted: 'Gönderildi',
  PreliminaryReview: 'Ön inceleme',
  Investigation: 'Araştırma',
  ImpactAssessment: 'Etki değerlendirmesi',
  QualityAssessment: 'KG değerlendirmesi',
  ActionImplementation: 'Aksiyon uygulama',
  EffectivenessReview: 'Etkinlik',
  ClosureApproval: 'Kapanış onayı',
  Closed: 'Kapalı',
  Voided: 'İptal',
}

const classificationLabels = { Minor: 'Minör', Major: 'Majör', Critical: 'Kritik' }
const deviationTypeOptions: Array<SelectOption<string>> = [
  { value: 'Proses', label: 'Proses' },
  { value: 'Ürün', label: 'Ürün' },
  { value: 'Ekipman', label: 'Ekipman' },
  { value: 'Doküman', label: 'Doküman' },
  { value: 'Diğer', label: 'Diğer' },
]
const riskOptions: Array<SelectOption<number>> = [1, 2, 3, 4, 5].map((value) => ({ value, label: String(value) }))

export function DeviationWorkspace() {
  const [dialogOpen, setDialogOpen] = useState(false)
  const [guideOpen, setGuideOpen] = useState(false)
  const [selectedDeviationId, setSelectedDeviationId] = useState<string | null>(null)
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [filters, setFilters] = useState<DeviationFilterState>(emptyDeviationFilters)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState<10 | 25 | 50 | 100>(25)
  const [sortBy, setSortBy] = useState('createdAtUtc')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc')
  const queryClient = useQueryClient()
  const { can } = useAuth()
  const columnFilters = useMemo(() => toColumnFilters(filters), [filters])
  const searchRequest = useMemo(() => ({
    page: page + 1,
    pageSize,
    sortBy,
    sortDirection,
    filters: columnFilters,
  }), [page, pageSize, sortBy, sortDirection, columnFilters])
  const deviations = useQuery({
    queryKey: ['deviations', searchRequest],
    queryFn: ({ signal }) => searchDeviations(searchRequest, signal),
    placeholderData: (previous) => previous,
    retry: false,
  })
  const submitMutation = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => submitDeviation(id, version),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['deviations'] }),
  })

  return (
    <Box component="section" id="deviations" className="deviation-section">
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        sx={{ justifyContent: 'space-between', alignItems: { xs: 'flex-start', sm: 'center' }, gap: 2 }}
      >
        <Box>
          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
            <Typography variant="h2">Sapma iş listesi</Typography>
            <Chip size="small" color="primary" label="M.01 aktif" />
          </Stack>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Taslakları kaydedin, risk puanını otomatik hesaplayın ve kontrollü iş akışına gönderin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.2} sx={{ flexWrap: 'wrap' }}>
          <ModuleInfoButton module="M.01" onClick={() => setGuideOpen(true)} />
          {can(Permissions.deviationCreate) && <Button variant="contained" size="large" startIcon={<AddRounded />} onClick={() => setDialogOpen(true)}>Yeni sapma</Button>}
        </Stack>
      </Stack>

      <Stack direction="row" spacing={1.5} sx={{ mt: 3, alignItems: 'center' }}>
        <AdvancedFilterButton open={filtersOpen} activeCount={columnFilters.length} onClick={() => setFiltersOpen((current) => !current)} />
        <Typography variant="body2" color="text.secondary">
          {deviations.data ? `${deviations.data.totalCount} kayıt` : 'Kayıtlar yükleniyor'}
        </Typography>
      </Stack>

      {filtersOpen && (
        <DeviationFilters
          value={filters}
          onApply={(next) => { setFilters(next); setPage(0) }}
        />
      )}

      <Paper className="deviation-table-card" elevation={0}>
        {deviations.isError && (
          <Alert severity="warning" sx={{ m: 2 }}>
            Sapma veritabanına ulaşılamadı. PostgreSQL ve güncel API çalıştığında liste otomatik yüklenecek.
          </Alert>
        )}
        {submitMutation.isError && (
          <Alert severity="error" sx={{ m: 2 }}>
            {submitMutation.error.message}
          </Alert>
        )}
        <TableContainer>
          <Table aria-label="Sapma iş listesi">
            <TableHead>
              <TableRow>
                <SortableHeader field="recordNumber" label="Kayıt" sortBy={sortBy} direction={sortDirection} onSort={handleSort} />
                <SortableHeader field="title" label="Başlık / Bölüm" sortBy={sortBy} direction={sortDirection} onSort={handleSort} />
                <SortableHeader field="riskScore" label="Risk" sortBy={sortBy} direction={sortDirection} onSort={handleSort} />
                <SortableHeader field="status" label="Durum" sortBy={sortBy} direction={sortDirection} onSort={handleSort} />
                <SortableHeader field="targetDateUtc" label="Hedef" sortBy={sortBy} direction={sortDirection} onSort={handleSort} />
                <TableCell align="right">İşlem</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {deviations.isLoading && <TableLoadingRows columns={6} />}
              {deviations.data?.items.map((deviation) => (
                <DeviationRow
                  deviation={deviation}
                  key={deviation.id}
                  submitting={submitMutation.isPending && submitMutation.variables?.id === deviation.id}
                  canSubmit={can(Permissions.deviationCreate)}
                  onSubmit={() => submitMutation.mutate({ id: deviation.id, version: deviation.version })}
                  onOpen={() => setSelectedDeviationId(deviation.id)}
                />
              ))}
              {deviations.isSuccess && deviations.data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>
                    <Box className="empty-state">
                      <Typography sx={{ fontWeight: 750 }}>Henüz sapma kaydı yok</Typography>
                      <Typography variant="body2" color="text.secondary">
                        İlk kontrollü kalite kaydını oluşturmak için “Yeni sapma”yı kullanın.
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
          count={Number(deviations.data?.totalCount ?? 0)}
          page={page}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[10, 25, 50, 100]}
          labelRowsPerPage="Sayfa boyutu"
          labelDisplayedRows={({ from, to, count }) => `${from}–${to} / ${count}`}
          onPageChange={(_, nextPage) => setPage(nextPage)}
          onRowsPerPageChange={(event) => {
            setPageSize(Number(event.target.value) as 10 | 25 | 50 | 100)
            setPage(0)
          }}
        />
      </Paper>

      <DeviationDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
      <ModuleGuideDialog module="M.01" open={guideOpen} onClose={() => setGuideOpen(false)} />
      <Suspense fallback={null}>
        <DeviationDetailsDialog id={selectedDeviationId} onClose={() => setSelectedDeviationId(null)} />
      </Suspense>
    </Box>
  )

  function handleSort(field: string) {
    if (field === sortBy) setSortDirection((current) => current === 'asc' ? 'desc' : 'asc')
    else { setSortBy(field); setSortDirection('asc') }
    setPage(0)
  }
}

function SortableHeader({ field, label, sortBy, direction, onSort }: {
  field: string
  label: string
  sortBy: string
  direction: 'asc' | 'desc'
  onSort: (field: string) => void
}) {
  return (
    <TableCell sortDirection={sortBy === field ? direction : false}>
      <TableSortLabel
        active={sortBy === field}
        direction={sortBy === field ? direction : 'asc'}
        onClick={() => onSort(field)}
      >
        {label}
      </TableSortLabel>
    </TableCell>
  )
}

function DeviationRow({ deviation, submitting, canSubmit, onSubmit, onOpen }: {
  deviation: DeviationListItem
  submitting: boolean
  canSubmit: boolean
  onSubmit: () => void
  onOpen: () => void
}) {
  const riskColor = deviation.classification === 'Critical'
    ? 'error'
    : deviation.classification === 'Major' ? 'warning' : 'success'

  return (
    <TableRow hover>
      <TableCell>
        <Typography className="record-number">{deviation.recordNumber}</Typography>
        {deviation.capaRequired && <Chip size="small" label="DÖF gerekli" color="warning" variant="outlined" />}
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 700 }}>{deviation.title}</Typography>
        <Typography variant="caption" color="text.secondary">{deviation.detectedDepartment}</Typography>
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={riskColor}
          label={`${classificationLabels[deviation.classification]} · ${deviation.riskScore}`}
        />
      </TableCell>
      <TableCell>{statusLabels[deviation.status] ?? deviation.status}</TableCell>
      <TableCell>{formatDate(deviation.targetDateUtc)}</TableCell>
      <TableCell align="right">
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
          <Button size="small" onClick={onOpen}>Aç</Button>
          {canSubmit && deviation.status === 'Draft' && (
            <Button size="small" variant="outlined" disabled={submitting} onClick={onSubmit}>
              {submitting ? 'Gönderiliyor…' : 'İş akışına gönder'}
            </Button>
          )}
        </Stack>
      </TableCell>
    </TableRow>
  )
}

function DeviationDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const defaults = useMemo(() => defaultFormValues(), [])
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<DeviationFormValues>({ defaultValues: defaults })
  const factors = useWatch({ control, name: ['likelihood', 'severity', 'detectability'] })
  const riskScore = factors.reduce((total, factor) => total * Number(factor || 1), 1)
  const riskClass = riskScore <= 20 ? 'Minör' : riskScore <= 50 ? 'Majör' : 'Kritik'
  const createMutation = useMutation({
    mutationFn: createDeviation,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['deviations'] })
      reset(defaultFormValues())
      onClose()
    },
  })

  const close = () => {
    if (!createMutation.isPending) {
      createMutation.reset()
      reset(defaultFormValues())
      onClose()
    }
  }

  const onSubmit = handleSubmit((values) => createMutation.mutate({
    ...values,
    likelihood: Number(values.likelihood),
    severity: Number(values.severity),
    detectability: Number(values.detectability),
    occurredAtUtc: new Date(values.occurredAtUtc).toISOString(),
    detectedAtUtc: new Date(values.detectedAtUtc).toISOString(),
  }))

  return (
    <Dialog open={open} onClose={close} maxWidth="md" fullWidth>
      <Box component="form" onSubmit={onSubmit}>
        <ModalHeader
          onClose={close}
          closeDisabled={createMutation.isPending}
          closeLabel="Yeni sapma penceresini kapat"
        >
          <Typography variant="h5" sx={{ fontWeight: 800 }}>Yeni sapma kaydı</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Taslak oluşturulduğunda kayıt numarası sunucuda atomik olarak atanır.
          </Typography>
        </ModalHeader>
        <DialogContent>
          <Stack spacing={2.25}>
            {createMutation.isError && <Alert severity="error">{createMutation.error.message}</Alert>}
            <TextField
              label="Sapma başlığı"
              fullWidth
              error={Boolean(errors.title)}
              helperText={errors.title?.message}
              {...register('title', { required: 'Başlık zorunludur.', maxLength: 200 })}
            />
            <Box className="form-grid two-columns">
              <Controller
                name="deviationType"
                control={control}
                render={({ field }) => (
                  <SearchableSelect
                    label="Sapma türü"
                    value={field.value}
                    options={deviationTypeOptions}
                    onChange={(next) => field.onChange(next ?? '')}
                    size="medium"
                  />
                )}
              />
              <TextField label="Tespit eden bölüm" {...register('detectedDepartment', { required: true })} />
              <TextField label="Proses aşaması" {...register('processStage', { required: true })} />
              <TextField
                label="Gerçekleşme zamanı"
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                {...register('occurredAtUtc', { required: true })}
              />
              <TextField
                label="Tespit zamanı"
                type="datetime-local"
                slotProps={{ inputLabel: { shrink: true } }}
                {...register('detectedAtUtc', { required: true })}
              />
            </Box>
            <TextField
              label="Gerçekleşen sapmanın tanımı"
              multiline
              minRows={3}
              {...register('description', { required: 'Sapma tanımı zorunludur.' })}
            />
            <Box className="form-grid two-columns">
              <TextField
                label="Beklenen/onaylı durum"
                multiline
                minRows={2}
                {...register('expectedState', { required: 'Beklenen durum zorunludur.' })}
              />
              <TextField
                label="Acil aksiyon"
                multiline
                minRows={2}
                {...register('immediateAction', { required: 'Acil aksiyon zorunludur.' })}
              />
            </Box>
            <Paper variant="outlined" className="risk-panel">
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
                <Controller name="likelihood" control={control} render={({ field }) => <RiskSelect label="Olasılık" value={field.value} onChange={field.onChange} />} />
                <Controller name="severity" control={control} render={({ field }) => <RiskSelect label="Şiddet" value={field.value} onChange={field.onChange} />} />
                <Controller name="detectability" control={control} render={({ field }) => <RiskSelect label="Tespit edilebilirlik" value={field.value} onChange={field.onChange} />} />
                <Box className="risk-result">
                  <Typography variant="caption" color="text.secondary">O × Ş × T</Typography>
                  <Typography variant="h5" sx={{ fontWeight: 850 }}>{riskScore}</Typography>
                  <Typography variant="caption" sx={{ fontWeight: 750 }}>{riskClass}</Typography>
                </Box>
              </Stack>
            </Paper>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={close} disabled={createMutation.isPending}>Vazgeç</Button>
          <Button type="submit" variant="contained" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'Kaydediliyor…' : 'Taslak oluştur'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}

function RiskSelect({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) {
  return <SearchableSelect label={label} value={value} options={riskOptions} onChange={(next) => onChange(next ?? 1)} />
}

function defaultFormValues(): DeviationFormValues {
  const now = new Date()
  const detected = toLocalDateTime(now)
  const occurred = toLocalDateTime(new Date(now.getTime() - 30 * 60 * 1000))

  return {
    title: '',
    description: '',
    expectedState: '',
    immediateAction: '',
    deviationType: 'Proses',
    detectedDepartment: 'Üretim',
    processStage: 'Dolum',
    occurredAtUtc: occurred,
    detectedAtUtc: detected,
    likelihood: 2,
    severity: 2,
    detectability: 2,
  }
}

function toLocalDateTime(date: Date) {
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium' }).format(new Date(value))
}
