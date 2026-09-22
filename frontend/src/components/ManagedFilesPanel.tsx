import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AttachFileRounded,
  CloudUploadRounded,
  DownloadRounded,
  VerifiedRounded,
} from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import {
  downloadManagedFile,
  listManagedFiles,
  uploadManagedFile,
  type ManagedFile,
} from "../api/managedFiles";

export function ManagedFilesPanel({ aggregateType, aggregateId }: {
  aggregateType: string;
  aggregateId: string;
}) {
  const client = useQueryClient();
  const [category, setCategory] = useState("Kanıt");
  const [selected, setSelected] = useState<File | null>(null);
  const files = useQuery({
    queryKey: ["managed-files", aggregateType, aggregateId],
    queryFn: ({ signal }) => listManagedFiles(aggregateType, aggregateId, signal),
    retry: false,
  });
  const upload = useMutation({
    mutationFn: () => uploadManagedFile(aggregateType, aggregateId, category, selected!),
    onSuccess: async () => {
      setSelected(null);
      await client.invalidateQueries({ queryKey: ["managed-files", aggregateType, aggregateId] });
    },
  });
  const download = useMutation({ mutationFn: downloadManagedFile });

  return (
    <Paper variant="outlined" sx={{ mt: 2, p: 2 }}>
      <Stack direction={{ xs: "column", md: "row" }} sx={{ justifyContent: "space-between", gap: 2 }}>
        <Stack direction="row" spacing={1.2} sx={{ alignItems: "center" }}>
          <Box className="section-heading-icon tone-indigo"><AttachFileRounded /></Box>
          <Box>
            <Typography sx={{ fontWeight: 800 }}>Kanıt ve ek dosyalar</Typography>
            <Typography variant="caption" color="text.secondary">
              İzinli türler virüs taramasından geçer; SHA-256 ve HMAC bütünlüğüyle saklanır.
            </Typography>
          </Box>
          {files.isFetching && <CircularProgress size={20} />}
        </Stack>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
          <TextField size="small" label="Kategori" value={category}
            slotProps={{ htmlInput: { maxLength: 128 } }}
            onChange={(event) => setCategory(event.target.value)} />
          <Button component="label" variant="outlined" startIcon={<CloudUploadRounded />}>
            {selected?.name ?? "Dosya seç"}
            <input hidden type="file" accept=".pdf,.docx,.xlsx,.png,.jpg,.jpeg,.txt,.csv"
              onChange={(event) => setSelected(event.target.files?.[0] ?? null)} />
          </Button>
          <Button variant="contained" disabled={!selected || !category.trim() || upload.isPending}
            onClick={() => upload.mutate()}>
            {upload.isPending ? "Taranıyor" : "Yükle"}
          </Button>
        </Stack>
      </Stack>
      {(files.isError || upload.isError || download.isError) && (
        <Alert severity="error" sx={{ mt: 1.5 }}>
          {(upload.error ?? download.error ?? files.error)?.message ?? "Dosya işlemi başarısız."}
        </Alert>
      )}
      {files.isSuccess && files.data.length === 0 && (
        <Alert severity="info" sx={{ mt: 1.5 }}>Bu kayda bağlı kanıt dosyası bulunmuyor.</Alert>
      )}
      <Stack spacing={1} sx={{ mt: files.data?.length ? 1.5 : 0 }}>
        {files.data?.map((file) => <FileRow key={file.id} file={file}
          onDownload={() => download.mutate(file)} />)}
      </Stack>
    </Paper>
  );
}

function FileRow({ file, onDownload }: { file: ManagedFile; onDownload: () => void }) {
  return (
    <Paper variant="outlined" sx={{ p: 1.25 }}>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ justifyContent: "space-between", gap: 1 }}>
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="body2" sx={{ fontWeight: 750, overflowWrap: "anywhere" }}>
            {file.fileName}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {file.category} · {formatBytes(file.size)} · {file.uploadedBy} · {formatDate(file.uploadedAtUtc)}
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
            SHA-256 {file.sha256.slice(0, 20)}… · Saklama: {formatDate(file.retainUntilUtc)}
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
          <Chip size="small" color="success" variant="outlined" icon={<VerifiedRounded />} label="Bütünlük kayıtlı" />
          <Button size="small" startIcon={<DownloadRounded />} onClick={onDownload}>İndir</Button>
        </Stack>
      </Stack>
    </Paper>
  );
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / 1024 / 1024).toFixed(1)} MB`;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" })
    .format(new Date(value));
}
