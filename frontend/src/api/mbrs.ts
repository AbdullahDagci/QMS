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
export interface MbrOption {
  code: string;
  name: string;
}
export interface MbrOptions {
  users: Array<{ id: string; name: string; department: string | null }>;
  products: MbrOption[];
  dosageForms: MbrOption[];
  batchUnits: MbrOption[];
  sites: MbrOption[];
  lines: MbrOption[];
  phases: MbrOption[];
  parameterUnits: MbrOption[];
}
export interface MbrList {
  id: string;
  recordNumber: string;
  productCode: string;
  productName: string;
  documentVersion: string;
  siteName: string;
  lineName: string;
  author: string;
  status: string;
  createdAtUtc: string;
  version: number;
}
export interface MbrStep {
  id: string;
  order: number;
  phaseCode: string;
  phaseName: string;
  instruction: string;
  materialOrEquipmentReference: string | null;
  isCritical: boolean;
  parameter: string | null;
  lowerLimit: number | null;
  upperLimit: number | null;
  unitCode: string | null;
  unitName: string | null;
}
export interface MbrDetails {
  record: MbrList & {
    qualityRecordId: string;
    previousVersionId: string | null;
    dosageFormCode: string;
    dosageFormName: string;
    strength: string;
    batchSize: number;
    batchUnitCode: string;
    batchUnitName: string;
    siteCode: string;
    lineCode: string;
    changeReason: string;
    authorUserId: string;
    reviewerUserId: string;
    reviewer: string;
    approverUserId: string;
    approver: string;
    effectiveAtUtc: string | null;
    updatedAtUtc: string;
  };
  steps: MbrStep[];
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
export const searchMbrs = (signal?: AbortSignal) =>
  req<PagedResponse<MbrList>>("/api/v1/mbrs/search", {
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
export const mbrOptions = (signal?: AbortSignal) =>
  req<MbrOptions>("/api/v1/mbrs/options", { signal });
export const mbrDetails = (id: string, signal?: AbortSignal) =>
  req<MbrDetails>(`/api/v1/mbrs/${id}/details`, { signal });
export const createMbr = (x: object) =>
  req<MbrDetails>("/api/v1/mbrs", {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const addMbrStep = (id: string, x: object) =>
  req<MbrDetails>(`/api/v1/mbrs/${id}/steps`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export const transitionMbr = (id: string, x: object) =>
  req<MbrDetails>(`/api/v1/mbrs/${id}/transitions`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(x),
  });
export async function downloadMbrFinalReport(id: string) {
  const response = await qmsFetch(`/api/v1/mbrs/${id}/final-report`);
  if (!response.ok) throw new Error("Nihai MBR PDF'i indirilemedi.");
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `mbr-${id}.pdf`;
  anchor.click();
  URL.revokeObjectURL(url);
}
