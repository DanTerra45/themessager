-- Mercadito (MySQL 8+)
-- Schema con auditoría

SET NAMES utf8mb4;


-- ============================================================
-- TABLA DE PROVEEDORES
-- ============================================================
CREATE TABLE `proveedores` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `codigo` VARCHAR(6) NOT NULL,
  `razonSocial` VARCHAR(150) NOT NULL,
  `direccion` VARCHAR(150),
  `contacto` VARCHAR(100),
  `telefono` VARCHAR(20),
  `rubro` VARCHAR(50),
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_proveedores_codigo` UNIQUE (`codigo`),
  CONSTRAINT `chk_proveedores_codigo_formato` CHECK (`codigo` REGEXP '^PRV[0-9]{3}$'),
  CONSTRAINT `chk_proveedores_razon_social_no_vacia` CHECK (TRIM(`razonSocial`) <> '')
);

CREATE INDEX `proveedores_idx_estado_codigo` ON `proveedores` (`estado`, `codigo`);
CREATE INDEX `proveedores_idx_estado_razon` ON `proveedores` (`estado`, `razonSocial`);

CREATE TABLE `supplier_code_sequence` (
  `id` TINYINT UNSIGNED PRIMARY KEY,
  `nextValue` INT NOT NULL,
  CONSTRAINT `chk_supplier_code_sequence_id` CHECK (`id` = 1),
  CONSTRAINT `chk_supplier_code_sequence_next_value` CHECK (`nextValue` BETWEEN 1 AND 1000)
);

-- ============================================================
-- TABLA DE CATEGORÍAS
-- ============================================================
CREATE TABLE `categorias` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `codigo` VARCHAR(6) NOT NULL,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(150) NOT NULL,
  `productosActivosCount` INT NOT NULL DEFAULT 0,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_categorias_codigo` UNIQUE (`codigo`),
  CONSTRAINT `chk_categorias_codigo_formato` CHECK (`codigo` REGEXP '^C[0-9]{5}$'),
  CONSTRAINT `chk_categorias_nombre_no_vacio` CHECK (TRIM(`nombre`) <> ''),
  CONSTRAINT `chk_categorias_descripcion_no_vacia` CHECK (TRIM(`descripcion`) <> ''),
  CONSTRAINT `chk_categorias_productos_activos_count` CHECK (`productosActivosCount` >= 0),
  FULLTEXT KEY `categorias_ft_nombre` (`nombre`)
);

CREATE TABLE `category_code_sequence` (
  `id` TINYINT UNSIGNED PRIMARY KEY,
  `nextValue` INT NOT NULL,
  CONSTRAINT `chk_category_code_sequence_id` CHECK (`id` = 1),
  CONSTRAINT `chk_category_code_sequence_next_value` CHECK (`nextValue` BETWEEN 1 AND 100000)
);

-- ============================================================
-- TABLA DE PRODUCTOS
-- ============================================================
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
  CONSTRAINT `chk_products_stock_positivo` CHECK (`stock` >= 0),
  CONSTRAINT `chk_products_precio_positivo` CHECK (`precio` >= 0.01),
  FULLTEXT KEY `products_ft_nombre` (`nombre`)
);

-- ============================================================
-- TABLA DE EMPLEADOS
-- ============================================================
CREATE TABLE `empleados` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `ci` BIGINT NOT NULL,
  `complemento` VARCHAR(2),
  `complementoNormalizado` VARCHAR(2) GENERATED ALWAYS AS (COALESCE(`complemento`, '')) STORED,
  `nombres` VARCHAR(40) NOT NULL,
  `primerApellido` VARCHAR(40) NOT NULL,
  `segundoApellido` VARCHAR(40),
  `cargo` ENUM ('Cajero', 'Inventario') NOT NULL,
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

-- ============================================================
-- TABLA RELACIONAL: Categoría de Producto
-- ============================================================
CREATE TABLE `categoriaDeProducto` (
  `productId` BIGINT NOT NULL,
  `categoriaId` BIGINT NOT NULL,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`productId`, `categoriaId`),
  CONSTRAINT `fk_categoriaDeProducto_productId` FOREIGN KEY (`productId`) REFERENCES `products` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_categoriaDeProducto_categoriaId` FOREIGN KEY (`categoriaId`) REFERENCES `categorias` (`id`) ON DELETE CASCADE
);

-- ============================================================
-- TABLA DE USUARIOS (para login)
-- ============================================================
CREATE TABLE `usuarios` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `username` VARCHAR(50) NOT NULL,
  `passwordHash` VARCHAR(255) NOT NULL,
  `email` VARCHAR(100),
  `empleadoId` BIGINT,
  `rol` ENUM ('Admin', 'Operador', 'Auditor') NOT NULL DEFAULT 'Operador',
  `estado` ENUM ('A', 'I', 'B') NOT NULL DEFAULT 'A',
  `empleadoActivoUnico` BIGINT GENERATED ALWAYS AS (CASE WHEN `estado` = 'A' THEN `empleadoId` ELSE NULL END) STORED,
  `ultimoLogin` DATETIME,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_usuarios_username` UNIQUE (`username`),
  CONSTRAINT `uq_usuarios_email` UNIQUE (`email`),
  CONSTRAINT `uq_usuarios_activos_empleado` UNIQUE (`empleadoActivoUnico`),
  CONSTRAINT `chk_usuarios_username_no_vacio` CHECK (TRIM(`username`) <> ''),
  CONSTRAINT `chk_usuarios_username_formato` CHECK (`username` REGEXP '^[a-z0-9._-]{4,40}$'),
  CONSTRAINT `chk_usuarios_password_hash_no_vacio` CHECK (TRIM(`passwordHash`) <> ''),
  CONSTRAINT `chk_usuarios_email_formato` CHECK (`email` IS NULL OR `email` REGEXP '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'),
  CONSTRAINT `fk_usuarios_empleadoId` FOREIGN KEY (`empleadoId`) REFERENCES `empleados` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `password_reset_tokens` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `usuarioId` BIGINT NOT NULL,
  `tokenHash` CHAR(64) NOT NULL,
  `expiresAtUtc` DATETIME NOT NULL,
  `usedAtUtc` DATETIME NULL,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT `uq_password_reset_tokens_token_hash` UNIQUE (`tokenHash`),
  CONSTRAINT `fk_password_reset_tokens_usuarioId` FOREIGN KEY (`usuarioId`) REFERENCES `usuarios` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `chk_password_reset_tokens_expiracion` CHECK (`expiresAtUtc` > `fechaRegistro`)
);

CREATE TABLE `email_outbox` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `toAddress` VARCHAR(255) NOT NULL,
  `toName` VARCHAR(150),
  `subject` VARCHAR(255) NOT NULL,
  `plainTextBody` TEXT NOT NULL,
  `htmlBody` MEDIUMTEXT,
  `status` ENUM ('P', 'R', 'S', 'E') NOT NULL DEFAULT 'P',
  `attempts` INT NOT NULL DEFAULT 0,
  `nextAttemptAtUtc` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `lastAttemptAtUtc` DATETIME NULL,
  `sentAtUtc` DATETIME NULL,
  `lastError` VARCHAR(1000) NULL,
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `chk_email_outbox_to_address_no_vacio` CHECK (TRIM(`toAddress`) <> ''),
  CONSTRAINT `chk_email_outbox_subject_no_vacio` CHECK (TRIM(`subject`) <> ''),
  CONSTRAINT `chk_email_outbox_plain_text_no_vacio` CHECK (TRIM(`plainTextBody`) <> ''),
  CONSTRAINT `chk_email_outbox_attempts_positivos` CHECK (`attempts` >= 0)
);

-- ============================================================
-- TABLA DE AUDITORÍA (log de operaciones C/U/D)
-- ============================================================
CREATE TABLE `auditoria` (
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
  CONSTRAINT `fk_auditoria_usuarioId` FOREIGN KEY (`usuarioId`) REFERENCES `usuarios` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `chk_auditoria_accion_valida` CHECK (`accion` IN ('C', 'U', 'D'))
);

CREATE INDEX `auditoria_idx_tabla_registro` ON `auditoria` (`tabla`, `registroId`);
CREATE INDEX `auditoria_idx_usuario` ON `auditoria` (`usuarioId`, `timestamp`);
CREATE INDEX `auditoria_idx_timestamp` ON `auditoria` (`timestamp`);


-- ============================================================
-- ÍNDICES
-- ============================================================
CREATE INDEX `categorias_index_1` ON `categorias` (`estado`, `nombre`);
CREATE INDEX `categorias_index_2` ON `categorias` (`estado`, `id`);
CREATE INDEX `categorias_index_3` ON `categorias` (`estado`, `codigo`);
CREATE INDEX `categorias_index_4` ON `categorias` (`estado`, `productosActivosCount`, `id`);

CREATE INDEX `products_index_1` ON `products` (`estado`, `nombre`);
CREATE INDEX `products_index_2` ON `products` (`estado`, `lote`, `fechaCaducidad`);
CREATE INDEX `products_index_3` ON `products` (`estado`, `fechaCaducidad`);
CREATE INDEX `products_index_4` ON `products` (`estado`, `stock`);
CREATE INDEX `products_index_5` ON `products` (`estado`, `precio`);
CREATE INDEX `products_index_6` ON `products` (`estado`, `id`);
CREATE INDEX `empleados_index_4` ON `empleados` (`estado`, `primerApellido`, `segundoApellido`, `nombres`);
CREATE INDEX `empleados_index_5` ON `empleados` (`estado`, `ci`);
CREATE INDEX `empleados_index_6` ON `empleados` (`estado`, `cargo`);
CREATE INDEX `empleados_index_7` ON `empleados` (`estado`, `id`);

CREATE INDEX `categoriaDeProducto_idx_categoria` ON `categoriaDeProducto` (`categoriaId`);

CREATE INDEX `usuarios_idx_estado` ON `usuarios` (`estado`, `username`);
CREATE INDEX `usuarios_idx_empleado` ON `usuarios` (`empleadoId`);
CREATE INDEX `usuarios_idx_estado_rol` ON `usuarios` (`estado`, `rol`);
CREATE INDEX `password_reset_tokens_idx_usuario_activo` ON `password_reset_tokens` (`usuarioId`, `usedAtUtc`, `expiresAtUtc`);
CREATE INDEX `password_reset_tokens_idx_expiracion` ON `password_reset_tokens` (`expiresAtUtc`);
CREATE INDEX `email_outbox_idx_status_next_attempt` ON `email_outbox` (`status`, `nextAttemptAtUtc`, `id`);
CREATE INDEX `email_outbox_idx_fecha_registro` ON `email_outbox` (`fechaRegistro`);

-- ============================================================
-- DATOS INICIALES
-- ============================================================
INSERT INTO `category_code_sequence` (`id`, `nextValue`)
VALUES (1, 1)
ON DUPLICATE KEY UPDATE `nextValue` = `nextValue`;

INSERT INTO `supplier_code_sequence` (`id`, `nextValue`)
VALUES (1, 1)
ON DUPLICATE KEY UPDATE `nextValue` = `nextValue`;

-- Usuario admin por defecto (password: admin123 - hash Argon2id generado con Sodium.Core)
INSERT INTO `usuarios` (`id`, `username`, `passwordHash`, `email`, `empleadoId`, `rol`, `estado`)
VALUES (1, 'admin', '$argon2id$v=19$m=32768,t=4,p=1$3QZWbg3x4RUra6Q7g9bTAg$ssnF1bWetEdepyx4oa5WI9ZF/l9CLjscemQmdpe27GU', 'admin@mercadito.local', NULL, 'Admin', 'A')
ON DUPLICATE KEY UPDATE `username` = `username`;

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
