import type { ColumnFilter, PagedResponse } from "./deviations";
import { qmsFetch } from "./http";
export interface ExternalAuditListItem {
  id: string;
  recordNumber: string;
  title: string;
  auditKind: string;
  auditorOrganization: string;
  isGovernmentAuthority: boolean;
  authorityCountry: string;
  owner: string;
  plannedStartUtc: string;
  responseDueAtUtc: string;
  status: string;
  findingCount: number;
  openFindingCount: number;
  documentCount: number;
  exportedDocumentCount: number;
  createdAtUtc: string;
  version: number;
}
export interface ExternalAuditRecord {
  id: string;
  qualityRecordId: string;
  recordNumber: string;
  title: string;
  auditKind: string;
  auditorOrganization: string;
  isGovernmentAuthority: boolean;
  authorityCountry: string;
  officialReference: string;
  scope: string;
  site: string;
  owner: string;
  notifiedAtUtc: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  responseDueAtUtc: string;
  authorizedCloserUserId: string | null;
  authorizedCloser: string | null;
  closureLetterReference: string | null;
  closureEvidence: string | null;
  authorityAccepted: boolean | null;
  closureLetterReceivedAtUtc: string | null;
  closureNote: string | null;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  closedAtUtc: string | null;
  version: number;
}
export interface ExternalAuditDocument {
  id: string;
  controlledDocumentId: string | null;
  documentCode: string;
  title: string;
  confidentiality: string;
  status: string;
  exportVersion: number;
  exportedAtUtc: string | null;
}
export interface ExternalAuditPackageAccess {
  id: string;
  documentRequestId: string;
  exportedBy: string;
  recipient: string;
  purpose: string;
  evidence: string;
  exportVersion: number;
  accessedAtUtc: string;
}
export interface ExternalAuditFinding {
  id: string;
  number: string;
  title: string;
  description: string;
  officialReference: string;
  classification: string;
  capaRequired: boolean;
  linkedCapaId: string | null;
  linkedCapaNumber: string | null;
  owner: string;
  responseDueAtUtc: string;
  officialResponse: string | null;
  commitment: string | null;
  commitmentDueAtUtc: string | null;
  verificationNote: string | null;
  status: string;
  createdAtUtc: string;
  closedAtUtc: string | null;
}
export interface ExternalAuditDetails {
  record: ExternalAuditRecord;
  documentRequests: ExternalAuditDocument[];
  packageAccesses: ExternalAuditPackageAccess[];
  findings: ExternalAuditFinding[];
  auditTrail: Array<{
    id: string;
    version: number;
    eventType: string;
    actor: string;
    occurredAtUtc: string;
    reason: string | null;
    payload?: Record<string, unknown>;
  }>;
  availableTransitions: Array<{
    code: string;
    label: string;
    noteRequired: boolean;
  }>;
}
export interface CreateExternalAuditInput {
  title: string;
  auditKind: string;
  auditorOrganization: string;
  isGovernmentAuthority: boolean;
  authorityCountry: string;
  officialReference: string;
  scope: string;
  site: string;
  owner: string;
  notifiedAtUtc: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  responseDueAtUtc: string;
  authorizedCloserUserId: string | null;
  authorizedCloser: string | null;
  documentRequests: Array<{
    controlledDocumentId: string | null;
    documentCode: string;
    title: string;
    confidentiality: string;
  }>;
}
const headers = { "Content-Type": "application/json" };
async function request<T>(url: string, init?: RequestInit) {
  const r = await qmsFetch(url, init);
  if (!r.ok) {
    const p = (await r.json().catch(() => null)) as {
      detail?: string;
      title?: string;
    } | null;
    throw new Error(p?.detail ?? p?.title ?? `İstek başarısız (${r.status})`);
  }
  return r.json() as Promise<T>;
}
export const searchExternalAudits = (
  input: {
    page: number;
    pageSize: 10 | 25 | 50 | 100;
    sortBy: string;
    sortDirection: "asc" | "desc";
    filters: ColumnFilter[];
  },
  signal?: AbortSignal,
) =>
  request<PagedResponse<ExternalAuditListItem>>(
    "/api/v1/external-audits/search",
    { method: "POST", headers, body: JSON.stringify(input), signal },
  );
export const getExternalAuditDetails = (id: string, signal?: AbortSignal) =>
  request<ExternalAuditDetails>(`/api/v1/external-audits/${id}/details`, {
    signal,
  });
export const createExternalAudit = (input: CreateExternalAuditInput) =>
  request<ExternalAuditDetails>("/api/v1/external-audits", {
    method: "POST",
    headers,
    body: JSON.stringify(input),
  });
export const transitionExternalAudit = (
  id: string,
  expectedVersion: number,
  transition: string,
  note?: string,
) =>
  request<ExternalAuditDetails>(`/api/v1/external-audits/${id}/transitions`, {
    method: "POST",
    headers,
    body: JSON.stringify({ expectedVersion, transition, note }),
  });
export const exportExternalAuditDocument = (
  id: string,
  requestId: string,
  expectedVersion: number,
  recipient: string,
  purpose: string,
  evidence: string,
) =>
  request<ExternalAuditDetails>(
    `/api/v1/external-audits/${id}/documents/${requestId}/export`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({ expectedVersion, recipient, purpose, evidence }),
    },
  );
export const addExternalAuditFinding = (
  id: string,
  expectedVersion: number,
  input: {
    title: string;
    description: string;
    officialReference: string;
    classification: string;
    capaRequired: boolean;
    owner: string;
    responseDueAtUtc: string;
  },
) =>
  request<ExternalAuditDetails>(`/api/v1/external-audits/${id}/findings`, {
    method: "POST",
    headers,
    body: JSON.stringify({ expectedVersion, ...input }),
  });
export const respondExternalAuditFinding = (
  id: string,
  findingId: string,
  expectedVersion: number,
  officialResponse: string,
  commitment: string,
  commitmentDueAtUtc: string,
) =>
  request<ExternalAuditDetails>(
    `/api/v1/external-audits/${id}/findings/${findingId}/response`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({
        expectedVersion,
        officialResponse,
        commitment,
        commitmentDueAtUtc,
      }),
    },
  );
export const closeExternalAuditFinding = (
  id: string,
  findingId: string,
  expectedVersion: number,
  verificationNote: string,
) =>
  request<ExternalAuditDetails>(
    `/api/v1/external-audits/${id}/findings/${findingId}/close`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({ expectedVersion, verificationNote }),
    },
  );
export const recordExternalAuditClosureLetter = (
  id: string,
  expectedVersion: number,
  reference: string,
  receivedAtUtc: string,
  evidence: string,
  accepted: boolean,
) =>
  request<ExternalAuditDetails>(
    `/api/v1/external-audits/${id}/closure-letter`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({
        expectedVersion,
        reference,
        receivedAtUtc,
        evidence,
        accepted,
      }),
    },
  );
