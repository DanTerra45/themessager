-- ServicioDeVentas (PostgreSQL 15+)
--
-- Ejecutar con:
-- psql -h 127.0.0.1 -p 5452 -U postgres -d mercadito_db_sales -f schema/mercadito_postgres_ventas_sample.sql

-- Cliente por defecto para ventas sin NIT.
INSERT INTO customer (id, ci, complement, ci_nit, razon_social, created_by)
VALUES (0, 0, NULL, '0', 'SIN NIT', 1);

-- Clientes de prueba.
INSERT INTO customer (id, ci, complement, ci_nit, razon_social, phone, email, address, created_by)
VALUES
    (1, 7483920, NULL, '7483920', 'PEREZ JUAN CARLOS', '71234567', 'juan.perez@mercadito.local', 'Zona Norte', 10),
    (2, 4930291, '1B', '4930291-1B', 'MAMANI MARIA ELENA', '72345678', 'maria.mamani@mercadito.local', 'Zona Sur', 10);

SELECT setval(
    pg_get_serial_sequence('customer', 'id'),
    CASE
        WHEN COALESCE((SELECT MAX(id) FROM customer), 0) > 0 THEN (SELECT MAX(id) FROM customer)
        ELSE 1
    END,
    COALESCE((SELECT MAX(id) FROM customer), 0) > 0
);

-- Ventas de prueba.
INSERT INTO sales (id, code, customer_id, operator_id, operator_username, channel, payment_method, total_price, state, created_at)
VALUES
    (1, 'VTA-2026-000001', 0, 5, 'cajero.01', 'Mostrador', 'Efectivo', 130.00, 'Confirmed', CURRENT_TIMESTAMP - INTERVAL '2 hours'),
    (2, 'VTA-2026-000002', 1, 5, 'cajero.01', 'Mostrador', 'Tarjeta', 45.00, 'Confirmed', CURRENT_TIMESTAMP - INTERVAL '1 hour'),
    (3, 'VTA-2026-000003', 2, 6, 'supervisor.01', 'Mostrador', 'QR', 50.00, 'Cancelled', CURRENT_TIMESTAMP - INTERVAL '30 minutes');

SELECT setval(
    pg_get_serial_sequence('sales', 'id'),
    CASE
        WHEN COALESCE((SELECT MAX(id) FROM sales), 0) > 0 THEN (SELECT MAX(id) FROM sales)
        ELSE 1
    END,
    COALESCE((SELECT MAX(id) FROM sales), 0) > 0
);

-- Detalles.
INSERT INTO sale_details (
    product_id,
    product_name_snapshot,
    lot_code,
    unit_price,
    quantity,
    sale_id,
    created_by_user_id,
    updated_by_user_id)
VALUES
    (1, 'Coca Cola 2L', '2000000001', 50.00, 2, 1, 5, 5),
    (2, 'Papas Fritas Familiares', '2000000002', 30.00, 1, 1, 5, 5),
    (3, 'Hamburguesa Doble', '2000000003', 15.00, 3, 2, 5, 5),
    (1, 'Coca Cola 2L', '2000000001', 50.00, 1, 3, 6, 6);

-- Motivo de anulación.
INSERT INTO cancel_reason (
    sale_id,
    cancel_reason,
    operator_id,
    operator_username,
    created_by_user_id,
    updated_by_user_id)
VALUES (3, 'El cliente se arrepintió de la compra antes de pagar.', 6, 'supervisor.01', 6, 6);

UPDATE sales
SET cancelled_by_user_id = 6,
    cancelled_by_username = 'supervisor.01',
    cancelled_at = CURRENT_TIMESTAMP - INTERVAL '20 minutes'
WHERE id = 3;

INSERT INTO sales_audit_log (
    sale_id,
    action,
    actor_user_id,
    actor_username,
    previous_state,
    current_state,
    previous_data,
    new_data)
VALUES
    (
        1,
        'CREATE',
        5,
        'cajero.01',
        NULL,
        'Confirmed',
        NULL,
        '{"code":"VTA-2026-000001","customerId":0,"total":130.00,"channel":"Mostrador","paymentMethod":"Efectivo"}'::jsonb
    ),
    (
        3,
        'UPDATE',
        6,
        'supervisor.01',
        'Confirmed',
        'Cancelled',
        '{"state":"Confirmed"}'::jsonb,
        '{"state":"Cancelled","reason":"El cliente se arrepintió de la compra antes de pagar."}'::jsonb
    );

INSERT INTO sales_saga_log (
    sale_id,
    step,
    status,
    actor_user_id,
    actor_username,
    product_id,
    quantity,
    payload)
VALUES
    (
        1,
        'reserve-stock',
        'succeeded',
        5,
        'cajero.01',
        1,
        2,
        '{"operation":"register-sale","productId":1,"quantity":2}'::jsonb
    ),
    (
        3,
        'recover-stock',
        'succeeded',
        6,
        'supervisor.01',
        1,
        1,
        '{"operation":"cancel-sale","saleId":3,"productId":1,"quantity":1}'::jsonb
    );
