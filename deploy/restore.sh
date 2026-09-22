#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
  echo "Usage: QMS_RESTORE_CONFIRM=RESTORE_QMS $0 /absolute/path/to/backup" >&2
  exit 2
fi
if [ "${QMS_RESTORE_CONFIRM:-}" != "RESTORE_QMS" ]; then
  echo "Restore is destructive. Set QMS_RESTORE_CONFIRM=RESTORE_QMS explicitly." >&2
  exit 3
fi
backup_path=$1
if [ "${backup_path#/}" = "$backup_path" ] || [ ! -f "$backup_path/database.dump" ] \
   || [ ! -f "$backup_path/qms-files.tar.gz" ] || [ ! -f "$backup_path/SHA256SUMS" ]; then
  echo "A valid absolute backup directory is required." >&2
  exit 2
fi
(cd "$backup_path" && sha256sum -c SHA256SUMS)

compose_file="deploy/docker-compose.yml"
docker compose -f "$compose_file" stop qms-web qms-api qms-worker >/dev/null
docker compose -f "$compose_file" exec -T qms-postgres sh -c \
  'pg_restore --clean --if-exists --no-owner --no-privileges --username="$POSTGRES_USER" --dbname="$POSTGRES_DB"' \
  < "$backup_path/database.dump"
docker run --rm -v qms_qms-files:/target -v "$backup_path:/backup:ro" alpine:3.22 \
  sh -c 'find /target -mindepth 1 -maxdepth 1 -exec rm -rf -- {} + && tar -xzf /backup/qms-files.tar.gz -C /target'

docker compose -f "$compose_file" --profile tools run --rm qms-migrator
docker compose -f "$compose_file" start qms-api qms-worker qms-web >/dev/null
echo "Restore completed. Verify /health/ready and run the integrity checks before releasing users."
