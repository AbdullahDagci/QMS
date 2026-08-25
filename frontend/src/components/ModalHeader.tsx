import type { ReactNode } from 'react'
import { DialogTitle, IconButton, Tooltip } from '@mui/material'
import { CloseRounded } from '@mui/icons-material'

export function ModalHeader({
  children,
  onClose,
  closeDisabled = false,
  closeLabel = 'Pencereyi kapat',
}: {
  children: ReactNode
  onClose: () => void
  closeDisabled?: boolean
  closeLabel?: string
}) {
  return (
    <DialogTitle className="modal-sticky-header modal-header-accent">
      <div className="modal-header-content">{children}</div>
      <Tooltip title={closeLabel}>
        <span className="modal-close-button-wrapper">
          <IconButton
            aria-label={closeLabel}
            className="modal-close-button"
            disabled={closeDisabled}
            onClick={onClose}
          >
            <CloseRounded />
          </IconButton>
        </span>
      </Tooltip>
    </DialogTitle>
  )
}
