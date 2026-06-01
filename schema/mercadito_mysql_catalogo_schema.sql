-- Mercadito Catalog Service (MySQL 8+)
-- Scope: only catalog microservice tables

SET NAMES utf8mb4;

-- ============================================================
-- CATEGORIAS
-- ============================================================
CREATE TABLE IF NOT EXISTS `categorias` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `codigo` VARCHAR(6) NOT NULL,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(150) NOT NULL,
  `productosActivosCount` INT NOT NULL DEFAULT 0,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `created_by_user_id` BIGINT NOT NULL,
  `updated_by_user_id` BIGINT NOT NULL,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_categorias_codigo` UNIQUE (`codigo`),
  CONSTRAINT `chk_categorias_codigo_formato` CHECK (`codigo` REGEXP '^C[0-9]{5}$'),
  CONSTRAINT `chk_categorias_nombre_no_vacio` CHECK (TRIM(`nombre`) <> ''),
  CONSTRAINT `chk_categorias_descripcion_no_vacia` CHECK (TRIM(`descripcion`) <> ''),
  CONSTRAINT `chk_categorias_productos_activos_count` CHECK (`productosActivosCount` >= 0),
  CONSTRAINT `chk_categorias_created_by_valido` CHECK (`created_by_user_id` > 0),
  CONSTRAINT `chk_categorias_updated_by_valido` CHECK (`updated_by_user_id` > 0),
  FULLTEXT KEY `categorias_ft_nombre` (`nombre`)
);

CREATE TABLE IF NOT EXISTS `category_code_sequence` (
  `id` TINYINT UNSIGNED PRIMARY KEY,
  `nextValue` INT NOT NULL,
  CONSTRAINT `chk_category_code_sequence_id` CHECK (`id` = 1),
  CONSTRAINT `chk_category_code_sequence_next_value` CHECK (`nextValue` BETWEEN 1 AND 100000)
);

-- ============================================================
-- PRODUCTS
-- ============================================================
CREATE TABLE IF NOT EXISTS `products` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(150) NOT NULL,
  `lote` VARCHAR(40) NOT NULL,
  `fechaCaducidad` DATE NOT NULL,
  `precio` DECIMAL(10,2) NOT NULL,
  `stock` INT NOT NULL,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `created_by_user_id` BIGINT NOT NULL,
  `updated_by_user_id` BIGINT NOT NULL,
  `activoUnico` TINYINT GENERATED ALWAYS AS (CASE WHEN `estado` = 'A' THEN 1 ELSE NULL END) STORED,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_products_activos_nombre_lote_fechacaducidad` UNIQUE (`nombre`, `lote`, `fechaCaducidad`, `activoUnico`),
  CONSTRAINT `chk_products_nombre_no_vacio` CHECK (TRIM(`nombre`) <> ''),
  CONSTRAINT `chk_products_descripcion_no_vacia` CHECK (TRIM(`descripcion`) <> ''),
  CONSTRAINT `chk_products_lote_no_vacio` CHECK (TRIM(`lote`) <> ''),
  CONSTRAINT `chk_products_lote_formato` CHECK (`lote` REGEXP '^[0-9]{1,40}$'),
  CONSTRAINT `chk_products_stock_positivo` CHECK (`stock` >= 0),
  CONSTRAINT `chk_products_precio_positivo` CHECK (`precio` >= 0.01),
  CONSTRAINT `chk_products_created_by_valido` CHECK (`created_by_user_id` > 0),
  CONSTRAINT `chk_products_updated_by_valido` CHECK (`updated_by_user_id` > 0),
  FULLTEXT KEY `products_ft_nombre` (`nombre`)
);

-- ============================================================
-- RELACION PRODUCTO-CATEGORIA
-- ============================================================
CREATE TABLE IF NOT EXISTS `categoriaDeProducto` (
  `productId` BIGINT NOT NULL,
  `categoriaId` BIGINT NOT NULL,
  `created_by_user_id` BIGINT NOT NULL,
  `updated_by_user_id` BIGINT NOT NULL,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`productId`, `categoriaId`),
  CONSTRAINT `chk_categoriaDeProducto_created_by_valido` CHECK (`created_by_user_id` > 0),
  CONSTRAINT `chk_categoriaDeProducto_updated_by_valido` CHECK (`updated_by_user_id` > 0),
  CONSTRAINT `fk_categoriaDeProducto_productId` FOREIGN KEY (`productId`) REFERENCES `products` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_categoriaDeProducto_categoriaId` FOREIGN KEY (`categoriaId`) REFERENCES `categorias` (`id`) ON DELETE CASCADE
);

-- ============================================================
-- AUDITORIA (PROPIA DEL CATALOGO)
-- ============================================================
CREATE TABLE IF NOT EXISTS `auditoria` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `usuarioId` BIGINT NOT NULL,
  `usuarioUsername` VARCHAR(50) NOT NULL,
  `accion` ENUM('C', 'U', 'D') NOT NULL,
  `tabla` VARCHAR(64) NOT NULL,
  `registroId` BIGINT NOT NULL,
  `ipAddress` VARCHAR(45),
  `userAgent` VARCHAR(255),
  `datosAnteriores` JSON,
  `datosNuevos` JSON,
  `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT `chk_auditoria_accion_valida` CHECK (`accion` IN ('C', 'U', 'D'))
);

-- ============================================================
-- INDICES
-- ============================================================
CREATE INDEX `categorias_idx_estado_nombre` ON `categorias` (`estado`, `nombre`);
CREATE INDEX `categorias_idx_estado_id` ON `categorias` (`estado`, `id`);
CREATE INDEX `categorias_idx_estado_codigo` ON `categorias` (`estado`, `codigo`);
CREATE INDEX `categorias_idx_estado_count_id` ON `categorias` (`estado`, `productosActivosCount`, `id`);

CREATE INDEX `products_idx_estado_nombre` ON `products` (`estado`, `nombre`);
CREATE INDEX `products_idx_estado_lote_fecha` ON `products` (`estado`, `lote`, `fechaCaducidad`);
CREATE INDEX `products_idx_estado_fecha` ON `products` (`estado`, `fechaCaducidad`);
CREATE INDEX `products_idx_estado_stock` ON `products` (`estado`, `stock`);
CREATE INDEX `products_idx_estado_precio` ON `products` (`estado`, `precio`);
CREATE INDEX `products_idx_estado_id` ON `products` (`estado`, `id`);

CREATE INDEX `categoriaDeProducto_idx_categoria` ON `categoriaDeProducto` (`categoriaId`);
CREATE INDEX `auditoria_idx_tabla_registro` ON `auditoria` (`tabla`, `registroId`);
CREATE INDEX `auditoria_idx_usuario_timestamp` ON `auditoria` (`usuarioId`, `timestamp`);
CREATE INDEX `auditoria_idx_timestamp` ON `auditoria` (`timestamp`);

-- ============================================================
-- DATOS INICIALES
-- ============================================================
INSERT INTO `category_code_sequence` (`id`, `nextValue`)
VALUES (1, 1)
ON DUPLICATE KEY UPDATE `nextValue` = `nextValue`;

-- ============================================================
-- TRIGGERS
-- ============================================================
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
