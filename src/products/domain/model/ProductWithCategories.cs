using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
{
    public class ProductWithCategoriesModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public DateTime Lote{ get; set; }
        public DateTime FechaDeCaducidad { get; set; }
        public decimal Price { get; set; }
        public List<string> Categories { get; set; }
        public ProductWithCategoriesModel() { }
        public ProductWithCategoriesModel(Guid id, string name, string description, int stock, DateTime lote, DateTime fechaDeCaducidad, decimal price)
        {
            Id = id;
            Name = name;
            Description = description;
            Stock = stock;
            Lote = lote;
            FechaDeCaducidad = fechaDeCaducidad;
            Price = price;
        }
    }
}