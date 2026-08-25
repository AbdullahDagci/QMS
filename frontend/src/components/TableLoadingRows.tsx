import { Skeleton, TableCell, TableRow } from '@mui/material'

export function TableLoadingRows({ columns, rows = 6 }: { columns: number; rows?: number }) {
  return Array.from({ length: rows }, (_, rowIndex) => (
    <TableRow aria-label={rowIndex === 0 ? 'Liste yükleniyor' : undefined} key={rowIndex}>
      {Array.from({ length: columns }, (_, columnIndex) => (
        <TableCell key={columnIndex}>
          <Skeleton
            animation="wave"
            height={22}
            width={columnIndex === 1 ? '82%' : columnIndex === columns - 1 ? '58%' : '68%'}
          />
          {columnIndex === 0 && <Skeleton animation="wave" height={14} width="45%" />}
        </TableCell>
      ))}
    </TableRow>
  ))
}
