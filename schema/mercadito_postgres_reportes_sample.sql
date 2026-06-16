INSERT INTO sale_reports (
    sale_id, code, business_date, created_at, generated_at, status,
    customer_id, customer_ci_nit, customer_business_name,
    operator_id, operator_username, channel, payment_method,
    total, amount_in_words, cancel_reason,
    cancelled_by_user_id, cancelled_by_username, cancelled_at,
    created_by_user_id, created_by_username,
    updated_by_user_id, updated_by_username, updated_at, report_updated_at
) VALUES
(1, 'VTA-2026-000001', CURRENT_DATE, CURRENT_TIMESTAMP - INTERVAL '4 hour', CURRENT_TIMESTAMP - INTERVAL '4 hour', 'Registrada', 1, '1234567', 'Consumidor Final', 1, 'admin', 'Mostrador', 'Efectivo', 18.50, 'DIECIOCHO 50/100 BOLIVIANOS', NULL, NULL, NULL, NULL, 1, 'admin', 1, 'admin', CURRENT_TIMESTAMP - INTERVAL '4 hour', CURRENT_TIMESTAMP - INTERVAL '4 hour'),
(2, 'VTA-2026-000002', CURRENT_DATE, CURRENT_TIMESTAMP - INTERVAL '2 hour', CURRENT_TIMESTAMP - INTERVAL '2 hour', 'Anulada', 2, '9988776', 'Distribuidora Mercado Norte', 1, 'admin', 'Mostrador', 'QR', 42.00, 'CUARENTA Y DOS 00/100 BOLIVIANOS', 'Cliente desistió de la compra.', 1, 'admin', CURRENT_TIMESTAMP - INTERVAL '90 minute', 1, 'admin', 1, 'admin', CURRENT_TIMESTAMP - INTERVAL '90 minute', CURRENT_TIMESTAMP - INTERVAL '90 minute')
ON CONFLICT (sale_id) DO NOTHING;

INSERT INTO sale_report_lines (sale_report_id, product_id, product_name, lot_code, quantity, unit_price, subtotal)
SELECT id, 1, 'Arroz Premium', 'LT-001', 2, 5.25, 10.50 FROM sale_reports WHERE sale_id = 1
UNION ALL
SELECT id, 2, 'Aceite Familiar', 'LT-002', 1, 8.00, 8.00 FROM sale_reports WHERE sale_id = 1
UNION ALL
SELECT id, 3, 'Azúcar Morena', 'LT-003', 3, 14.00, 42.00 FROM sale_reports WHERE sale_id = 2;

INSERT INTO sale_report_events (sale_id, event_type, actor_user_id, actor_username, payload) VALUES
(1, 'sales.registered', 1, 'admin', '{"saleId":1,"code":"VTA-2026-000001"}'::jsonb),
(2, 'sales.registered', 1, 'admin', '{"saleId":2,"code":"VTA-2026-000002"}'::jsonb),
(2, 'sales.cancelled', 1, 'admin', '{"saleId":2,"reason":"Cliente desistió de la compra."}'::jsonb);
