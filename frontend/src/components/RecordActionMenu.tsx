import { useState, type MouseEvent, type ReactNode } from "react";
import {
  ArrowDropDownRounded,
  MoreHorizRounded,
  OpenInNewRounded,
} from "@mui/icons-material";
import {
  Button,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
} from "@mui/material";

export interface RecordActionItem {
  label: string;
  description?: string;
  icon?: ReactNode;
  disabled?: boolean;
  onClick: () => void;
}

export function RecordActionMenu({
  onOpen,
  items = [],
}: {
  onOpen: () => void;
  items?: RecordActionItem[];
}) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const close = () => setAnchor(null);
  const run = (action: () => void) => {
    close();
    action();
  };

  return (
    <>
      <Button
        className="row-action-trigger"
        variant="outlined"
        startIcon={<MoreHorizRounded />}
        endIcon={<ArrowDropDownRounded />}
        onClick={(event: MouseEvent<HTMLButtonElement>) => {
          event.stopPropagation();
          setAnchor(event.currentTarget);
        }}
      >
        İşlemler
      </Button>
      <Menu
        anchorEl={anchor}
        open={Boolean(anchor)}
        onClose={close}
        className="row-action-menu"
        onClick={(event) => event.stopPropagation()}
      >
        <MenuItem onClick={() => run(onOpen)}>
          <ListItemIcon>
            <OpenInNewRounded fontSize="small" />
          </ListItemIcon>
          <ListItemText
            primary="Aç"
            secondary="Kayıt ayrıntılarını görüntüle"
          />
        </MenuItem>
        {items.map((item) => (
          <MenuItem
            key={item.label}
            disabled={item.disabled}
            onClick={() => run(item.onClick)}
          >
            {item.icon && <ListItemIcon>{item.icon}</ListItemIcon>}
            <ListItemText primary={item.label} secondary={item.description} />
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}
