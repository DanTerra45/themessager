-- Active: 1775250727504@@127.0.0.1@5432@mercadito
-- 1. CREAR LA FUNCIÓN PRIMERO (Fuera de la transacción para evitar problemas de sintaxis)
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 2. AHORA SÍ, INICIAMOS LA TRANSACCIÓN PARA LAS TABLAS E INSERCIONES
BEGIN;

CREATE TABLE email_outbox (
  id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  to_address VARCHAR(100) NOT NULL,
  to_name VARCHAR(50),
  subject VARCHAR(255),
  plain_text_body TEXT,
  html_body TEXT,
  status VARCHAR(50) NOT NULL,
  attempts INT DEFAULT 0,
  next_attempt TIMESTAMPTZ,
  last_attempt TIMESTAMPTZ,
  sent_at_utc TIMESTAMPTZ,
  last_error TEXT,
  created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMPTZ
);

CREATE TABLE users (
  id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  username VARCHAR(40) NOT NULL,
  email VARCHAR(100) UNIQUE NOT NULL,
  password VARCHAR(255) NOT NULL,
  role VARCHAR(20) NOT NULL,
  creator_id INT,
  last_login TIMESTAMPTZ,
  state VARCHAR(20) NOT NULL DEFAULT 'Active',
  need_change_password BOOLEAN NOT NULL DEFAULT FALSE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMPTZ,
  
  CONSTRAINT chk_user_role CHECK (role IN ('Admin', 'Auditor', 'Operator')),
  CONSTRAINT chk_user_state CHECK (state IN ('Active', 'Inactive'))
);

CREATE TABLE user_story (
  id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  user_id INT NOT NULL,
  operator_id INT NOT NULL,
  previous_state VARCHAR(20) NOT NULL,
  actual_state VARCHAR(20) NOT NULL,
  disable_reason TEXT,
  created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
  
  CONSTRAINT chk_story_prev_state CHECK (previous_state IN ('Active', 'Inactive')),
  CONSTRAINT chk_story_act_state CHECK (actual_state IN ('Active', 'Inactive'))
);

CREATE TABLE password_reset_token (
  id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  user_id INT NOT NULL,
  token_hash VARCHAR(64) NOT NULL,
  expires_at TIMESTAMPTZ NOT NULL,
  used_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Índices
CREATE INDEX idx_users_username ON users (username);
CREATE INDEX idx_user_story_operator ON user_story (operator_id);

-- Llaves Foráneas
ALTER TABLE password_reset_token 
  ADD CONSTRAINT fk_password_reset_token_user_id 
  FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE;

ALTER TABLE user_story 
  ADD CONSTRAINT fk_user_story_user_id 
  FOREIGN KEY (user_id) REFERENCES users (id);

ALTER TABLE user_story 
  ADD CONSTRAINT fk_user_story_operator_id 
  FOREIGN KEY (operator_id) REFERENCES users (id);

ALTER TABLE users 
  ADD CONSTRAINT fk_users_creator_id 
  FOREIGN KEY (creator_id) REFERENCES users (id) ON DELETE SET NULL;

-- Triggers
CREATE TRIGGER set_timestamp_users
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER set_timestamp_email_outbox
BEFORE UPDATE ON email_outbox
FOR EACH ROW
EXECUTE FUNCTION trigger_set_timestamp();

-- Inserción inicial
INSERT INTO users (username, email, password, role, need_change_password) 
VALUES ('admin', 'admin@admin.com', 'rjx+LLenULbaYVCgkQdwzJPXGqVUxERVptCNmdzrGRDZLfawwk0KAu5UbNRY3bRj', 'Admin', FALSE);

COMMIT;