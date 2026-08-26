import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router'
import {
  Avatar,
  Badge,
  Box,
  Drawer,
  Button,
  IconButton,
  InputAdornment,
  Stack,
  Menu,
  MenuItem,
  ListItemText,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import {
  CloseRounded,
  HubRounded,
  MenuRounded,
  NotificationsNoneRounded,
  SearchRounded,
} from '@mui/icons-material'
import { GlobalNetworkLoader } from '../components/GlobalNetworkLoader'
import { administrationItem, allNavigationItems, dashboardItem, navigationGroups } from '../navigation'
import { Permissions, useAuth } from '../security/AuthContext'
import { getNotifications, markAllNotificationsRead, markNotificationRead, type NotificationList } from '../api/notifications'

export function AppLayout() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [profileAnchor, setProfileAnchor] = useState<HTMLElement | null>(null)
  const [notificationAnchor, setNotificationAnchor] = useState<HTMLElement | null>(null)
  const [notifications, setNotifications] = useState<NotificationList>({ unreadCount: 0, items: [] })
  const location = useLocation()
  const navigate = useNavigate()
  const { user, can } = useAuth()
  const { logout } = useAuth()
  const currentItem = allNavigationItems.find((item) =>
    item.path === '/' ? location.pathname === '/' : location.pathname.startsWith(item.path))

  const refreshNotifications = () => getNotifications().then(setNotifications).catch(() => undefined)
  useEffect(() => {
    refreshNotifications()
    const timer = window.setInterval(refreshNotifications, 30_000)
    return () => window.clearInterval(timer)
  }, [user.profile])

  const openNotification = async (item: NotificationList['items'][number]) => {
    if (!item.readAtUtc) await markNotificationRead(item.id)
    setNotificationAnchor(null)
    await refreshNotifications()
    if (item.link) navigate(item.link)
  }

  return (
    <Box className="application-shell">
      <Box component="aside" className="desktop-sidebar">
        <SidebarContent onNavigate={() => undefined} canManageAccess={can(Permissions.administrationManage)} />
      </Box>

      <Drawer
        open={mobileMenuOpen}
        onClose={() => setMobileMenuOpen(false)}
        className="mobile-sidebar"
        slotProps={{ paper: { className: 'mobile-sidebar-paper' } }}
      >
        <IconButton
          aria-label="Menüyü kapat"
          className="mobile-menu-close"
          onClick={() => setMobileMenuOpen(false)}
        >
          <CloseRounded />
        </IconButton>
        <SidebarContent onNavigate={() => setMobileMenuOpen(false)} canManageAccess={can(Permissions.administrationManage)} />
      </Drawer>

      <Box className="app-main-column">
        <GlobalNetworkLoader />
        <Box component="header" className="app-topbar">
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', minWidth: 0 }}>
            <IconButton
              aria-label="Modül menüsünü aç"
              className="mobile-menu-button"
              onClick={() => setMobileMenuOpen(true)}
            >
              <MenuRounded />
            </IconButton>
            <Box sx={{ minWidth: 0 }}>
              <Typography variant="caption" color="text.secondary" className="topbar-context">
                Kalite Yönetim Sistemi
              </Typography>
              <Typography className="topbar-title" noWrap>{currentItem?.title ?? 'QMS'}</Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
            <TextField
              className="global-search"
              size="small"
              placeholder="Kayıt veya modül ara"
              slotProps={{
                input: {
                  startAdornment: <InputAdornment position="start"><SearchRounded fontSize="small" /></InputAdornment>,
                },
              }}
            />
            <Tooltip title="Bildirimler">
              <IconButton aria-label={`Bildirimler${notifications.unreadCount ? ` (${notifications.unreadCount} okunmamış)` : ''}`} onClick={(event) => setNotificationAnchor(event.currentTarget)}>
                <Badge color="error" badgeContent={notifications.unreadCount} max={99}><NotificationsNoneRounded /></Badge>
              </IconButton>
            </Tooltip>
            <Menu anchorEl={notificationAnchor} open={Boolean(notificationAnchor)} onClose={() => setNotificationAnchor(null)} slotProps={{ paper: { sx: { width: 380, maxWidth: 'calc(100vw - 24px)', maxHeight: 520 } } }}>
              <Box sx={{ px: 2, py: 1, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Typography sx={{ fontWeight: 800 }}>Bildirimler</Typography>
                {notifications.unreadCount > 0 && <Button size="small" onClick={async () => { await markAllNotificationsRead(); await refreshNotifications() }}>Tümünü okundu yap</Button>}
              </Box>
              {notifications.items.length === 0 && <MenuItem disabled>Henüz bildirim yok.</MenuItem>}
              {notifications.items.map((item) => (
                <MenuItem key={item.id} onClick={() => openNotification(item)} sx={{ alignItems: 'flex-start', whiteSpace: 'normal', bgcolor: item.readAtUtc ? undefined : 'action.hover' }}>
                  <ListItemText primary={`${item.moduleCode} · ${item.title}`} secondary={<>{item.message}<br />{new Date(item.createdAtUtc).toLocaleString('tr-TR')}</>} slotProps={{ primary: { sx: { fontWeight: item.readAtUtc ? 600 : 800 } } }} />
                </MenuItem>
              ))}
            </Menu>
            <Button className="user-profile-button" onClick={(event) => setProfileAnchor(event.currentTarget)}>
              <Avatar className="user-avatar">{initials(user.displayName)}</Avatar>
              <Box className="user-profile-copy">
                <Typography className="user-profile-name">{user.displayName}</Typography>
                <Typography className="user-profile-role">{roleLabel(user.roles[0])}</Typography>
              </Box>
            </Button>
            <Menu anchorEl={profileAnchor} open={Boolean(profileAnchor)} onClose={() => setProfileAnchor(null)}>
              <MenuItem onClick={async () => { setProfileAnchor(null); await logout() }}><ListItemText primary="Oturumu kapat" /></MenuItem>
            </Menu>
          </Stack>
        </Box>

        <Box component="main" className="page-content">
          <Box className="page-content-card">
            <Outlet />
          </Box>
        </Box>
      </Box>
    </Box>
  )
}

function initials(name: string) {
  return name.split(' ').map((part) => part[0]).join('').slice(0, 2).toLocaleUpperCase('tr-TR')
}

function roleLabel(role?: string) {
  const labels: Record<string, string> = {
    Administrator: 'Sistem yöneticisi', QualityAssurance: 'Kalite Güvence', Approver: 'Onaylayan',
    DeviationReporter: 'Sapma bildiren', Investigator: 'Araştırmacı', ActionOwner: 'Aksiyon sorumlusu', QualityViewer: 'İzleyici',
    QualifiedPerson: 'Mesul Müdür', DepartmentManager: 'Bölüm yöneticisi', RegulatoryAffairs: 'Ruhsat sorumlusu',
    DocumentController: 'Doküman kontrol', TrainingCoordinator: 'Eğitim koordinatörü',
  }
  return role ? labels[role] ?? role : 'Rol atanmadı'
}

function SidebarContent({ onNavigate, canManageAccess }: { onNavigate: () => void; canManageAccess: boolean }) {
  return (
    <Box className="sidebar-content">
      <Stack direction="row" spacing={1.4} className="sidebar-brand">
        <Box className="sidebar-brand-mark"><HubRounded /></Box>
        <Box>
          <Typography className="sidebar-brand-name">QMS</Typography>
          <Typography className="sidebar-brand-subtitle">Kalite operasyon merkezi</Typography>
        </Box>
      </Stack>

      <Box component="nav" aria-label="Ana modül menüsü" className="sidebar-navigation">
        <NavigationLink item={dashboardItem} onNavigate={onNavigate} />
        {navigationGroups.map((group) => (
          <Box className="sidebar-group" key={group.title}>
            <Typography className="sidebar-section-label">{group.title}</Typography>
            {group.items.map((item) => (
              <NavigationLink item={item} onNavigate={onNavigate} key={item.path} />
            ))}
          </Box>
        ))}
        {canManageAccess && <Box className="sidebar-group"><Typography className="sidebar-section-label">Sistem</Typography><NavigationLink item={administrationItem} onNavigate={onNavigate} /></Box>}
      </Box>

      <Box className="sidebar-footer">
        <Box className="sidebar-status-dot" />
        <Box>
          <Typography variant="caption" sx={{ color: '#d0d5dd', fontWeight: 700 }}>Sistem çevrimiçi</Typography>
          <Typography variant="caption" sx={{ color: '#667085', display: 'block' }}>Tek kurum ortamı</Typography>
        </Box>
      </Box>
    </Box>
  )
}

function NavigationLink({ item, onNavigate }: {
  item: typeof dashboardItem
  onNavigate: () => void
}) {
  const Icon = item.icon
  return (
    <NavLink
      end={item.path === '/'}
      to={item.path}
      className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
      onClick={onNavigate}
    >
      <Icon className="sidebar-link-icon" />
      <span className="sidebar-link-title">{item.title}</span>
      {item.code && <span className="sidebar-link-code">{item.code}</span>}
    </NavLink>
  )
}
