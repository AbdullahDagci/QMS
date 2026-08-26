import type { PagedResponse } from "./deviations";
import { qmsFetch } from "./http";
const json = { "Content-Type": "application/json" };
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
export interface WorkItemOption {
  code: string;
  name: string;
}
export interface WorkItemOptions {
  users: Array<{ id: string; name: string; department: string | null }>;
  categories: WorkItemOption[];
  priorities: WorkItemOption[];
}
export interface WorkItemList {
  id: string;
  recordNumber: string;
  title: string;
  category: string;
  priority: string;
  owner: string;
  dueAtUtc: string;
  isOverdue: boolean;
  status: string;
  createdAtUtc: string;
  version: number;
}
export interface WorkItemDetails {
  record: WorkItemList & {
    qualityRecordId: string;
    sourceModule: string | null;
    sourceRecordId: string | null;
    sourceRecordNumber: string | null;
    description: string;
    ownerUserId: string;
    ownerDepartmentId: string | null;
    ownerDepartment: string | null;
    verifierUserId: string;
    verifier: string;
    completionEvidence: string | null;
    verificationNote: string | null;
    updatedAtUtc: string;
    startedAtUtc: string | null;
    submittedAtUtc: string | null;
    completedAtUtc: string | null;
    cancelledAtUtc: string | null;
  };
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
export interface Lookup {
  id: string;
  category: string;
  code: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
}
export const searchWorkItems = (signal?: AbortSignal) =>
  request<PagedResponse<WorkItemList>>("/api/v1/work-items/search", {
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
export const getWorkItemOptions = (signal?: AbortSignal) =>
  request<WorkItemOptions>("/api/v1/work-items/options", { signal });
export const getWorkItem = (id: string, signal?: AbortSignal) =>
  request<WorkItemDetails>(`/api/v1/work-items/${id}/details`, { signal });
export const createWorkItem = (input: object) =>
  request<WorkItemDetails>("/api/v1/work-items", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const transitionWorkItem = (id: string, input: object) =>
  request<WorkItemDetails>(`/api/v1/work-items/${id}/transitions`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const getWorkItemLookups = () =>
  request<Lookup[]>("/api/v1/work-items/lookups");
export const createWorkItemLookup = (input: object) =>
  request<Lookup>("/api/v1/work-items/lookups", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const updateWorkItemLookup = (id: string, input: object) =>
  request<Lookup>(`/api/v1/work-items/lookups/${id}`, {
    method: "PUT",
    headers: json,
    body: JSON.stringify(input),
  });
export async function downloadWorkItemFinalReport(id: string) {
  const response = await qmsFetch(`/api/v1/work-items/${id}/final-report`);
  if (!response.ok) throw new Error("Nihai iş kaydı PDF'i indirilemedi.");
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `is-kaydi-${id}.pdf`;
  anchor.click();
  URL.revokeObjectURL(url);
}
