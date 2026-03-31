-- Mercadito query benchmark
-- Usage:
--   mysql -u root -p < schema/explain_benchmark.sql

SET NAMES utf8mb4;

USE app_test;

SELECT @@version AS mysql_version;
SELECT DATABASE() AS current_database;

-- ------------------------------------------------------------
-- Parameters
-- ------------------------------------------------------------
SET @active_state := 'A';
SET @page_size := 10;
SET @offset := 0;

SET @search_term := 'aceite';
SET @search_pattern := CONCAT(
    '%',
    REPLACE(
        REPLACE(
            REPLACE(@search_term, '\\', '\\\\'),
            '%',
            '\\%'
        ),
        '_',
        '\\_'
    ),
    '%'
);

SET @category_id := COALESCE(
    (SELECT id FROM categorias WHERE estado = @active_state ORDER BY id LIMIT 1),
    -1
);

SET @product_id := COALESCE(
    (SELECT id FROM products WHERE estado = @active_state ORDER BY id LIMIT 1),
    -1
);

SET @employee_ci := COALESCE(
    (SELECT ci FROM empleados WHERE estado = @active_state ORDER BY id LIMIT 1),
    5000000
);

SET @employee_complemento := COALESCE(
    (SELECT complementoNormalizado FROM empleados WHERE estado = @active_state ORDER BY id LIMIT 1),
    ''
);

SET @exclude_employee_id := COALESCE(
    (SELECT id FROM empleados WHERE estado = @active_state ORDER BY id LIMIT 1),
    0
);

SELECT
    @active_state AS active_state,
    @page_size AS page_size,
    @offset AS page_offset,
    @search_term AS search_term,
    @category_id AS category_id,
    @product_id AS product_id,
    @employee_ci AS employee_ci,
    @employee_complemento AS employee_complemento,
    @exclude_employee_id AS exclude_employee_id;

-- ------------------------------------------------------------
-- PRODUCTS
-- ------------------------------------------------------------

SELECT 'products:list_ids:no_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT p.id
FROM products p
WHERE p.estado = @active_state
ORDER BY p.nombre ASC, p.id ASC
LIMIT 10 OFFSET 0;

SELECT 'products:list_details:no_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    p.id AS Id,
    p.nombre AS Name,
    p.descripcion AS Description,
    p.stock AS Stock,
    p.lote AS Batch,
    p.fechaCaducidad AS ExpirationDate,
    p.precio AS Price,
    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') AS CategoriesString
FROM products p
LEFT JOIN categoriaDeProducto pc ON p.id = pc.productId
LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @active_state
WHERE p.estado = @active_state
AND p.id IN (
    SELECT paged.id
    FROM (
        SELECT p.id
        FROM products p
        WHERE p.estado = @active_state
        ORDER BY p.nombre ASC, p.id ASC
        LIMIT 10 OFFSET 0
    ) paged
)
GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio;

SELECT 'products:list_ids:with_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT p.id
FROM products p
WHERE p.estado = @active_state
AND p.id IN (
    SELECT matched.ProductId
    FROM (
        SELECT p2.id AS ProductId
        FROM products p2
        WHERE p2.estado = @active_state
        AND p2.nombre LIKE @search_pattern ESCAPE '\\'
        UNION
        SELECT pc2.productId AS ProductId
        FROM categoriaDeProducto pc2
        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
        WHERE c2.estado = @active_state
        AND c2.nombre LIKE @search_pattern ESCAPE '\\'
    ) matched
)
ORDER BY p.nombre ASC, p.id ASC
LIMIT 10 OFFSET 0;

SELECT 'products:list_details:with_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    p.id AS Id,
    p.nombre AS Name,
    p.descripcion AS Description,
    p.stock AS Stock,
    p.lote AS Batch,
    p.fechaCaducidad AS ExpirationDate,
    p.precio AS Price,
    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') AS CategoriesString
FROM products p
LEFT JOIN categoriaDeProducto pc ON p.id = pc.productId
LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @active_state
WHERE p.estado = @active_state
AND p.id IN (
    SELECT paged.id
    FROM (
        SELECT p.id
        FROM products p
        WHERE p.estado = @active_state
        AND p.id IN (
            SELECT matched.ProductId
            FROM (
                SELECT p2.id AS ProductId
                FROM products p2
                WHERE p2.estado = @active_state
                AND p2.nombre LIKE @search_pattern ESCAPE '\\'
                UNION
                SELECT pc2.productId AS ProductId
                FROM categoriaDeProducto pc2
                INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
                WHERE c2.estado = @active_state
                AND c2.nombre LIKE @search_pattern ESCAPE '\\'
            ) matched
        )
        ORDER BY p.nombre ASC, p.id ASC
        LIMIT 10 OFFSET 0
    ) paged
)
GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio;

SELECT 'products:list_by_category_ids:no_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT p.id
FROM products p
INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
WHERE pc.categoriaId = @category_id
AND p.estado = @active_state
ORDER BY p.nombre ASC, p.id ASC
LIMIT 10 OFFSET 0;

SELECT 'products:list_by_category_details:no_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    p.id AS Id,
    p.nombre AS Name,
    p.descripcion AS Description,
    p.stock AS Stock,
    p.lote AS Batch,
    p.fechaCaducidad AS ExpirationDate,
    p.precio AS Price,
    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') AS CategoriesString
FROM products p
LEFT JOIN categoriaDeProducto pc ON p.id = pc.productId
LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @active_state
WHERE p.estado = @active_state
AND p.id IN (
    SELECT paged.id
    FROM (
        SELECT p.id
        FROM products p
        INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
        WHERE pc.categoriaId = @category_id
        AND p.estado = @active_state
        ORDER BY p.nombre ASC, p.id ASC
        LIMIT 10 OFFSET 0
    ) paged
)
GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio;

SELECT 'products:list_by_category_ids:with_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT p.id
FROM products p
INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
WHERE pc.categoriaId = @category_id
AND p.estado = @active_state
AND p.id IN (
    SELECT matched.ProductId
    FROM (
        SELECT p2.id AS ProductId
        FROM products p2
        WHERE p2.estado = @active_state
        AND p2.nombre LIKE @search_pattern ESCAPE '\\'
        UNION
        SELECT pc2.productId AS ProductId
        FROM categoriaDeProducto pc2
        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
        WHERE c2.estado = @active_state
        AND c2.nombre LIKE @search_pattern ESCAPE '\\'
    ) matched
)
ORDER BY p.nombre ASC, p.id ASC
LIMIT 10 OFFSET 0;

SELECT 'products:list_by_category_details:with_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    p.id AS Id,
    p.nombre AS Name,
    p.descripcion AS Description,
    p.stock AS Stock,
    p.lote AS Batch,
    p.fechaCaducidad AS ExpirationDate,
    p.precio AS Price,
    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') AS CategoriesString
FROM products p
LEFT JOIN categoriaDeProducto pc ON p.id = pc.productId
LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @active_state
WHERE p.estado = @active_state
AND p.id IN (
    SELECT paged.id
    FROM (
        SELECT p.id
        FROM products p
        INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
        WHERE pc.categoriaId = @category_id
        AND p.estado = @active_state
        AND p.id IN (
            SELECT matched.ProductId
            FROM (
                SELECT p2.id AS ProductId
                FROM products p2
                WHERE p2.estado = @active_state
                AND p2.nombre LIKE @search_pattern ESCAPE '\\'
                UNION
                SELECT pc2.productId AS ProductId
                FROM categoriaDeProducto pc2
                INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
                WHERE c2.estado = @active_state
                AND c2.nombre LIKE @search_pattern ESCAPE '\\'
            ) matched
        )
        ORDER BY p.nombre ASC, p.id ASC
        LIMIT 10 OFFSET 0
    ) paged
)
GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio;

SELECT 'products:count:no_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(*)
FROM products p
WHERE p.estado = @active_state;

SELECT 'products:count:with_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(*)
FROM products p
WHERE p.estado = @active_state
AND p.id IN (
    SELECT matched.ProductId
    FROM (
        SELECT p2.id AS ProductId
        FROM products p2
        WHERE p2.estado = @active_state
        AND p2.nombre LIKE @search_pattern ESCAPE '\\'
        UNION
        SELECT pc2.productId AS ProductId
        FROM categoriaDeProducto pc2
        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
        WHERE c2.estado = @active_state
        AND c2.nombre LIKE @search_pattern ESCAPE '\\'
    ) matched
);

SELECT 'products:count_by_category:no_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(DISTINCT p.id)
FROM products p
INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
WHERE pc.categoriaId = @category_id
AND p.estado = @active_state;

SELECT 'products:count_by_category:with_search' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(DISTINCT p.id)
FROM products p
INNER JOIN categoriaDeProducto pc ON p.id = pc.productId
WHERE pc.categoriaId = @category_id
AND p.estado = @active_state
AND p.id IN (
    SELECT matched.ProductId
    FROM (
        SELECT p2.id AS ProductId
        FROM products p2
        WHERE p2.estado = @active_state
        AND p2.nombre LIKE @search_pattern ESCAPE '\\'
        UNION
        SELECT pc2.productId AS ProductId
        FROM categoriaDeProducto pc2
        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
        WHERE c2.estado = @active_state
        AND c2.nombre LIKE @search_pattern ESCAPE '\\'
    ) matched
);

SELECT 'products:get_by_id_for_edit' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    p.id AS Id,
    p.nombre AS Name,
    p.descripcion AS Description,
    p.stock AS Stock,
    p.lote AS Batch,
    p.fechaCaducidad AS ExpirationDate,
    p.precio AS Price,
    COALESCE(GROUP_CONCAT(DISTINCT cp.categoriaId ORDER BY cp.categoriaId SEPARATOR ','), '') AS CategoryIdsString
FROM products p
LEFT JOIN categoriaDeProducto cp ON p.id = cp.productId
WHERE p.id = @product_id
AND p.estado = @active_state
GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio;

-- ------------------------------------------------------------
-- CATEGORIES
-- ------------------------------------------------------------

SELECT 'categories:get_all_active' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    id AS Id,
    codigo AS Code,
    nombre AS Name,
    descripcion AS Description,
    0 AS ProductCount
FROM categorias
WHERE estado = @active_state
ORDER BY nombre ASC;

SELECT 'categories:get_page_default_sort' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    c.id AS Id,
    c.codigo AS Code,
    c.nombre AS Name,
    c.descripcion AS Description,
    COUNT(DISTINCT p.id) AS ProductCount
FROM categorias c
LEFT JOIN categoriaDeProducto cp ON c.id = cp.categoriaId
LEFT JOIN products p ON cp.productId = p.id AND p.estado = @active_state
WHERE c.estado = @active_state
GROUP BY c.id, c.codigo, c.nombre, c.descripcion
ORDER BY c.nombre ASC, c.id ASC
LIMIT 10 OFFSET 0;

SELECT 'categories:count_active' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(*)
FROM categorias
WHERE estado = @active_state;

SELECT 'categories:next_code_preview' AS benchmark_case;
EXPLAIN ANALYZE
SELECT codigo
FROM categorias
ORDER BY codigo DESC
LIMIT 1;

-- ------------------------------------------------------------
-- EMPLOYEES
-- ------------------------------------------------------------

SELECT 'employees:get_page_default_sort' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    id AS Id,
    ci AS Ci,
    complemento AS Complemento,
    nombres AS Nombres,
    primerApellido AS PrimerApellido,
    segundoApellido AS SegundoApellido,
    rol AS Rol,
    numeroContacto AS NumeroContacto
FROM empleados
WHERE estado = @active_state
ORDER BY primerApellido ASC, segundoApellido ASC, nombres ASC, id ASC
LIMIT 10 OFFSET 0;

SELECT 'employees:count_active' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(*)
FROM empleados
WHERE estado = @active_state;

SELECT 'employees:get_by_id' AS benchmark_case;
EXPLAIN ANALYZE
SELECT
    id AS Id,
    ci AS Ci,
    complemento AS Complemento,
    nombres AS Nombres,
    primerApellido AS PrimerApellido,
    segundoApellido AS SegundoApellido,
    rol AS Rol,
    numeroContacto AS NumeroContacto
FROM empleados
WHERE id = @exclude_employee_id
AND estado = @active_state;

SELECT 'employees:identity_count:create_flow' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(*)
FROM empleados
WHERE ci = @employee_ci
AND complementoNormalizado = @employee_complemento
AND activoUnico = 1;

SELECT 'employees:identity_count:update_flow' AS benchmark_case;
EXPLAIN ANALYZE
SELECT COUNT(*)
FROM empleados
WHERE ci = @employee_ci
AND complementoNormalizado = @employee_complemento
AND activoUnico = 1
AND id <> @exclude_employee_id;
