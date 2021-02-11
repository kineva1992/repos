using System;
using System.Collections.Generic;
using System.Text;
using sportsstores.Controllers;
using sportsstores.Models;
using sportsstores.Models.ViewModels;
using Moq;
using Xunit;
using System.Linq;

namespace sportsstores.test
{
    public class ProductControllerTests
    {
        [Fact]
        public void Can_Paginate() {
            //Организация

            Mock<IProductRepository> moq = new Mock<IProductRepository>();

            moq.Setup(m => m.Products).Returns((new Product[] {
            new Product{ProductID = 1, Name= "P1" },
            new Product{ProductID = 2, Name = "P2" },
            new Product{ProductID = 3, Name = "P3" },
            new Product {ProductID = 4, Name = "P4" }
            }).AsQueryable<Product>());

            ProductController controller = new ProductController(moq.Object);
            controller.PageSize = 3;

            //Действие
            ProductsListViewModel result = 
                controller.List(null,2).ViewData.Model as ProductsListViewModel;

            //Утверждение

            Product[] prodArray = result.products.ToArray();
            Assert.True(prodArray.Length == 2);
            Assert.Equal("P4", prodArray[1].Name);
            Assert.Equal("P5", prodArray[2].Name);
        }

        [Fact]
        public void Can_Send_View_Model() {
            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] { 
            new Product{ProductID = 2, Name = "P2" },
            new Product{ProductID = 3, Name = "P3" },
            new Product{ProductID = 4, Name = "P4" },
            new Product{ProductID = 5, Name = "P5" }
            }).AsQueryable<Product>());

            //Организация

            ProductController controller = 
                new ProductController(mock.Object) { PageSize = 3 };

            //Действие
            ProductsListViewModel result = 
                controller.List(null, 2).ViewData.Model as ProductsListViewModel;
            //Утверждение
            PadingInfo PageInfo = result.padingInfo;
            Assert.Equal(2, PageInfo.CurentPage);
            Assert.Equal(3, PageInfo.ItemPerPage);
            Assert.Equal(5, PageInfo.TotalItems);
            Assert.Equal(2, PageInfo.TotalPages);
        }

        [Fact]
        public void Can_Filtr_Products() {
            //Организация - создание имитационного хранилища
            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] { 
            new Product{ProductID = 1, Name = "P1", Category ="Cat1"},
            new Product{ProductID = 2, Name = "P2", Category ="Cat2"},
            new Product{ProductID = 3, Name = "P3", Category ="Cat3"},
            new Product{ProductID = 4, Name = "P4", Category ="Cat4"},
            new Product{ProductID = 5, Name = "P5", Category ="Cat5"}
            }).AsQueryable<Product>());

            //Организация и создание контроллера с размером в 3 элемента.
            ProductController controller = new ProductController(mock.Object) { PageSize = 3 };

            //Действие 

            Product[] result = 
                (controller.List("Cat2", 1).ViewData.Model as ProductsListViewModel).products.ToArray();

            //Утверждение 
            Assert.Equal(2, result.Length);
            Assert.True(result[0].Name == "P2" && result[0].Category == "Cat2");
            Assert.True(result[1].Name == "P4" && result[1].Category == "Cat2");
        }
    }
}
