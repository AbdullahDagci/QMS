import type { SvgIconComponent } from '@mui/icons-material'
import {
  AnalyticsRounded,
  AssignmentTurnedInRounded,
  BusinessRounded,
  ChangeCircleRounded,
  ChatBubbleOutlineRounded,
  DashboardRounded,
  DescriptionRounded,
  FactCheckRounded,
  HealthAndSafetyRounded,
  LocalShippingRounded,
  MenuBookRounded,
  PaletteRounded,
  SchoolRounded,
  ScienceRounded,
  TaskAltRounded,
  TravelExploreRounded,
  WarningAmberRounded,
  AdminPanelSettingsRounded,
} from '@mui/icons-material'

export interface NavigationItem {
  code?: string
  title: string
  path: string
  icon: SvgIconComponent
  active?: boolean
}

export interface NavigationGroup {
  title: string
  items: NavigationItem[]
}

export const dashboardItem: NavigationItem = {
  title: 'Dashboard', path: '/', icon: DashboardRounded, active: true,
}

export const administrationItem: NavigationItem = { title: 'Kullanıcı ve Yetkiler', path: '/administration/access', icon: AdminPanelSettingsRounded, active: true }

export const navigationGroups: NavigationGroup[] = [
  {
    title: 'Kalite süreçleri',
    items: [
      { code: 'M.01', title: 'Sapma Yönetimi', path: '/modules/deviations', icon: WarningAmberRounded, active: true },
      { code: 'M.02', title: 'DÖF Yönetimi', path: '/modules/m02', icon: TaskAltRounded, active: true },
      { code: 'M.03', title: 'Değişiklik Kontrol', path: '/modules/m03', icon: ChangeCircleRounded, active: true },
    ],
  },
  {
    title: 'Doküman ve yetkinlik',
    items: [
      { code: 'M.04', title: 'Doküman Yönetimi', path: '/modules/m04', icon: DescriptionRounded, active: true },
      { code: 'M.05', title: 'Eğitim Yönetimi', path: '/modules/m05', icon: SchoolRounded, active: true },
      { code: 'M.12', title: 'MBR Yönetimi', path: '/modules/m12', icon: MenuBookRounded },
      { code: 'M.13', title: 'Artwork Yönetimi', path: '/modules/m13', icon: PaletteRounded },
    ],
  },
  {
    title: 'Denetim ve dış taraflar',
    items: [
      { code: 'M.06', title: 'Müşteri Şikayetleri', path: '/modules/m06', icon: ChatBubbleOutlineRounded, active: true },
      { code: 'M.07', title: 'İç Denetimler', path: '/modules/m07', icon: FactCheckRounded, active: true },
      { code: 'M.08', title: 'Dış Denetimler', path: '/modules/m08', icon: TravelExploreRounded, active: true },
      { code: 'M.09', title: 'Tedarikçi Denetimi', path: '/modules/m09', icon: LocalShippingRounded },
      { code: 'M.16', title: 'Tedarikçi Değerlendirme', path: '/modules/m16', icon: BusinessRounded },
    ],
  },
  {
    title: 'Risk, laboratuvar ve güvenlik',
    items: [
      { code: 'M.10', title: 'İş Takip ve Aksiyon', path: '/modules/m10', icon: AssignmentTurnedInRounded },
      { code: 'M.11', title: 'Risk Yönetimi (FMEA)', path: '/modules/m11', icon: AnalyticsRounded },
      { code: 'M.14', title: 'Limit Dışı Durum', path: '/modules/m14', icon: ScienceRounded },
      { code: 'M.15', title: 'Farmakovijilans', path: '/modules/m15', icon: HealthAndSafetyRounded },
    ],
  },
]

export const allNavigationItems = [dashboardItem, ...navigationGroups.flatMap((group) => group.items), administrationItem]
