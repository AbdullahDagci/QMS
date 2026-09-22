#!/usr/bin/env bash
set -Eeuo pipefail

runtime_user="${QMS_DB_APP_USER:-qms_app}"
if [[ ! "$runtime_user" =~ ^[a-z_][a-z0-9_]{0,62}$ ]]; then
  echo "QMS_DB_APP_USER is not a valid PostgreSQL role name." >&2
  exit 2
fi
runtime_password="$(tr -d '\r\n' < /run/secrets/qms_db_app_password)"
if [[ -z "$runtime_password" ]]; then
  echo "Runtime database password secret is empty." >&2
  exit 2
fi

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  --set=runtime_user="$runtime_user" --set=runtime_password="$runtime_password" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'runtime_user', :'runtime_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'runtime_user')
\gexec
SQL
