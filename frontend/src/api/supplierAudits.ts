import type { ColumnFilter, PagedResponse } from "./deviations";
import { qmsFetch } from "./http";

export interface SupplierAuditOptions {
  users: Array<{ id: string; name: string; department: string | null }>;
  countries: Array<{ code: string; name: string }>;
  criticalities: Array<{ code: string; name: string }>;
  findingClassifications: Array<{ code: string; name: string }>;
}
export interface SupplierAuditLookupDefinition { id: string; category: string; code: string; name: string; sortOrder: number; isActive: boolean }

export interface SupplierAuditListItem {
  id: string;
  recordNumber: string;
  supplierCode: string;
  supplierName: string;
  supplierScope: string;
  materialOrService: string;
  criticality: string;
  riskScore: number;
  riskBand: string;
  recommendedFrequencyMonths: number;
  qualificationStatus: string;
  plannedStartUtc: string;
  findingCount: number;
  openFindingCount: number;
  status: string;
  createdAtUtc: string;
  version: number;
}
export interface SupplierAuditRecord extends SupplierAuditListItem {
  qualityRecordId: string;
  supplierEvaluationId: string | null;
  country: string;
  pastPerformanceScore: number;
  openFindingSnapshot: number;
  scope: string;
  site: string;
  leadAuditorUserId: string;
  leadAuditor: string;
  leadAuditorDepartment: string;
  purchasingOwner: string;
  purchasingOwnerUserId: string;
  verifierUserId: string;
  verifier: string;
  qualityApproverUserId: string;
  qualityApprover: string;
  plannedEndUtc: string;
  checklistVersion: string;
  checklistLockedAtUtc: string | null;
  resultRationale: string | null;
  qualificationValidUntilUtc: string | null;
  requalificationRequired: boolean;
  updatedAtUtc: string;
  closedAtUtc: string | null;
}
export interface SupplierAuditChecklist {
  id: string;
  order: number;
  category: string;
  question: string;
  reference: string;
  status: string;
  evidence: string | null;
  note: string | null;
  answeredAtUtc: string | null;
}
export interface SupplierAuditFinding {
  id: string;
  number: string;
  title: string;
  description: string;
  requirementReference: string;
  classification: string;
  capaRequired: boolean;
  linkedCapaId: string | null;
  linkedCapaNumber: string | null;
  ownerUserId: string;
  owner: string;
  responseDueAtUtc: string;
  supplierResponse: string | null;
  commitment: string | null;
  commitmentDueAtUtc: string | null;
  evidence: string | null;
  verificationNote: string | null;
  status: string;
  createdAtUtc: string;
  closedAtUtc: string | null;
}
export interface SupplierAuditInvitation {
  id: string;
  recipientEmail: string;
  expiresAtUtc: string;
  createdAtUtc: string;
  usedAtUtc: string | null;
}
export interface SupplierAuditDetails {
  record: SupplierAuditRecord;
  checklist: SupplierAuditChecklist[];
  findings: SupplierAuditFinding[];
  invitations: SupplierAuditInvitation[];
  auditTrail: Array<{
    id: string;
    version: number;
    eventType: string;
    actor: string;
    occurredAtUtc: string;
    reason: string | null;
    payload?: Record<string, unknown>;
  }>;
  availableTransitions: Array<{ code: string; label: string }>;
  signatures: Array<{ id: string; recordVersion: number; signedByUserId: string; signedBy: string; meaning: string; signedAtUtc: string; hash: string; reason: string | null }>;
}
export interface CreateSupplierAuditInput {
  supplierEvaluationId: string | null;
  supplierCode: string;
  supplierName: string;
  supplierScope: string;
  materialOrService: string;
  country: string;
  criticality: string;
  pastPerformanceScore: number;
  openFindingCount: number;
  scope: string;
  site: string;
  leadAuditorUserId: string;
  leadAuditor: string;
  leadAuditorDepartment: string;
  purchasingOwner: string;
  purchasingOwnerUserId: string;
  verifierUserId: string;
  qualityApproverUserId: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  checklistVersion: string;
  checklist: Array<{ category: string; question: string; reference: string }>;
}

const json = { "Content-Type": "application/json" };
async function request<T>(url: string, init?: RequestInit) {
  const response = await qmsFetch(url, init);
  if (!response.ok) {
    const p = (await response.json().catch(() => null)) as {
      detail?: string;
      title?: string;
    } | null;
    throw new Error(
      p?.detail ?? p?.title ?? `İstek başarısız (${response.status})`,
    );
  }
  return response.json() as Promise<T>;
}
export const searchSupplierAudits = (
  input: {
    page: number;
    pageSize: 10 | 25 | 50 | 100;
    sortBy: string;
    sortDirection: "asc" | "desc";
    filters: ColumnFilter[];
  },
  signal?: AbortSignal,
) =>
  request<PagedResponse<SupplierAuditListItem>>(
    "/api/v1/supplier-audits/search",
    { method: "POST", headers: json, body: JSON.stringify(input), signal },
  );
export const getSupplierAuditDetails = (id: string, signal?: AbortSignal) =>
  request<SupplierAuditDetails>(`/api/v1/supplier-audits/${id}/details`, {
    signal,
  });
export const getSupplierAuditOptions = (signal?: AbortSignal) =>
  request<SupplierAuditOptions>("/api/v1/supplier-audits/options", { signal });
export const getSupplierAuditLookupDefinitions = () => request<SupplierAuditLookupDefinition[]>("/api/v1/supplier-audits/lookups");
export const createSupplierAuditLookupDefinition = (input: { category: string; code: string; name: string; sortOrder: number }) => request<SupplierAuditLookupDefinition>("/api/v1/supplier-audits/lookups", { method: "POST", headers: json, body: JSON.stringify(input) });
export const updateSupplierAuditLookupDefinition = (id: string, input: { name: string; sortOrder: number; isActive: boolean }) => request<SupplierAuditLookupDefinition>(`/api/v1/supplier-audits/lookups/${id}`, { method: "PUT", headers: json, body: JSON.stringify(input) });
export async function downloadSupplierAuditFinalReport(id: string) { const response = await qmsFetch(`/api/v1/supplier-audits/${id}/final-report`); if (!response.ok) throw new Error("Nihai tedarikçi denetimi PDF'i indirilemedi."); const blob = await response.blob(); const url = URL.createObjectURL(blob); const anchor = document.createElement("a"); anchor.href = url; anchor.download = `tedarikci-denetimi-${id}.pdf`; anchor.click(); URL.revokeObjectURL(url); }
export const createSupplierAudit = (input: CreateSupplierAuditInput) =>
  request<SupplierAuditDetails>("/api/v1/supplier-audits", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const transitionSupplierAudit = (
  id: string,
  expectedVersion: number,
  transition: string,
  signaturePassword?: string,
  signatureMeaningAccepted = false,
) =>
  request<SupplierAuditDetails>(`/api/v1/supplier-audits/${id}/transitions`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, transition, signaturePassword, signatureMeaningAccepted }),
  });
export const answerSupplierAuditChecklist = (
  id: string,
  itemId: string,
  expectedVersion: number,
  status: string,
  evidence: string,
  note: string,
) =>
  request<SupplierAuditDetails>(
    `/api/v1/supplier-audits/${id}/checklist/${itemId}/answer`,
    {
      method: "POST",
      headers: json,
      body: JSON.stringify({ expectedVersion, status, evidence, note }),
    },
  );
export const addSupplierAuditFinding = (
  id: string,
  expectedVersion: number,
  input: {
    title: string;
    description: string;
    requirementReference: string;
    classification: string;
    capaRequired: boolean;
    ownerUserId: string;
    owner: string;
    responseDueAtUtc: string;
  },
) =>
  request<SupplierAuditDetails>(`/api/v1/supplier-audits/${id}/findings`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, ...input }),
  });
export const respondSupplierAuditFinding = (
  id: string,
  findingId: string,
  expectedVersion: number,
  supplierResponse: string,
  commitment: string,
  commitmentDueAtUtc: string,
) =>
  request<SupplierAuditDetails>(
    `/api/v1/supplier-audits/${id}/findings/${findingId}/response`,
    {
      method: "POST",
      headers: json,
      body: JSON.stringify({
        expectedVersion,
        supplierResponse,
        commitment,
        commitmentDueAtUtc,
      }),
    },
  );
export const submitSupplierAuditEvidence = (
  id: string,
  findingId: string,
  expectedVersion: number,
  evidence: string,
) =>
  request<SupplierAuditDetails>(
    `/api/v1/supplier-audits/${id}/findings/${findingId}/evidence`,
    {
      method: "POST",
      headers: json,
      body: JSON.stringify({ expectedVersion, evidence }),
    },
  );
export const closeSupplierAuditFinding = (
  id: string,
  findingId: string,
  expectedVersion: number,
  verificationNote: string,
  signaturePassword: string,
  signatureMeaningAccepted: boolean,
) =>
  request<SupplierAuditDetails>(
    `/api/v1/supplier-audits/${id}/findings/${findingId}/close`,
    {
      method: "POST",
      headers: json,
      body: JSON.stringify({ expectedVersion, verificationNote, signaturePassword, signatureMeaningAccepted }),
    },
  );
export const createSupplierAuditInvitation = (
  id: string,
  expectedVersion: number,
  recipientEmail: string,
  expiresAtUtc: string,
) =>
  request<{
    details: SupplierAuditDetails;
    oneTimeToken: string;
    invitationPath: string;
  }>(`/api/v1/supplier-audits/${id}/invitations`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, recipientEmail, expiresAtUtc }),
  });
export const recordSupplierAuditResult = (
  id: string,
  expectedVersion: number,
  decision: string,
  rationale: string,
  validUntilUtc: string | null,
  requalificationRequired: boolean,
  signaturePassword: string,
  signatureMeaningAccepted: boolean,
) =>
  request<SupplierAuditDetails>(`/api/v1/supplier-audits/${id}/result`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({
      expectedVersion,
      decision,
      rationale,
      validUntilUtc,
      requalificationRequired,
      signaturePassword,
      signatureMeaningAccepted,
    }),
  });
