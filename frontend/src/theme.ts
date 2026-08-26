import { alpha, createTheme } from '@mui/material/styles'

const controlTeal = '#087f78'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: controlTeal, dark: '#075f5b', light: '#51aaa4' },
    secondary: { main: '#315a9f', dark: '#203f77', light: '#7897c8' },
    success: { main: '#23845b' },
    warning: { main: '#b66b16' },
    error: { main: '#c34250' },
    background: { default: '#f4f5f0', paper: '#ffffff' },
    text: { primary: '#172b34', secondary: '#66767b' },
    divider: '#dfe5e1',
  },
  typography: {
    fontFamily: 'Aptos, "Avenir Next", "Segoe UI", sans-serif',
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
          border: '1px solid #dde5e1',
          boxShadow: '0 14px 38px rgba(27, 57, 62, 0.07)',
        },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: { root: { borderRadius: 9, minHeight: 40 } },
    },
    MuiPaper: {
      styleOverrides: { rounded: { borderRadius: 16 } },
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
      styleOverrides: { root: { backgroundColor: '#f3f7f5' } },
    },
    MuiTableCell: {
      styleOverrides: {
        head: { color: '#40565d', fontSize: '0.72rem', fontWeight: 800, letterSpacing: '0.035em', textTransform: 'uppercase' },
        root: { borderColor: '#e4e9e6' },
      },
    },
    MuiChip: {
      styleOverrides: { root: { fontWeight: 650 } },
    },
    MuiSkeleton: {
      styleOverrides: { root: { backgroundColor: alpha(controlTeal, 0.08) } },
    },
  },
})
