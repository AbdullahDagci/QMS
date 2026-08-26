import type { PagedResponse } from "./deviations";
import { qmsFetch } from "./http";
const json = { "Content-Type": "application/json" };
async function request<T>(url: string, init?: RequestInit) {
  const response = await qmsFetch(url, init);
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      detail?: string;
    } | null;
    throw new Error(problem?.detail ?? "İşlem tamamlanamadı.");
  }
  return (await response.json()) as T;
}
export interface SpecializedOption {
  code: string;
  name: string;
}
export interface SpecializedOptions {
  users: Array<{ id: string; name: string; department: string | null }>;
  types: SpecializedOption[];
  subjects: SpecializedOption[];
  scopes: SpecializedOption[];
}
export interface SpecializedList {
  id: string;
  recordNumber: string;
  title: string;
  typeName: string;
  subjectName: string;
  scopeName: string;
  owner: string;
  status: string;
  dueAtUtc: string;
  createdAtUtc: string;
  version: number;
}
export interface SpecializedLookup {
  id: string;
  moduleCode: string;
  category: string;
  code: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
}
export interface SpecializedDetails {
  record: SpecializedList & {
    qualityRecordId: string;
    moduleCode: string;
    typeCode: string;
    subjectCode: string;
    scopeCode: string;
    reference: string;
    description: string;
    structuredData: Record<string, unknown>;
    ownerUserId: string;
    reviewerUserId: string;
    reviewer: string;
    approverUserId: string;
    approver: string;
    updatedAtUtc: string;
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
export const searchSpecialized = (module: string, signal?: AbortSignal) =>
  request<PagedResponse<SpecializedList>>(
    `/api/v1/specialized/${module}/search`,
    {
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
    },
  );
export const specializedOptions = (module: string, signal?: AbortSignal) =>
  request<SpecializedOptions>(`/api/v1/specialized/${module}/options`, {
    signal,
  });
export const specializedDetails = (
  module: string,
  id: string,
  signal?: AbortSignal,
) =>
  request<SpecializedDetails>(`/api/v1/specialized/${module}/${id}/details`, {
    signal,
  });
export const createSpecialized = (module: string, input: object) =>
  request<SpecializedDetails>(`/api/v1/specialized/${module}`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const transitionSpecialized = (
  module: string,
  id: string,
  input: object,
) =>
  request<SpecializedDetails>(
    `/api/v1/specialized/${module}/${id}/transitions`,
    { method: "POST", headers: json, body: JSON.stringify(input) },
  );
export const specializedLookups = (module: string, signal?: AbortSignal) =>
  request<SpecializedLookup[]>(`/api/v1/specialized/${module}/lookups`, {
    signal,
  });
export const createSpecializedLookup = (module: string, input: object) =>
  request<SpecializedLookup>(`/api/v1/specialized/${module}/lookups`, {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const updateSpecializedLookup = (
  module: string,
  id: string,
  input: object,
) =>
  request<SpecializedLookup>(`/api/v1/specialized/${module}/lookups/${id}`, {
    method: "PUT",
    headers: json,
    body: JSON.stringify(input),
  });
export async function downloadSpecializedFinalReport(
  module: string,
  id: string,
) {
  const response = await qmsFetch(
    `/api/v1/specialized/${module}/${id}/final-report`,
  );
  if (!response.ok) throw new Error("Nihai imzalı PDF indirilemedi.");
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `${module}-${id}-nihai.pdf`;
  anchor.click();
  URL.revokeObjectURL(url);
}
