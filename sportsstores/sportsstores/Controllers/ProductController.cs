﻿using Microsoft.AspNetCore.Mvc;
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
        //public ViewResult List() => View(repository.Products);

        //public ViewResult List(int ProductPage = 1)

            //=> View(new ProductsListViewModel { 
            //    repository.Products.
            //    OrderBy(p => p.ProductID).
            //    Skip((ProductPage - 1) * PageSize).
            //    Take(PageSize),

            //    PadingInfo = new PadingInfo{
            //    CurentPage = ProductPage,
            //    ItemPerPage = PageSize,
            //    TotalItems = repository.Products.Count()
            //    }
            //   });

        public ViewResult List(int productPage = 1)
           => View(new ProductsListViewModel
           {
               products = repository.Products
                   .OrderBy(p => p.ProductID)
                   .Skip((productPage - 1) * PageSize)
                   .Take(PageSize),
               padingInfo = new PadingInfo
               {
                   CurentPage = productPage,
                   ItemPerPage = PageSize,
                   TotalItems = repository.Products.Count()
               }
           });


    } 
    }
