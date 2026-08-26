import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  currentProfileKey,
  QMS_PROFILE_KEY,
  QMS_SESSION_KEY,
} from "../api/http";
import {
  getCurrentUser,
  logout as logoutRequest,
  type CurrentUser,
} from "../api/security";

export const Permissions = {
  qualityView: "quality.view",
  deviationCreate: "deviation.create",
  deviationInvestigate: "deviation.investigate",
  deviationManage: "deviation.manage",
  capaPlan: "capa.plan",
  capaCompleteAction: "capa.complete-action",
  capaVerify: "capa.verify",
  capaManage: "capa.manage",
  changeCreate: "change.create",
  changeReview: "change.review",
  changeExecute: "change.execute",
  changeApprove: "change.approve",
  documentCreate: "document.create",
  documentWrite: "document.write",
  documentReview: "document.review",
  documentApprove: "document.approve",
  documentDistribute: "document.distribute",
  documentRead: "document.read",
  trainingView: "training.view",
  trainingManage: "training.manage",
  trainingComplete: "training.complete",
  trainingApprove: "training.approve",
  complaintView: "complaint.view",
  complaintCreate: "complaint.create",
  complaintInvestigate: "complaint.investigate",
  complaintManage: "complaint.manage",
  complaintApprove: "complaint.approve",
  internalAuditView: "internal-audit.view",
  internalAuditPlan: "internal-audit.plan",
  internalAuditExecute: "internal-audit.execute",
  internalAuditRespond: "internal-audit.respond",
  internalAuditApprove: "internal-audit.approve",
  externalAuditView: "external-audit.view",
  externalAuditCreate: "external-audit.create",
  externalAuditPrepare: "external-audit.prepare",
  externalAuditRespond: "external-audit.respond",
  externalAuditApprove: "external-audit.approve",
  supplierAuditView: "supplier-audit.view",
  supplierAuditPlan: "supplier-audit.plan",
  supplierAuditExecute: "supplier-audit.execute",
  supplierAuditRespond: "supplier-audit.respond",
  supplierAuditApprove: "supplier-audit.approve",
  workTrackingView: "work-tracking.view",
  workTrackingCreate: "work-tracking.create",
  workTrackingManage: "work-tracking.manage",
  workTrackingVerify: "work-tracking.verify",
  riskView: "risk.view",
  riskCreate: "risk.create",
  riskManage: "risk.manage",
  riskApprove: "risk.approve",
  mbrView: "mbr.view",
  mbrCreate: "mbr.create",
  mbrWrite: "mbr.write",
  mbrReview: "mbr.review",
  mbrApprove: "mbr.approve",
  specializedView: "specialized.view",
  specializedManage: "specialized.manage",
  administrationManage: "administration.manage",
} as const;

const fallbackUser: CurrentUser = {
  id: "",
  displayName: "Oturum doğrulanıyor",
  profile: "",
  roles: [],
  permissions: [],
  availableProfiles: [],
};

interface AuthValue {
  user: CurrentUser;
  loading: boolean;
  can: (permission: string) => boolean;
  selectProfile: (profile: string) => void;
  authenticated: boolean;
  refresh: () => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthValue>({
  user: fallbackUser,
  loading: false,
  can: () => true,
  selectProfile: () => undefined,
  authenticated: true,
  refresh: async () => undefined,
  logout: async () => undefined,
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState(currentProfileKey);
  const [authenticated, setAuthenticated] = useState(() =>
    Boolean(window.localStorage.getItem(QMS_SESSION_KEY)),
  );
  const queryClient = useQueryClient();
  const currentUser = useQuery({
    queryKey: ["current-user", profile],
    queryFn: ({ signal }) => getCurrentUser(signal),
    retry: false,
  });
  const user = currentUser.data ?? fallbackUser;
  const value = useMemo<AuthValue>(
    () => ({
      user,
      loading: currentUser.isLoading,
      can: (permission) => user.permissions.includes(permission),
      selectProfile: (next) => {
        window.localStorage.setItem(QMS_PROFILE_KEY, next);
        setProfile(next);
        // Record details contain user-specific workflow capabilities. Do not
        // carry an assignee's cached transitions into another test profile.
        void queryClient.resetQueries();
      },
      authenticated,
      refresh: async () => {
        setAuthenticated(Boolean(window.localStorage.getItem(QMS_SESSION_KEY)));
        // A successful login changes the authorization context for every
        // request, not only /me. Refetch all active data with the new token.
        await queryClient.resetQueries();
      },
      logout: async () => {
        await logoutRequest();
        setAuthenticated(false);
        // Prevent permission-sensitive responses (available transitions,
        // assignments, notifications) from leaking into the next session.
        queryClient.clear();
      },
    }),
    [user, currentUser.isLoading, queryClient, authenticated],
  );
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
