import { qmsFetch } from './http'

const jsonHeaders = { 'Content-Type': 'application/json' }

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await qmsFetch(url, init)
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail ?? problem?.title ?? `İşlem tamamlanamadı (${response.status})`)
  }
  return await response.json() as T
}

export type FormFieldType = 'shortText' | 'longText' | 'number' | 'date' | 'dateTime' | 'singleSelect' | 'multiSelect' | 'checkbox'
export type FormTextAlignment = 'left' | 'center' | 'right' | 'justify'
export interface FormTextStyle {
  fontFamily: string
  fontSize: number
  bold: boolean
  italic: boolean
  underline: boolean
  alignment: FormTextAlignment
  color: string
  backgroundColor: string
}
export interface FormDocumentSettings {
  pageSize: 'A4'
  orientation: 'portrait' | 'landscape'
  marginTop: number
  marginRight: number
  marginBottom: number
  marginLeft: number
  defaultFontFamily: string
  defaultFontSize: number
}
export interface FormOption { value: string; label: string }
export interface FormCondition { fieldKey: string; operator: 'equals' | 'notEquals' | 'hasValue'; value?: unknown }
export interface FormField {
  key: string
  label: string
  type: FormFieldType
  required: boolean
  helpText?: string | null
  placeholder?: string | null
  width: 6 | 12
  order: number
  maxLength?: number | null
  min?: number | null
  max?: number | null
  unit?: string | null
  options: FormOption[]
  visibilityCondition?: FormCondition | null
  requiredCondition?: FormCondition | null
}
export interface FormSection {
  key: string
  title: string
  description?: string | null
  order: number
  columns: 1 | 2
  fields: FormField[]
  blocks?: FormDocumentBlock[]
}
export interface FormBlockPosition { x: number; y: number; width: number; height: number }
export interface FormFieldBlock { key: string; type: 'field'; order: number; fieldKey: string; style: FormTextStyle; position?: FormBlockPosition }
export interface FormTextRun { text: string; style: FormTextStyle }
export interface FormTextBlock { key: string; type: 'text'; order: number; text: string; style: FormTextStyle; runs?: FormTextRun[]; position?: FormBlockPosition }
export interface FormTableCell {
  key: string
  row: number
  column: number
  text: string
  fieldKey: string | null
  style: FormTextStyle
  backgroundColor: string
  runs?: FormTextRun[]
  colSpan?: number
  rowSpan?: number
}
export interface FormTableBlock {
  key: string
  type: 'table'
  order: number
  rows: number
  columns: number
  headerRows: number
  borderColor: string
  borderWidth: number
  cells: FormTableCell[]
  columnWidths?: number[]
  position?: FormBlockPosition
}
export type FormDocumentBlock = FormFieldBlock | FormTextBlock | FormTableBlock
export interface ElectronicFormSchema { engineVersion: 1; document?: FormDocumentSettings; sections: FormSection[] }
export interface OutputTemplateConfiguration {
  title: string
  footerText: string
  primaryColor: string
  includeEmptyFields: boolean
  includeAuditTrail: boolean
  includeSignatures: boolean
}
export interface ElectronicFormListItem {
  id: string
  code: string
  name: string
  description: string
  category: string
  kind: string
  isActive: boolean
  latestVersionNumber: number
  currentPublishedVersionId: string | null
  currentPublishedVersionNumber: number | null
  latestVersionStatus: string | null
  latestVersionId: string | null
  definitionVersion: number
  updatedAtUtc: string
}
export interface ElectronicFormVersion {
  id: string
  versionNumber: number
  status: 'Draft' | 'InReview' | 'Published'
  engineSchemaVersion: number
  schema: ElectronicFormSchema
  changeSummary: string
  workflowType: string
  createdByUserId: string
  createdBy: string
  createdAtUtc: string
  updatedAtUtc: string
  submittedAtUtc: string | null
  publishedByUserId: string | null
  publishedBy: string | null
  publishedAtUtc: string | null
  rowVersion: number
  outputTemplateId: string
  outputTemplate: OutputTemplateConfiguration
  outputTemplateRowVersion: number
}
export interface ElectronicFormDetails { definition: ElectronicFormListItem; versions: ElectronicFormVersion[] }
export interface ElectronicFormRecordListItem {
  id: string
  qualityRecordId: string
  recordNumber: string
  formCode: string
  formName: string
  formVersionNumber: number
  status: 'Draft' | 'Submitted' | 'Closed'
  createdBy: string
  createdByUserId: string
  createdAtUtc: string
  updatedAtUtc: string
  version: number
}
export interface ElectronicFormRecordDetails {
  record: ElectronicFormRecordListItem
  formDefinitionId: string
  formVersionId: string
  outputTemplateId: string
  schema: ElectronicFormSchema
  outputTemplate: OutputTemplateConfiguration
  data: Record<string, unknown>
  auditTrail: Array<{ eventType: string; actor: string; occurredAtUtc: string; version: number; reason: string | null }>
  signatures: Array<{ id: string; meaning: string; signer: string; signedAtUtc: string; recordVersion: number; contentHash: string; comment: string | null }>
}

export const listElectronicForms = (signal?: AbortSignal) => request<ElectronicFormListItem[]>('/api/v1/electronic-forms/definitions', { signal })
export const getElectronicForm = (id: string, signal?: AbortSignal) => request<ElectronicFormDetails>(`/api/v1/electronic-forms/definitions/${id}`, { signal })
export const createElectronicForm = (input: object) => request<ElectronicFormDetails>('/api/v1/electronic-forms/definitions', { method: 'POST', headers: jsonHeaders, body: JSON.stringify(input) })
export const updateElectronicForm = (id: string, input: object) => request<ElectronicFormDetails>(`/api/v1/electronic-forms/definitions/${id}/draft`, { method: 'PUT', headers: jsonHeaders, body: JSON.stringify(input) })
export const submitElectronicFormForReview = (id: string, expectedVersion: number, comment?: string) => request<ElectronicFormDetails>(`/api/v1/electronic-forms/definitions/${id}/submit-review`, { method: 'POST', headers: jsonHeaders, body: JSON.stringify({ expectedVersion, comment, password: null, meaningAccepted: false }) })
export const publishElectronicForm = (id: string, input: object) => request<ElectronicFormDetails>(`/api/v1/electronic-forms/definitions/${id}/publish`, { method: 'POST', headers: jsonHeaders, body: JSON.stringify(input) })
export const startElectronicFormVersion = (id: string, input: object) => request<ElectronicFormDetails>(`/api/v1/electronic-forms/definitions/${id}/versions`, { method: 'POST', headers: jsonHeaders, body: JSON.stringify(input) })

export const listElectronicFormRecords = (signal?: AbortSignal) => request<ElectronicFormRecordListItem[]>('/api/v1/electronic-forms/records', { signal })
export const getElectronicFormRecord = (id: string, signal?: AbortSignal) => request<ElectronicFormRecordDetails>(`/api/v1/electronic-forms/records/${id}`, { signal })
export const createElectronicFormRecord = (formDefinitionId: string, data: Record<string, unknown>) => request<ElectronicFormRecordDetails>('/api/v1/electronic-forms/records', { method: 'POST', headers: jsonHeaders, body: JSON.stringify({ formDefinitionId, data }) })
export const updateElectronicFormRecord = (id: string, expectedVersion: number, data: Record<string, unknown>) => request<ElectronicFormRecordDetails>(`/api/v1/electronic-forms/records/${id}/draft`, { method: 'PUT', headers: jsonHeaders, body: JSON.stringify({ expectedVersion, data }) })
export const submitElectronicFormRecord = (id: string, expectedVersion: number) => request<ElectronicFormRecordDetails>(`/api/v1/electronic-forms/records/${id}/submit`, { method: 'POST', headers: jsonHeaders, body: JSON.stringify({ expectedVersion }) })
export const approveElectronicFormRecord = (id: string, input: object) => request<ElectronicFormRecordDetails>(`/api/v1/electronic-forms/records/${id}/approve`, { method: 'POST', headers: jsonHeaders, body: JSON.stringify(input) })

export async function downloadElectronicFormFinalReport(id: string, fallbackName: string) {
  const response = await qmsFetch(`/api/v1/electronic-forms/records/${id}/final-report`)
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string } | null
    throw new Error(problem?.detail ?? 'Nihai elektronik form PDF’i indirilemedi.')
  }
  const disposition = response.headers.get('content-disposition') ?? ''
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1]
  const plain = /filename="?([^";]+)"?/i.exec(disposition)?.[1]
  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = encoded ? decodeURIComponent(encoded) : plain ?? `${fallbackName}.pdf`
  anchor.click()
  URL.revokeObjectURL(url)
}
