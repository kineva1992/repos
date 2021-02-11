using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sportsstores.Models;
using sportsstores.Models.ViewModels;

namespace sportsstores.Controllers
{
    public class ProductController : Controller
    {
        private IProductRepository repository;
        public int PageSize = 4;
        public ProductController(IProductRepository repo) {
            repository = repo;
        }
    
        public ViewResult List(string category ,int productPage = 1)
           => View(new ProductsListViewModel
           {
               products = repository.Products
                   .Where(p=> category == null || p.Category == category)
                   .OrderBy(p => p.ProductID)
                   .Skip((productPage - 1) * PageSize)
                   .Take(PageSize),
               padingInfo = new PadingInfo
               {
                   CurentPage = productPage,
                   ItemPerPage = PageSize,
                   TotalItems = repository.Products.Count()
               },
               CurrentCategory = category
           });


    } 
    }
