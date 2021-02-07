using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SportsStore.Models;

namespace SportsStore.Models.DB
{
    public class EFproductRepository:IProductRepository
    {
        private ApplicationDBContext context;

        public EFproductRepository(ApplicationDBContext ctx) {
            context = ctx;
        }

        public IQueryable<Product> Products => context.Products;

      }
}
