namespace Mercadito.src.products.data.entity
{
    public class ProductCategory
    {
        public long ProductId { get; set; }
        public long CategoryId { get; set; }

        public ProductCategory() { }

        public ProductCategory(long productId, long categoryId)
        {
            ProductId = productId;
            CategoryId = categoryId;
        }
    }
}
