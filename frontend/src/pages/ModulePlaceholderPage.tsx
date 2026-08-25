import { Box, Button, Chip, Paper, Typography } from '@mui/material'
import { ConstructionRounded, KeyboardBackspaceRounded } from '@mui/icons-material'
import { Link, useLocation } from 'react-router'
import { allNavigationItems } from '../navigation'

export function ModulePlaceholderPage() {
  const location = useLocation()
  const module = allNavigationItems.find((item) => item.path === location.pathname)

  return (
    <Paper className="placeholder-page" elevation={0}>
      <Box className="placeholder-icon"><ConstructionRounded /></Box>
      <Chip label={`${module?.code ?? 'Modül'} · Planlandı`} color="primary" variant="outlined" />
      <Typography variant="h3">{module?.title ?? 'Modül'}</Typography>
      <Typography color="text.secondary" sx={{ maxWidth: 560, lineHeight: 1.7 }}>
        Bu modül bağlı kalite zinciri mimarisinde planlandı. M.01 onayından sonra analiz sırasına göre iş akışı,
        veri modeli ve ekranları geliştirilecek.
      </Typography>
      <Button component={Link} to="/" startIcon={<KeyboardBackspaceRounded />}>Dashboard'a dön</Button>
    </Paper>
  )
}
