import type { ColumnFilter, PagedResponse } from "./deviations";
import { qmsFetch } from "./http";

export type TrainingStatus =
  | "Planned"
  | "Assigned"
  | "InProgress"
  | "Assessment"
  | "TrainerApproval"
  | "Completed"
  | "Failed"
  | "Expired"
  | "Cancelled";
export interface TrainingListItem {
  id: string;
  recordNumber: string;
  employeeUserId: string;
  employeeName: string;
  positionId: string;
  position: string;
  courseCode: string;
  courseTitle: string;
  documentCode: string | null;
  documentRevision: string | null;
  assessmentMode: string;
  deliveryMethod: string;
  status: TrainingStatus;
  progressPercent: number;
  dueAtUtc: string;
  expiresAtUtc: string | null;
  isCriticalQualification: boolean;
  attemptCount: number;
  createdAtUtc: string;
  version: number;
}
export interface TrainingRecord extends TrainingListItem {
  qualityRecordId: string;
  matrixRuleId: string | null;
  controlledDocumentId: string | null;
  documentRevisionId: string | null;
  documentTrainingRequirementId: string | null;
  passingScore: number;
  validityMonths: number;
  maxAttempts: number;
  plannedYear: number;
  sessionCode: string | null;
  trainer: string | null;
  startedAtUtc: string | null;
  acknowledgedAtUtc: string | null;
  acknowledgementMeaning: string | null;
  completedAtUtc: string | null;
  trainerApprovalNote: string | null;
  updatedAtUtc: string;
}
export interface TrainingAttempt {
  id: string;
  attemptNumber: number;
  score: number;
  practicalPassed: boolean;
  passed: boolean;
  evidence: string;
  evaluator: string;
  assessedAtUtc: string;
}
export interface TrainingDetails {
  record: TrainingRecord;
  attempts: TrainingAttempt[];
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
  signatures: Array<{id:string;recordVersion:number;signerUserId:string;signerName:string;meaning:string;signedAtUtc:string;contentHash:string;comment:string|null}>;
  actionableTaskRoles: string[];
}
export interface TrainingMatrixRule {
  id: string;
  positionId: string;
  position: string;
  courseCode: string;
  courseTitle: string;
  controlledDocumentId: string | null;
  documentCode: string | null;
  assessmentMode: string;
  deliveryMethod: string;
  passingScore: number;
  validityMonths: number;
  isCriticalQualification: boolean;
  isActive: boolean;
  effectiveAtUtc: string;
}
export interface TrainingOptions {
  employees: Array<{ id: string; displayName: string; positionId: string; position: string }>;
  documents: Array<{
    id: string;
    revisionId: string;
    documentCode: string;
    title: string;
    revision: string;
  }>;
  positions: Array<{id:string;code:string;name:string;departmentName:string|null}>;
  assessmentModes: Array<{code:string;name:string}>;
  deliveryMethods: Array<{code:string;name:string}>;
}
export interface CreateTrainingInput {
  matrixRuleId?: string | null;
  controlledDocumentId?: string | null;
  documentRevisionId?: string | null;
  employeeUserId: string;
  positionId: string;
  courseCode: string;
  courseTitle: string;
  assessmentMode: string;
  deliveryMethod: string;
  passingScore: number;
  validityMonths: number;
  maxAttempts: number;
  isCriticalQualification: boolean;
  dueAtUtc: string;
  sessionCode?: string | null;
  trainer?: string | null;
  assignNow: boolean;
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
export const searchTrainings = (
  input: {
    page: number;
    pageSize: 10 | 25 | 50 | 100;
    sortBy: string;
    sortDirection: "asc" | "desc";
    filters: ColumnFilter[];
  },
  signal?: AbortSignal,
) =>
  request<PagedResponse<TrainingListItem>>("/api/v1/trainings/search", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
    signal,
  });
export const getTrainingDetails = (id: string, signal?: AbortSignal) =>
  request<TrainingDetails>(`/api/v1/trainings/${id}/details`, { signal });
export async function downloadTrainingFinalReport(id:string):Promise<{blob:Blob;fileName:string;sha256:string|null}>{const response=await qmsFetch(`/api/v1/trainings/${id}/final-report`);if(!response.ok){const p=await response.json().catch(()=>null) as {detail?:string}|null;throw new Error(p?.detail??`Rapor alınamadı (${response.status})`);}const disposition=response.headers.get("content-disposition")??"";const encoded=/filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1];const plain=/filename="?([^";]+)"?/i.exec(disposition)?.[1];return{blob:await response.blob(),fileName:encoded?decodeURIComponent(encoded):(plain??"egitim-nihai-kayit.pdf"),sha256:response.headers.get("x-content-sha256")};}
export const getTrainingOptions = (signal?: AbortSignal) =>
  request<TrainingOptions>("/api/v1/trainings/options", { signal });
export const getTrainingMatrix = (signal?: AbortSignal) =>
  request<TrainingMatrixRule[]>("/api/v1/trainings/matrix", { signal });
export const createTraining = (input: CreateTrainingInput) =>
  request<TrainingDetails>("/api/v1/trainings", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const createTrainingMatrix = (input: {
  positionId: string;
  courseCode: string;
  courseTitle: string;
  controlledDocumentId: string | null;
  assessmentMode: string;
  deliveryMethod: string;
  passingScore: number;
  validityMonths: number;
  isCriticalQualification: boolean;
  effectiveAtUtc: string;
}) =>
  request<TrainingMatrixRule>("/api/v1/trainings/matrix", {
    method: "POST",
    headers: json,
    body: JSON.stringify(input),
  });
export const transitionTraining = (
  id: string,
  expectedVersion: number,
  transition: string,
  note?: string,
  signaturePassword?: string,
  signatureMeaningAccepted = false,
) =>
  request<TrainingDetails>(`/api/v1/trainings/${id}/transitions`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, transition, note, signaturePassword, signatureMeaningAccepted }),
  });
export const acknowledgeTraining = (
  id: string,
  expectedVersion: number,
  signatureMeaning: string,
  signaturePassword: string,
  signatureMeaningAccepted: boolean,
) =>
  request<TrainingDetails>(`/api/v1/trainings/${id}/acknowledge`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, signatureMeaning, signaturePassword, signatureMeaningAccepted }),
  });
export const recordTrainingAssessment = (
  id: string,
  expectedVersion: number,
  score: number,
  practicalPassed: boolean,
  evidence: string,
  signaturePassword: string,
  signatureMeaningAccepted: boolean,
) =>
  request<TrainingDetails>(`/api/v1/trainings/${id}/assessment`, {
    method: "POST",
    headers: json,
    body: JSON.stringify({ expectedVersion, score, practicalPassed, evidence, signaturePassword, signatureMeaningAccepted }),
  });
export interface TrainingLookupDefinition{id:string;category:string;code:string;name:string;sortOrder:number;isActive:boolean}
export const listTrainingLookupDefinitions=(signal?:AbortSignal)=>request<TrainingLookupDefinition[]>("/api/v1/trainings/lookup-definitions",{signal});
export const createTrainingLookupDefinition=(input:{category:string;code:string;name:string;sortOrder:number})=>request<TrainingLookupDefinition>("/api/v1/trainings/lookup-definitions",{method:"POST",headers:json,body:JSON.stringify(input)});
export const updateTrainingLookupDefinition=(id:string,input:{name:string;sortOrder:number;isActive:boolean})=>request<TrainingLookupDefinition>(`/api/v1/trainings/lookup-definitions/${id}`,{method:"PUT",headers:json,body:JSON.stringify(input)});
