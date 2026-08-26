import type { ColumnFilter, PagedResponse } from "./deviations";
import { qmsFetch } from "./http";

export interface ComplaintListItem {
  id: string;
  recordNumber: string;
  customerName: string;
  country: string;
  product: string;
  batchNumber: string | null;
  complaintType: string;
  severity: string;
  status: string;
  owner: string;
  preliminaryResponseDueAtUtc: string;
  finalResponseDueAtUtc: string;
  suspectedAdverseEvent: boolean;
  trendFlagged: boolean;
  similarComplaintCount: number;
  createdAtUtc: string;
  version: number;
}
export interface ComplaintRecord extends ComplaintListItem {
  qualityRecordId: string;
  channel: string;
  eventAtUtc: string;
  receivedAtUtc: string;
  description: string;
  hasHealthImpact: boolean;
  sampleExpected: boolean;
  returnExpected: boolean;
  attachmentSummary: string;
  ownerUserId: string;
  linkedDeviationId: string | null;
  linkedDeviationNumber: string | null;
  linkedCapaId: string | null;
  linkedCapaNumber: string | null;
  pharmacovigilanceRecordId: string | null;
  pharmacovigilanceRecordNumber: string | null;
  pharmacovigilanceStatus: string | null;
  impactAssessment: string | null;
  confirmedRootCause: string | null;
  capaRequired: boolean | null;
  closureNote: string | null;
  updatedAtUtc: string;
  closedAtUtc: string | null;
}
export interface ComplaintInvestigation {
  id: string;
  departmentId: string;
  department: string;
  investigatorUserId: string;
  investigator: string;
  status: string;
  findings: string | null;
  rootCauseContribution: string | null;
  completedAtUtc: string | null;
}
export interface ComplaintResponseVersion {
  id: string;
  responseType: string;
  versionNumber: number;
  content: string;
  status: string;
  preparedByUserId: string;
  preparedBy: string;
  approvedByUserId: string | null;
  approvedBy: string | null;
  createdAtUtc: string;
  approvedAtUtc: string | null;
}
export interface ComplaintDetails {
  record: ComplaintRecord;
  investigations: ComplaintInvestigation[];
  responses: ComplaintResponseVersion[];
  auditTrail: Array<{
    id: string;
    version: number;
    eventType: string;
    actor: string;
    occurredAtUtc: string;
    reason: string | null;
    payload?: Record<string, unknown>;
  }>;
  signatures: Array<{ id: string; recordVersion: number; signerUserId: string; signer: string; meaning: string; signedAtUtc: string; contentHash: string; comment: string | null }>;
  actionableTaskRoles: string[];
  availableTransitions: Array<{
    code: string;
    label: string;
    noteRequired: boolean;
  }>;
}
export interface CreateComplaintInput {
  channel: string;
  customerName: string;
  country: string;
  product: string;
  batchNumber: string | null;
  eventAtUtc: string;
  receivedAtUtc: string;
  complaintType: string;
  description: string;
  severity: string;
  hasHealthImpact: boolean;
  suspectedAdverseEvent: boolean;
  sampleExpected: boolean;
  returnExpected: boolean;
  attachmentSummary: string;
  ownerUserId: string;
  preliminaryResponseDueAtUtc: string;
  finalResponseDueAtUtc: string;
  investigationDepartmentIds: string[];
}
export interface ComplaintOptions { owners: Array<{ id: string; name: string; departmentId: string | null; department: string | null }>; departments: Array<{ id: string; code: string; name: string; investigatorUserId: string; investigator: string }>; channels: Array<{ code: string; name: string }>; countries: Array<{ code: string; name: string }>; complaintTypes: Array<{ code: string; name: string }> }
export interface ComplaintLookupDefinition { id: string; category: string; code: string; name: string; sortOrder: number; isActive: boolean }
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
export const searchComplaints = (
  input: {
    page: number;
    pageSize: 10 | 25 | 50 | 100;
    sortBy: string;
    sortDirection: "asc" | "desc";
    filters: ColumnFilter[];
  },
  signal?: AbortSignal,
) =>
  request<PagedResponse<ComplaintListItem>>("/api/v1/complaints/search", {
    method: "POST",
    headers,
    body: JSON.stringify(input),
    signal,
  });
export const getComplaintDetails = (id: string, signal?: AbortSignal) =>
  request<ComplaintDetails>(`/api/v1/complaints/${id}/details`, { signal });
export const downloadComplaintFinalReport = async (id: string, recordNumber: string) => { const response = await qmsFetch(`/api/v1/complaints/${id}/final-report`); if (!response.ok) throw new Error("Nihai şikâyet PDF'i indirilemedi."); const blob = await response.blob(); const url = URL.createObjectURL(blob); const anchor = document.createElement("a"); anchor.href = url; anchor.download = `${recordNumber}-nihai-sikayet.pdf`; anchor.click(); URL.revokeObjectURL(url); };
export const getComplaintOptions = (signal?: AbortSignal) => request<ComplaintOptions>("/api/v1/complaints/options", { signal });
export const getComplaintLookupDefinitions = () => request<ComplaintLookupDefinition[]>("/api/v1/complaints/lookup-definitions");
export const createComplaintLookupDefinition = (input: { category: string; code: string; name: string; sortOrder: number }) => request<ComplaintLookupDefinition>("/api/v1/complaints/lookup-definitions", { method: "POST", headers, body: JSON.stringify(input) });
export const updateComplaintLookupDefinition = (id: string, input: { name: string; sortOrder: number; isActive: boolean }) => request<ComplaintLookupDefinition>(`/api/v1/complaints/lookup-definitions/${id}`, { method: "PUT", headers, body: JSON.stringify(input) });
export const createComplaint = (input: CreateComplaintInput) =>
  request<ComplaintDetails>("/api/v1/complaints", {
    method: "POST",
    headers,
    body: JSON.stringify(input),
  });
export const transitionComplaint = (
  id: string,
  expectedVersion: number,
  transition: string,
  note?: string,
  signaturePassword?: string,
  signatureMeaningAccepted = false,
) =>
  request<ComplaintDetails>(`/api/v1/complaints/${id}/transitions`, {
    method: "POST",
    headers,
    body: JSON.stringify({ expectedVersion, transition, note, signaturePassword, signatureMeaningAccepted }),
  });
export const addComplaintResponse = (
  id: string,
  expectedVersion: number,
  responseType: string,
  content: string,
) =>
  request<ComplaintDetails>(`/api/v1/complaints/${id}/responses`, {
    method: "POST",
    headers,
    body: JSON.stringify({ expectedVersion, responseType, content }),
  });
export const approveComplaintResponse = (
  id: string,
  responseId: string,
  expectedVersion: number,
  signaturePassword: string,
  signatureMeaningAccepted: boolean,
) =>
  request<ComplaintDetails>(
    `/api/v1/complaints/${id}/responses/${responseId}/approve`,
    { method: "POST", headers, body: JSON.stringify({ expectedVersion, signaturePassword, signatureMeaningAccepted }) },
  );
export const completeComplaintInvestigation = (
  id: string,
  investigationId: string,
  expectedVersion: number,
  findings: string,
  rootCauseContribution: string,
) =>
  request<ComplaintDetails>(
    `/api/v1/complaints/${id}/investigations/${investigationId}/complete`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({
        expectedVersion,
        findings,
        rootCauseContribution,
      }),
    },
  );
export const completeComplaintImpact = (
  id: string,
  expectedVersion: number,
  impactAssessment: string,
  confirmedRootCause: string,
) =>
  request<ComplaintDetails>(`/api/v1/complaints/${id}/impact`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      expectedVersion,
      impactAssessment,
      confirmedRootCause,
    }),
  });
export const decideComplaintCapa = (
  id: string,
  expectedVersion: number,
  capaRequired: boolean,
  ownerUserId: string,
  targetDateUtc: string,
  effectivenessRequired: boolean,
) =>
  request<ComplaintDetails>(`/api/v1/complaints/${id}/capa-decision`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      expectedVersion,
      capaRequired,
      ownerUserId,
      targetDateUtc,
      effectivenessRequired,
    }),
  });
