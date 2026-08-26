import { useEffect, useState } from 'react'
import { Alert, Avatar, Box, Button, CircularProgress, Paper, Stack, TextField, Typography } from '@mui/material'
import { LockRounded, ScienceRounded } from '@mui/icons-material'
import { getQuickProfiles, login, quickLogin, type UserProfileOption } from '../api/security'
import { useAuth } from '../security/AuthContext'

const roleNames: Record<string,string> = { Administrator:'Sistem Yöneticisi', QualityAssurance:'Kalite Güvence', Approver:'Onaylayan', DeviationReporter:'Sapma Bildiren', Investigator:'Araştırmacı', ActionOwner:'Aksiyon Sorumlusu', QualityViewer:'İzleyici', QualifiedPerson:'Mesul Müdür', DepartmentManager:'Bölüm Yöneticisi', RegulatoryAffairs:'Ruhsatlandırma', DocumentController:'Doküman Kontrol', TrainingCoordinator:'Eğitim Koordinatörü' }
export function LoginPage() {
  const { refresh } = useAuth(); const [email,setEmail]=useState(''); const [password,setPassword]=useState(''); const [profiles,setProfiles]=useState<UserProfileOption[]>([]); const [busy,setBusy]=useState(''); const [error,setError]=useState('')
  useEffect(()=>{ getQuickProfiles().then(setProfiles) },[])
  const finish=async(action:Promise<unknown>,key:string)=>{setBusy(key);setError('');try{await action;await refresh()}catch(e){setError(e instanceof Error?e.message:'Giriş başarısız.')}finally{setBusy('')}}
  return <Box className="login-stage"><Box className="login-atmosphere"/><Paper className="login-panel" elevation={0}>
    <Box className="login-brand"><Box className="login-brand-mark">Q</Box><Box><Typography className="login-eyebrow">QUALISPHERE</Typography><Typography variant="h3">Kalite operasyonlarına güvenli giriş</Typography></Box></Box>
    <Typography color="text.secondary">Yetkinize atanmış kayıtları, kararları ve imzaları tek çalışma alanında yönetin.</Typography>
    {error&&<Alert severity="error">{error}</Alert>}
    <Stack spacing={2}><TextField label="E-posta" value={email} onChange={e=>setEmail(e.target.value)} autoComplete="username"/><TextField label="Parola" type="password" value={password} onChange={e=>setPassword(e.target.value)} autoComplete="current-password" onKeyDown={e=>{if(e.key==='Enter')finish(login(email,password),'login')}}/><Button size="large" variant="contained" startIcon={busy==='login'?<CircularProgress size={18}/>:<LockRounded/>} disabled={!email||!password||Boolean(busy)} onClick={()=>finish(login(email,password),'login')}>Giriş yap</Button></Stack>
    {profiles.length>0&&<Box className="quick-login-zone"><Stack direction="row" spacing={1} sx={{alignItems:'center'}}><ScienceRounded/><Box><Typography sx={{fontWeight:850}}>Hızlı test girişleri</Typography><Typography variant="caption" color="text.secondary">Geliştirme ortamına özeldir</Typography></Box></Stack><Box className="quick-profile-grid">{profiles.map(p=><Button key={p.key} className="quick-profile-card" disabled={Boolean(busy)} onClick={()=>finish(quickLogin(p.key),p.key)}><Avatar>{p.displayName.split(' ').map(x=>x[0]).join('').slice(0,2)}</Avatar><Box><strong>{p.displayName}</strong><span>{p.roles.map(r=>roleNames[r]??r).join(' · ')}</span></Box>{busy===p.key&&<CircularProgress size={16}/>}</Button>)}</Box></Box>}
  </Paper><Box className="login-side-note"><span>GMP</span><Typography>Her kararın sahibi,<br/>her değişikliğin izi var.</Typography></Box></Box>
}
