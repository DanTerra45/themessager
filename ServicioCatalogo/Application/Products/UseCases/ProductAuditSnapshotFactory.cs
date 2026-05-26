using ServicioCatalogo.Application.Products.Models;
using ServicioCatalogo.Domain.Products.Entities;

namespace ServicioCatalogo.Application.Products.UseCases
{
    internal static class ProductAuditSnapshotFactory
    {
        public static object BuildCreatedSnapshot(Product product)
        {
            ArgumentNullException.ThrowIfNull(product);

            return new Dictionary<string, object?>
            {
                ["Precio"] = product.Price
            };
        }

        public static object BuildDeletedSnapshot(ProductForEditModel product)
        {
            ArgumentNullException.ThrowIfNull(product);

            return new Dictionary<string, object?>
            {
                ["Precio"] = product.Price
            };
        }

        public static (object? PreviousData, object? NewData)? BuildImportantUpdateSnapshot(
            ProductForEditModel? previousProduct,
            Product updatedProduct)
        {
            ArgumentNullException.ThrowIfNull(updatedProduct);

            if (previousProduct == null)
            {
                var fallbackNewData = new Dictionary<string, object?>
                {
                    ["Precio"] = updatedProduct.Price
                };
                return (null, fallbackNewData);
            }

            if (previousProduct.Price == updatedProduct.Price)
            {
                return null;
            }

            var previousData = new Dictionary<string, object?>
            {
                ["Precio"] = previousProduct.Price
            };

            var newData = new Dictionary<string, object?>
            {
                ["Precio"] = updatedProduct.Price
            };

            return (previousData, newData);
        }
    }
}

