using Moq;
using sportsstores.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using sportsstores.Components;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace sportsstores.test
{
    
    public class NavigationMenuViewComponentTest
    {

        [Fact]
        public void Can_Select_Controllers() {
            //Организация
            Mock<IProductRepository> mock = new Mock<IProductRepository>();

            mock.Setup(m => m.Products).Returns((new Product[] { 
            new Product{ProductID = 1, Name = "P1", Category = "Apples" },
            new Product{ProductID = 2, Name = "P2", Category = "Apples" },
            new Product{ProductID = 3, Name = "P3", Category = "Plums" },
            new Product{ProductID = 4, Name = "P4", Category = "Oranges" }
            }).AsQueryable<Product>());

            NavigationMenuViewComponent target = 
                new NavigationMenuViewComponent(mock.Object);

            //Действие - получение набора категорий

            string[] result = ((IEnumerable<string>) (target.Invoke() as ViewViewComponentResult)
                .ViewData.Model).ToArray();

            //Утверждение
            Assert.True(Enumerable.SequenceEqual(new string[] { "Apples", "Oranges", "Plums" }, result));
        }
        [Fact]
        public void Indicates_Selected_category()
        {

            string categoryToSelect = "Apples";

            Mock<IProductRepository> mock = new Mock<IProductRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
            new Product{ProductID = 1, Name = "P1", Category = "Apples"},
            new Product{ProductID = 2, Name = "P2", Category = "Oranges"}
            }).AsQueryable<Product>());

            NavigationMenuViewComponent target =
                new NavigationMenuViewComponent(mock.Object);

            //Действие - получение набора категорий

            target.ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext
                {
                    RouteData = new RouteData()

                }


            };

            target.RouteData.Values["category"] = categoryToSelect;

            //Действие
            string result = (string)(target.Invoke() 
                as ViewViewComponentResult).ViewData["SelectedCategory"];

            //Утверждение
            Assert.Equal(categoryToSelect, result);
        }
       
    }
}
