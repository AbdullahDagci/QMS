import { qmsFetch } from "./http";

export interface UserProfileOption {
  key: string;
  displayName: string;
  departmentName: string;
  roles: string[];
}
export interface CurrentUser {
  id: string;
  displayName: string;
  profile: string;
  roles: string[];
  permissions: string[];
  availableProfiles: UserProfileOption[];
}

export interface LoginResult {
  expiresAtUtc: string;
  profileKey: string;
  displayName: string;
}
async function loginJson(response: Response) {
  if (!response.ok) throw new Error("E-posta veya parola hatalı.");
  return response.json() as Promise<LoginResult>;
}
export const login = (email: string, password: string) =>
  qmsFetch("/api/v1/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  }).then(loginJson);
export const quickLogin = (profileKey: string) =>
  qmsFetch("/api/v1/auth/quick-login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ profileKey }),
  }).then(loginJson);
export const getQuickProfiles = async (): Promise<UserProfileOption[]> => {
  const response = await fetch("/api/v1/auth/quick-profiles");
  if (!response.ok) return [];
  return response.json() as Promise<UserProfileOption[]>;
};
export const logout = async () => {
  await qmsFetch("/api/v1/auth/logout", { method: "POST" }).catch(
    () => undefined,
  );
};

export async function changePassword(currentPassword: string, newPassword: string) {
  const response = await qmsFetch("/api/v1/auth/change-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ currentPassword, newPassword }),
  });
  if (response.ok) return;
  const body = await response.json().catch(() => null) as {
    errors?: Record<string, string[]>;
    detail?: string;
    title?: string;
  } | null;
  const validation = body?.errors ? Object.values(body.errors).flat().join(" ") : null;
  throw new Error(validation || body?.detail || body?.title || "Parola değiştirilemedi.");
}

export async function getCurrentUser(
  signal?: AbortSignal,
): Promise<CurrentUser> {
  const response = await qmsFetch("/api/v1/auth/me", { signal });
  if (!response.ok)
    throw new Error(`Kullanıcı bilgisi alınamadı (${response.status})`);
  return response.json() as Promise<CurrentUser>;
}
