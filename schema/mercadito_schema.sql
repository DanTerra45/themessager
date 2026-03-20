-- Mercadito (MySQL 8+)

SET NAMES utf8mb4;

CREATE TABLE `categorias` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `codigo` VARCHAR(6) NOT NULL,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(150) NOT NULL,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_categorias_codigo` UNIQUE (`codigo`),
  CONSTRAINT `chk_categorias_codigo_formato` CHECK (`codigo` REGEXP '^C[0-9]{5}$'),
  CONSTRAINT `chk_categorias_nombre_no_vacio` CHECK (TRIM(`nombre`) <> ''),
  CONSTRAINT `chk_categorias_descripcion_no_vacia` CHECK (TRIM(`descripcion`) <> '')
);

CREATE TABLE `products` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(150) NOT NULL,
  `lote` VARCHAR(40) NOT NULL,
  `fechaCaducidad` DATE NOT NULL,
  `precio` DECIMAL(10,2) NOT NULL,
  `stock` INT NOT NULL,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `activoUnico` TINYINT GENERATED ALWAYS AS (CASE WHEN `estado` = 'A' THEN 1 ELSE NULL END) STORED,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_products_activos_nombre_lote_fechacaducidad` UNIQUE (`nombre`, `lote`, `fechaCaducidad`, `activoUnico`),
  CONSTRAINT `chk_products_nombre_no_vacio` CHECK (TRIM(`nombre`) <> ''),
  CONSTRAINT `chk_products_descripcion_no_vacia` CHECK (TRIM(`descripcion`) <> ''),
  CONSTRAINT `chk_products_lote_no_vacio` CHECK (TRIM(`lote`) <> ''),
  CONSTRAINT `chk_products_lote_formato` CHECK (`lote` REGEXP '^[0-9]{1,40}$'),
  -- Nota: MySQL no permite funciones no determinísticas (como CURRENT_DATE/CURDATE)
  -- dentro de CHECK. La validación dinámica de fecha se aplica con triggers.
  CONSTRAINT `chk_products_stock_positivo` CHECK (`stock` >= 0),
  CONSTRAINT `chk_products_precio_positivo` CHECK (`precio` >= 0.01)
);

CREATE TABLE `empleados` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `ci` BIGINT NOT NULL,
  `complemento` VARCHAR(2),
  `complementoNormalizado` VARCHAR(2) GENERATED ALWAYS AS (COALESCE(`complemento`, '')) STORED,
  `nombres` VARCHAR(40) NOT NULL,
  `primerApellido` VARCHAR(40) NOT NULL,
  `segundoApellido` VARCHAR(40),
  `rol` ENUM ('Cajero', 'Inventario') NOT NULL,
  `numeroContacto` VARCHAR(40) NOT NULL,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `activoUnico` TINYINT GENERATED ALWAYS AS (CASE WHEN `estado` = 'A' THEN 1 ELSE NULL END) STORED,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_empleados_activos_ci_complemento` UNIQUE (`ci`, `complementoNormalizado`, `activoUnico`),
  CONSTRAINT `chk_empleados_ci_rango` CHECK (`ci` BETWEEN 1000000 AND 99999999),
  CONSTRAINT `chk_empleados_nombres_no_vacios` CHECK (TRIM(`nombres`) <> ''),
  CONSTRAINT `chk_empleados_primer_apellido_no_vacio` CHECK (TRIM(`primerApellido`) <> ''),
  CONSTRAINT `chk_empleados_contacto_no_vacio` CHECK (TRIM(`numeroContacto`) <> ''),
  CONSTRAINT `chk_empleados_contacto_formato` CHECK (`numeroContacto` REGEXP '^[+]591[0-9]{8}$'),
  CONSTRAINT `chk_empleados_complemento_formato` CHECK (`complemento` IS NULL OR `complemento` REGEXP '^[0-9][A-Z]$')
);

CREATE TABLE `categoriaDeProducto` (
  `productId` BIGINT NOT NULL,
  `categoriaId` BIGINT NOT NULL,
  PRIMARY KEY (`productId`, `categoriaId`),
  CONSTRAINT `fk_categoriaDeProducto_productId` FOREIGN KEY (`productId`) REFERENCES `products` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_categoriaDeProducto_categoriaId` FOREIGN KEY (`categoriaId`) REFERENCES `categorias` (`id`) ON DELETE CASCADE
);

CREATE INDEX `categorias_index_1` ON `categorias` (`estado`, `nombre`);
CREATE INDEX `categorias_index_2` ON `categorias` (`estado`, `id`);
CREATE INDEX `categorias_index_3` ON `categorias` (`estado`, `codigo`);

CREATE INDEX `products_index_1` ON `products` (`estado`, `nombre`);
CREATE INDEX `products_index_2` ON `products` (`estado`, `lote`, `fechaCaducidad`);
CREATE INDEX `products_index_3` ON `products` (`estado`, `fechaCaducidad`);
CREATE INDEX `products_index_4` ON `products` (`estado`, `stock`);
CREATE INDEX `products_index_5` ON `products` (`estado`, `precio`);
CREATE INDEX `products_index_6` ON `products` (`estado`, `id`);

CREATE INDEX `empleados_index_4` ON `empleados` (`estado`, `primerApellido`, `segundoApellido`, `nombres`);
CREATE INDEX `empleados_index_5` ON `empleados` (`estado`, `ci`);
CREATE INDEX `empleados_index_6` ON `empleados` (`estado`, `rol`);
CREATE INDEX `empleados_index_7` ON `empleados` (`estado`, `id`);

CREATE INDEX `categoriaDeProducto_idx_categoria` ON `categoriaDeProducto` (`categoriaId`);

-- Evita warnings de "trigger no existe" en primeras ejecuciones.
SET @prev_sql_notes := @@sql_notes;
SET sql_notes = 0;
DROP TRIGGER IF EXISTS `trg_products_validate_expiration_insert`;
DROP TRIGGER IF EXISTS `trg_products_validate_expiration_update`;
SET sql_notes = @prev_sql_notes;

DELIMITER $$
CREATE TRIGGER `trg_products_validate_expiration_insert`
BEFORE INSERT ON `products`
FOR EACH ROW
BEGIN
  IF NEW.`fechaCaducidad` < CURRENT_DATE() THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'La fecha de caducidad no puede ser menor a la fecha actual.';
  END IF;
END$$

CREATE TRIGGER `trg_products_validate_expiration_update`
BEFORE UPDATE ON `products`
FOR EACH ROW
BEGIN
  IF NEW.`fechaCaducidad` < CURRENT_DATE() THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'La fecha de caducidad no puede ser menor a la fecha actual.';
  END IF;
END$$
DELIMITER ;
