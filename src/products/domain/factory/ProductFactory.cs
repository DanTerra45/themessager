using Mercadito.src.products.data.entity;
using Mercadito.src.products.domain.dto;
using Shared.Domain;
using System;

namespace Mercadito.src.products.domain.factory
{
    public class ProductFactory : IProductFactory
    {
        public Product CreateForInsert(CreateProductDto dto)
        {
            return new Product
            {
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description),
                Stock = dto.Stock.GetValueOrDefault(0),
                Batch = NormalizeBatch(dto.Batch),
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price.GetValueOrDefault(0m),
                CategoryIds = dto.CategoryIds != null ? dto.CategoryIds.AsReadOnly() : Array.Empty<long>()
            };
        }

        public Product CreateForUpdate(UpdateProductDto dto)
        {
            return new Product
            {
                Id = dto.Id,
                Name = NormalizeName(dto.Name),
                Description = NormalizeRequired(dto.Description),
                Stock = dto.Stock.GetValueOrDefault(0),
                Batch = NormalizeBatch(dto.Batch),
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price.GetValueOrDefault(0m),
                CategoryIds = dto.CategoryIds != null ? dto.CategoryIds.AsReadOnly() : Array.Empty<long>()
            };
        }

        public Result<Product> TryCreateForInsert(CreateProductDto dto)
        {
            if (dto == null) return Result<Product>.Failure("Product data is required.");
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result<Product>.Failure("El nombre es obligatorio.");
            if (dto.Name.Length > 150) return Result<Product>.Failure("El nombre no puede exceder 150 caracteres.");
            if (string.IsNullOrWhiteSpace(dto.Description)) return Result<Product>.Failure("La descripción es obligatoria.");
            if (dto.Description.Length > 150) return Result<Product>.Failure("La descripción no puede exceder 150 caracteres.");
            if (string.IsNullOrWhiteSpace(dto.Batch)) return Result<Product>.Failure("Lote es obligatorio.");
            if (dto.Batch.Length > 40) return Result<Product>.Failure("Lote no puede exceder 40 caracteres.");
            if (dto.ExpirationDate == default) return Result<Product>.Failure("La fecha de caducidad es obligatoria.");
            if (dto.Price.HasValue && dto.Price.Value <= 0m) return Result<Product>.Failure("El precio debe ser mayor que cero.");
            if (dto.CategoryIds == null || dto.CategoryIds.Count == 0) return Result<Product>.Failure("Debe seleccionar al menos una categoría.");

            // Validate category ids
            var distinctCategoryIds = new HashSet<long>();
            foreach (var categoryId in dto.CategoryIds)
            {
                if (categoryId <= 0) return Result<Product>.Failure("Las categorías seleccionadas son inválidas.");
                if (!distinctCategoryIds.Add(categoryId)) return Result<Product>.Failure("No puede repetir categorías para el mismo producto.");
            }

            var product = CreateForInsert(dto);
            return Result<Product>.Success(product);
        }

        public Result<Product> TryCreateForUpdate(UpdateProductDto dto)
        {
            if (dto == null) return Result<Product>.Failure("Product data is required.");
            if (dto.Id <= 0) return Result<Product>.Failure("Id de producto inválido.");
            return TryCreateForInsert(new CreateProductDto
            {
                Name = dto.Name,
                Description = dto.Description,
                Stock = dto.Stock,
                Batch = dto.Batch,
                ExpirationDate = dto.ExpirationDate,
                Price = dto.Price,
                CategoryIds = dto.CategoryIds
            });
        }

        private static string NormalizeRequired(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Trim();
        }

        private static string NormalizeName(string value)
        {
            return CollapseSpaces(NormalizeRequired(value));
        }

        private static string NormalizeBatch(string value)
        {
            return NormalizeRequired(value);
        }

        private static string CollapseSpaces(string value)
        {
            var segments = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', segments);
        }
    }
}
