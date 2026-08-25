import { alpha, createTheme } from '@mui/material/styles'

const mistBlue = '#5f7f92'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: mistBlue, dark: '#476577', light: '#96afbd' },
    secondary: { main: '#718f7b', dark: '#557060', light: '#afc4b5' },
    success: { main: '#3f8f62' },
    warning: { main: '#b7791f' },
    error: { main: '#c94b55' },
    background: { default: '#f4f6f5', paper: '#ffffff' },
    text: { primary: '#263238', secondary: '#6b7780' },
    divider: '#e4e7ec',
  },
  typography: {
    fontFamily: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    h1: { fontWeight: 760, letterSpacing: '-0.035em' },
    h2: { fontWeight: 740, letterSpacing: '-0.025em' },
    h3: { fontWeight: 730, letterSpacing: '-0.02em' },
    h4: { fontWeight: 720, letterSpacing: '-0.02em' },
    button: { textTransform: 'none', fontWeight: 680 },
  },
  shape: { borderRadius: 12 },
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          border: '1px solid #e4e7ec',
          boxShadow: '0 8px 28px rgba(16, 24, 40, 0.045)',
        },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: { root: { borderRadius: 10 } },
    },
    MuiPaper: {
      styleOverrides: { rounded: { borderRadius: 14 } },
    },
    MuiDialogTitle: {
      styleOverrides: {
        root: {
          position: 'sticky',
          top: 0,
          zIndex: 3,
          paddingRight: 72,
          backgroundColor: '#ffffff',
          borderBottom: '1px solid #e4e7ec',
        },
      },
    },
    MuiTableHead: {
      styleOverrides: { root: { backgroundColor: '#f8fafc' } },
    },
    MuiTableCell: {
      styleOverrides: {
        head: { color: '#475467', fontSize: '0.75rem', fontWeight: 750 },
        root: { borderColor: '#eaecf0' },
      },
    },
    MuiChip: {
      styleOverrides: { root: { fontWeight: 650 } },
    },
    MuiSkeleton: {
      styleOverrides: { root: { backgroundColor: alpha(mistBlue, 0.08) } },
    },
  },
})
