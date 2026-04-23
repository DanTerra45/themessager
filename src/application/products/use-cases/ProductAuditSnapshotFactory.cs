using Mercadito.src.application.products.models;
using Mercadito.src.domain.products.entities;

namespace Mercadito.src.application.products.usecases
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
