import type { PagedResponse } from "./deviations";
import { qmsFetch } from "./http";
const json = { "Content-Type": "application/json" };
async function req<T>(u: string, i?: RequestInit) {
  const r = await qmsFetch(u, i);
  if (!r.ok) {
    const p = (await r.json().catch(() => null)) as {
      detail?: string;
      title?: string;
    } | null;
    throw new Error(p?.detail ?? p?.title ?? `İstek başarısız (${r.status})`);
  }
  return r.json() as Promise<T>;
}
export interface RiskOptions {
  users: Array<{ id: string; name: string; department: string | null }>;
  categories: Array<{ code: string; name: string }>;
  methodologies: Array<{ code: string; name: string }>;
  matrixVersions: Array<{ code: string; name: string }>;
}
export interface RiskList {
  id: string;
  recordNumber: string;
  process: string;
  category: string;
  methodology: string;
  maxInitialRpn: number;
  openActionCount: number;
  owner: string;
  status: string;
  createdAtUtc: string;
  version: number;
}
export interface RiskItem {
  id: string;
  failureMode: string;
  effect: string;
  cause: string;
  existingControls: string;
  severity: number;
  occurrence: number;
  detectability: number;
  initialRpn: number;
  action: string | null;
  actionOwnerUserId: string | null;
  actionOwner: string | null;
  actionDueAtUtc: string | null;
  actionEvidence: string | null;
  residualSeverity: number | null;
  residualOccurrence: number | null;
  residualDetectability: number | null;
  residualRpn: number | null;
  residualRationale: string | null;
  status: string;
}
export interface RiskDetails {
  record: RiskList & {
    qualityRecordId: string;
    scope: string;
    matrixVersion: string;
    actionThreshold: number;
    ownerUserId: string;
    ownerDepartment: string | null;
    approverUserId: string;
    approver: string;
    updatedAtUtc: string;
    closedAtUtc: string | null;
  };
  items: RiskItem[];
  auditTrail: Array<{
    id: string;
    version: number;
    eventType: string;
    actor: string;
    occurredAtUtc: string;
    reason: string | null;
  }>;
  signatures: Array<{
    id: string;
    recordVersion: number;
    signer: string;
    meaning: string;
    signedAtUtc: string;
    contentHash: string;
    comment: string | null;
  }>;
  availableTransitions: Array<{
    code: string;
    label: string;
    requiresSignature: boolean;
  }>;
}
export interface RiskLookup {
  id: string;
  category: string;
  code: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
}
export const searchRisks = (signal?: AbortSignal) =>
  req<PagedResponse<RiskList>>("/api/v1/risks/search", {
    method: "POST",
    headers: json,
    body: JSON.stringify({
      page: 1,
      pageSize: 25,
      sortBy: "createdAtUtc",
      sortDirection: "desc",
      filters: [],
    }),
    signal,
  });
export const riskOptions = (signal?: AbortSignal) =>
  req<RiskOptions>("/api/v1/risks/options", { signal });
export const riskDetails = (id: string, signal?: AbortSignal) =>
  req<RiskDetails>(`/api/v1/risks/${id}/details`, { signal });
export const createRisk = (x: object) =>
  req<RiskDetails>("/api/v1/risks", {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const addRiskItem = (id: string, x: object) =>
  req<RiskDetails>(`/api/v1/risks/${id}/items`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const completeRiskAction = (id: string, item: string, x: object) =>
  req<RiskDetails>(`/api/v1/risks/${id}/items/${item}/complete`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const setResidualRisk = (id: string, item: string, x: object) =>
  req<RiskDetails>(`/api/v1/risks/${id}/items/${item}/residual`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const transitionRisk = (id: string, x: object) =>
  req<RiskDetails>(`/api/v1/risks/${id}/transitions`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const riskLookups = () => req<RiskLookup[]>("/api/v1/risks/lookups");
export const createRiskLookup = (x: object) =>
  req<RiskLookup>("/api/v1/risks/lookups", {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const updateRiskLookup = (id: string, x: object) =>
  req<RiskLookup>(`/api/v1/risks/lookups/${id}`, {
    method: "PUT",
    headers: json,
    body: JSON.stringify(x),
  });
export async function downloadRiskFinalReport(id: string) {
  const response = await qmsFetch(`/api/v1/risks/${id}/final-report`);
  if (!response.ok) throw new Error("Nihai FMEA PDF'i indirilemedi.");
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `fmea-${id}.pdf`;
  anchor.click();
  URL.revokeObjectURL(url);
}
