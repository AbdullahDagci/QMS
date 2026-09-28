export type DeviationStatus =
  | 'Draft'
  | 'Submitted'
  | 'PreliminaryReview'
  | 'Investigation'
  | 'ImpactAssessment'
  | 'QualityAssessment'
  | 'ActionImplementation'
  | 'EffectivenessReview'
  | 'ClosureApproval'
  | 'Closed'
  | 'Voided'

export interface DeviationListItem {
  id: string
  recordNumber: string
  title: string
  detectedDepartment: string
  riskScore: number
  riskMatrixVersion: string
  classification: 'Minor' | 'Major' | 'Critical'
  capaRequired: boolean
  status: DeviationStatus
  targetDateUtc: string
  createdAtUtc: string
  version: number
}

export interface CreateDeviationInput {
  title: string
  description: string
  expectedState: string
  immediateAction: string
  deviationType: string
  detectedDepartment: string
  processStage: string
  occurredAtUtc: string
  detectedAtUtc: string
  likelihood: number
  severity: number
  detectability: number
}

export interface DeviationTypeDefinition { id: string; code: string; name: string; sortOrder: number; isActive: boolean }
export interface DeviationLookups {
  departments: Array<{ id: string; code: string; name: string }>
  deviationTypes: DeviationTypeDefinition[]
}
export interface DeviationAssignmentRule { id: string; taskRole: string; assignedUserId: string; assignedUserName: string; detectedDepartment: string | null; deviationType: string | null; minimumRiskScore: number | null; priority: number; isActive: boolean }

export interface DeviationRecord extends DeviationListItem {
  qualityRecordId: string
  description: string
  expectedState: string
  immediateAction: string
  deviationType: string
  processStage: string
  occurredAtUtc: string
  detectedAtUtc: string
  likelihood: number
  severity: number
  detectability: number
  preliminaryReviewNote: string | null
  qualityAssessmentNote: string | null
  effectivenessRequired: boolean
  effectivenessAssessmentNote: string | null
  closureJustification: string | null
  closedAtUtc: string | null
  updatedAtUtc: string
}

export interface DeviationDetails {
  record: DeviationRecord
  investigations: Array<{
    id: string
    method: string
    rootCauseCategory: string
    rootCauseDescription: string
    conclusion: string
    investigatorUserId: string
    investigatorName: string
    investigatorDepartment: string
    completedAtUtc: string
  }>
  batchImpacts: Array<{
    id: string
    batchNumber: string
    isAffected: boolean
    isLocked: boolean
    disposition: string
    rationale: string
    assessedByUserId: string
    assessedByName: string
    assessedByDepartment: string
    assessedAtUtc: string
  }>
  linkedCapas: Array<{
    id: string
    recordNumber: string
    title: string
    owner: string
    status: string
    targetDateUtc: string
  }>
  auditTrail: Array<{
    id: string
    version: number
    eventType: string
    actor: string
    occurredAtUtc: string
    reason: string | null
    payload?: Record<string, unknown>
  }>
  availableTransitions: Array<{
    code: string
    label: string
    noteRequired: boolean
  }>
  signatures: Array<{ id: string; recordVersion: number; signerUserId: string; signerName: string; meaning: string; signedAtUtc: string; contentHash: string; comment: string | null }>
  canAddInvestigation: boolean
  canAddBatchImpact: boolean
  nextAssignee: DeviationNextAssignee | null
}

export interface DeviationNextAssignee {
  taskRole: string
  userId: string | null
  userName: string | null
  departmentName: string | null
}

export interface ColumnFilter {
  field: string
  operator: string
  value?: string
  valueTo?: string
  values?: string[]
}

export interface DeviationSearchInput {
  page: number
  pageSize: 10 | 25 | 50 | 100
  sortBy: string
  sortDirection: 'asc' | 'desc'
  filters: ColumnFilter[]
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export async function listDeviations(signal?: AbortSignal): Promise<DeviationListItem[]> {
  return request('/api/v1/deviations', { signal })
}

export async function searchDeviations(
  input: DeviationSearchInput,
  signal?: AbortSignal,
): Promise<PagedResponse<DeviationListItem>> {
  return request('/api/v1/deviations/search', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
    signal,
  })
}

export async function createDeviation(input: CreateDeviationInput): Promise<DeviationListItem> {
  return request('/api/v1/deviations', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
}

export const getDeviationLookups = (signal?: AbortSignal): Promise<DeviationLookups> => request('/api/v1/deviations/lookups', { signal })
export const listDeviationTypes = (signal?: AbortSignal): Promise<DeviationTypeDefinition[]> => request('/api/v1/deviations/types', { signal })
export const createDeviationType = (input: { code: string; name: string; sortOrder: number }): Promise<DeviationTypeDefinition> => request('/api/v1/deviations/types', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })
export const updateDeviationType = (id: string, input: { name: string; sortOrder: number; isActive: boolean }): Promise<DeviationTypeDefinition> => request(`/api/v1/deviations/types/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })
export const listDeviationAssignmentRules = (signal?: AbortSignal): Promise<DeviationAssignmentRule[]> => request('/api/v1/deviations/assignment-rules', { signal })
export const createDeviationAssignmentRule = (input: Omit<DeviationAssignmentRule, 'id' | 'assignedUserName'>): Promise<DeviationAssignmentRule> => request('/api/v1/deviations/assignment-rules', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })
export const updateDeviationAssignmentRule = (id: string, input: Omit<DeviationAssignmentRule, 'id' | 'assignedUserName'>): Promise<DeviationAssignmentRule> => request(`/api/v1/deviations/assignment-rules/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })

export async function submitDeviation(id: string, expectedVersion: number): Promise<DeviationListItem> {
  return request(`/api/v1/deviations/${id}/submit`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ expectedVersion }),
  })
}

export async function getDeviationDetails(id: string, signal?: AbortSignal): Promise<DeviationDetails> {
  return request(`/api/v1/deviations/${id}/details`, { signal })
}

export async function downloadDeviationFinalReport(id: string): Promise<{ blob: Blob; fileName: string; sha256: string | null }> {
  const response = await qmsFetch(`/api/v1/deviations/${id}/final-report`)
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail ?? problem?.title ?? `PDF indirilemedi (${response.status})`)
  }
  const disposition = response.headers.get('content-disposition') ?? ''
  const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1]
  const plain = disposition.match(/filename="?([^";]+)"?/i)?.[1]
  return {
    blob: await response.blob(),
    fileName: encoded ? decodeURIComponent(encoded) : plain ?? 'sapma-nihai-kayit.pdf',
    sha256: response.headers.get('x-document-sha256'),
  }
}

export async function transitionDeviation(
  id: string,
  input: {
    transition: string
    expectedVersion: number
    note?: string
    effectivenessRequired?: boolean
    isEffective?: boolean
    signaturePassword?: string
    signatureMeaningAccepted?: boolean
  },
): Promise<DeviationDetails> {
  return request(`/api/v1/deviations/${id}/transitions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
}

export async function addDeviationInvestigation(
  id: string,
  input: {
    expectedVersion: number
    method: string
    rootCauseCategory: string
    rootCauseDescription: string
    conclusion: string
  },
): Promise<DeviationDetails> {
  return request(`/api/v1/deviations/${id}/investigations`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
}

export async function addDeviationBatchImpact(
  id: string,
  input: {
    expectedVersion: number
    batchNumber: string
    isAffected: boolean
    isLocked: boolean
    disposition: string
    rationale: string
  },
): Promise<DeviationDetails> {
  return request(`/api/v1/deviations/${id}/batch-impacts`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await qmsFetch(url, init)
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail ?? problem?.title ?? `İstek başarısız (${response.status})`)
  }

  return response.json() as Promise<T>
}
import { qmsFetch } from './http'
