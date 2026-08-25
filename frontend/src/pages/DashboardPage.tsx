import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
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
  FactCheckRounded,
  GppMaybeRounded,
  Inventory2Rounded,
  LinkRounded,
  TaskAltRounded,
  WarningAmberRounded,
} from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
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

  return (
    <Stack spacing={3.5}>
      <Box className="dashboard-heading">
        <Box>
          <Typography className="page-eyebrow">OPERASYONEL GÖRÜNÜM</Typography>
          <Typography variant="h3">Kalite kontrol merkezi</Typography>
          <Typography color="text.secondary" sx={{ mt: 0.75 }}>
            Açık kayıtları, riskleri ve bağlı kalite zincirini tek ekrandan izleyin.
          </Typography>
        </Box>
        <Button component={Link} to="/modules/deviations" variant="contained" endIcon={<ArrowForwardRounded />}>
          Sapma iş listesi
        </Button>
      </Box>

      {summary.isError && (
        <Alert severity="warning">Dashboard verileri alınamadı. API bağlantısını kontrol edin.</Alert>
      )}

      <Box className="metric-grid">
        <MetricCard label="Toplam sapma" value={summary.data?.totalDeviations} loading={summary.isLoading} icon={Inventory2Rounded} tone="indigo" />
        <MetricCard label="Açık kayıt" value={summary.data?.openDeviations} loading={summary.isLoading} icon={AssignmentLateRounded} tone="cyan" />
        <MetricCard label="Majör / kritik" value={summary.data?.majorOrCriticalDeviations} loading={summary.isLoading} icon={GppMaybeRounded} tone="amber" />
        <MetricCard label="Termin gecikmiş" value={summary.data?.overdueDeviations} loading={summary.isLoading} icon={WarningAmberRounded} tone="rose" />
        <MetricCard label="DÖF gerekli" value={summary.data?.capaRequiredDeviations} loading={summary.isLoading} icon={TaskAltRounded} tone="green" />
      </Box>

      <Box className="dashboard-main-grid">
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

        <Paper className="dashboard-panel attention-panel" elevation={0}>
          <Typography variant="h6" sx={{ fontWeight: 750 }}>Dikkat gerektirenler</Typography>
          <Typography variant="body2" color="text.secondary">Önceliklendirme özeti</Typography>
          <Stack spacing={1.5} sx={{ mt: 2.5 }}>
            <AttentionRow label="Termin gecikmiş kayıt" value={summary.data?.overdueDeviations} tone="error" loading={summary.isLoading} />
            <AttentionRow label="Yüksek riskli açık kayıt" value={summary.data?.majorOrCriticalDeviations} tone="warning" loading={summary.isLoading} />
            <AttentionRow label="DÖF bağlantısı bekleyen" value={summary.data?.capaRequiredDeviations} tone="primary" loading={summary.isLoading} />
          </Stack>
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
    <Card className="metric-card">
      <CardContent>
        <Box className={`metric-icon tone-${tone}`}><Icon /></Box>
        {loading ? <Skeleton width={58} height={46} /> : <Typography className="metric-value">{value ?? 0}</Typography>}
        <Typography variant="body2" color="text.secondary">{label}</Typography>
      </CardContent>
    </Card>
  )
}

function AttentionRow({ label, value, tone, loading }: {
  label: string
  value?: number
  tone: 'error' | 'warning' | 'primary'
  loading: boolean
}) {
  return (
    <Box className="attention-row">
      <Typography variant="body2">{label}</Typography>
      {loading ? <Skeleton width={28} /> : <Chip color={tone} size="small" label={value ?? 0} />}
    </Box>
  )
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
