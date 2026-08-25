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
  department: string;
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
  preparedBy: string;
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
  owner: string;
  preliminaryResponseDueAtUtc: string;
  finalResponseDueAtUtc: string;
  investigationDepartments: string[];
}
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
) =>
  request<ComplaintDetails>(`/api/v1/complaints/${id}/transitions`, {
    method: "POST",
    headers,
    body: JSON.stringify({ expectedVersion, transition, note }),
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
) =>
  request<ComplaintDetails>(
    `/api/v1/complaints/${id}/responses/${responseId}/approve`,
    { method: "POST", headers, body: JSON.stringify({ expectedVersion }) },
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
  owner: string,
  targetDateUtc: string,
  effectivenessRequired: boolean,
) =>
  request<ComplaintDetails>(`/api/v1/complaints/${id}/capa-decision`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      expectedVersion,
      capaRequired,
      owner,
      targetDateUtc,
      effectivenessRequired,
    }),
  });
