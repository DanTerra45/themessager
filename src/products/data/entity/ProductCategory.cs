using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
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