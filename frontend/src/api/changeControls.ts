import type { ColumnFilter, PagedResponse } from "./deviations";
import { qmsFetch } from "./http";

export type ChangeControlStatus =
  | "Draft"
  | "PreliminaryReview"
  | "DepartmentReview"
  | "BoardReview"
  | "PlanApproval"
  | "Implementation"
  | "CommissioningApproval"
  | "PostImplementationVerification"
  | "ClosureApproval"
  | "Closed"
  | "RolledBack"
  | "Voided";
export interface ChangeControlListItem {
  id: string;
  recordNumber: string;
  sourceCapaId: string | null;
  sourceRecordNumber: string | null;
  changeType: string;
  title: string;
  owner: string;
  targetDateUtc: string;
  riskLevel: string;
  regulatoryImpact: string;
  status: ChangeControlStatus;
  assessmentCount: number;
  completedAssessmentCount: number;
  actionCount: number;
  verifiedActionCount: number;
  createdAtUtc: string;
  version: number;
}
export interface ChangeUserOption {
  id: string;
  displayName: string;
  departmentName: string | null;
}
export interface ChangeDepartmentOption {
  id: string;
  code: string;
  name: string;
  reviewerUserId: string;
  reviewerName: string;
}
export interface ChangeControlLookups {
  owners: ChangeUserOption[];
  departments: ChangeDepartmentOption[];
  changeTypes: ChangeLookupOption[];
  riskLevels: ChangeLookupOption[];
  regulatoryImpacts: ChangeLookupOption[];
  actionCategories: ChangeLookupOption[];
}
export interface ChangeLookupOption {
  code: string;
  name: string;
}
export interface ChangeLookupDefinition extends ChangeLookupOption {
  id: string;
  category: string;
  sortOrder: number;
  isActive: boolean;
}
export interface CreateChangeControlInput {
  sourceCapaId?: string | null;
  changeType: string;
  title: string;
  currentState: string;
  proposedState: string;
  justification: string;
  scope: string;
  isTemporary: boolean;
  temporaryUntilUtc?: string | null;
  ownerUserId: string;
  targetDateUtc: string;
  riskLevel: string;
  riskSummary: string;
  productImpact: boolean;
  siteImpact: boolean;
  validationRequired: boolean;
  regulatoryImpact: string;
  rollbackPlan: string;
  impactedDepartmentIds: string[];
}
export interface ChangeControlRecord extends ChangeControlListItem {
  qualityRecordId: string;
  currentState: string;
  proposedState: string;
  justification: string;
  scope: string;
  isTemporary: boolean;
  temporaryUntilUtc: string | null;
  ownerUserId: string | null;
  riskSummary: string;
  productImpact: boolean;
  siteImpact: boolean;
  validationRequired: boolean;
  rollbackPlan: string;
  authorityApprovalReference: string | null;
  commissionedAtUtc: string | null;
  postImplementationResult: string | null;
  closureNote: string | null;
  updatedAtUtc: string;
  closedAtUtc: string | null;
}
export interface ChangeAssessment {
  id: string;
  departmentId: string | null;
  department: string;
  reviewerUserId: string | null;
  reviewer: string;
  status: "Pending" | "Approved" | "Rejected";
  impactSummary: string | null;
  requiredActions: string | null;
  completedAtUtc: string | null;
}
export interface ChangeAction {
  id: string;
  category: string;
  description: string;
  ownerUserId: string | null;
  owner: string;
  targetDateUtc: string;
  isBlocking: boolean;
  status: "Planned" | "CompletionRequested" | "Verified" | "Rejected";
  completionEvidence: string | null;
  verificationNote: string | null;
  completedAtUtc: string | null;
  verifiedAtUtc: string | null;
}
export interface ChangeControlDetails {
  record: ChangeControlRecord;
  assessments: ChangeAssessment[];
  actions: ChangeAction[];
  auditTrail: Array<{
    id: string;
    version: number;
    eventType: string;
    actor: string;
    occurredAtUtc: string;
    reason: string | null;
    payload?: Record<string, unknown>;
  }>;
  signatures: Array<{
    id: string;
    recordVersion: number;
    signerUserId: string;
    signerName: string;
    meaning: string;
    signedAtUtc: string;
    contentHash: string;
    comment: string | null;
  }>;
  availableTransitions: Array<{
    code: string;
    label: string;
    noteRequired: boolean;
  }>;
  actionableTaskRoles: string[];
}

export async function searchChangeControls(
  input: {
    page: number;
    pageSize: 10 | 25 | 50 | 100;
    sortBy: string;
    sortDirection: "asc" | "desc";
    filters: ColumnFilter[];
  },
  signal?: AbortSignal,
): Promise<PagedResponse<ChangeControlListItem>> {
  return request("/api/v1/change-controls/search", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
    signal,
  });
}
export async function getChangeControlLookups(
  signal?: AbortSignal,
): Promise<ChangeControlLookups> {
  return request("/api/v1/change-controls/lookups", { signal });
}
export async function listChangeLookupDefinitions(
  signal?: AbortSignal,
): Promise<ChangeLookupDefinition[]> {
  return request("/api/v1/change-controls/lookup-definitions", { signal });
}
export async function createChangeLookupDefinition(input: {
  category: string;
  code: string;
  name: string;
  sortOrder: number;
}): Promise<ChangeLookupDefinition> {
  return request("/api/v1/change-controls/lookup-definitions", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
}
export async function updateChangeLookupDefinition(
  id: string,
  input: { name: string; sortOrder: number; isActive: boolean },
): Promise<ChangeLookupDefinition> {
  return request(`/api/v1/change-controls/lookup-definitions/${id}`, {
    method: "PUT",
    headers: json,
    body: JSON.stringify(input),
  });
}
export async function createChangeControl(
  input: CreateChangeControlInput,
): Promise<ChangeControlDetails> {
  return request("/api/v1/change-controls", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
}
export async function getChangeControlDetails(
  id: string,
  signal?: AbortSignal,
): Promise<ChangeControlDetails> {
  return request(`/api/v1/change-controls/${id}/details`, { signal });
}
export async function downloadChangeControlFinalReport(
  id: string,
): Promise<{ blob: Blob; fileName: string; sha256: string | null }> {
  const response = await qmsFetch(`/api/v1/change-controls/${id}/final-report`);
  if (!response.ok) {
    const p = (await response.json().catch(() => null)) as {
      detail?: string;
    } | null;
    throw new Error(p?.detail ?? `Rapor alınamadı (${response.status})`);
  }
  const disposition = response.headers.get("content-disposition") ?? "";
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1];
  const plain = /filename="?([^";]+)"?/i.exec(disposition)?.[1];
  return {
    blob: await response.blob(),
    fileName: encoded
      ? decodeURIComponent(encoded)
      : (plain ?? "degisiklik-nihai-kayit.pdf"),
    sha256: response.headers.get("x-content-sha256"),
  };
}
export async function completeChangeAssessment(
  id: string,
  assessmentId: string,
  expectedVersion: number,
  approved: boolean,
  impactSummary: string,
  requiredActions: string,
): Promise<ChangeControlDetails> {
  return request(
    `/api/v1/change-controls/${id}/assessments/${assessmentId}/complete`,
    {
      method: "POST",
      headers: json,
      body: JSON.stringify({
        expectedVersion,
        approved,
        impactSummary,
        requiredActions,
      }),
    },
  );
}
export async function addChangeAction(
  id: string,
  input: {
    expectedVersion: number;
    category: string;
    description: string;
    ownerUserId: string;
    targetDateUtc: string;
    isBlocking: boolean;
  },
): Promise<ChangeControlDetails> {
  return request(`/api/v1/change-controls/${id}/actions`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
}
export async function completeChangeAction(
  id: string,
  actionId: string,
  expectedVersion: number,
  evidence: string,
): Promise<ChangeControlDetails> {
  return request(`/api/v1/change-controls/${id}/actions/${actionId}/complete`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, evidence }),
  });
}
export async function verifyChangeAction(
  id: string,
  actionId: string,
  expectedVersion: number,
  approved: boolean,
  note: string,
): Promise<ChangeControlDetails> {
  return request(`/api/v1/change-controls/${id}/actions/${actionId}/verify`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, approved, note }),
  });
}
export async function setAuthorityApproval(
  id: string,
  expectedVersion: number,
  reference: string,
): Promise<ChangeControlDetails> {
  return request(`/api/v1/change-controls/${id}/authority-approval`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, reference }),
  });
}
export async function transitionChangeControl(
  id: string,
  expectedVersion: number,
  transition: string,
  note?: string,
  successful = true,
  signaturePassword?: string,
  signatureMeaningAccepted = false,
): Promise<ChangeControlDetails> {
  return request(`/api/v1/change-controls/${id}/transitions`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({
      expectedVersion,
      transition,
      note,
      successful,
      signaturePassword,
      signatureMeaningAccepted,
    }),
  });
}

const json = { "Content-Type": "application/json" };
async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await qmsFetch(url, init);
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      detail?: string;
      title?: string;
    } | null;
    throw new Error(
      problem?.detail ??
        problem?.title ??
        `İstek başarısız (${response.status})`,
    );
  }
  return response.json() as Promise<T>;
}
