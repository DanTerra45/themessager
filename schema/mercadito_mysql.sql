CREATE TABLE `categorias` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `codigo` VARCHAR(6) UNIQUE,
  `nombre` VARCHAR(150),
  `descripcion` VARCHAR(150),
  `estado` CHAR(1),
  `fechaRegistro` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE `products` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `nombre` VARCHAR(150),
  `descripcion` VARCHAR(150),
  `lote` DATETIME,
  `fechaCaducidad` DATETIME,
  `precio` DECIMAL(10,2),
  `stock` INT,
  `estado` CHAR(1),
  `fechaRegistro` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE `empleados` (
  `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
  `ci` BIGINT,
  `complemento` VARCHAR(20),
  `nombres` VARCHAR(40),
  `primerApellido` VARCHAR(40),
  `segundoApellido` VARCHAR(40),
  `rol` ENUM ('Cajero', 'Inventario'),
  `numeroContacto` VARCHAR(40),
  `estado` CHAR(1),
  `fechaRegistro` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `ultimaActualizacion` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE `categoriaDeProducto` (
  `productId` BIGINT,
  `categoriaId` BIGINT,
  PRIMARY KEY (`productId`, `categoriaId`)
);

CREATE INDEX `products_index_1` ON `products` (`lote`, `fechaCaducidad`);
CREATE INDEX `products_index_3` ON `products` (`fechaCaducidad`);
CREATE INDEX `empleados_index_4` ON `empleados` (`ci`);
CREATE INDEX `empleados_index_5` ON `empleados` (`rol`);

CREATE INDEX `categoriaDeProducto_idx_categoria` ON `categoriaDeProducto` (`categoriaId`);
ALTER TABLE `categoriaDeProducto` ADD FOREIGN KEY (`categoriaId`) REFERENCES `categorias` (`id`) ON DELETE CASCADE;
ALTER TABLE `categoriaDeProducto` ADD FOREIGN KEY (`productId`) REFERENCES `products` (`id`) ON DELETE CASCADE;