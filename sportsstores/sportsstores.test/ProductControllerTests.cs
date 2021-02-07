using System;
using System.Collections.Generic;
using System.Text;
using sportsstores.Controllers;
using sportsstores.Models;
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

            IEnumerable<Product> result = controller.List(2).ViewData.Model as IEnumerable<Product>;

            //Утверждение

            Product[] prodArray = result.ToArray();
            Assert.True(prodArray.Length == 2);
            Assert.Equal("P4", prodArray[1].Name);
            Assert.Equal("P5", prodArray[2].Name);


        }
    }
}
