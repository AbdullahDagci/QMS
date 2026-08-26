import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Skeleton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import {
  ArrowForwardRounded,
  AssignmentLateRounded,
  BarChartRounded,
  DonutLargeRounded,
  FactCheckRounded,
  GppMaybeRounded,
  Inventory2Rounded,
  LinkRounded,
  TaskAltRounded,
  WarningAmberRounded,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import type { CSSProperties } from 'react'
import { getDashboardSummary, type DashboardDeviation } from '../api/dashboard'

const statusLabels: Record<string, string> = {
  Draft: 'Taslak', Submitted: 'Gönderildi', PreliminaryReview: 'Ön inceleme',
  Investigation: 'Araştırma', ImpactAssessment: 'Etki değerlendirmesi',
  QualityAssessment: 'KG değerlendirmesi', ActionImplementation: 'Aksiyon uygulama',
  EffectivenessReview: 'Etkinlik', ClosureApproval: 'Kapanış onayı', Closed: 'Kapalı', Voided: 'İptal',
}

const chainSteps = [
  { title: 'Sapma', description: 'Bulguyu kaydet', icon: WarningAmberRounded },
  { title: 'DÖF', description: 'Kök nedeni gider', icon: TaskAltRounded },
  { title: 'Değişiklik', description: 'Etkileri yönet', icon: LinkRounded },
  { title: 'Eğitim', description: 'Yetkinliği doğrula', icon: FactCheckRounded },
  { title: 'Denetim', description: 'Kanıtla ve kapat', icon: GppMaybeRounded },
]

export function DashboardPage() {
  const summary = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: ({ signal }) => getDashboardSummary(signal),
    retry: false,
  })

  const total = summary.data?.totalDeviations ?? 0
  const open = summary.data?.openDeviations ?? 0
  const closed = Math.max(0, total - open)
  const operationalBars = [
    { label: 'Açık kayıt', value: open, tone: 'teal' },
    { label: 'Majör / kritik', value: summary.data?.majorOrCriticalDeviations ?? 0, tone: 'amber' },
    { label: 'Termin gecikmiş', value: summary.data?.overdueDeviations ?? 0, tone: 'rose' },
    { label: 'DÖF gerekli', value: summary.data?.capaRequiredDeviations ?? 0, tone: 'blue' },
  ]
  const recent = summary.data?.recentDeviations ?? []
  const riskDistribution = [
    { label: 'Minör', value: recent.filter((item) => item.classification === 'Minor').length, tone: 'minor' },
    { label: 'Majör', value: recent.filter((item) => item.classification === 'Major').length, tone: 'major' },
    { label: 'Kritik', value: recent.filter((item) => item.classification === 'Critical').length, tone: 'critical' },
  ]

  return (
    <Stack spacing={2.5} className="dashboard-command-center">
      <Box className="dashboard-heading dashboard-command-hero">
        <Box>
          <Typography className="page-eyebrow">CANLI KALİTE OPERASYONU</Typography>
          <Typography variant="h3">Kalite kontrol merkezi</Typography>
          <Typography sx={{ mt: 1 }}>
            Risk sinyallerini, açık iş yükünü ve kapanış performansını tek bakışta yönetin.
          </Typography>
        </Box>
        <Button component={Link} to="/modules/deviations" variant="contained" endIcon={<ArrowForwardRounded />}>
          Sapma iş listesi
        </Button>
      </Box>

      {summary.isError && (
        <Alert severity="warning">Dashboard verileri alınamadı. API bağlantısını kontrol edin.</Alert>
      )}

      <Box className="metric-grid dashboard-metric-strip">
        <MetricCard label="Toplam sapma" value={summary.data?.totalDeviations} loading={summary.isLoading} icon={Inventory2Rounded} tone="indigo" />
        <MetricCard label="Açık kayıt" value={summary.data?.openDeviations} loading={summary.isLoading} icon={AssignmentLateRounded} tone="cyan" />
        <MetricCard label="Majör / kritik" value={summary.data?.majorOrCriticalDeviations} loading={summary.isLoading} icon={GppMaybeRounded} tone="amber" />
        <MetricCard label="Termin gecikmiş" value={summary.data?.overdueDeviations} loading={summary.isLoading} icon={WarningAmberRounded} tone="rose" />
        <MetricCard label="DÖF gerekli" value={summary.data?.capaRequiredDeviations} loading={summary.isLoading} icon={TaskAltRounded} tone="green" />
      </Box>

      <Box className="dashboard-analytics-grid">
        <Paper className="dashboard-panel operational-chart-panel" elevation={0}>
          <PanelHeading icon={BarChartRounded} eyebrow="OPERASYON YÜKÜ" title="Açık işlerin dağılımı" description="Toplam sapma hacmine göre güncel operasyon göstergeleri" />
          {summary.isLoading ? <Skeleton variant="rounded" height={210} /> : <OperationalBarChart rows={operationalBars} total={total} />}
        </Paper>
        <Paper className="dashboard-panel closure-chart-panel" elevation={0}>
          <PanelHeading icon={DonutLargeRounded} eyebrow="KAPANIŞ DURUMU" title="Portföy dengesi" description="Açık ve sonuçlandırılmış sapmalar" />
          {summary.isLoading ? <Skeleton variant="circular" width={180} height={180} /> : <ClosureDonut open={open} closed={closed} />}
        </Paper>
      </Box>

      <Box className="dashboard-lower-grid">
        <Paper className="dashboard-panel recent-records-panel" elevation={0}>
          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 750 }}>Son sapma kayıtları</Typography>
              <Typography variant="body2" color="text.secondary">En son oluşturulan kalite kayıtları</Typography>
            </Box>
            <Button component={Link} to="/modules/deviations" size="small">Tümünü gör</Button>
          </Stack>
          <RecentDeviations loading={summary.isLoading} records={summary.data?.recentDeviations ?? []} />
        </Paper>

        <Paper className="dashboard-panel risk-chart-panel" elevation={0}>
          <PanelHeading icon={GppMaybeRounded} eyebrow="RİSK PROFİLİ" title="Son kayıtların dağılımı" description="En yeni sapmalardaki sınıflandırma yoğunluğu" />
          {summary.isLoading ? <Skeleton variant="rounded" height={170} /> : <RiskBars rows={riskDistribution} />}
        </Paper>
      </Box>

      <Paper className="dashboard-panel quality-chain-panel" elevation={0}>
        <Box>
          <Typography variant="h6" sx={{ fontWeight: 750 }}>Bağlı kalite zinciri</Typography>
          <Typography variant="body2" color="text.secondary">
            Her kayıt kaynak bulgudan kapanış kanıtına kadar ilişkilendirilir.
          </Typography>
        </Box>
        <Box className="quality-chain">
          {chainSteps.map((step, index) => {
            const Icon = step.icon
            return (
              <Box className="quality-chain-step" key={step.title}>
                <Box className="quality-chain-icon"><Icon /></Box>
                <Box>
                  <Typography sx={{ fontWeight: 720 }}>{step.title}</Typography>
                  <Typography variant="caption" color="text.secondary">{step.description}</Typography>
                </Box>
                {index < chainSteps.length - 1 && <ArrowForwardRounded className="quality-chain-arrow" />}
              </Box>
            )
          })}
        </Box>
      </Paper>
    </Stack>
  )
}

function MetricCard({ label, value, loading, icon: Icon, tone }: {
  label: string
  value?: number
  loading: boolean
  icon: typeof Inventory2Rounded
  tone: string
}) {
  return (
    <Paper className="metric-card" elevation={0}>
      <Box className="metric-card-content">
        <Box className={`metric-icon tone-${tone}`}><Icon /></Box>
        {loading ? <Skeleton width={58} height={46} /> : <Typography className="metric-value">{value ?? 0}</Typography>}
        <Typography variant="body2" color="text.secondary">{label}</Typography>
      </Box>
    </Paper>
  )
}

function PanelHeading({ icon: Icon, eyebrow, title, description }: { icon: typeof Inventory2Rounded; eyebrow: string; title: string; description: string }) {
  return (
    <Stack direction="row" spacing={1.4} className="dashboard-panel-heading">
      <Box className="dashboard-panel-icon"><Icon /></Box>
      <Box><Typography variant="overline">{eyebrow}</Typography><Typography variant="h6">{title}</Typography><Typography variant="body2" color="text.secondary">{description}</Typography></Box>
    </Stack>
  )
}

function OperationalBarChart({ rows, total }: { rows: Array<{ label: string; value: number; tone: string }>; total: number }) {
  const scale = Math.max(total, ...rows.map((row) => row.value), 1)
  return <Box className="operational-bars" role="img" aria-label="Operasyon yükü çubuk grafiği">
    {rows.map((row) => <Box className="operational-bar-row" key={row.label}>
      <Stack direction="row" className="operational-bar-label"><Typography>{row.label}</Typography><Typography>{row.value}</Typography></Stack>
      <Box className="operational-bar-track"><Box className={`operational-bar-fill tone-${row.tone}`} sx={{ width: `${Math.max(row.value ? 6 : 0, row.value / scale * 100)}%` }} /></Box>
    </Box>)}
    <Stack direction="row" className="chart-axis"><span>0</span><span>{Math.round(scale / 2)}</span><span>{scale}</span></Stack>
  </Box>
}

function ClosureDonut({ open, closed }: { open: number; closed: number }) {
  const total = open + closed
  const openPercent = total ? Math.round(open / total * 100) : 0
  return <Box className="closure-donut-layout">
    <Box className="closure-donut" role="img" aria-label={`Açık kayıt oranı yüzde ${openPercent}`} sx={{ '--open-angle': `${openPercent * 3.6}deg` } as CSSProperties}>
      <Box><Typography>{openPercent}%</Typography><Typography variant="caption">açık</Typography></Box>
    </Box>
    <Box className="donut-legend"><span><i className="is-open" />Açık <strong>{open}</strong></span><span><i className="is-closed" />Kapalı <strong>{closed}</strong></span></Box>
  </Box>
}

function RiskBars({ rows }: { rows: Array<{ label: string; value: number; tone: string }> }) {
  const max = Math.max(...rows.map((row) => row.value), 1)
  return <Box className="risk-bars" role="img" aria-label="Son kayıtların risk dağılımı">
    {rows.map((row) => <Box className="risk-bar-column" key={row.label}>
      <Typography className="risk-bar-value">{row.value}</Typography>
      <Box className="risk-bar-track"><Box className={`risk-bar-fill tone-${row.tone}`} sx={{ height: `${Math.max(row.value ? 12 : 3, row.value / max * 100)}%` }} /></Box>
      <Typography variant="caption">{row.label}</Typography>
    </Box>)}
  </Box>
}

function RecentDeviations({ loading, records }: { loading: boolean; records: DashboardDeviation[] }) {
  return (
    <TableContainer>
      <Table size="small" aria-label="Son sapma kayıtları">
        <TableHead><TableRow><TableCell>Kayıt</TableCell><TableCell>Başlık</TableCell><TableCell>Risk</TableCell><TableCell>Durum</TableCell></TableRow></TableHead>
        <TableBody>
          {loading && Array.from({ length: 4 }, (_, index) => (
            <TableRow key={index}>{Array.from({ length: 4 }, (_, cell) => <TableCell key={cell}><Skeleton animation="wave" /></TableCell>)}</TableRow>
          ))}
          {!loading && records.map((record) => (
            <TableRow hover key={record.id}>
              <TableCell><Typography className="record-number">{record.recordNumber}</Typography></TableCell>
              <TableCell><Typography variant="body2" sx={{ fontWeight: 680 }}>{record.title}</Typography></TableCell>
              <TableCell><Chip size="small" color={record.classification === 'Critical' ? 'error' : record.classification === 'Major' ? 'warning' : 'success'} label={record.riskScore} /></TableCell>
              <TableCell><Typography variant="body2" color="text.secondary">{statusLabels[record.status] ?? record.status}</Typography></TableCell>
            </TableRow>
          ))}
          {!loading && records.length === 0 && (
            <TableRow><TableCell colSpan={4}><Box className="compact-empty-state">Henüz sapma kaydı bulunmuyor.</Box></TableCell></TableRow>
          )}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
