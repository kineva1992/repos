
using Microsoft.AspNetCore.Mvc;
using sportsstores.Models;

namespace sportsstores.Components
{
    public class CartSymaryViewComponent : ViewComponent
    {
        private Cart cart;

        public void CartSumaryViewComponent(Cart cartServices) {
            cart = cartServices;
        }

        public IViewComponentResult Invike() {
            return View(cart);
        }

    }
}
