#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
  echo "Usage: BACKUP_DIRECTORY=/absolute/path $0 <backup-name>" >&2
  exit 2
fi
if [ -z "${BACKUP_DIRECTORY:-}" ] || [ "${BACKUP_DIRECTORY#/}" = "$BACKUP_DIRECTORY" ]; then
  echo "BACKUP_DIRECTORY must be an absolute path." >&2
  exit 2
fi

backup_name=$1
case "$backup_name" in *[!A-Za-z0-9._-]*|'') echo "Invalid backup name." >&2; exit 2;; esac
backup_path="${BACKUP_DIRECTORY%/}/$backup_name"
if [ -e "$backup_path" ]; then
  echo "Backup target already exists: $backup_path" >&2
  exit 3
fi
mkdir -p "$backup_path"

compose_file="deploy/docker-compose.yml"
resume_services=false
cleanup() {
  if [ "$resume_services" = true ]; then
    docker compose -f "$compose_file" start qms-api qms-worker qms-web >/dev/null
  fi
}
trap cleanup EXIT INT TERM

docker compose -f "$compose_file" stop qms-web qms-api qms-worker >/dev/null
resume_services=true
docker compose -f "$compose_file" exec -T qms-postgres sh -c \
  'pg_dump --format=custom --no-owner --no-privileges --username="$POSTGRES_USER" "$POSTGRES_DB"' \
  > "$backup_path/database.dump"
docker run --rm -v qms_qms-files:/source:ro -v "$backup_path:/backup" alpine:3.22 \
  sh -c 'cd /source && tar -czf /backup/qms-files.tar.gz .'
sha256sum "$backup_path/database.dump" "$backup_path/qms-files.tar.gz" \
  > "$backup_path/SHA256SUMS"
date -u +'%Y-%m-%dT%H:%M:%SZ' > "$backup_path/created-at-utc.txt"

echo "Consistent QMS backup created: $backup_path"
