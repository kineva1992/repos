using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sportsstores.Models.ViewModels
{
    public class ProductsListViewModel
    {
        public IEnumerable<Product> products { get; set; }
        public PadingInfo padingInfo { get; set; }
        public string CurrentCategory { get; set; }

    }
}
