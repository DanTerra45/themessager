using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
{
    public class Category
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // public List<Product>? Products { get; set; }
    }
}