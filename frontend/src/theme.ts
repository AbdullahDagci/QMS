import { alpha, createTheme } from '@mui/material/styles'

const metronicBlue = '#009ef7'
const metronicInk = '#181c32'
const metronicMuted = '#7e8299'
const metronicBorder = '#e4e6ef'
const metronicCanvas = '#f5f8fa'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: metronicBlue,
      dark: '#008bd9',
      light: '#50bfff',
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#7239ea',
    },
    success: {
      main: '#50cd89',
      dark: '#3aa76d',
    },
    warning: {
      main: '#ffc700',
      dark: '#d9a900',
    },
    error: {
      main: '#f1416c',
      dark: '#d9214f',
    },
    info: {
      main: '#009ef7',
    },
    background: {
      default: metronicCanvas,
      paper: '#ffffff',
    },
    text: {
      primary: metronicInk,
      secondary: metronicMuted,
    },
    divider: '#eff2f5',
  },
  shape: {
    borderRadius: 8,
  },
  typography: {
    fontFamily: 'Inter, Aptos, "Segoe UI", Roboto, Arial, sans-serif',
    h1: { fontWeight: 700, letterSpacing: '-0.025em' },
    h2: { fontWeight: 700, letterSpacing: '-0.022em' },
    h3: { fontWeight: 700, letterSpacing: '-0.018em' },
    h4: { fontWeight: 700, letterSpacing: '-0.016em' },
    h5: { fontWeight: 700, letterSpacing: '-0.012em' },
    h6: { fontWeight: 700 },
    button: {
      fontWeight: 600,
      textTransform: 'none',
    },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: metronicCanvas,
          color: metronicInk,
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          border: '1px solid #eff2f5',
          borderRadius: 12,
          boxShadow: '0 0 20px 0 rgba(76, 87, 125, 0.05)',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        rounded: {
          borderRadius: 12,
        },
      },
    },
    MuiButton: {
      defaultProps: {
        disableElevation: true,
      },
      styleOverrides: {
        root: {
          minHeight: 42,
          borderRadius: 8,
          paddingInline: 18,
          '&.MuiButton-containedPrimary': {
            boxShadow: `0 4px 12px ${alpha(metronicBlue, 0.18)}`,
            '&:hover': {
              backgroundColor: '#008bd9',
              boxShadow: `0 6px 16px ${alpha(metronicBlue, 0.24)}`,
            },
          },
        },
        outlined: {
          borderColor: metronicBorder,
          '&:hover': {
            borderColor: metronicBlue,
            backgroundColor: '#f1faff',
          },
        },
      },
    },
    MuiTextField: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiFormControl: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          minHeight: 46,
          borderRadius: 8,
          backgroundColor: metronicCanvas,
          color: metronicInk,
          transition: 'border-color 160ms ease, box-shadow 160ms ease, background-color 160ms ease',
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: metronicBorder,
          },
          '&:hover .MuiOutlinedInput-notchedOutline': {
            borderColor: '#b5b5c3',
          },
          '&.Mui-focused': {
            backgroundColor: '#ffffff',
            boxShadow: `0 0 0 3px ${alpha(metronicBlue, 0.12)}`,
          },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderColor: metronicBlue,
            borderWidth: 1,
          },
          '&.MuiInputBase-multiline': {
            padding: '12px 14px',
          },
        },
        input: {
          padding: '11.5px 14px',
        },
      },
    },
    MuiInputLabel: {
      styleOverrides: {
        root: {
          color: metronicMuted,
          '&.Mui-focused': {
            color: metronicBlue,
          },
        },
      },
    },
    MuiAutocomplete: {
      defaultProps: {
        size: 'small',
        autoHighlight: true,
        openOnFocus: true,
      },
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            minHeight: 46,
            paddingTop: 4,
            paddingBottom: 4,
          },
          '& .MuiAutocomplete-input': {
            minHeight: 28,
            paddingTop: '0 !important',
            paddingBottom: '0 !important',
          },
        },
        paper: {
          marginTop: 6,
          border: '1px solid #eff2f5',
          borderRadius: 8,
          boxShadow: '0 8px 30px rgba(76, 87, 125, 0.16)',
        },
        option: {
          minHeight: 40,
          '&[aria-selected="true"]': {
            backgroundColor: '#f1faff',
            color: metronicBlue,
          },
          '&.Mui-focused': {
            backgroundColor: '#f5f8fa',
          },
        },
      },
    },
    MuiSelect: {
      styleOverrides: {
        select: {
          display: 'flex',
          alignItems: 'center',
        },
      },
    },
    MuiTableHead: {
      styleOverrides: {
        root: {
          backgroundColor: '#f9f9f9',
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: {
          color: '#a1a5b7',
          fontSize: '0.72rem',
          fontWeight: 700,
          letterSpacing: '0.06em',
          textTransform: 'uppercase',
        },
        root: {
          borderColor: '#eff2f5',
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          height: 28,
          border: '1px solid transparent',
          borderRadius: 6,
          fontWeight: 700,
          letterSpacing: '0.005em',
          '&.MuiChip-sizeSmall': {
            height: 26,
          },
          '&.MuiChip-colorPrimary, &.MuiChip-colorInfo': {
            color: '#006eab',
            borderColor: '#bde5fb',
            backgroundColor: '#f1faff',
          },
          '&.MuiChip-colorSuccess': {
            color: '#1f7a4d',
            borderColor: '#b9ecd2',
            backgroundColor: '#e8fff3',
          },
          '&.MuiChip-colorError': {
            color: '#c7184a',
            borderColor: '#ffc1d1',
            backgroundColor: '#fff5f8',
          },
          '&.MuiChip-colorWarning': {
            color: '#806500',
            borderColor: '#f3dfa0',
            backgroundColor: '#fff8dd',
          },
          '&.MuiChip-colorDefault': {
            color: '#5e6278',
            borderColor: '#e4e6ef',
            backgroundColor: '#f9f9f9',
          },
          '& .MuiChip-icon': {
            color: 'inherit',
          },
        },
        label: {
          paddingLeft: 9,
          paddingRight: 9,
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          border: '1px solid #eff2f5',
          borderRadius: 12,
          boxShadow: '0 20px 60px rgba(24, 28, 50, 0.2)',
        },
      },
    },
    MuiDialogTitle: {
      styleOverrides: {
        root: {
          borderBottom: '1px solid #eff2f5',
          fontWeight: 700,
        },
      },
    },
    MuiSkeleton: {
      styleOverrides: {
        root: {
          backgroundColor: alpha(metronicBlue, 0.09),
        },
      },
    },
  },
})
