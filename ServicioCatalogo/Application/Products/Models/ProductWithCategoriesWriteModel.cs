using ServicioCatalogo.Domain.Products.Entities;

namespace ServicioCatalogo.Application.Products.Models
{
    public class ProductWithCategoriesWriteModel
    {
        public required Product Product { get; set; }
        public required IReadOnlyList<long> CategoryIds { get; set; }
    }
}

