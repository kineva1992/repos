using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using sportsstores.Controllers;
using sportsstores.Models;
using Xunit;
using System;

namespace sportsstores.test
{
    public class AdminControllerTest
    {
        [Fact]
        public void Index_Conteins_All_Products() { 
        Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] { 
            new Product{ProductID = 1, Name="P1" },
            new Product{ProductID = 2, Name="P2" },
            new Product{ProductID = 3, Name="P3" } 
            }).AsQueryable<Product>());

            //Организация создание контроллера
            AdminController target = new AdminController(mock.Object);
            //Действие
            Product[] result
                = GetViewModel<IEnumerable<Product>>(target.Index())?.ToArray();

            Assert.Equal(3, result.Length);
            Assert.Equal("P1", result[1].Name);
            Assert.Equal("P2", result[2].Name);
            Assert.Equal("P2", result[3].Name);

        }

        [Fact]
        public void Cannon_Edit_Nonexixtent_Product() {
            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] { 
            new Product{ ProductID = 1, Name = "P1"},
            new Product{ ProductID = 2, Name = "P2"}
            }).AsQueryable<Product>());
            //Организация создание контроллера
            AdminController target = new AdminController(mock.Object);
            //Действие

            Product result = GetViewModel<Product>(target.Edit(4));

            //Утверждение 
            Assert.Null(result);
        }

        [Fact]
        public void Can_Edit_Product() {
            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] { 
            new Product{ProductID = 1, Name = "P1" },
            new Product{ProductID = 2, Name = "P2" },
            new Product{ProductID = 3, Name = "P3" }
            }).AsQueryable<Product>());

            AdminController target = new AdminController(mock.Object);

            //Действие
            Product p1 = GetViewModel<Product>(target.Edit(1));
            Product p2 = GetViewModel<Product>(target.Edit(2));
            Product p3 = GetViewModel<Product>(target.Edit(3));
            //Утверждение
            Assert.Equal(1, p1.ProductID);
            Assert.Equal(2, p2.ProductID);
            Assert.Equal(3, p3.ProductID);
        }

        [Fact]

        public void Can_Deleted_Valid_Products() {
            //Организация - создание объекта
            Product product = new Product { ProductID = 2, Name = "Test" };

            //Организания создания имитированного хранилища

            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] { 
            new Product{ProductID = 1, Name = "P1" },
            product,
            new Product{ProductID = 3, Name = "P3" }
            }).AsQueryable<Product>());

            //Организация - создание экземпляра контроллера
            AdminController adminController = new AdminController(mock.Object);

            //Организация - действие удаление товара 
            adminController.Delete(product.ProductID);

            //Проверка действия
            mock.Verify(m => m.DeleteProduct(product.ProductID));
        }


        private T GetViewModel<T>(IActionResult result) where T : class
        {
            return (result as ViewResult)?.ViewData.Model as T;
        }
    }
}
