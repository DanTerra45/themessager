using System;
using Dapper;
namespace Mercadito
{ 
    public class RegisterNewProductWithCategoryDto : CreateProductDto 
    {
        public long CategoryId{get; set;}
    }
}