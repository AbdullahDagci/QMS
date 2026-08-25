import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { currentProfileKey, QMS_PROFILE_KEY } from "../api/http";
import { getCurrentUser, type CurrentUser } from "../api/security";

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
}

const AuthContext = createContext<AuthValue>({
  user: fallbackUser,
  loading: false,
  can: () => true,
  selectProfile: () => undefined,
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [profile, setProfile] = useState(currentProfileKey);
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
        queryClient.invalidateQueries();
      },
    }),
    [user, currentUser.isLoading, queryClient],
  );
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
