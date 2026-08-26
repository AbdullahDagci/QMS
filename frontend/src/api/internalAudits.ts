import type { ColumnFilter, PagedResponse } from "./deviations";
import { qmsFetch } from "./http";

export interface InternalAuditListItem {
  id: string;
  recordNumber: string;
  planYear: number;
  title: string;
  auditType: string;
  auditeeDepartment: string;
  leadAuditor: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  isUnplanned: boolean;
  status: string;
  findingCount: number;
  openFindingCount: number;
  createdAtUtc: string;
  version: number;
}
export interface InternalAuditRecord extends InternalAuditListItem {
  qualityRecordId: string;
  scope: string;
  objectives: string;
  criteria: string;
  leadAuditorUserId: string;
  auditeeDepartmentId: string;
  leadAuditorDepartment: string;
  unplannedReason: string | null;
  checklistVersion: string;
  checklistLockedAtUtc: string | null;
  independenceConfirmed: boolean;
  summary: string | null;
  updatedAtUtc: string;
  closedAtUtc: string | null;
}
export interface AuditChecklistItem {
  id: string;
  order: number;
  question: string;
  reference: string;
  status: string;
  evidence: string | null;
  note: string | null;
  answeredAtUtc: string | null;
}
export interface AuditFinding {
  id: string;
  number: string;
  title: string;
  description: string;
  requirementReference: string;
  impact: number;
  likelihood: number;
  riskScore: number;
  classification: string;
  capaRequired: boolean;
  linkedCapaId: string | null;
  linkedCapaNumber: string | null;
  owner: string;
  ownerUserId: string;
  targetDateUtc: string;
  response: string | null;
  correctiveAction: string | null;
  verificationNote: string | null;
  status: string;
  createdAtUtc: string;
  closedAtUtc: string | null;
}
export interface InternalAuditDetails {
  record: InternalAuditRecord;
  checklist: AuditChecklistItem[];
  findings: AuditFinding[];
  auditTrail: Array<{
    id: string;
    version: number;
    eventType: string;
    actor: string;
    occurredAtUtc: string;
    reason: string | null;
    payload?: Record<string, unknown>;
  }>;
  signatures: Array<{id:string;recordVersion:number;signerUserId:string;signer:string;meaning:string;signedAtUtc:string;contentHash:string;comment:string|null}>;
  actionableTaskRoles: string[];
  availableTransitions: Array<{
    code: string;
    label: string;
    noteRequired: boolean;
  }>;
}
export interface CreateInternalAuditInput {
  planYear: number;
  title: string;
  auditType: string;
  scope: string;
  objectives: string;
  criteria: string;
  auditeeDepartment: string;
  auditeeDepartmentId: string;
  leadAuditorUserId: string;
  leadAuditor: string;
  leadAuditorDepartment: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  isUnplanned: boolean;
  unplannedReason: string | null;
  checklistVersion: string;
  questions: Array<{ question: string; reference: string }>;
}
export interface InternalAuditOptions { departments: Array<{id:string;name:string;department:string|null}>; leadAuditors:Array<{id:string;name:string;department:string|null}>; findingOwners:Array<{id:string;name:string;department:string|null}>; auditTypes:Array<{code:string;name:string}>; }
export interface InternalAuditLookupDefinition{id:string;category:string;code:string;name:string;sortOrder:number;isActive:boolean}

const headers = { "Content-Type": "application/json" };
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
export const searchInternalAudits = (
  input: {
    page: number;
    pageSize: 10 | 25 | 50 | 100;
    sortBy: string;
    sortDirection: "asc" | "desc";
    filters: ColumnFilter[];
  },
  signal?: AbortSignal,
) =>
  request<PagedResponse<InternalAuditListItem>>(
    "/api/v1/internal-audits/search",
    { method: "POST", headers, body: JSON.stringify(input), signal },
  );
export const getInternalAuditDetails = (id: string, signal?: AbortSignal) =>
  request<InternalAuditDetails>(`/api/v1/internal-audits/${id}/details`, {
    signal,
  });
export const getInternalAuditOptions = (signal?: AbortSignal) => request<InternalAuditOptions>("/api/v1/internal-audits/options", { signal });
export const getInternalAuditLookupDefinitions=()=>request<InternalAuditLookupDefinition[]>("/api/v1/internal-audits/lookup-definitions");
export const createInternalAuditLookupDefinition=(input:{category:string;code:string;name:string;sortOrder:number})=>request<InternalAuditLookupDefinition>("/api/v1/internal-audits/lookup-definitions",{method:"POST",headers,body:JSON.stringify(input)});
export const updateInternalAuditLookupDefinition=(id:string,input:{name:string;sortOrder:number;isActive:boolean})=>request<InternalAuditLookupDefinition>(`/api/v1/internal-audits/lookup-definitions/${id}`,{method:"PUT",headers,body:JSON.stringify(input)});
export async function downloadInternalAuditFinalReport(id:string){const response=await qmsFetch(`/api/v1/internal-audits/${id}/final-report`);if(!response.ok)throw new Error("Nihai iç denetim PDF'i indirilemedi.");const blob=await response.blob();const url=URL.createObjectURL(blob);const a=document.createElement("a");a.href=url;a.download=`ic-denetim-${id}.pdf`;a.click();URL.revokeObjectURL(url);}
export const createInternalAudit = (input: CreateInternalAuditInput) =>
  request<InternalAuditDetails>("/api/v1/internal-audits", {
    method: "POST",
    headers,
    body: JSON.stringify(input),
  });
export const transitionInternalAudit = (
  id: string,
  expectedVersion: number,
  transition: string,
  note?: string,
  signaturePassword?: string,
  signatureMeaningAccepted = false,
) =>
  request<InternalAuditDetails>(`/api/v1/internal-audits/${id}/transitions`, {
    method: "POST",
    headers,
    body: JSON.stringify({ expectedVersion, transition, note, signaturePassword, signatureMeaningAccepted }),
  });
export const answerAuditQuestion = (
  id: string,
  itemId: string,
  expectedVersion: number,
  status: string,
  evidence: string,
  note: string,
) =>
  request<InternalAuditDetails>(
    `/api/v1/internal-audits/${id}/checklist/${itemId}/answer`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({ expectedVersion, status, evidence, note }),
    },
  );
export const addAuditFinding = (
  id: string,
  expectedVersion: number,
  input: {
    title: string;
    description: string;
    requirementReference: string;
    impact: number;
    likelihood: number;
    capaRequired: boolean;
    ownerUserId: string;
    owner: string;
    targetDateUtc: string;
  },
) =>
  request<InternalAuditDetails>(`/api/v1/internal-audits/${id}/findings`, {
    method: "POST",
    headers,
    body: JSON.stringify({ expectedVersion, ...input }),
  });
export const respondAuditFinding = (
  id: string,
  findingId: string,
  expectedVersion: number,
  response: string,
  correctiveAction: string,
) =>
  request<InternalAuditDetails>(
    `/api/v1/internal-audits/${id}/findings/${findingId}/response`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({ expectedVersion, response, correctiveAction }),
    },
  );
export const closeAuditFinding = (
  id: string,
  findingId: string,
  expectedVersion: number,
  verificationNote: string,
  signaturePassword: string,
  signatureMeaningAccepted: boolean,
) =>
  request<InternalAuditDetails>(
    `/api/v1/internal-audits/${id}/findings/${findingId}/close`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({ expectedVersion, verificationNote, signaturePassword, signatureMeaningAccepted }),
    },
  );
