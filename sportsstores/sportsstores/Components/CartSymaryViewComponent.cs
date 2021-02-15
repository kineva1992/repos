
//using Microsoft.AspNetCore.Mvc;
//using sportsstores.Models;

//namespace sportsstores.Components
//{
//    public class CartSymaryViewComponent : ViewComponent
//    {
//        private Cart cart;

//        public CartSymaryViewComponent(Cart cartService)
//        {
//            cart = cartService;
//        }

//        public IViewComponentResult Invoke() {
//            return View(cart);
//        }

//    }
//}

using Microsoft.AspNetCore.Mvc;
using sportsstores.Models;

namespace SportsStore.Components
{

    public class CartSummaryViewComponent : ViewComponent
    {
        private Cart cart;

        public CartSummaryViewComponent(Cart cartService)
        {
            cart = cartService;
        }

        public IViewComponentResult Invoke()
        {
            return View(cart);
        }
    }
}