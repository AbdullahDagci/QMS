import { LinearProgress } from '@mui/material'
import { useIsFetching, useIsMutating } from '@tanstack/react-query'

export function GlobalNetworkLoader() {
  const activeRequests = useIsFetching() + useIsMutating()

  if (activeRequests === 0) return null

  return (
    <LinearProgress
      aria-label="Uygulama verileri yükleniyor"
      className="global-network-loader"
      color="secondary"
    />
  )
}
