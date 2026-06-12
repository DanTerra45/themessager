-- ServicioUsuarios (PostgreSQL)
-- Ejecutar con:
-- psql -h 127.0.0.1 -p 5432 -U postgres -d mercadito_db_users -f schema/mercadito_postgres_usuarios_sample.sql

INSERT INTO users
(id, username, email, password, role, creator_id, last_login, need_change_password, state, created_at, updated_at)
VALUES
(1, 'admin', 'admin@local', 'sample-hash-admin', 'Admin', NULL, NULL, false, 'Active', NOW(), NOW()),
(2, 'cajero1', 'cajero1@local', 'sample-hash-cajero', 'Operator', 1, NULL, true, 'Active', NOW(), NOW()),
(3, 'auditor1', 'auditor1@local', 'sample-hash-auditor', 'Auditor', 1, NULL, true, 'Active', NOW(), NOW())
ON CONFLICT DO NOTHING;

INSERT INTO user_story
(user_id, operator_id, previous_state, actual_state, disable_reason, created_at)
VALUES
(2, 1, 'Active', 'Active', NULL, NOW()),
(3, 1, 'Active', 'Active', NULL, NOW())
ON CONFLICT DO NOTHING;