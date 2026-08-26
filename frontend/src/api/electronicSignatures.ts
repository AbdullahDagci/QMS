import { qmsFetch } from "./http";

export type ElectronicSignatureVerification = {
  signatureId: string;
  isValid: boolean;
  providerType: string;
  signatureMethod: string;
  aggregateType: string;
  aggregateId: string;
  recordVersion: number;
  operation: string;
  meaning: string;
  signer: string;
  signedAtUtc: string;
  contentHash: string;
  verificationMessage: string;
};

export async function verifyElectronicSignature(
  signatureId: string,
): Promise<ElectronicSignatureVerification> {
  const response = await qmsFetch(
    `/api/v1/e-signatures/${signatureId}/verification`,
  );
  if (!response.ok) {
    throw new Error(
      response.status === 404
        ? "Elektronik imza kaydı bulunamadı."
        : `Elektronik imza doğrulanamadı (${response.status}).`,
    );
  }
  return response.json() as Promise<ElectronicSignatureVerification>;
}
