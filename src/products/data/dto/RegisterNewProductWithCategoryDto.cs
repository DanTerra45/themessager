using System;
using Dapper;
namespace Mercadito
{ 
    public class RegisterNewProductWithCategoryDto : CreateProductDto 
    {
        public Guid CategoryId{get; set;}
    }
}