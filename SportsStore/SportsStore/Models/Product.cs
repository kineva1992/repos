using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportsStore.Models
{
    public class Product
    {
        public int ProductId { set; get; }
        public string Name { set; get; }
        public string Discription { set; get; }
        public Decimal Price { set; get; }
        public string Category { set; get; }
    }
}
