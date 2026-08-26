import { qmsFetch } from './http'

export interface Department { id: string; code: string; name: string; managerUserId: string | null; managerName: string | null; isActive: boolean }
export interface Position { id: string; code: string; name: string; isManagement: boolean; isActive: boolean }
export interface RoleDefinition { code: string; name: string; description: string }
export interface AccessUser { id: string; profileKey: string; displayName: string; email: string; departmentId: string | null; departmentName: string | null; isActive: boolean; roles: string[]; positions: Position[] }
export interface Delegation { id: string; delegatorUserId: string; delegatorName: string; delegateUserId: string; delegateName: string; scope: string; reason: string; startsAtUtc: string; endsAtUtc: string; revokedAtUtc: string | null }
export interface WorkflowAssignment { id: string; aggregateType: string; aggregateId: string; taskRole: string; assignedUserId: string; assignedUserName: string; assignedDepartmentId: string | null; departmentName: string | null; status: string; assignedAtUtc: string; dueAtUtc: string | null; completedAtUtc: string | null }
export interface AccessOverview { departments: Department[]; positions: Position[]; users: AccessUser[]; delegations: Delegation[]; roles: RoleDefinition[]; activeAssignments: WorkflowAssignment[] }

async function json<T>(response: Response): Promise<T> { if (!response.ok) throw new Error((await response.text()) || `İşlem başarısız (${response.status})`); return response.json() as Promise<T> }
export const getAccessOverview = (signal?: AbortSignal) => qmsFetch('/api/v1/admin/access/overview', { signal }).then(json<AccessOverview>)
export const createDepartment = (input: { code: string; name: string; managerUserId: string | null }) => qmsFetch('/api/v1/admin/access/departments', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }).then(json<Department>)
export const updateDepartment = (id: string, input: { name: string; managerUserId: string | null; isActive: boolean }) => qmsFetch(`/api/v1/admin/access/departments/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }).then(json<Department>)
export const updateUserAccess = (id: string, input: { departmentId: string | null; roles: string[]; positionIds: string[]; isActive: boolean }) => qmsFetch(`/api/v1/admin/access/users/${id}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }).then(json<AccessUser>)
export const createUser = (input: { displayName: string; email: string; password: string; departmentId: string; roles: string[]; positionIds: string[] }) => qmsFetch('/api/v1/admin/access/users', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }).then(json<AccessUser>)
export const createDelegation = (input: { delegatorUserId: string; delegateUserId: string; scope: string; reason: string; startsAtUtc: string; endsAtUtc: string }) => qmsFetch('/api/v1/admin/access/delegations', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }).then(json<Delegation>)
export const revokeDelegation = async (id: string) => { const response = await qmsFetch(`/api/v1/admin/access/delegations/${id}/revoke`, { method: 'POST' }); if (!response.ok) throw new Error(`Delegasyon kaldırılamadı (${response.status})`) }
export const getWorkflowAssignments = (aggregateType: string, aggregateId: string, signal?: AbortSignal) =>
  qmsFetch(`/api/v1/workflow/assignments/${aggregateType}/${aggregateId}`, { signal }).then(json<WorkflowAssignment[]>)
export const createWorkflowAssignment = (input: { aggregateType: string; aggregateId: string; taskRole: string; assignedUserId: string; assignedDepartmentId: string | null; dueAtUtc: string | null }) =>
  qmsFetch('/api/v1/admin/access/assignments', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }).then(json<WorkflowAssignment>)
