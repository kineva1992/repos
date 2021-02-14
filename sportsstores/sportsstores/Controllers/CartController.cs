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
        private Cart carts;

        public CartController(IProductRepository repo, Cart cartServices)
        {
            repository = repo;
            carts = cartServices;
        }

        public ViewResult Index(string returnUrl)
        {
            return View(new CartIndexViewModel
            {
                Cart = carts,
                ReturnUrl = returnUrl
            });

        }
        //Создание метода логики для функции добавлении в корзину
        public RedirectToActionResult AddToCart(int productId, string returnUrl)
        {

            Product product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);
            if (product != null)
            {
                carts.AddItem(product, 1);
                
            }
            return RedirectToAction("Index", new { returnUrl });

        }
        //Создание метода логигики удаление из карзины
        public RedirectToActionResult RemoveFromCart(int productId, string returnUrl)
        {
            Product product = repository.Products
                    .FirstOrDefault(p => p.ProductID == productId);
            if (product != null)
            {
                carts.RemoveLine(product);
            }
            return RedirectToAction("Index", new { returnUrl });
        }

        // Создание внутренего метода GetCart
        private Cart GetCart()
        {
            Cart cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();
            return cart;
        }

        // Создание внутренего метода SaveCart
        private void SaveCart(Cart cart)
        {
            HttpContext.Session.SetJson("Cart", cart);
        }

    }
}

//using System.Linq;
//using Microsoft.AspNetCore.Mvc;
//using sportsstores.Models;
//using sportsstores.Models.ViewModels;

//namespace sportsstores.Controllers
//{

//    public class CartController : Controller
//    {
//        private IProductRepository repository;
//        private Cart cart;

//        public CartController(IProductRepository repo, Cart cartService)
//        {
//            repository = repo;
//            cart = cartService;
//        }

//        public ViewResult Index(string returnUrl)
//        {
//            return View(new CartIndexViewModel
//            {
//                Cart = cart,
//                ReturnUrl = returnUrl
//            });
//        }

//        public RedirectToActionResult AddToCart(int productId, string returnUrl)
//        {
//            Product product = repository.Products
//                .FirstOrDefault(p => p.ProductID == productId);
//            if (product != null)
//            {
//                cart.AddItem(product, 1);
//            }
//            return RedirectToAction("Index", new { returnUrl });
//        }

//        public RedirectToActionResult RemoveFromCart(int productId,
//                string returnUrl)
//        {
//            Product product = repository.Products
//                .FirstOrDefault(p => p.ProductID == productId);

//            if (product != null)
//            {
//                cart.RemoveLine(product);
//            }
//            return RedirectToAction("Index", new { returnUrl });
//        }
//    }
//}
