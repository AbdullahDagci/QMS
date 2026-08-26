import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { AppLayout } from "./layout/AppLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { ModulePlaceholderPage } from "./pages/ModulePlaceholderPage";
import { DeviationWorkspace } from "./features/deviations/DeviationWorkspace";
import { CapaWorkspace } from "./features/capas/CapaWorkspace";
import { AccessManagementPage } from "./pages/AccessManagementPage";
import { ChangeControlWorkspace } from "./features/change-controls/ChangeControlWorkspace";
import { DocumentWorkspace } from "./features/documents/DocumentWorkspace";
import { TrainingWorkspace } from "./features/trainings/TrainingWorkspace";
import { ComplaintWorkspace } from "./features/complaints/ComplaintWorkspace";
import { InternalAuditWorkspace } from "./features/internal-audits/InternalAuditWorkspace";
import { ExternalAuditWorkspace } from "./features/external-audits/ExternalAuditWorkspace";
import { SupplierAuditWorkspace } from "./features/supplier-audits/SupplierAuditWorkspace";
import { WorkItemWorkspace } from "./features/work-items/WorkItemWorkspace";
import { RiskWorkspace } from "./features/risks/RiskWorkspace";
import { MbrWorkspace } from "./features/mbrs/MbrWorkspace";
import { SpecializedWorkspace } from "./features/specialized/SpecializedWorkspace";
import "./App.css";
import { useAuth } from "./security/AuthContext";
import { LoginPage } from "./pages/LoginPage";

function App() {
  const { authenticated } = useAuth();
  if (!authenticated) return <LoginPage />;
  return (
    <BrowserRouter>
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
    </BrowserRouter>
  );
}

export default App;
