﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using sportsstores.Models;

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
        //public ViewResult List() =>
        //    View(orderRepository.Orders.Where(o=> !o.Shiped));

        //[HttpPost]
        //public IActionResult MarkShiped(int orderID) {
        //    Order order = orderRepository.Orders
        //        .FirstOrDefault(o => o.OrderID == orderID);

        //    if (order != null) {
        //        order.Shiped = true;
        //        orderRepository.SaveOrder(order);
        //    }
        //    return RedirectToAction(nameof(List));
        //}
        public ViewResult List() =>
           View(orderRepository.Orders.Where(o => !o.Shiped));
        [HttpPost]
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
