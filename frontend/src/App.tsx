import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { AppLayout } from "./layout/AppLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { ModulePlaceholderPage } from "./pages/ModulePlaceholderPage";
import "./App.css";
import { useAuth } from "./security/AuthContext";
import { LoginPage } from "./pages/LoginPage";

const DeviationWorkspace = lazy(() => import("./features/deviations/DeviationWorkspace").then((m) => ({ default: m.DeviationWorkspace })));
const CapaWorkspace = lazy(() => import("./features/capas/CapaWorkspace").then((m) => ({ default: m.CapaWorkspace })));
const AccessManagementPage = lazy(() => import("./pages/AccessManagementPage").then((m) => ({ default: m.AccessManagementPage })));
const ChangeControlWorkspace = lazy(() => import("./features/change-controls/ChangeControlWorkspace").then((m) => ({ default: m.ChangeControlWorkspace })));
const DocumentWorkspace = lazy(() => import("./features/documents/DocumentWorkspace").then((m) => ({ default: m.DocumentWorkspace })));
const TrainingWorkspace = lazy(() => import("./features/trainings/TrainingWorkspace").then((m) => ({ default: m.TrainingWorkspace })));
const ComplaintWorkspace = lazy(() => import("./features/complaints/ComplaintWorkspace").then((m) => ({ default: m.ComplaintWorkspace })));
const InternalAuditWorkspace = lazy(() => import("./features/internal-audits/InternalAuditWorkspace").then((m) => ({ default: m.InternalAuditWorkspace })));
const ExternalAuditWorkspace = lazy(() => import("./features/external-audits/ExternalAuditWorkspace").then((m) => ({ default: m.ExternalAuditWorkspace })));
const SupplierAuditWorkspace = lazy(() => import("./features/supplier-audits/SupplierAuditWorkspace").then((m) => ({ default: m.SupplierAuditWorkspace })));
const WorkItemWorkspace = lazy(() => import("./features/work-items/WorkItemWorkspace").then((m) => ({ default: m.WorkItemWorkspace })));
const RiskWorkspace = lazy(() => import("./features/risks/RiskWorkspace").then((m) => ({ default: m.RiskWorkspace })));
const MbrWorkspace = lazy(() => import("./features/mbrs/MbrWorkspace").then((m) => ({ default: m.MbrWorkspace })));
const SpecializedWorkspace = lazy(() => import("./features/specialized/SpecializedWorkspace").then((m) => ({ default: m.SpecializedWorkspace })));
const ElectronicFormsWorkspace = lazy(() => import("./features/electronic-forms/ElectronicFormsWorkspace").then((m) => ({ default: m.ElectronicFormsWorkspace })));

function App() {
  const { authenticated } = useAuth();
  if (!authenticated) return <LoginPage />;
  return (
    <BrowserRouter>
      <Suspense fallback={<div className="route-loading" role="progressbar" aria-label="Modül yükleniyor" />}>
        <Routes>
          <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="modules/deviations" element={<DeviationWorkspace />} />
          <Route path="modules/m02" element={<CapaWorkspace />} />
          <Route path="modules/m03" element={<ChangeControlWorkspace />} />
          <Route path="modules/m04" element={<DocumentWorkspace />} />
          <Route path="modules/m05" element={<TrainingWorkspace />} />
          <Route path="modules/m06" element={<ComplaintWorkspace />} />
          <Route path="modules/m07" element={<InternalAuditWorkspace />} />
          <Route path="modules/m08" element={<ExternalAuditWorkspace />} />
          <Route path="modules/m09" element={<SupplierAuditWorkspace />} />
          <Route path="modules/m10" element={<WorkItemWorkspace />} />
          <Route path="modules/m11" element={<RiskWorkspace />} />
          <Route path="modules/m12" element={<MbrWorkspace />} />
          <Route path="forms" element={<ElectronicFormsWorkspace />} />
          <Route
            path="modules/m13"
            element={<SpecializedWorkspace module="m13" />}
          />
          <Route
            path="modules/m14"
            element={<SpecializedWorkspace module="m14" />}
          />
          <Route
            path="modules/m15"
            element={<SpecializedWorkspace module="m15" />}
          />
          <Route
            path="modules/m16"
            element={<SpecializedWorkspace module="m16" />}
          />
          <Route
            path="administration/access"
            element={<AccessManagementPage />}
          />
          <Route
            path="modules/:moduleCode"
            element={<ModulePlaceholderPage />}
          />
          <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
}

export default App;
