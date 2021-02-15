using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace sportsstores.Models
{
    
    public class EFOrderRepository: IOrderRepository
    {
        private ApplicationDbContext contex;
        public EFOrderRepository(ApplicationDbContext ctx) {
            contex = ctx;
        }
        public IQueryable<Order> Orders => contex.Orders
            .Include(o=>o.Lines)
            .ThenInclude(l => l.Product);

        public void SaveOrder(Order order) {
            contex.AttachRange(order.Lines.Select(l => l.Product));
            if (order.OrderID == 0) {
                contex.Orders.Add(order);
            }
            contex.SaveChanges();
        }

    }
}
