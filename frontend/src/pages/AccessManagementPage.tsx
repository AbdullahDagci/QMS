import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  FormControlLabel,
  LinearProgress,
  Paper,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import {
  AccountTreeRounded,
  AddRounded,
  AdminPanelSettingsRounded,
  BadgeRounded,
  GroupsRounded,
  ManageAccountsRounded,
  SwapHorizRounded,
  TaskAltRounded,
} from "@mui/icons-material";
import {
  createDelegation,
  createUser,
  createDepartment,
  createWorkflowAssignment,
  getAccessOverview,
  revokeDelegation,
  updateUserAccess,
  updateDepartment,
  type Department,
  type AccessOverview,
  type AccessUser,
} from "../api/access";
import { ModalHeader } from "../components/ModalHeader";
import { Permissions, useAuth } from "../security/AuthContext";
import { getChangeControlDetails } from "../api/changeControls";

const taskLabels: Record<string, string> = {
  Initiator: "Başlatan",
  ProcessAuthority: "İşlem yetkilisi",
  Investigator: "Araştırmacı",
  ActionOwner: "Aksiyon sorumlusu",
  Evaluator: "Değerlendiren",
  Approver: "Onaylayan",
  QualifiedPerson: "Mesul Müdür",
  ComplaintCoordinator: "Şikâyet koordinatörü",
  ComplaintInvestigator: "Şikâyet araştırmacısı",
  ResponseApprover: "Müşteri yanıtı onaylayanı",
  PharmacovigilanceReviewer: "Farmakovijilans değerlendiricisi",
  AuditPlanner: "Denetim planlayıcısı",
  LeadAuditor: "Baş denetçi",
  AuditeeResponder: "Denetlenen bölüm yanıtlayanı",
  ExternalAuditCoordinator: "Dış denetim koordinatörü",
  DocumentPackageController: "Talep paketi kontrolörü",
  ExternalAuditeeResponder: "Resmi bulgu yanıtlayanı",
  ExternalAuditAuthorizedCloser: "Yetkili kapanış sorumlusu",
  SupplierAuditPlanner: "Tedarikçi denetimi planlayıcısı",
  SupplierAuditLeadAuditor: "Tedarikçi baş denetçisi",
  SupplierResponder: "Tedarikçi yanıt sorumlusu",
  SupplierAuditVerifier: "Tedarikçi kanıt doğrulayıcısı",
  SupplierQualityApprover: "Tedarikçi kalite onaylayanı",
};

export function AccessManagementPage() {
  const { can } = useAuth();
  const [tab, setTab] = useState(0);
  const [editing, setEditing] = useState<AccessUser | null>(null);
  const [userCreatorOpen, setUserCreatorOpen] = useState(false);
  const [delegationOpen, setDelegationOpen] = useState(false);
  const [assignmentOpen, setAssignmentOpen] = useState(false);
  const [departmentEditor, setDepartmentEditor] = useState<Department | "new" | null>(null);
  const overview = useQuery({
    queryKey: ["access-overview"],
    queryFn: ({ signal }) => getAccessOverview(signal),
    retry: false,
    enabled: can(Permissions.administrationManage),
  });

  if (!can(Permissions.administrationManage))
    return (
      <Alert severity="error">
        Kullanıcı ve yetki yönetimi yalnız Sistem Yöneticisi rolüne açıktır.
      </Alert>
    );
  if (overview.isLoading)
    return (
      <Paper className="access-loading">
        <LinearProgress />
        <Typography>Organizasyon ve yetki modeli yükleniyor…</Typography>
      </Paper>
    );
  if (overview.isError || !overview.data)
    return (
      <Alert severity="error">
        Yetki modeli alınamadı: {overview.error?.message}
      </Alert>
    );
  const data = overview.data;

  return (
    <Box component="section" className="access-page">
      <Stack
        direction={{ xs: "column", md: "row" }}
        className="access-page-heading"
      >
        <Stack direction="row" spacing={1.4} sx={{ alignItems: "center" }}>
          <Box className="section-heading-icon tone-teal">
            <AdminPanelSettingsRounded />
          </Box>
          <Box>
            <Typography variant="h2">Kullanıcı ve Yetki Yönetimi</Typography>
            <Typography color="text.secondary">
              Sistem rolü, organizasyon pozisyonu ve kayıt görevi aynı kontrol
              merkezinde.
            </Typography>
          </Box>
        </Stack>
        <Stack
          direction="row"
          spacing={1.2}
          useFlexGap
          sx={{ flexWrap: "wrap" }}
        >
          <Button variant="outlined" startIcon={<AddRounded />} onClick={() => setUserCreatorOpen(true)}>Kullanıcı ekle</Button>
          <Button
            variant="outlined"
            startIcon={<TaskAltRounded />}
            onClick={() => setAssignmentOpen(true)}
          >
            Kayıt görevi ata
          </Button>
          <Button
            variant="contained"
            startIcon={<SwapHorizRounded />}
            onClick={() => setDelegationOpen(true)}
          >
            Delegasyon tanımla
          </Button>
        </Stack>
      </Stack>
      <Box className="access-metric-grid">
        <AccessMetric
          icon={GroupsRounded}
          value={data.users.filter((x) => x.isActive).length}
          label="Aktif kullanıcı"
          tone="blue"
        />
        <AccessMetric
          icon={AccountTreeRounded}
          value={data.departments.filter((x) => x.isActive).length}
          label="Bölüm"
          tone="green"
        />
        <AccessMetric
          icon={BadgeRounded}
          value={data.positions.filter((x) => x.isActive).length}
          label="Pozisyon"
          tone="amber"
        />
        <AccessMetric
          icon={TaskAltRounded}
          value={data.activeAssignments.length}
          label="Aktif kayıt görevi"
          tone="rose"
        />
      </Box>
      <Paper className="access-workspace" elevation={0}>
        <Tabs
          value={tab}
          onChange={(_, value: number) => setTab(value)}
          variant="scrollable"
          scrollButtons="auto"
        >
          <Tab label={`Kullanıcılar (${data.users.length})`} />
          <Tab label={`Rol matrisi (${data.roles.length})`} />
          <Tab label="Organizasyon" />
          <Tab
            label={`Delegasyon (${data.delegations.filter((x) => !x.revokedAtUtc).length})`}
          />
          <Tab label={`Kayıt görevleri (${data.activeAssignments.length})`} />
        </Tabs>
        <Box className="access-tab-panel">
          {tab === 0 && <UsersTable data={data} onEdit={setEditing} />}
          {tab === 1 && <RoleMatrix data={data} />}
          {tab === 2 && <OrganizationView data={data} onEdit={setDepartmentEditor} />}
          {tab === 3 && <DelegationsView data={data} />}
          {tab === 4 && <AssignmentsView data={data} />}
        </Box>
      </Paper>
      <UserAccessDialog
        user={editing}
        data={data}
        onClose={() => setEditing(null)}
      />
      <DelegationDialog
        open={delegationOpen}
        data={data}
        onClose={() => setDelegationOpen(false)}
      />
      <AssignmentDialog
        open={assignmentOpen}
        data={data}
        onClose={() => setAssignmentOpen(false)}
      />
      <CreateUserDialog open={userCreatorOpen} data={data} onClose={() => setUserCreatorOpen(false)} />
      <DepartmentDialog department={departmentEditor} data={data} onClose={() => setDepartmentEditor(null)} />
    </Box>
  );
}

function AccessMetric({
  icon: Icon,
  value,
  label,
  tone,
}: {
  icon: typeof GroupsRounded;
  value: number;
  label: string;
  tone: string;
}) {
  return (
    <Paper className={`access-metric access-tone-${tone}`} elevation={0}>
      <Box className="access-metric-icon">
        <Icon />
      </Box>
      <Box>
        <Typography variant="h4">{value}</Typography>
        <Typography color="text.secondary">{label}</Typography>
      </Box>
    </Paper>
  );
}

function UsersTable({
  data,
  onEdit,
}: {
  data: AccessOverview;
  onEdit: (user: AccessUser) => void;
}) {
  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Kullanıcı</TableCell>
            <TableCell>Bölüm / pozisyon</TableCell>
            <TableCell>Sistem rolleri</TableCell>
            <TableCell>Durum</TableCell>
            <TableCell align="right">İşlem</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data.users.map((user) => (
            <TableRow key={user.id} hover>
              <TableCell>
                <Stack
                  direction="row"
                  spacing={1.2}
                  sx={{ alignItems: "center" }}
                >
                  <Box className="access-user-avatar">
                    {initials(user.displayName)}
                  </Box>
                  <Box>
                    <Typography sx={{ fontWeight: 780 }}>
                      {user.displayName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {user.email}
                    </Typography>
                  </Box>
                </Stack>
              </TableCell>
              <TableCell>
                <Typography>
                  {user.departmentName ?? "Bölüm atanmamış"}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {user.positions.map((x) => x.name).join(" · ") ||
                    "Pozisyon atanmamış"}
                </Typography>
              </TableCell>
              <TableCell>
                <Stack
                  direction="row"
                  spacing={0.7}
                  useFlexGap
                  sx={{ flexWrap: "wrap" }}
                >
                  {user.roles.map((role) => (
                    <Chip
                      size="small"
                      key={role}
                      label={
                        data.roles.find((x) => x.code === role)?.name ?? role
                      }
                    />
                  ))}
                </Stack>
              </TableCell>
              <TableCell>
                <Chip
                  size="small"
                  color={user.isActive ? "success" : "default"}
                  label={user.isActive ? "Aktif" : "Pasif"}
                />
              </TableCell>
              <TableCell align="right">
                <Button
                  startIcon={<ManageAccountsRounded />}
                  onClick={() => onEdit(user)}
                >
                  Yetkileri düzenle
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

function RoleMatrix({ data }: { data: AccessOverview }) {
  return (
    <Box className="role-card-grid">
      {data.roles.map((role) => {
        const users = data.users.filter((user) =>
          user.roles.includes(role.code),
        );
        return (
          <Paper
            variant="outlined"
            className="role-definition-card"
            key={role.code}
          >
            <Stack
              direction="row"
              sx={{ justifyContent: "space-between", gap: 1 }}
            >
              <Box className="role-definition-icon">
                <AdminPanelSettingsRounded />
              </Box>
              <Chip size="small" label={`${users.length} kullanıcı`} />
            </Stack>
            <Typography variant="h6">{role.name}</Typography>
            <Typography variant="body2" color="text.secondary">
              {role.description}
            </Typography>
            <Stack
              direction="row"
              spacing={0.7}
              useFlexGap
              sx={{ flexWrap: "wrap", mt: 2 }}
            >
              {users.map((user) => (
                <Chip
                  size="small"
                  variant="outlined"
                  key={user.id}
                  label={user.displayName}
                />
              ))}
            </Stack>
          </Paper>
        );
      })}
    </Box>
  );
}

function OrganizationView({ data, onEdit }: { data: AccessOverview; onEdit: (department: Department | "new") => void }) {
  return (
    <>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", mb: 2 }}><Typography variant="h6" sx={{ fontWeight: 800 }}>Bölüm ve pozisyon yapısı</Typography><Button variant="contained" startIcon={<AddRounded />} onClick={() => onEdit("new")}>Yeni bölüm</Button></Stack>
      <Box className="department-grid">
        {data.departments.map((department) => (
          <Paper
            variant="outlined"
            className="department-card"
            key={department.id}
          >
            <Stack direction="row" sx={{ justifyContent: "space-between" }}>
              <Box className="department-code">{department.code}</Box>
              <Chip
                size="small"
                color={department.isActive ? "success" : "default"}
                label={department.isActive ? "Aktif" : "Pasif"}
              />
            </Stack>
            <Typography variant="h6">{department.name}</Typography>
            <Typography variant="body2" color="text.secondary">
              Yönetici: {department.managerName ?? "Atanmadı"}
            </Typography>
            <Typography variant="caption">
              {
                data.users.filter((user) => user.departmentId === department.id)
                  .length
              }{" "}
              kullanıcı
            </Typography>
            <Button sx={{ mt: 1 }} onClick={() => onEdit(department)}>Bölümü düzenle</Button>
          </Paper>
        ))}
      </Box>
      <Typography variant="h6" sx={{ fontWeight: 800, mt: 3, mb: 2 }}>
        Pozisyon kataloğu
      </Typography>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: "wrap" }}>
        {data.positions.map((position) => (
          <Chip
            key={position.id}
            color={position.isManagement ? "primary" : "default"}
            variant="outlined"
            label={`${position.name}${position.isManagement ? " · Yönetim" : ""}`}
          />
        ))}
      </Stack>
    </>
  );
}

function DepartmentDialog({ department, data, onClose }: { department: Department | "new" | null; data: AccessOverview; onClose: () => void }) {
  const client = useQueryClient(); const editing = department && department !== "new" ? department : null;
  const [code, setCode] = useState(""); const [name, setName] = useState(""); const [managerId, setManagerId] = useState<string | null>(null); const [active, setActive] = useState(true);
  useEffect(() => { if (!department) return; setCode(editing?.code ?? ""); setName(editing?.name ?? ""); setManagerId(editing?.managerUserId ?? null); setActive(editing?.isActive ?? true); }, [department, editing]);
  const manager = data.users.find(x => x.id === managerId) ?? null;
  const mutation = useMutation({ mutationFn: () => editing ? updateDepartment(editing.id, { name, managerUserId: managerId, isActive: active }) : createDepartment({ code, name, managerUserId: managerId }), onSuccess: async () => { await client.invalidateQueries({ queryKey: ["access-overview"] }); onClose(); } });
  return <Dialog open={Boolean(department)} onClose={onClose} maxWidth="sm" fullWidth><ModalHeader onClose={onClose}><Typography variant="h5" sx={{ fontWeight: 800 }}>{editing ? "Bölümü düzenle" : "Yeni bölüm"}</Typography><Typography variant="body2" color="text.secondary">Bölüm yöneticisi, M.03 bölüm değerlendirmelerinde öncelikli değerlendirici olur.</Typography></ModalHeader><DialogContent dividers><Stack spacing={2} sx={{ pt: 1 }}>{mutation.isError && <Alert severity="error">{mutation.error.message}</Alert>}<TextField label="Bölüm kodu" value={code} disabled={Boolean(editing)} onChange={e => setCode(e.target.value.toUpperCase())} /><TextField label="Bölüm adı" value={name} onChange={e => setName(e.target.value)} /><Autocomplete options={data.users.filter(x => x.isActive)} value={manager} onChange={(_, value) => setManagerId(value?.id ?? null)} getOptionLabel={x => `${x.displayName} · ${x.departmentName ?? "Bölümsüz"}`} renderInput={params => <TextField {...params} label="Bölüm yöneticisi / değerlendiricisi" />} />{editing && <FormControlLabel control={<Checkbox checked={active} onChange={e => setActive(e.target.checked)} />} label="Bölüm aktif" />}</Stack></DialogContent><DialogActions><Button onClick={onClose}>Vazgeç</Button><Button variant="contained" disabled={!name.trim() || (!editing && !code.trim()) || mutation.isPending} onClick={() => mutation.mutate()}>Kaydet</Button></DialogActions></Dialog>;
}

function DelegationsView({ data }: { data: AccessOverview }) {
  const client = useQueryClient();
  const revoke = useMutation({
    mutationFn: revokeDelegation,
    onSuccess: () =>
      client.invalidateQueries({ queryKey: ["access-overview"] }),
  });
  return (
    <Stack spacing={1.2}>
      {data.delegations.map((item) => (
        <Paper variant="outlined" className="delegation-card" key={item.id}>
          <Stack
            direction={{ xs: "column", md: "row" }}
            sx={{ justifyContent: "space-between", gap: 2 }}
          >
            <Box>
              <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                <Typography sx={{ fontWeight: 800 }}>
                  {item.delegatorName}
                </Typography>
                <SwapHorizRounded color="action" />
                <Typography sx={{ fontWeight: 800 }}>
                  {item.delegateName}
                </Typography>
                <Chip size="small" label={item.scope} />
              </Stack>
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{ mt: 0.7 }}
              >
                {item.reason}
              </Typography>
              <Typography variant="caption">
                {dateTime(item.startsAtUtc)} – {dateTime(item.endsAtUtc)}
              </Typography>
            </Box>
            {item.revokedAtUtc ? (
              <Chip label="Kaldırıldı" />
            ) : (
              <Button color="error" onClick={() => revoke.mutate(item.id)}>
                Delegasyonu kaldır
              </Button>
            )}
          </Stack>
        </Paper>
      ))}
      {data.delegations.length === 0 && (
        <Alert severity="info">Henüz delegasyon tanımlanmadı.</Alert>
      )}
    </Stack>
  );
}

function AssignmentsView({ data }: { data: AccessOverview }) {
  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Kayıt</TableCell>
            <TableCell>Görev</TableCell>
            <TableCell>Atanan</TableCell>
            <TableCell>Bölüm</TableCell>
            <TableCell>Hedef</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data.activeAssignments.map((item) => (
            <TableRow key={item.id}>
              <TableCell>
                <Typography sx={{ fontWeight: 750 }}>
                  {item.aggregateType}
                </Typography>
                <Typography variant="caption">{item.aggregateId}</Typography>
              </TableCell>
              <TableCell>
                <Chip
                  size="small"
                  label={taskLabels[item.taskRole] ?? item.taskRole}
                />
              </TableCell>
              <TableCell>{item.assignedUserName}</TableCell>
              <TableCell>{item.departmentName ?? "—"}</TableCell>
              <TableCell>
                {item.dueAtUtc ? dateTime(item.dueAtUtc) : "—"}
              </TableCell>
            </TableRow>
          ))}
          {data.activeAssignments.length === 0 && (
            <TableRow>
              <TableCell colSpan={5}>
                <Alert severity="info">Aktif kayıt görevi bulunmuyor.</Alert>
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

function CreateUserDialog({ open, data, onClose }: { open: boolean; data: AccessOverview; onClose: () => void }) {
  const client=useQueryClient(); const [name,setName]=useState(''); const [email,setEmail]=useState(''); const [password,setPassword]=useState(''); const [department,setDepartment]=useState(''); const [roles,setRoles]=useState<string[]>([]); const [positions,setPositions]=useState<string[]>([])
  const mutation=useMutation({mutationFn:()=>createUser({displayName:name,email,password,departmentId:department,roles,positionIds:positions}),onSuccess:async()=>{await client.invalidateQueries({queryKey:['access-overview']});setName('');setEmail('');setPassword('');setDepartment('');setRoles([]);setPositions([]);onClose()}})
  return <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth><ModalHeader onClose={onClose}><Typography variant="h5" sx={{fontWeight:800}}>Yeni kullanıcı oluştur</Typography><Typography variant="body2" color="text.secondary">Kimlik, ilk parola, bölüm ve yetki kapsamını birlikte tanımlayın.</Typography></ModalHeader><DialogContent dividers><Stack spacing={2.2} sx={{pt:1}}>
    {mutation.isError&&<Alert severity="error">{mutation.error.message}</Alert>}<Stack direction={{xs:'column',md:'row'}} spacing={2}><TextField fullWidth label="Ad soyad" value={name} onChange={e=>setName(e.target.value)}/><TextField fullWidth label="E-posta" type="email" value={email} onChange={e=>setEmail(e.target.value)}/></Stack><TextField label="İlk parola" type="password" helperText="En az 12 karakter; büyük/küçük harf, rakam ve özel karakter kullanın." value={password} onChange={e=>setPassword(e.target.value)}/>
    <Autocomplete options={data.departments.filter(x=>x.isActive)} getOptionLabel={x=>`${x.code} · ${x.name}`} value={data.departments.find(x=>x.id===department)??null} onChange={(_,v)=>setDepartment(v?.id??'')} renderInput={p=><TextField {...p} label="Bölüm"/>}/><Autocomplete multiple options={data.roles} getOptionLabel={x=>x.name} value={data.roles.filter(x=>roles.includes(x.code))} onChange={(_,v)=>setRoles(v.map(x=>x.code))} renderInput={p=><TextField {...p} label="Sistem rolleri"/>}/><Autocomplete multiple options={data.positions.filter(x=>x.isActive)} getOptionLabel={x=>x.name} value={data.positions.filter(x=>positions.includes(x.id))} onChange={(_,v)=>setPositions(v.map(x=>x.id))} renderInput={p=><TextField {...p} label="Pozisyonlar"/>}/>
  </Stack></DialogContent><DialogActions><Button onClick={onClose}>Vazgeç</Button><Button variant="contained" disabled={mutation.isPending||!name||!email||password.length<12||!department||roles.length===0} onClick={()=>mutation.mutate()}>Kullanıcıyı oluştur</Button></DialogActions></Dialog>
}

function UserAccessDialog({
  user,
  data,
  onClose,
}: {
  user: AccessUser | null;
  data: AccessOverview;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const [departmentId, setDepartmentId] = useState<string | null>(null);
  const [roles, setRoles] = useState<string[]>([]);
  const [positions, setPositions] = useState<string[]>([]);
  const [active, setActive] = useState(true);
  useEffect(() => {
    if (user) {
      setDepartmentId(user.departmentId);
      setRoles(user.roles);
      setPositions(user.positions.map((x) => x.id));
      setActive(user.isActive);
    }
  }, [user]);
  const mutation = useMutation({
    mutationFn: () =>
      updateUserAccess(user!.id, {
        departmentId,
        roles,
        positionIds: positions,
        isActive: active,
      }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ["access-overview"] });
      onClose();
    },
  });
  return (
    <Dialog open={Boolean(user)} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Typography variant="h5" sx={{ fontWeight: 800 }}>
          Kullanıcı yetkilerini düzenle
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {user?.displayName} için sistem rolü, bölüm ve pozisyon atamalarını
          yönetin.
        </Typography>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2.2} sx={{ pt: 1 }}>
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
          <Autocomplete
            options={data.departments}
            getOptionLabel={(x) => `${x.code} · ${x.name}`}
            value={data.departments.find((x) => x.id === departmentId) ?? null}
            onChange={(_, value) => setDepartmentId(value?.id ?? null)}
            renderInput={(params) => <TextField {...params} label="Bölüm" />}
          />
          <Autocomplete
            multiple
            options={data.roles}
            getOptionLabel={(x) => x.name}
            value={data.roles.filter((x) => roles.includes(x.code))}
            onChange={(_, value) => setRoles(value.map((x) => x.code))}
            renderInput={(params) => (
              <TextField {...params} label="Sistem rolleri" />
            )}
          />
          <Autocomplete
            multiple
            options={data.positions}
            getOptionLabel={(x) => x.name}
            value={data.positions.filter((x) => positions.includes(x.id))}
            onChange={(_, value) => setPositions(value.map((x) => x.id))}
            renderInput={(params) => (
              <TextField {...params} label="Organizasyon pozisyonları" />
            )}
          />
          <Alert severity="info">
            Onaylayan rolü verilse bile kullanıcı kendi oluşturduğu kaydı
            onaylayamaz. Bu kural API tarafından uygulanır.
          </Alert>
          <FormControlLabel
            control={
              <Checkbox
                checked={active}
                onChange={(event) => setActive(event.target.checked)}
              />
            }
            label="Kullanıcı aktif"
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={mutation.isPending || !departmentId || roles.length === 0}
          onClick={() => mutation.mutate()}
        >
          Yetkileri kaydet
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function DelegationDialog({
  open,
  data,
  onClose,
}: {
  open: boolean;
  data: AccessOverview;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [scope, setScope] = useState("ALL");
  const [reason, setReason] = useState("");
  const [starts, setStarts] = useState(localDateTime(new Date()));
  const [ends, setEnds] = useState(
    localDateTime(new Date(Date.now() + 7 * 86400000)),
  );
  const mutation = useMutation({
    mutationFn: () =>
      createDelegation({
        delegatorUserId: from,
        delegateUserId: to,
        scope,
        reason,
        startsAtUtc: new Date(starts).toISOString(),
        endsAtUtc: new Date(ends).toISOString(),
      }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ["access-overview"] });
      onClose();
    },
  });
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Typography variant="h5" sx={{ fontWeight: 800 }}>
          Delegasyon tanımla
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Belirli modül veya tüm QMS görevlerini kontrollü süreyle devredin.
        </Typography>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
          <Autocomplete
            options={data.users.filter((x) => x.isActive)}
            getOptionLabel={(x) => x.displayName}
            onChange={(_, value) => setFrom(value?.id ?? "")}
            renderInput={(params) => (
              <TextField {...params} label="Görevi devreden" />
            )}
          />
          <Autocomplete
            options={data.users.filter((x) => x.isActive && x.id !== from)}
            getOptionLabel={(x) => x.displayName}
            onChange={(_, value) => setTo(value?.id ?? "")}
            renderInput={(params) => (
              <TextField {...params} label="Vekil kullanıcı" />
            )}
          />
          <Autocomplete
            options={["ALL", "M.01", "M.02"]}
            value={scope}
            onChange={(_, value) => setScope(value ?? "ALL")}
            renderInput={(params) => <TextField {...params} label="Kapsam" />}
          />
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <TextField
              fullWidth
              type="datetime-local"
              label="Başlangıç"
              value={starts}
              onChange={(e) => setStarts(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              fullWidth
              type="datetime-local"
              label="Bitiş"
              value={ends}
              onChange={(e) => setEnds(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
          <TextField
            label="Delegasyon gerekçesi"
            multiline
            minRows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          startIcon={<AddRounded />}
          disabled={!from || !to || !reason.trim() || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          Delegasyonu kaydet
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function AssignmentDialog({
  open,
  data,
  onClose,
}: {
  open: boolean;
  data: AccessOverview;
  onClose: () => void;
}) {
  const client = useQueryClient();
  const [aggregateType, setAggregateType] = useState("Deviation");
  const [aggregateId, setAggregateId] = useState("");
  const [taskRole, setTaskRole] = useState("Investigator");
  const [userId, setUserId] = useState("");
  const [dueAt, setDueAt] = useState(
    localDateTime(new Date(Date.now() + 7 * 86400000)),
  );
  const selectedUser = data.users.find((user) => user.id === userId);
  const mutation = useMutation({
    mutationFn: () =>
      createWorkflowAssignment({
        aggregateType,
        aggregateId: aggregateId.trim(),
        taskRole,
        assignedUserId: userId,
        assignedDepartmentId: selectedUser?.departmentId ?? null,
        dueAtUtc: dueAt ? new Date(dueAt).toISOString() : null,
      }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ["access-overview"] });
      setAggregateId("");
      setUserId("");
      onClose();
    },
  });
  const validId =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
      aggregateId.trim(),
    );
  const changeDetails = useQuery({ queryKey: ["change-control-assignment-options", aggregateId], queryFn: ({ signal }) => getChangeControlDetails(aggregateId.trim(), signal), enabled: aggregateType === "ChangeControl" && validId, retry: false });
  const availableTaskRoles = aggregateType === "ChangeControl" && changeDetails.data
    ? changeDetails.data.assessments.filter(item => item.status === "Pending").map(item => `Assessment:${item.id}`)
    : Object.keys(taskLabels);
  const taskLabel = (value: string) => value.startsWith("Assessment:") ? `${changeDetails.data?.assessments.find(item => `Assessment:${item.id}` === value)?.department ?? "Bölüm"} değerlendirmesi` : taskLabels[value] ?? value;
  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <ModalHeader onClose={onClose}>
        <Typography variant="h5" sx={{ fontWeight: 800 }}>
          Kayıt görevi ata
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Aktif görevi başka bir yetkiliye devredin veya kayıt zincirine
          kontrollü görev ekleyin.
        </Typography>
      </ModalHeader>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {mutation.isError && (
            <Alert severity="error">{mutation.error.message}</Alert>
          )}
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <Autocomplete
              fullWidth
              options={[
                "Deviation",
                "Capa",
                "ChangeControl",
                "Complaint",
                "InternalAudit",
                "ExternalAudit",
                "SupplierAudit",
              ]}
              value={aggregateType}
              onChange={(_, value) => { setAggregateType(value ?? "Deviation"); setTaskRole(value === "ChangeControl" ? "" : "Investigator"); }}
              getOptionLabel={(value) =>
                value === "Deviation"
                  ? "M.01 · Sapma"
                  : value === "Capa"
                    ? "M.02 · DÖF"
                    : value === "ChangeControl"
                      ? "M.03 · Değişiklik Kontrol"
                    : value === "Complaint"
                      ? "M.06 · Müşteri Şikâyeti"
                      : value === "InternalAudit"
                        ? "M.07 · İç Denetim"
                        : value === "ExternalAudit"
                          ? "M.08 · Dış Denetim"
                          : "M.09 · Tedarikçi Denetimi"
              }
              renderInput={(params) => (
                <TextField {...params} label="Kayıt türü" />
              )}
            />
            <TextField
              fullWidth
              label="Kayıt teknik kimliği (UUID)"
              value={aggregateId}
              error={Boolean(aggregateId) && !validId}
              helperText={
                aggregateId && !validId
                  ? "Geçerli kayıt UUID değeri girin."
                  : "Kayıt detay adresindeki open değeridir."
              }
              onChange={(event) => setAggregateId(event.target.value)}
            />
          </Stack>
          <Autocomplete
            options={availableTaskRoles}
            value={taskRole}
            getOptionLabel={taskLabel}
            onChange={(_, value) => setTaskRole(value ?? "")}
            renderInput={(params) => (
              <TextField {...params} label="Kayıt görevi" />
            )}
          />
          <Autocomplete
            options={data.users.filter((user) => user.isActive)}
            getOptionLabel={(user) =>
              `${user.displayName} · ${user.departmentName ?? "Bölümsüz"}`
            }
            value={selectedUser ?? null}
            onChange={(_, value) => setUserId(value?.id ?? "")}
            renderInput={(params) => (
              <TextField {...params} label="Atanacak kullanıcı" />
            )}
          />
          <TextField
            type="datetime-local"
            label="Görev hedef tarihi"
            value={dueAt}
            onChange={(event) => setDueAt(event.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <Alert severity="warning">
            Aynı görevdeki önceki aktif atama iptal edilir. Kaydı oluşturan
            kullanıcı Onaylayan veya Mesul Müdür olarak atanamaz.
          </Alert>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Vazgeç</Button>
        <Button
          variant="contained"
          disabled={!validId || !taskRole || !userId || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          Görevi ata
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function initials(value: string) {
  return value
    .split(" ")
    .map((x) => x[0])
    .join("")
    .slice(0, 2)
    .toLocaleUpperCase("tr-TR");
}
function dateTime(value: string) {
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
function localDateTime(value: Date) {
  const local = new Date(value.getTime() - value.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}
