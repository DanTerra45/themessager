-- ServicioEmpleados (PostgreSQL 18+)
-- Ejecutar con:
-- psql -h 127.0.0.1 -p 5412 -U postgres -d mercadito_employees -f schema/mercadito_postgres_empleados_sample.sql

INSERT INTO empleados (ci, complemento, nombres, primerapellido, segundoapellido, cargo, numerocontacto, estado, created_by_user_id, updated_by_user_id)
VALUES
  (7234561, '1A', 'Carlos', 'Paredes', 'Quispe', 'Cajero', '71234567', 'A', 1, 1),
  (8456721, NULL, 'Mariela', 'Rojas', 'Lopez', 'Inventario', '74567890', 'A', 1, 1),
  (9345217, '2B', 'Lucia', 'Arias', NULL, 'Cajero', '78901234', 'A', 1, 1)
ON CONFLICT DO NOTHING;
