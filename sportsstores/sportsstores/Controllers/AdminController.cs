using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using sportsstores.Models;
using Microsoft.AspNetCore.Authorization;

namespace sportsstores.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private IProductRepository repository;

        public AdminController(IProductRepository prod) {
            repository = prod;
        }
        //Реализация функции изменить
        public ViewResult Index() => View(repository.Products);

        public ViewResult Edit(int productId) =>
            View(repository.Products
                .FirstOrDefault(e => e.ProductID == productId));
        [HttpPost]
        public IActionResult Edit(Product product) {
            if (ModelState.IsValid)
            {
                repository.SaveProduct(product);
                TempData["message"] = $"{product.Name} has been saved";
                return RedirectToAction("Index");
            }
            else {
                return View(product);
            }
        }
        // Создание нового товара

        public ViewResult Create() => View("Edit", new Product());

        [HttpPost]
        public IActionResult Delete(int prodictID) {
            Product deletedProduct = repository.DeleteProduct(prodictID);
            if (deletedProduct != null)
            {
                TempData["message"] = $"{deletedProduct.Name} was deleted";
            }
            

            return RedirectToAction("Index");
        }



    }
}
