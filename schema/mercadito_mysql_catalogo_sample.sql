-- Mercadito Catalog Service (MySQL 8+)
-- Sample data only for catalog microservice
-- Usage: mysql -h 127.0.0.1 -P 3306 -u root -p mercadito_db < schema/mercadito_mysql_catalogo_sample.sql

SET NAMES utf8mb4;

START TRANSACTION;

-- ============================================================
-- CATEGORIAS
-- ============================================================
INSERT INTO `categorias` (`codigo`, `nombre`, `descripcion`, `estado`)
VALUES
  ('C00001', 'BEBIDAS', 'Gaseosas, jugos, agua y bebidas listas para consumo', 'A'),
  ('C00002', 'LACTEOS', 'Leche, yogurt, quesos y derivados refrigerados', 'A'),
  ('C00003', 'ABARROTES', 'Productos de despensa y uso diario', 'A'),
  ('C00004', 'LIMPIEZA', 'Productos para limpieza y desinfeccion del hogar', 'A'),
  ('C00005', 'SNACKS', 'Galletas, chips, chocolates y botanas', 'A'),
  ('C00006', 'PANADERIA', 'Panes y horneados de consumo diario', 'A'),
  ('C00007', 'CONGELADOS', 'Productos conservados a baja temperatura', 'A'),
  ('C00008', 'CARNES', 'Cortes y preparados de carne fresca o refrigerada', 'A')
AS `incoming_categoria`
ON DUPLICATE KEY UPDATE
  `nombre` = `incoming_categoria`.`nombre`,
  `descripcion` = `incoming_categoria`.`descripcion`,
  `estado` = 'A';

UPDATE `category_code_sequence` AS `sequence_row`
INNER JOIN (
  SELECT LEAST(COALESCE(MAX(CAST(SUBSTRING(`codigo`, 2, 5) AS UNSIGNED)), 0) + 1, 100000) AS `nextValue`
  FROM `categorias`
) AS `incoming_value`
  ON `sequence_row`.`id` = 1
SET `sequence_row`.`nextValue` = GREATEST(`sequence_row`.`nextValue`, `incoming_value`.`nextValue`);

-- ============================================================
-- PRODUCTS
-- ============================================================
INSERT INTO `products` (`nombre`, `descripcion`, `lote`, `fechaCaducidad`, `precio`, `stock`, `estado`)
VALUES
  ('Coca Cola 2L', 'Bebida gaseosa sabor cola 2 litros', '2000000001', '2027-12-31', 12.50, 30, 'A'),
  ('Agua Mineral 2L', 'Agua purificada sin gas 2 litros', '2000000002', '2027-08-15', 5.00, 45, 'A'),
  ('Leche Entera 1L', 'Leche entera larga vida', '2000000003', '2027-10-20', 8.90, 25, 'A'),
  ('Yogurt Natural 1L', 'Yogurt natural sin azucar', '2000000004', '2027-09-15', 14.00, 12, 'A'),
  ('Arroz 5Kg', 'Arroz de grano largo bolsa de 5kg', '2000000005', '2028-05-30', 42.00, 18, 'A'),
  ('Aceite Vegetal 1L', 'Aceite vegetal refinado', '2000000006', '2027-11-01', 18.00, 22, 'A'),
  ('Detergente 900g', 'Detergente en polvo multiuso 900g', '2000000007', '2028-01-10', 16.50, 20, 'A'),
  ('Lavandina 2L', 'Desinfectante para pisos y banos', '2000000008', '2028-03-21', 9.50, 15, 'A'),
  ('Papas Fritas 120g', 'Snack salado crocante 120 gramos', '2000000009', '2027-02-11', 7.00, 40, 'A'),
  ('Chocolate Barra 80g', 'Chocolate de leche en barra 80 gramos', '2000000010', '2027-10-20', 5.20, 50, 'A'),
  ('Pan Molde Integral', 'Pan de molde integral rebanado', '2000000011', '2027-06-28', 11.00, 10, 'A'),
  ('Croissant Mantequilla', 'Croissant horneado con mantequilla', '2000000012', '2027-06-26', 4.50, 22, 'A'),
  ('Nuggets Pollo 500g', 'Nuggets de pollo congelados 500 gramos', '2000000013', '2027-08-30', 24.00, 13, 'A'),
  ('Vegetales Mixtos 1Kg', 'Mezcla de vegetales congelados 1kg', '2000000014', '2027-11-19', 21.50, 11, 'A'),
  ('Carne Molida 1Kg', 'Carne molida fresca 1kg', '2000000015', '2027-07-18', 48.00, 7, 'A'),
  ('Pechuga Pollo 1Kg', 'Pechuga de pollo refrigerada 1kg', '2000000016', '2027-07-16', 36.50, 9, 'A')
AS `incoming_product`
ON DUPLICATE KEY UPDATE
  `descripcion` = `incoming_product`.`descripcion`,
  `precio` = `incoming_product`.`precio`,
  `stock` = `incoming_product`.`stock`,
  `estado` = 'A';

-- ============================================================
-- RELACIONES PRODUCTO-CATEGORIA
-- ============================================================
INSERT IGNORE INTO `categoriaDeProducto` (`productId`, `categoriaId`)
SELECT p.`id`, c.`id`
FROM (
  SELECT 'Coca Cola 2L' AS `nombre`, '2000000001' AS `lote`, '2027-12-31' AS `fechaCaducidad`, 'C00001' AS `codigo`
  UNION ALL SELECT 'Agua Mineral 2L', '2000000002', '2027-08-15', 'C00001'
  UNION ALL SELECT 'Leche Entera 1L', '2000000003', '2027-10-20', 'C00002'
  UNION ALL SELECT 'Yogurt Natural 1L', '2000000004', '2027-09-15', 'C00002'
  UNION ALL SELECT 'Arroz 5Kg', '2000000005', '2028-05-30', 'C00003'
  UNION ALL SELECT 'Aceite Vegetal 1L', '2000000006', '2027-11-01', 'C00003'
  UNION ALL SELECT 'Detergente 900g', '2000000007', '2028-01-10', 'C00004'
  UNION ALL SELECT 'Lavandina 2L', '2000000008', '2028-03-21', 'C00004'
  UNION ALL SELECT 'Papas Fritas 120g', '2000000009', '2027-02-11', 'C00005'
  UNION ALL SELECT 'Chocolate Barra 80g', '2000000010', '2027-10-20', 'C00005'
  UNION ALL SELECT 'Pan Molde Integral', '2000000011', '2027-06-28', 'C00006'
  UNION ALL SELECT 'Croissant Mantequilla', '2000000012', '2027-06-26', 'C00006'
  UNION ALL SELECT 'Nuggets Pollo 500g', '2000000013', '2027-08-30', 'C00007'
  UNION ALL SELECT 'Nuggets Pollo 500g', '2000000013', '2027-08-30', 'C00008'
  UNION ALL SELECT 'Vegetales Mixtos 1Kg', '2000000014', '2027-11-19', 'C00007'
  UNION ALL SELECT 'Carne Molida 1Kg', '2000000015', '2027-07-18', 'C00008'
  UNION ALL SELECT 'Pechuga Pollo 1Kg', '2000000016', '2027-07-16', 'C00008'
) AS m
INNER JOIN `products` p
  ON p.`nombre` = m.`nombre`
 AND p.`lote` = m.`lote`
 AND p.`fechaCaducidad` = m.`fechaCaducidad`
 AND p.`estado` = 'A'
INNER JOIN `categorias` c
  ON c.`codigo` = m.`codigo`
 AND c.`estado` = 'A';

-- ============================================================
-- RECALCULO DEL CONTADOR DE PRODUCTOS ACTIVOS
-- ============================================================
UPDATE `categorias` c
LEFT JOIN (
  SELECT
    cp.`categoriaId` AS `CategoryId`,
    COUNT(DISTINCT cp.`productId`) AS `ActiveProductCount`
  FROM `categoriaDeProducto` cp
  INNER JOIN `products` p ON p.`id` = cp.`productId`
  WHERE p.`estado` = 'A'
  GROUP BY cp.`categoriaId`
) AS counts
  ON counts.`CategoryId` = c.`id`
SET c.`productosActivosCount` = COALESCE(counts.`ActiveProductCount`, 0);

COMMIT;
