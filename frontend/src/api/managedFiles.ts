import { qmsFetch } from "./http";

export interface ManagedFile {
  id: string;
  aggregateType: string;
  aggregateId: string;
  category: string;
  fileName: string;
  contentType: string;
  size: number;
  sha256: string;
  uploadedBy: string;
  uploadedAtUtc: string;
  retainUntilUtc: string;
}

export async function listManagedFiles(aggregateType: string, aggregateId: string,
  signal?: AbortSignal): Promise<ManagedFile[]> {
  return request(`/api/v1/files/${aggregateType}/${aggregateId}`, { signal });
}

export async function uploadManagedFile(aggregateType: string, aggregateId: string,
  category: string, file: File): Promise<ManagedFile> {
  const form = new FormData();
  form.append("category", category);
  form.append("file", file);
  return request(`/api/v1/files/${aggregateType}/${aggregateId}`, {
    method: "POST",
    body: form,
  });
}

export async function downloadManagedFile(file: ManagedFile): Promise<void> {
  const response = await qmsFetch(`/api/v1/files/${file.id}/download`);
  if (!response.ok) throw await problem(response);
  const url = URL.createObjectURL(await response.blob());
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = file.fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await qmsFetch(url, init);
  if (!response.ok) throw await problem(response);
  return response.json() as Promise<T>;
}

async function problem(response: Response) {
  const body = await response.json().catch(() => null) as { detail?: string; title?: string } | null;
  return new Error(body?.detail ?? body?.title ?? `İstek başarısız (${response.status})`);
}
