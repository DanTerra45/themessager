using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mercadito
{
    public class CreateProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public DateTime Lote{ get; set; }
        public DateTime FechaDeCaducidad { get; set; }
        public decimal Price { get; set; }
        public CreateProductDto() { }
        public CreateProductDto(string name, string description, int stock, DateTime lote, DateTime fechaDeCaducidad, decimal price)
        {
            Name = name;
            Description = description;
            Stock = stock;
            Lote = lote;
            FechaDeCaducidad = fechaDeCaducidad;
            Price = price;
        }
    }
}

