import { Alert, Typography } from "@mui/material";
import { AssignmentIndRounded } from "@mui/icons-material";
import type { DeviationNextAssignee } from "../../api/deviations";
import { taskRoleLabel } from "./taskRoles";

export function NextAssigneeNotice({
  nextAssignee,
  when,
}: {
  nextAssignee: DeviationNextAssignee;
  when: string;
}) {
  const role = taskRoleLabel(nextAssignee.taskRole);
  if (!nextAssignee.userId)
    return (
      <Alert severity="warning" sx={{ width: "100%" }}>
        {when} {role.toLocaleLowerCase("tr")} görevi için eşleşen atama kuralı
        yok; iş akışı bu adımda durur. Sistem yöneticisi görev matrisini
        kontrol etmelidir.
      </Alert>
    );
  return (
    <Alert
      severity="info"
      icon={<AssignmentIndRounded />}
      sx={{ width: "100%" }}
    >
      <Typography sx={{ fontWeight: 800 }}>
        {when} görev {nextAssignee.userName} kullanıcısına atanacak
      </Typography>
      <Typography variant="body2">
        {nextAssignee.departmentName ? `${nextAssignee.departmentName} · ` : ""}
        {role} · M.01 görev-atama matrisine göre
      </Typography>
    </Alert>
  );
}
