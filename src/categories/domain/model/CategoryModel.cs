using Dapper;

namespace Mercadito
{
    public class CategoryModel: Category
    {
        public int ProductCount { get; set; }
        public CategoryModel() : base()
        {
        }
        
    }
}