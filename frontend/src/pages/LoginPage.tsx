import { useEffect, useMemo, useState } from "react";
import {
  AccountTreeRounded,
  ArrowForwardRounded,
  BadgeRounded,
  HubRounded,
  LockRounded,
  MailOutlineRounded,
  ManageAccountsRounded,
  PasswordRounded,
  ScienceRounded,
  SearchRounded,
  ShieldRounded,
  TaskAltRounded,
  VerifiedUserRounded,
} from "@mui/icons-material";
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import {
  getQuickProfiles,
  login,
  quickLogin,
  type UserProfileOption,
} from "../api/security";
import { useAuth } from "../security/AuthContext";

const roleNames: Record<string, string> = {
  Administrator: "Sistem Yöneticisi",
  QualityAssurance: "Kalite Güvence",
  Approver: "Onaylayan",
  DeviationReporter: "Sapma Bildiren",
  Investigator: "Araştırmacı",
  ActionOwner: "Aksiyon Sorumlusu",
  QualityViewer: "İzleyici",
  QualifiedPerson: "Mesul Müdür",
  DepartmentManager: "Bölüm Yöneticisi",
  RegulatoryAffairs: "Ruhsatlandırma",
  DocumentController: "Doküman Kontrol",
  TrainingCoordinator: "Eğitim Koordinatörü",
  Learner: "Eğitim Katılımcısı",
  Trainer: "Eğitmen",
};

const initials = (name: string) =>
  name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toLocaleUpperCase("tr-TR");

export function LoginPage() {
  const { refresh } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [profiles, setProfiles] = useState<UserProfileOption[]>([]);
  const [profilesLoading, setProfilesLoading] = useState(true);
  const [mode, setMode] = useState<"quick" | "credentials">("quick");
  const [query, setQuery] = useState("");
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    getQuickProfiles()
      .then(setProfiles)
      .catch(() => setProfiles([]))
      .finally(() => setProfilesLoading(false));
  }, []);

  const filteredProfiles = useMemo(() => {
    const needle = query.trim().toLocaleLowerCase("tr-TR");
    if (!needle) return profiles;
    return profiles.filter((profile) =>
      [
        profile.displayName,
        profile.departmentName,
        ...profile.roles.map((role) => roleNames[role] ?? role),
      ]
        .join(" ")
        .toLocaleLowerCase("tr-TR")
        .includes(needle),
    );
  }, [profiles, query]);

  const finish = async (action: Promise<unknown>, key: string) => {
    setBusy(key);
    setError("");
    try {
      await action;
      await refresh();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Giriş başarısız.");
    } finally {
      setBusy("");
    }
  };

  const credentialLogin = () => finish(login(email, password), "credentials");

  return (
    <Box className="login-stage">
      <Box className="login-atmosphere" />
      <Box className="login-shell">
        <Box component="section" className="login-story">
          <Stack direction="row" spacing={1.4} className="login-story-brand">
            <Box className="login-brand-mark">
              <HubRounded />
            </Box>
            <Box>
              <Typography className="login-eyebrow">QUALISPHERE</Typography>
              <Typography className="login-product-line">
                Elektronik Kalite Yönetim Sistemi
              </Typography>
            </Box>
          </Stack>

          <Box className="login-story-copy">
            <Chip
              icon={<VerifiedUserRounded />}
              label="Tek kurum · kontrollü erişim"
              className="login-trust-chip"
            />
            <Typography component="h1">
              Kalite kararları
              <span> tek zincirde.</span>
            </Typography>
            <Typography>
              Sapmadan DÖF’e, dokümandan eğitime; her görev doğru kullanıcıya,
              her karar değiştirilemez geçmişe bağlanır.
            </Typography>
          </Box>

          <Box
            className="login-process-chain"
            aria-label="Bağlı kalite zinciri"
          >
            {[
              ["M.01", "Sapma"],
              ["M.02", "DÖF"],
              ["M.03", "Değişiklik"],
              ["M.04", "Doküman"],
              ["M.05+", "Bağlı süreçler"],
            ].map(([code, label], index) => (
              <Box className="login-process-step" key={code}>
                <Box>
                  <strong>{code}</strong>
                  <span>{label}</span>
                </Box>
                {index < 4 && <ArrowForwardRounded />}
              </Box>
            ))}
          </Box>

          <Box className="login-assurance-grid">
            <Box>
              <ShieldRounded />
              <span>
                <strong>Rol bazlı</strong>
                Yetki ve görev ayrılığı
              </span>
            </Box>
            <Box>
              <TaskAltRounded />
              <span>
                <strong>Denetlenebilir</strong>
                İmza ve işlem geçmişi
              </span>
            </Box>
            <Box>
              <AccountTreeRounded />
              <span>
                <strong>Bağlı kayıtlar</strong>
                Uçtan uca izlenebilirlik
              </span>
            </Box>
          </Box>
        </Box>

        <Paper component="section" className="login-panel" elevation={0}>
          <Box className="login-panel-head">
            <Box>
              <Typography className="login-panel-kicker">
                GÜVENLİ ÇALIŞMA ALANI
              </Typography>
              <Typography variant="h3">Doğru rolle giriş yapın</Typography>
              <Typography color="text.secondary">
                Yetki matrisini test etmek için bir profil seçin veya kurumsal
                hesabınızla giriş yapın.
              </Typography>
            </Box>
            <Box className="login-security-seal" title="Güvenli oturum">
              <LockRounded />
            </Box>
          </Box>

          <ToggleButtonGroup
            exclusive
            fullWidth
            value={mode}
            onChange={(_, next) => next && setMode(next)}
            className="login-mode-switch"
            aria-label="Giriş yöntemi"
          >
            <ToggleButton value="quick">
              <ScienceRounded /> Hızlı test girişleri
            </ToggleButton>
            <ToggleButton value="credentials">
              <ManageAccountsRounded /> E-posta ile giriş
            </ToggleButton>
          </ToggleButtonGroup>

          {error && <Alert severity="error">{error}</Alert>}

          {mode === "quick" ? (
            <Box className="quick-login-zone">
              <Stack
                direction={{ xs: "column", sm: "row" }}
                className="quick-login-toolbar"
              >
                <TextField
                  size="small"
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Kullanıcı, bölüm veya rol ara"
                  aria-label="Hızlı giriş profillerinde ara"
                  slotProps={{
                    htmlInput: {
                      "aria-label": "Hızlı giriş profillerinde ara",
                    },
                    input: {
                      startAdornment: (
                        <InputAdornment position="start">
                          <SearchRounded />
                        </InputAdornment>
                      ),
                    },
                  }}
                />
                <Chip
                  icon={<BadgeRounded />}
                  label={`${filteredProfiles.length} test profili`}
                  variant="outlined"
                />
              </Stack>

              {profilesLoading ? (
                <Box className="quick-login-loading">
                  <CircularProgress size={26} />
                  <Typography color="text.secondary">
                    Test kullanıcıları hazırlanıyor…
                  </Typography>
                </Box>
              ) : (
                <Box className="quick-profile-grid">
                  {filteredProfiles.map((profile) => (
                    <Button
                      key={profile.key}
                      className="quick-profile-card"
                      disabled={Boolean(busy)}
                      onClick={() =>
                        finish(quickLogin(profile.key), profile.key)
                      }
                    >
                      <Avatar>{initials(profile.displayName)}</Avatar>
                      <Box className="quick-profile-copy">
                        <strong>{profile.displayName}</strong>
                        <span>{profile.departmentName}</span>
                        <Box className="quick-profile-roles">
                          {profile.roles.map((role) => (
                            <small key={role}>{roleNames[role] ?? role}</small>
                          ))}
                        </Box>
                      </Box>
                      <Box className="quick-profile-entry">
                        {busy === profile.key ? (
                          <CircularProgress size={17} />
                        ) : (
                          <ArrowForwardRounded />
                        )}
                      </Box>
                    </Button>
                  ))}
                  {!filteredProfiles.length && (
                    <Box className="quick-login-empty">
                      <SearchRounded />
                      <Typography>
                        Aramayla eşleşen test profili yok.
                      </Typography>
                    </Box>
                  )}
                </Box>
              )}
            </Box>
          ) : (
            <Stack spacing={2} className="credential-login-form">
              <TextField
                label="E-posta"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                autoComplete="username"
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <MailOutlineRounded />
                      </InputAdornment>
                    ),
                  },
                }}
              />
              <TextField
                label="Parola"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="current-password"
                onKeyDown={(event) => {
                  if (event.key === "Enter") credentialLogin();
                }}
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <PasswordRounded />
                      </InputAdornment>
                    ),
                  },
                }}
              />
              <Button
                size="large"
                variant="contained"
                endIcon={
                  busy === "credentials" ? (
                    <CircularProgress size={18} color="inherit" />
                  ) : (
                    <ArrowForwardRounded />
                  )
                }
                disabled={!email || !password || Boolean(busy)}
                onClick={credentialLogin}
              >
                Güvenli giriş yap
              </Button>
            </Stack>
          )}

          <Box className="login-panel-foot">
            <LockRounded />
            <Typography variant="caption">
              Oturumlar süreli token ile korunur. Hızlı girişler yalnızca
              geliştirme ortamında görünür.
            </Typography>
          </Box>
        </Paper>
      </Box>
    </Box>
  );
}
