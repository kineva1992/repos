using System.Linq;
using Microsoft.AspNetCore.Mvc;
using sportsstores.Models;
using Microsoft.AspNetCore.Authorization;

namespace sportsstores.Controllers
{
    public class OrderController : Controller
    {
        private IOrderRepository orderRepository;
        private Cart cart;

        public OrderController(IOrderRepository repoServices, Cart cartServices) {
            orderRepository = repoServices;
            cart = cartServices;
        }
       [Authorize]
        public ViewResult List() =>
           View(orderRepository.Orders.Where(o => !o.Shiped));
        [HttpPost]
        [Authorize]
        public IActionResult MarkShipped(int orderID)
        {
            Order order = orderRepository.Orders
                .FirstOrDefault(o => o.OrderID == orderID);
            if (order != null)
            {
                order.Shiped = true;
                orderRepository.SaveOrder(order);
            }
            return RedirectToAction(nameof(List));
        }

        public ViewResult Checkout() => View(new Order());
        [HttpPost]
        public IActionResult Checkout(Order order) {
            //Если карзина пуста
            if (cart.Lines.Count() == 0) {
                ModelState.AddModelError("", "Ваша карзина пуста");
            }

            if (ModelState.IsValid)
            {
                order.Lines = cart.Lines.ToArray();
                orderRepository.SaveOrder(order);
                return RedirectToAction(nameof(Complated));
            }

            else {
                return View(order);
            }
        }


        public ViewResult Complated() {
            cart.Clear();
            return View();
        }
    }
}
