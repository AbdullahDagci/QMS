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
import "./App.css";

function App() {
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
