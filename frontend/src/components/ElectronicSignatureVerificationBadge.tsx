import { useMutation } from "@tanstack/react-query";
import { Button, Chip, Tooltip } from "@mui/material";
import { FactCheckRounded } from "@mui/icons-material";
import { verifyElectronicSignature } from "../api/electronicSignatures";

export function ElectronicSignatureVerificationBadge({
  signatureId,
}: {
  signatureId: string;
}) {
  const verification = useMutation({
    mutationFn: () => verifyElectronicSignature(signatureId),
  });

  if (verification.data) {
    const legacy = verification.data.providerType === "Legacy";
    return (
      <Tooltip title={verification.data.verificationMessage} arrow>
        <Chip
          size="small"
          variant="outlined"
          color={legacy ? "default" : verification.data.isValid ? "success" : "error"}
          icon={<FactCheckRounded />}
          label={
            legacy
              ? "Eski imza"
              : verification.data.isValid
                ? "Bütünlük doğrulandı"
                : "Doğrulama başarısız"
          }
        />
      </Tooltip>
    );
  }

  return (
    <Tooltip
      title={verification.error?.message ?? "İmzalı snapshot bütünlüğünü doğrula"}
      arrow
    >
      <span>
        <Button
          size="small"
          variant="text"
          color={verification.isError ? "error" : "primary"}
          startIcon={<FactCheckRounded />}
          disabled={verification.isPending}
          onClick={() => verification.mutate()}
        >
          {verification.isPending ? "Doğrulanıyor…" : "İmzayı doğrula"}
        </Button>
      </span>
    </Tooltip>
  );
}
