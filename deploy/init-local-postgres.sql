\set ON_ERROR_STOP on

SELECT 'CREATE ROLE qms_app LOGIN PASSWORD ''qms_dev_password'''
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'qms_app')
\gexec

ALTER ROLE qms_app WITH LOGIN PASSWORD 'qms_dev_password';

SELECT 'CREATE DATABASE qms OWNER qms_app'
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'qms')
\gexec

ALTER DATABASE qms OWNER TO qms_app;
GRANT ALL PRIVILEGES ON DATABASE qms TO qms_app;
