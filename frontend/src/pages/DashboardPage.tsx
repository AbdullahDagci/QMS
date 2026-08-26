import type { CSSProperties, ReactNode } from 'react'
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
  ArrowOutwardRounded,
  AssignmentLateRounded,
  AutoGraphRounded,
  BarChartRounded,
  DonutLargeRounded,
  FactCheckRounded,
  GppMaybeRounded,
  GridViewRounded,
  Inventory2Rounded,
  LinkRounded,
  TaskAltRounded,
  UpdateRounded,
  WarningAmberRounded,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { getDashboardSummary, type DashboardDeviation } from '../api/dashboard'

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

const chainSteps = [
  { code: 'M.01', title: 'Sapma', description: 'Bulguyu kaydet', icon: WarningAmberRounded, to: '/modules/deviations' },
  { code: 'M.02', title: 'DÖF', description: 'Kök nedeni gider', icon: TaskAltRounded, to: '/modules/m02' },
  { code: 'M.03', title: 'Değişiklik', description: 'Etkileri yönet', icon: LinkRounded, to: '/modules/m03' },
  { code: 'M.05', title: 'Eğitim', description: 'Yetkinliği doğrula', icon: FactCheckRounded, to: '/modules/m05' },
  { code: 'M.07', title: 'Denetim', description: 'Kanıtla ve kapat', icon: GppMaybeRounded, to: '/modules/m07' },
]

const quickActions = [
  { title: 'Sapma iş listesi', description: 'Açık kalite kayıtlarını yönet', icon: WarningAmberRounded, to: '/modules/deviations', tone: 'blue' },
  { title: 'DÖF merkezi', description: 'Aksiyon ve etkinlikleri izle', icon: TaskAltRounded, to: '/modules/m02', tone: 'green' },
  { title: 'Değişiklik kontrolü', description: 'Etki ve karar kapılarına git', icon: LinkRounded, to: '/modules/m03', tone: 'violet' },
  { title: 'Doküman yönetimi', description: 'Sürüm ve yürürlükleri aç', icon: FactCheckRounded, to: '/modules/m04', tone: 'amber' },
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
  const closureRate = total ? Math.round((closed / total) * 100) : 0
  const recent = summary.data?.recentDeviations ?? []
  const operationalRows = [
    { label: 'Açık kayıt', value: open, tone: 'blue' },
    { label: 'Majör / kritik', value: summary.data?.majorOrCriticalDeviations ?? 0, tone: 'red' },
    { label: 'Termin gecikmiş', value: summary.data?.overdueDeviations ?? 0, tone: 'amber' },
    { label: 'DÖF gerekli', value: summary.data?.capaRequiredDeviations ?? 0, tone: 'green' },
  ]
  const riskDistribution = [
    { label: 'Minör', value: recent.filter((item) => item.classification === 'Minor').length, tone: 'minor' },
    { label: 'Majör', value: recent.filter((item) => item.classification === 'Major').length, tone: 'major' },
    { label: 'Kritik', value: recent.filter((item) => item.classification === 'Critical').length, tone: 'critical' },
  ]

  return (
    <Box className="kt-dashboard">
      <Box component="header" className="kt-dashboard-toolbar">
        <Box>
          <Stack direction="row" spacing={0.8} className="kt-dashboard-breadcrumb">
            <GridViewRounded />
            <Typography>Kalite Yönetim Sistemi</Typography>
            <span>/</span>
            <Typography>Dashboard</Typography>
          </Stack>
          <Typography component="h1">Kalite kontrol merkezi</Typography>
          <Typography className="kt-dashboard-toolbar-copy">
            Öncelikli işleri, risk sinyallerini ve kapanış performansını tek çalışma alanından yönetin.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} className="kt-dashboard-toolbar-actions">
          <Button component={Link} to="/modules/m02" variant="outlined">DÖF iş listesi</Button>
          <Button component={Link} to="/modules/deviations" variant="contained" endIcon={<ArrowForwardRounded />}>Sapmaları aç</Button>
        </Stack>
      </Box>

      {summary.isError && (
        <Alert severity="warning" className="kt-dashboard-alert">Dashboard verileri alınamadı. API bağlantısını kontrol edin.</Alert>
      )}

      <Box className="kt-dashboard-overview-grid">
        <Paper className="kt-dashboard-focus-card" elevation={0}>
          <Box className="kt-dashboard-focus-orb" />
          <Box className="kt-dashboard-focus-content">
            <Stack direction="row" className="kt-dashboard-live-row">
              <Chip size="small" icon={<AutoGraphRounded />} label="Canlı operasyon" />
              <Typography>Güncel kalite portföyü</Typography>
            </Stack>
            {summary.isLoading ? (
              <Skeleton variant="rounded" height={150} className="kt-dark-skeleton" />
            ) : (
              <>
                <Typography className="kt-dashboard-focus-value">{open}</Typography>
                <Typography component="h2">açık kayıt kontrol bekliyor</Typography>
                <Typography className="kt-dashboard-focus-copy">
                  Toplam {total} sapmanın {closed} tanesi sonuçlandırıldı. Önceliği yüksek kayıtları iş listesinde yönetin.
                </Typography>
                <Box className="kt-dashboard-focus-progress">
                  <Stack direction="row"><Typography>Kapanış performansı</Typography><Typography>{closureRate}%</Typography></Stack>
                  <Box className="kt-dashboard-focus-track"><Box sx={{ width: `${closureRate}%` }} /></Box>
                </Box>
              </>
            )}
          </Box>
          <Button component={Link} to="/modules/deviations" endIcon={<ArrowOutwardRounded />}>Operasyonu yönet</Button>
        </Paper>

        <Box className="kt-dashboard-kpi-grid">
          <KpiCard label="Toplam sapma" helper="Portföy kapsamı" value={summary.data?.totalDeviations} loading={summary.isLoading} icon={Inventory2Rounded} tone="blue" />
          <KpiCard label="Majör / kritik" helper="Öncelikli takip" value={summary.data?.majorOrCriticalDeviations} loading={summary.isLoading} icon={GppMaybeRounded} tone="red" />
          <KpiCard label="Termin gecikmiş" helper="Hedef tarihi aştı" value={summary.data?.overdueDeviations} loading={summary.isLoading} icon={UpdateRounded} tone="amber" />
          <KpiCard label="DÖF gerekli" helper="Bağlı aksiyon" value={summary.data?.capaRequiredDeviations} loading={summary.isLoading} icon={TaskAltRounded} tone="green" />
        </Box>
      </Box>

      <Box className="kt-dashboard-analytics-grid">
        <Paper className="kt-dashboard-card kt-dashboard-operations-card" elevation={0}>
          <CardHeader icon={BarChartRounded} title="Operasyon yükü" description="Sapma portföyünün güncel iş dağılımı" action={<Chip size="small" label="Canlı" className="kt-dashboard-live-chip" />} />
          {summary.isLoading ? <Skeleton variant="rounded" height={220} /> : <OperationalBars rows={operationalRows} total={total} />}
        </Paper>

        <Paper className="kt-dashboard-card kt-dashboard-closure-card" elevation={0}>
          <CardHeader icon={DonutLargeRounded} title="Kapanış dengesi" description="Açık ve sonuçlandırılmış kayıt oranı" />
          {summary.isLoading ? <Skeleton variant="circular" width={176} height={176} /> : <ClosureDonut open={open} closed={closed} />}
        </Paper>
      </Box>

      <Box className="kt-dashboard-content-grid">
        <Paper className="kt-dashboard-card kt-dashboard-records-card" elevation={0}>
          <CardHeader icon={AssignmentLateRounded} title="Son sapma kayıtları" description="En son oluşturulan ve aksiyon bekleyen kayıtlar" action={<Button component={Link} to="/modules/deviations" size="small" endIcon={<ArrowForwardRounded />}>Tümünü gör</Button>} />
          <RecentDeviations loading={summary.isLoading} records={recent} />
        </Paper>

        <Stack spacing={2} className="kt-dashboard-side-column">
          <Paper className="kt-dashboard-card kt-dashboard-risk-card" elevation={0}>
            <CardHeader icon={GppMaybeRounded} title="Risk profili" description="Son kayıtlardaki sınıflandırma" />
            {summary.isLoading ? <Skeleton variant="rounded" height={170} /> : <RiskBars rows={riskDistribution} />}
          </Paper>

          <Paper className="kt-dashboard-card kt-dashboard-actions-card" elevation={0}>
            <CardHeader icon={GridViewRounded} title="Süreç kısayolları" description="Sık kullanılan çalışma alanları" />
            <Box className="kt-dashboard-quick-actions">
              {quickActions.map((action) => {
                const Icon = action.icon
                return (
                  <Box component={Link} to={action.to} className="kt-dashboard-quick-action" key={action.title}>
                    <Box className={`kt-dashboard-action-icon is-${action.tone}`}><Icon /></Box>
                    <Box><Typography>{action.title}</Typography><Typography>{action.description}</Typography></Box>
                    <ArrowForwardRounded />
                  </Box>
                )
              })}
            </Box>
          </Paper>
        </Stack>
      </Box>

      <Paper className="kt-dashboard-card kt-dashboard-chain-card" elevation={0}>
        <Box className="kt-dashboard-chain-heading">
          <Box className="kt-dashboard-card-icon"><FactCheckRounded /></Box>
          <Box><Typography component="h2">Bağlı kalite zinciri</Typography><Typography>Kaynak bulgudan kapanış kanıtına kadar uçtan uca izlenebilirlik.</Typography></Box>
        </Box>
        <Box className="kt-dashboard-chain">
          {chainSteps.map((step, index) => {
            const Icon = step.icon
            return (
              <Box component={Link} to={step.to} className="kt-dashboard-chain-step" key={step.code}>
                <Box className="kt-dashboard-chain-icon"><Icon /></Box>
                <Box><Typography className="kt-dashboard-chain-code">{step.code}</Typography><Typography className="kt-dashboard-chain-title">{step.title}</Typography><Typography className="kt-dashboard-chain-copy">{step.description}</Typography></Box>
                {index < chainSteps.length - 1 && <ArrowForwardRounded className="kt-dashboard-chain-arrow" />}
              </Box>
            )
          })}
        </Box>
      </Paper>
    </Box>
  )
}

function KpiCard({ label, helper, value, loading, icon: Icon, tone }: { label: string; helper: string; value?: number; loading: boolean; icon: typeof Inventory2Rounded; tone: string }) {
  return (
    <Paper className="kt-dashboard-kpi" elevation={0}>
      <Stack direction="row" className="kt-dashboard-kpi-topline"><Box className={`kt-dashboard-kpi-icon is-${tone}`}><Icon /></Box><ArrowOutwardRounded /></Stack>
      {loading ? <Skeleton width={64} height={48} /> : <Typography className="kt-dashboard-kpi-value">{value ?? 0}</Typography>}
      <Typography className="kt-dashboard-kpi-label">{label}</Typography>
      <Typography className="kt-dashboard-kpi-helper">{helper}</Typography>
    </Paper>
  )
}

function CardHeader({ icon: Icon, title, description, action }: { icon: typeof Inventory2Rounded; title: string; description: string; action?: ReactNode }) {
  return (
    <Stack direction="row" className="kt-dashboard-card-header">
      <Stack direction="row" spacing={1.4} className="kt-dashboard-card-title">
        <Box className="kt-dashboard-card-icon"><Icon /></Box>
        <Box><Typography component="h2">{title}</Typography><Typography>{description}</Typography></Box>
      </Stack>
      {action}
    </Stack>
  )
}

function OperationalBars({ rows, total }: { rows: Array<{ label: string; value: number; tone: string }>; total: number }) {
  const scale = Math.max(total, ...rows.map((row) => row.value), 1)
  return (
    <Box className="kt-dashboard-operation-bars" role="img" aria-label="Operasyon yükü çubuk grafiği">
      {rows.map((row) => (
        <Box className="kt-dashboard-operation-row" key={row.label}>
          <Stack direction="row"><Typography>{row.label}</Typography><Typography>{row.value}</Typography></Stack>
          <Box className="kt-dashboard-operation-track"><Box className={`is-${row.tone}`} sx={{ width: `${Math.max(row.value ? 5 : 0, (row.value / scale) * 100)}%` }} /></Box>
        </Box>
      ))}
      <Stack direction="row" className="kt-dashboard-operation-axis"><span>0</span><span>{Math.round(scale / 2)}</span><span>{scale}</span></Stack>
    </Box>
  )
}

function ClosureDonut({ open, closed }: { open: number; closed: number }) {
  const total = open + closed
  const openPercent = total ? Math.round((open / total) * 100) : 0
  return (
    <Box className="kt-dashboard-donut-layout">
      <Box className="kt-dashboard-donut" role="img" aria-label={`Açık kayıt oranı yüzde ${openPercent}`} sx={{ '--open-angle': `${openPercent * 3.6}deg` } as CSSProperties}>
        <Box><Typography>{openPercent}%</Typography><Typography>açık</Typography></Box>
      </Box>
      <Box className="kt-dashboard-donut-legend">
        <Stack direction="row"><span className="is-open" /><Typography>Açık kayıt</Typography><strong>{open}</strong></Stack>
        <Stack direction="row"><span className="is-closed" /><Typography>Kapalı kayıt</Typography><strong>{closed}</strong></Stack>
      </Box>
    </Box>
  )
}

function RiskBars({ rows }: { rows: Array<{ label: string; value: number; tone: string }> }) {
  const max = Math.max(...rows.map((row) => row.value), 1)
  return (
    <Box className="kt-dashboard-risk-bars" role="img" aria-label="Son kayıtların risk dağılımı">
      {rows.map((row) => (
        <Box className="kt-dashboard-risk-column" key={row.label}>
          <Typography>{row.value}</Typography>
          <Box className="kt-dashboard-risk-track"><Box className={`is-${row.tone}`} sx={{ height: `${Math.max(row.value ? 12 : 3, (row.value / max) * 100)}%` }} /></Box>
          <Typography>{row.label}</Typography>
        </Box>
      ))}
    </Box>
  )
}

function RecentDeviations({ loading, records }: { loading: boolean; records: DashboardDeviation[] }) {
  return (
    <TableContainer className="kt-dashboard-table-wrap">
      <Table aria-label="Son sapma kayıtları" className="kt-dashboard-table">
        <TableHead><TableRow><TableCell>Kayıt</TableCell><TableCell>Başlık</TableCell><TableCell>Risk</TableCell><TableCell>Durum</TableCell><TableCell>Hedef</TableCell><TableCell align="right">Aç</TableCell></TableRow></TableHead>
        <TableBody>
          {loading && Array.from({ length: 4 }, (_, index) => <TableRow key={index}>{Array.from({ length: 6 }, (_, cell) => <TableCell key={cell}><Skeleton animation="wave" /></TableCell>)}</TableRow>)}
          {!loading && records.map((record) => (
            <TableRow hover key={record.id}>
              <TableCell data-label="Kayıt"><Typography component={Link} to={`/modules/deviations?open=${record.id}`} className="kt-dashboard-record-number">{record.recordNumber}</Typography></TableCell>
              <TableCell data-label="Başlık"><Typography className="kt-dashboard-record-title">{record.title}</Typography></TableCell>
              <TableCell data-label="Risk"><Chip className={`semantic-risk-badge risk-${record.classification.toLowerCase()}`} size="small" label={`${riskLabel(record.classification)} · ${record.riskScore}`} /></TableCell>
              <TableCell data-label="Durum"><Chip size="small" className="kt-dashboard-status-chip" label={statusLabels[record.status] ?? record.status} /></TableCell>
              <TableCell data-label="Hedef"><Typography className="kt-dashboard-target-date">{formatDate(record.targetDateUtc)}</Typography></TableCell>
              <TableCell align="right" data-label="Aç"><Button component={Link} to={`/modules/deviations?open=${record.id}`} className="kt-dashboard-row-open" aria-label={`${record.recordNumber} kaydını aç`}><ArrowOutwardRounded /></Button></TableCell>
            </TableRow>
          ))}
          {!loading && records.length === 0 && <TableRow><TableCell colSpan={6}><Box className="kt-dashboard-empty">Henüz sapma kaydı bulunmuyor.</Box></TableCell></TableRow>}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

function riskLabel(value: DashboardDeviation['classification']) {
  return value === 'Minor' ? 'Minör' : value === 'Major' ? 'Majör' : 'Kritik'
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(value))
}
