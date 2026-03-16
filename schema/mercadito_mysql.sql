CREATE TABLE `categorias` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `codigo` VARCHAR(6) NOT NULL,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(150) NOT NULL,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `uq_categorias_codigo` UNIQUE (`codigo`),
  CONSTRAINT `chk_categorias_codigo_no_vacio` CHECK (`codigo` <> ''),
  CONSTRAINT `chk_categorias_nombre_no_vacio` CHECK (`nombre` <> ''),
  CONSTRAINT `chk_categorias_descripcion_no_vacia` CHECK (`descripcion` <> '')
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
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `chk_products_nombre_no_vacio` CHECK (`nombre` <> ''),
  CONSTRAINT `chk_products_descripcion_no_vacia` CHECK (`descripcion` <> ''),
  CONSTRAINT `chk_products_lote_no_vacio` CHECK (`lote` <> ''),
  CONSTRAINT `chk_products_stock_positivo` CHECK (`stock` >= 0),
  CONSTRAINT `chk_products_precio_positivo` CHECK (`precio` >= 0.01)
);

CREATE TABLE `empleados` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `ci` BIGINT NOT NULL,
  `complemento` VARCHAR(2),
  `nombres` VARCHAR(40) NOT NULL,
  `primerApellido` VARCHAR(40) NOT NULL,
  `segundoApellido` VARCHAR(40),
  `rol` ENUM ('Cajero', 'Inventario') NOT NULL,
  `numeroContacto` VARCHAR(40) NOT NULL,
  `estado` ENUM ('A', 'I') NOT NULL DEFAULT 'A',
  `fechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT `chk_empleados_ci_positivo` CHECK (`ci` > 0),
  CONSTRAINT `chk_empleados_nombres_no_vacios` CHECK (`nombres` <> ''),
  CONSTRAINT `chk_empleados_primer_apellido_no_vacio` CHECK (`primerApellido` <> ''),
  CONSTRAINT `chk_empleados_contacto_no_vacio` CHECK (`numeroContacto` <> ''),
  CONSTRAINT `chk_empleados_complemento_formato` CHECK (`complemento` IS NULL OR `complemento` REGEXP '^[0-9][A-Z]$')
);

CREATE TABLE `categoriaDeProducto` (
  `productId` BIGINT NOT NULL,
  `categoriaId` BIGINT NOT NULL,
  PRIMARY KEY (`productId`, `categoriaId`)
);

CREATE INDEX `categorias_index_1` ON `categorias` (`estado`, `nombre`);
CREATE INDEX `products_index_1` ON `products` (`estado`, `nombre`);
CREATE INDEX `products_index_2` ON `products` (`lote`, `fechaCaducidad`);
CREATE INDEX `products_index_3` ON `products` (`fechaCaducidad`);
CREATE INDEX `empleados_index_4` ON `empleados` (`estado`, `primerApellido`, `segundoApellido`, `nombres`);
CREATE INDEX `empleados_index_5` ON `empleados` (`ci`);
CREATE INDEX `empleados_index_6` ON `empleados` (`rol`);

CREATE INDEX `categoriaDeProducto_idx_categoria` ON `categoriaDeProducto` (`categoriaId`);
ALTER TABLE `categoriaDeProducto` ADD FOREIGN KEY (`categoriaId`) REFERENCES `categorias` (`id`) ON DELETE CASCADE;
ALTER TABLE `categoriaDeProducto` ADD FOREIGN KEY (`productId`) REFERENCES `products` (`id`) ON DELETE CASCADE;
