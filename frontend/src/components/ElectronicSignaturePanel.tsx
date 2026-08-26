import {
  Box,
  Checkbox,
  FormControlLabel,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { FingerprintRounded, LockRounded } from "@mui/icons-material";

type ElectronicSignaturePanelProps = {
  meaning: string;
  password: string;
  accepted: boolean;
  onPasswordChange: (value: string) => void;
  onAcceptedChange: (value: boolean) => void;
  disabled?: boolean;
};

export function ElectronicSignaturePanel({
  meaning,
  password,
  accepted,
  onPasswordChange,
  onAcceptedChange,
  disabled = false,
}: ElectronicSignaturePanelProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        mt: 2,
        p: 2,
        borderColor: "divider",
        bgcolor: "background.paper",
        borderRadius: 2,
      }}
      data-testid="electronic-signature-panel"
    >
      <Stack direction="row" spacing={1.25} sx={{ alignItems: "flex-start" }}>
        <Box
          sx={{
            width: 38,
            height: 38,
            borderRadius: 1.5,
            display: "grid",
            placeItems: "center",
            color: "primary.main",
            bgcolor: "primary.50",
            flex: "0 0 auto",
          }}
        >
          <FingerprintRounded fontSize="small" />
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <Typography sx={{ fontWeight: 700 }}>Elektronik imza</Typography>
          <Typography variant="body2" color="text.secondary">
            Kimliğiniz yeniden doğrulanır; imzalanan kayıt sürümü ve içeriğin
            özeti değiştirilemez imza kaydına bağlanır.
          </Typography>
        </Box>
      </Stack>

      <Box
        sx={{
          mt: 1.75,
          px: 1.5,
          py: 1.25,
          borderRadius: 1.5,
          bgcolor: "action.hover",
          border: "1px solid",
          borderColor: "divider",
        }}
      >
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ fontWeight: 700, textTransform: "uppercase", letterSpacing: 0.7 }}
        >
          İmzanın anlamı
        </Typography>
        <Typography variant="body2" sx={{ mt: 0.25, fontWeight: 600 }}>
          {meaning}
        </Typography>
      </Box>

      <TextField
        required
        fullWidth
        type="password"
        autoComplete="current-password"
        label="Parolanızı yeniden girin"
        value={password}
        disabled={disabled}
        onChange={(event) => onPasswordChange(event.target.value)}
        sx={{ mt: 1.75 }}
        slotProps={{ input: { startAdornment: <LockRounded fontSize="small" /> } }}
      />
      <FormControlLabel
        sx={{ mt: 0.75, alignItems: "flex-start" }}
        control={
          <Checkbox
            checked={accepted}
            disabled={disabled}
            onChange={(event) => onAcceptedChange(event.target.checked)}
          />
        }
        label={
          <Typography variant="body2" sx={{ pt: 1 }}>
            Yukarıdaki işlemin elektronik imza anlamını okudum ve kabul
            ediyorum.
          </Typography>
        }
      />
    </Paper>
  );
}
