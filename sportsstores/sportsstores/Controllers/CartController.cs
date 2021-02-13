using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using sportsstores.Models;
using sportsstores.Models.ViewModels;
using sportsstores.Infrastructure;

namespace sportsstores.Controllers
{
    public class CartController : Controller
    {
        private IProductRepository repository;

        public CartController(IProductRepository repo) {
            repository = repo;
        }

        public ViewResult Index(string returnUrl) {
            return View(new CartIndexViewModel { 
            cart = GetCart(),
            ReturnUrl = returnUrl
            });
        
        }
        //Создание метода логики для функции добавлении в корзину
        public RedirectToActionResult AddToCart(int productId, string returnUrl) {

            Product product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);
            if (product != null) {
                Cart cart = GetCart();
                cart.AddItem(product, 1);
                SaveCart(cart);
            }
            return RedirectToAction("Index", new { returnUrl});
 
        }
        //Создание метода логигики удаление из карзины
        public RedirectToActionResult RemoveFromCart(int productId, string returnUrl) {
            Product product = repository.Products
                    .FirstOrDefault(p => p.ProductID == productId);
            if (product != null)
            {
                Cart cart = GetCart();
                cart.RemoveLine(product);
                SaveCart(cart);
            }
            return RedirectToAction("Index", new { returnUrl });
        }

        // Создание внутренего метода GetCart
        private Cart GetCart() {
            Cart cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();
            return cart;
        }

        // Создание внутренего метода SaveCart
        private void SaveCart(Cart cart) {
            HttpContext.Session.SetJson("Cart", cart);
        }

    }
}
