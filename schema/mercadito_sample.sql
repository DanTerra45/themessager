-- Mercadito (MySQL 8+)
-- Usage (CLI): mysql -u <user> -p < schema/mercadito_sample.sql

SET NAMES utf8mb4;

START TRANSACTION;

-- CATEGORIAS
INSERT INTO `categorias` (`codigo`, `nombre`, `descripcion`, `estado`)
VALUES
  ('C00001', 'BEBIDAS', 'Gaseosas, jugos, agua y bebidas listas para consumo', 'A'),
  ('C00002', 'LACTEOS', 'Leche, yogurt, quesos y derivados refrigerados', 'A'),
  ('C00003', 'ABARROTES', 'Productos de despensa y uso diario', 'A'),
  ('C00004', 'LIMPIEZA', 'Productos para limpieza y desinfeccion del hogar', 'A'),
  ('C00005', 'SNACKS', 'Galletas, chips, chocolates y botanas', 'A'),
  ('C00006', 'PANADERIA', 'Panes y horneados de consumo diario', 'A'),
  ('C00007', 'CONGELADOS', 'Productos conservados a baja temperatura', 'A'),
  ('C00008', 'CARNES', 'Cortes y preparados de carne fresca o refrigerada', 'A'),
  ('C00009', 'HIGIENE PERSONAL', 'Cuidado personal e higiene diaria', 'A'),
  ('C00010', 'MASCOTAS', 'Alimentos y accesorios para mascotas', 'A'),
  ('C00011', 'BEBE', 'Productos de cuidado y alimentacion para bebe', 'A'),
  ('C00012', 'FRUTAS Y VERDURAS', 'Productos frescos de origen vegetal', 'A'),
  ('C00013', 'PAPELERIA', 'Insumos escolares y de oficina', 'A')
AS `incoming_categoria`
ON DUPLICATE KEY UPDATE
  `nombre` = `incoming_categoria`.`nombre`,
  `descripcion` = `incoming_categoria`.`descripcion`,
  `estado` = 'A';

REPLACE INTO `category_code_sequence` (`id`, `nextValue`)
SELECT
  1 AS `id`,
  LEAST(COALESCE(MAX(CAST(SUBSTRING(`codigo`, 2, 5) AS UNSIGNED)), 0) + 1, 100000) AS `nextValue`
FROM `categorias`;

-- PRODUCTOS
INSERT INTO `products`
  (`nombre`, `descripcion`, `lote`, `fechaCaducidad`, `precio`, `stock`, `estado`)
VALUES
  ('Coca Cola 2L', 'Bebida gaseosa sabor cola 2 litros', '2000000001', '2027-12-31', 12.50, 30, 'A'),
  ('Agua Mineral 2L', 'Agua purificada sin gas 2 litros', '2000000002', '2027-08-15', 5.00, 45, 'A'),
  ('Jugo Naranja 1L', 'Jugo pasteurizado sabor naranja 1 litro', '2000000003', '2027-04-30', 9.80, 20, 'A'),
  ('Te Frio Durazno 500ml', 'Bebida de te frio sabor durazno', '2000000004', '2027-06-10', 6.50, 28, 'A'),

  ('Leche Entera 1L', 'Leche entera larga vida', '2000000005', '2026-10-20', 8.90, 25, 'A'),
  ('Yogurt Natural 1L', 'Yogurt natural sin azucar', '2000000006', '2026-09-15', 14.00, 12, 'A'),
  ('Queso Fresco 500g', 'Queso fresco para consumo diario', '2000000007', '2026-12-20', 22.00, 16, 'A'),
  ('Mantequilla 200g', 'Mantequilla de mesa 200 gramos', '2000000008', '2027-01-12', 11.40, 18, 'A'),

  ('Arroz 5Kg', 'Arroz de grano largo bolsa de 5kg', '2000000009', '2028-05-30', 42.00, 18, 'A'),
  ('Azucar 1Kg', 'Azucar blanca refinada 1kg', '2000000010', '2028-08-18', 7.20, 35, 'A'),
  ('Fideo Spaghetti 1Kg', 'Pasta tipo spaghetti paquete de 1kg', '2000000011', '2028-02-14', 10.50, 24, 'A'),
  ('Aceite Vegetal 1L', 'Aceite vegetal refinado', '2000000012', '2027-11-01', 18.00, 22, 'A'),

  ('Detergente 900g', 'Detergente en polvo multiuso 900g', '2000000013', '2028-01-10', 16.50, 20, 'A'),
  ('Lavandina 2L', 'Desinfectante para pisos y banos', '2000000014', '2028-03-21', 9.50, 15, 'A'),
  ('Limpiavidrios 500ml', 'Limpiador de vidrios atomizador', '2000000015', '2027-09-05', 12.20, 14, 'A'),
  ('Jabon Vajilla 1L', 'Jabon liquido para vajilla 1 litro', '2000000016', '2027-07-25', 13.10, 19, 'A'),

  ('Papas Fritas 120g', 'Snack salado crocante 120 gramos', '2000000017', '2027-02-11', 7.00, 40, 'A'),
  ('Galletas Chocolate 150g', 'Galletas rellenas sabor chocolate', '2000000018', '2027-04-02', 6.50, 35, 'A'),
  ('Chocolate Barra 80g', 'Chocolate de leche en barra 80 gramos', '2000000019', '2027-10-20', 5.20, 50, 'A'),
  ('Mani Salado 100g', 'Mani tostado y salado 100 gramos', '2000000020', '2027-06-27', 4.80, 38, 'A'),

  ('Pan Molde Integral', 'Pan de molde integral rebanado', '2000000021', '2026-06-28', 11.00, 10, 'A'),
  ('Queque Vainilla', 'Queque sabor vainilla por unidad', '2000000022', '2026-06-22', 13.00, 8, 'A'),
  ('Croissant Mantequilla', 'Croissant horneado con mantequilla', '2000000023', '2026-06-26', 4.50, 22, 'A'),
  ('Pan Frances 12u', 'Paquete de pan frances por 12 unidades', '2000000024', '2026-06-25', 9.50, 17, 'A'),

  ('Nuggets Pollo 500g', 'Nuggets de pollo congelados 500 gramos', '2000000025', '2027-08-30', 24.00, 13, 'A'),
  ('Vegetales Mixtos 1Kg', 'Mezcla de vegetales congelados 1kg', '2000000026', '2027-11-19', 21.50, 11, 'A'),
  ('Hamburguesa Res 4u', 'Hamburguesas de res congeladas por 4 unidades', '2000000027', '2027-12-05', 29.90, 9, 'A'),
  ('Helado Vainilla 1L', 'Helado sabor vainilla de 1 litro', '2000000028', '2027-09-14', 26.00, 12, 'A'),

  ('Carne Molida 1Kg', 'Carne molida fresca 1kg', '2000000029', '2026-07-18', 48.00, 7, 'A'),
  ('Pechuga Pollo 1Kg', 'Pechuga de pollo refrigerada 1kg', '2000000030', '2026-07-16', 36.50, 9, 'A'),

  ('Shampoo Cabello Seco 400ml', 'Shampoo para cabello seco 400ml', '2000000031', '2028-04-10', 23.00, 14, 'A'),
  ('Pasta Dental Menta 120g', 'Pasta dental sabor menta 120 gramos', '2000000032', '2028-05-25', 10.80, 27, 'A'),
  ('Jabon Corporal Aloe 3u', 'Pack de jabon corporal aloe x3', '2000000033', '2028-02-28', 15.20, 18, 'A'),

  ('Arena Gato 5Kg', 'Arena sanitaria para gato 5kg', '2000000034', '2028-07-07', 32.00, 10, 'A'),
  ('Alimento Perro Adulto 8Kg', 'Alimento balanceado para perro adulto 8kg', '2000000035', '2028-08-09', 95.00, 6, 'A'),

  ('Panal Talla M 40u', 'Panal desechable talla M paquete de 40', '2000000036', '2028-01-31', 72.00, 12, 'A'),
  ('Toallitas Humedas Bebe 80u', 'Toallitas humedas para bebe x80', '2000000037', '2028-03-30', 18.50, 20, 'A'),
  ('Pure Manzana Bebe 113g', 'Pure de manzana para bebe 113 gramos', '2000000038', '2027-01-20', 9.20, 16, 'A'),

  ('Manzana Roja 1Kg', 'Manzana roja seleccionada por kilo', '2000000039', '2026-04-28', 14.00, 21, 'A'),
  ('Papa Blanca 2Kg', 'Papa blanca lavada bolsa de 2kg', '2000000040', '2026-05-18', 11.50, 26, 'A'),
  ('Tomate 1Kg', 'Tomate fresco para ensalada por kilo', '2000000041', '2026-04-22', 13.20, 19, 'A'),
  ('Zanahoria 1Kg', 'Zanahoria fresca por kilo', '2000000042', '2026-05-30', 9.80, 17, 'A'),

  ('Cuaderno Universitario 100h', 'Cuaderno universitario de 100 hojas', '2000000043', '2029-12-31', 16.00, 30, 'A'),
  ('Lapiz HB 12u', 'Caja de lapiz HB por 12 unidades', '2000000044', '2029-12-31', 12.00, 25, 'A'),
  ('Marcador Permanente Negro', 'Marcador permanente color negro', '2000000045', '2029-12-31', 7.50, 40, 'A')
AS `incoming_product`
ON DUPLICATE KEY UPDATE
  `descripcion` = `incoming_product`.`descripcion`,
  `precio` = `incoming_product`.`precio`,
  `stock` = `incoming_product`.`stock`,
  `estado` = 'A';

-- RELACIONES PRODUCTO-CATEGORIA
INSERT IGNORE INTO `categoriaDeProducto` (`productId`, `categoriaId`)
SELECT p.`id`, c.`id`
FROM (
  SELECT 'Coca Cola 2L' AS `nombre`, '2000000001' AS `lote`, '2027-12-31' AS `fechaCaducidad`, 'C00001' AS `codigo`
  UNION ALL SELECT 'Agua Mineral 2L', '2000000002', '2027-08-15', 'C00001'
  UNION ALL SELECT 'Jugo Naranja 1L', '2000000003', '2027-04-30', 'C00001'
  UNION ALL SELECT 'Te Frio Durazno 500ml', '2000000004', '2027-06-10', 'C00001'

  UNION ALL SELECT 'Leche Entera 1L', '2000000005', '2026-10-20', 'C00002'
  UNION ALL SELECT 'Yogurt Natural 1L', '2000000006', '2026-09-15', 'C00002'
  UNION ALL SELECT 'Queso Fresco 500g', '2000000007', '2026-12-20', 'C00002'
  UNION ALL SELECT 'Mantequilla 200g', '2000000008', '2027-01-12', 'C00002'

  UNION ALL SELECT 'Arroz 5Kg', '2000000009', '2028-05-30', 'C00003'
  UNION ALL SELECT 'Azucar 1Kg', '2000000010', '2028-08-18', 'C00003'
  UNION ALL SELECT 'Fideo Spaghetti 1Kg', '2000000011', '2028-02-14', 'C00003'
  UNION ALL SELECT 'Aceite Vegetal 1L', '2000000012', '2027-11-01', 'C00003'

  UNION ALL SELECT 'Detergente 900g', '2000000013', '2028-01-10', 'C00004'
  UNION ALL SELECT 'Lavandina 2L', '2000000014', '2028-03-21', 'C00004'
  UNION ALL SELECT 'Limpiavidrios 500ml', '2000000015', '2027-09-05', 'C00004'
  UNION ALL SELECT 'Jabon Vajilla 1L', '2000000016', '2027-07-25', 'C00004'

  UNION ALL SELECT 'Papas Fritas 120g', '2000000017', '2027-02-11', 'C00005'
  UNION ALL SELECT 'Galletas Chocolate 150g', '2000000018', '2027-04-02', 'C00005'
  UNION ALL SELECT 'Chocolate Barra 80g', '2000000019', '2027-10-20', 'C00005'
  UNION ALL SELECT 'Mani Salado 100g', '2000000020', '2027-06-27', 'C00005'

  UNION ALL SELECT 'Pan Molde Integral', '2000000021', '2026-06-28', 'C00006'
  UNION ALL SELECT 'Queque Vainilla', '2000000022', '2026-06-22', 'C00006'
  UNION ALL SELECT 'Croissant Mantequilla', '2000000023', '2026-06-26', 'C00006'
  UNION ALL SELECT 'Pan Frances 12u', '2000000024', '2026-06-25', 'C00006'

  UNION ALL SELECT 'Nuggets Pollo 500g', '2000000025', '2027-08-30', 'C00007'
  UNION ALL SELECT 'Nuggets Pollo 500g', '2000000025', '2027-08-30', 'C00008'
  UNION ALL SELECT 'Vegetales Mixtos 1Kg', '2000000026', '2027-11-19', 'C00007'
  UNION ALL SELECT 'Hamburguesa Res 4u', '2000000027', '2027-12-05', 'C00007'
  UNION ALL SELECT 'Hamburguesa Res 4u', '2000000027', '2027-12-05', 'C00008'
  UNION ALL SELECT 'Helado Vainilla 1L', '2000000028', '2027-09-14', 'C00007'

  UNION ALL SELECT 'Carne Molida 1Kg', '2000000029', '2026-07-18', 'C00008'
  UNION ALL SELECT 'Pechuga Pollo 1Kg', '2000000030', '2026-07-16', 'C00008'

  UNION ALL SELECT 'Shampoo Cabello Seco 400ml', '2000000031', '2028-04-10', 'C00009'
  UNION ALL SELECT 'Pasta Dental Menta 120g', '2000000032', '2028-05-25', 'C00009'
  UNION ALL SELECT 'Jabon Corporal Aloe 3u', '2000000033', '2028-02-28', 'C00009'

  UNION ALL SELECT 'Arena Gato 5Kg', '2000000034', '2028-07-07', 'C00010'
  UNION ALL SELECT 'Alimento Perro Adulto 8Kg', '2000000035', '2028-08-09', 'C00010'

  UNION ALL SELECT 'Panal Talla M 40u', '2000000036', '2028-01-31', 'C00011'
  UNION ALL SELECT 'Panal Talla M 40u', '2000000036', '2028-01-31', 'C00009'
  UNION ALL SELECT 'Toallitas Humedas Bebe 80u', '2000000037', '2028-03-30', 'C00011'
  UNION ALL SELECT 'Toallitas Humedas Bebe 80u', '2000000037', '2028-03-30', 'C00009'
  UNION ALL SELECT 'Pure Manzana Bebe 113g', '2000000038', '2027-01-20', 'C00011'

  UNION ALL SELECT 'Manzana Roja 1Kg', '2000000039', '2026-04-28', 'C00012'
  UNION ALL SELECT 'Papa Blanca 2Kg', '2000000040', '2026-05-18', 'C00012'
  UNION ALL SELECT 'Tomate 1Kg', '2000000041', '2026-04-22', 'C00012'
  UNION ALL SELECT 'Zanahoria 1Kg', '2000000042', '2026-05-30', 'C00012'

  UNION ALL SELECT 'Cuaderno Universitario 100h', '2000000043', '2029-12-31', 'C00013'
  UNION ALL SELECT 'Lapiz HB 12u', '2000000044', '2029-12-31', 'C00013'
  UNION ALL SELECT 'Marcador Permanente Negro', '2000000045', '2029-12-31', 'C00013'
) AS m
INNER JOIN `products` p
  ON p.`nombre` = m.`nombre`
 AND p.`lote` = m.`lote`
 AND p.`fechaCaducidad` = m.`fechaCaducidad`
 AND p.`estado` = 'A'
INNER JOIN `categorias` c
  ON c.`codigo` = m.`codigo`
 AND c.`estado` = 'A';

-- RECOMPUTAR CONTADOR CACHEADO DE PRODUCTOS ACTIVOS POR CATEGORIA
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

-- EMPLEADOS
INSERT INTO `empleados`
  (`ci`, `complemento`, `nombres`, `primerApellido`, `segundoApellido`, `rol`, `numeroContacto`, `estado`)
SELECT
  e.`ci`,
  e.`complemento`,
  e.`nombres`,
  e.`primerApellido`,
  e.`segundoApellido`,
  e.`rol`,
  e.`numeroContacto`,
  'A'
FROM (
  SELECT 6845123 AS `ci`, '1A' AS `complemento`, 'Luz Maria' AS `nombres`, 'Quispe' AS `primerApellido`, 'Rojas' AS `segundoApellido`, 'Cajero' AS `rol`, '+59171234567' AS `numeroContacto`
  UNION ALL SELECT 5901234, NULL, 'Carlos Alberto', 'Mamani', 'Lopez', 'Inventario', '+59172345678'
  UNION ALL SELECT 7312456, '2B', 'Ana Sofia', 'Perez', 'Guzman', 'Cajero', '+59177123456'
  UNION ALL SELECT 8023344, NULL, 'Jorge Luis', 'Vargas', 'Flores', 'Inventario', '+59122456789'
  UNION ALL SELECT 9276543, '3C', 'Mariana', 'Choque', 'Torrez', 'Cajero', '+59178901234'
  UNION ALL SELECT 7482311, NULL, 'Diego Andres', 'Camacho', 'Rios', 'Inventario', '+59171500123'
  UNION ALL SELECT 8832190, '4D', 'Paola', 'Suarez', 'Mendez', 'Cajero', '+59174455667'
  UNION ALL SELECT 6654321, NULL, 'Renzo', 'Paredes', 'Luna', 'Inventario', '+59172233445'
  UNION ALL SELECT 9134567, NULL, 'Valeria', 'Molina', 'Cruz', 'Cajero', '+59173322110'
  UNION ALL SELECT 7012458, '5E', 'Gabriel', 'Rojas', 'Soto', 'Inventario', '+59170011223'
  UNION ALL SELECT 8123401, NULL, 'Cynthia', 'Arce', 'Nina', 'Cajero', '+59175544123'
  UNION ALL SELECT 9567321, NULL, 'Bruno', 'Loayza', 'Herrera', 'Inventario', '+59176655443'
  UNION ALL SELECT 6234987, '6F', 'Martha', 'Velasco', 'Arias', 'Cajero', '+59178888999'
  UNION ALL SELECT 8899001, NULL, 'Adrian', 'Medina', 'Ruiz', 'Inventario', '+59171190022'
  UNION ALL SELECT 7400112, NULL, 'Nadia', 'Mendoza', 'Siles', 'Cajero', '+59177222334'
  UNION ALL SELECT 7999888, '7G', 'Pablo', 'Cortez', 'Salinas', 'Inventario', '+59171234567'
  UNION ALL SELECT 6555444, NULL, 'Erika', 'Flores', 'Ledezma', 'Cajero', '+59173444555'
  UNION ALL SELECT 9444333, NULL, 'Martin', 'Alarcon', 'Lima', 'Inventario', '+59176550011'
) AS e
WHERE NOT EXISTS (
  SELECT 1
  FROM `empleados` ex
  WHERE ex.`ci` = e.`ci`
    AND COALESCE(ex.`complemento`, '') = COALESCE(e.`complemento`, '')
    AND ex.`estado` = 'A'
);

COMMIT;
