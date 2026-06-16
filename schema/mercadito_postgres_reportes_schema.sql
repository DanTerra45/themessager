CREATE TABLE IF NOT EXISTS sale_reports (
    id BIGSERIAL PRIMARY KEY,
    sale_id BIGINT NOT NULL UNIQUE,
    code VARCHAR(30) NOT NULL,
    business_date DATE NOT NULL,
    created_at TIMESTAMP NOT NULL,
    generated_at TIMESTAMP NOT NULL,
    status VARCHAR(20) NOT NULL,
    customer_id BIGINT NOT NULL,
    customer_ci_nit VARCHAR(40) NOT NULL,
    customer_business_name VARCHAR(150) NOT NULL,
    operator_id BIGINT NOT NULL,
    operator_username VARCHAR(100) NOT NULL,
    channel VARCHAR(40) NOT NULL,
    payment_method VARCHAR(40) NOT NULL,
    total NUMERIC(12,2) NOT NULL,
    amount_in_words TEXT NOT NULL,
    cancel_reason TEXT NULL,
    cancelled_by_user_id BIGINT NULL,
    cancelled_by_username VARCHAR(100) NULL,
    cancelled_at TIMESTAMP NULL,
    created_by_user_id BIGINT NOT NULL,
    created_by_username VARCHAR(100) NOT NULL,
    updated_by_user_id BIGINT NULL,
    updated_by_username VARCHAR(100) NULL,
    updated_at TIMESTAMP NULL,
    report_created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    report_updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS sale_report_lines (
    id BIGSERIAL PRIMARY KEY,
    sale_report_id BIGINT NOT NULL REFERENCES sale_reports(id) ON DELETE CASCADE,
    product_id BIGINT NOT NULL,
    product_name VARCHAR(150) NOT NULL,
    lot_code VARCHAR(60) NULL,
    quantity INT NOT NULL,
    unit_price NUMERIC(12,2) NOT NULL,
    subtotal NUMERIC(12,2) NOT NULL
);

CREATE TABLE IF NOT EXISTS sale_report_events (
    id BIGSERIAL PRIMARY KEY,
    sale_id BIGINT NOT NULL,
    event_type VARCHAR(40) NOT NULL,
    actor_user_id BIGINT NOT NULL,
    actor_username VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_sale_reports_business_date ON sale_reports (business_date DESC);
CREATE INDEX IF NOT EXISTS ix_sale_reports_status ON sale_reports (status);
CREATE INDEX IF NOT EXISTS ix_sale_reports_payment_method ON sale_reports (payment_method);
CREATE INDEX IF NOT EXISTS ix_sale_report_lines_sale_report_id ON sale_report_lines (sale_report_id);
CREATE INDEX IF NOT EXISTS ix_sale_report_events_sale_id ON sale_report_events (sale_id, created_at DESC);
