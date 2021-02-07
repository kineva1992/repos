using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SportsStore.Models;

namespace SportsStore.Controllers
{
    public class ProductController : Controller
    {
        //Создание закрытого экземпляра интерфейса IProductRepository
        private IProductRepository repository;

        // создание открытого экземпляра интерфейса 
        public ProductController(IProductRepository repo) {
            repository = repo; 
        }

        public ViewResult List() => View(repository.Products);
        
    }
}
