import type { ReactNode } from 'react'
import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material'
import { FilterAltRounded, RestartAltRounded, SearchRounded } from '@mui/icons-material'

export function AdvancedFilterButton({ open, activeCount, onClick }: {
  open: boolean
  activeCount: number
  onClick: () => void
}) {
  return (
    <Button
      variant={open ? 'contained' : 'outlined'}
      startIcon={<FilterAltRounded />}
      onClick={onClick}
    >
      Gelişmiş filtreler{activeCount ? ` (${activeCount})` : ''}
    </Button>
  )
}

export function AdvancedFilterPanel({
  children,
  activeCount,
  onApply,
  onClear,
}: {
  children: ReactNode
  activeCount: number
  onApply: () => void
  onClear: () => void
}) {
  return (
    <Paper variant="outlined" className="filter-panel advanced-filter-panel">
      <Stack direction={{ xs: 'column', sm: 'row' }} className="advanced-filter-heading">
        <Stack direction="row" spacing={1.2} sx={{ alignItems: 'center', minWidth: 0 }}>
          <Box className="section-heading-icon tone-violet"><FilterAltRounded /></Box>
          <Box sx={{ minWidth: 0 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
              <Typography sx={{ fontWeight: 800 }}>Gelişmiş kolon filtreleri</Typography>
              {activeCount > 0 && <Chip size="small" color="primary" label={`${activeCount} aktif`} />}
            </Stack>
            <Typography variant="caption" color="text.secondary">Filtreler veri tipine göre sunucuda uygulanır.</Typography>
          </Box>
        </Stack>
      </Stack>

      <Box className="filter-grid advanced-filter-grid">{children}</Box>

      <Stack direction={{ xs: 'column-reverse', sm: 'row' }} className="advanced-filter-actions">
        <Button variant="outlined" startIcon={<RestartAltRounded />} onClick={onClear}>Filtreleri temizle</Button>
        <Button variant="contained" startIcon={<SearchRounded />} onClick={onApply}>Filtreleri uygula</Button>
      </Stack>
    </Paper>
  )
}
