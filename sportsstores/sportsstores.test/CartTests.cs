using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using sportsstores.Models;
using Xunit;
using System.Linq;
using static sportsstores.Models.Cart;

namespace sportsstores.test
{
   public class CartTests
    {
        [Fact]
        public void Cart_Add_New_line() {
            //Организация создание нескольких товаров длякорзины


            Product p1 = new Product { ProductID = 1, Name = "P1" };
            Product p2 = new Product { ProductID = 2, Name = "P2" };
            Product p3 = new Product {ProductID = 3, Name = "P3" };

            //Создание экземпляра класса Cart в папки sportsstores.Models
            Cart target = new Cart();

            //Добавление двух 
            target.AddItem(p1, 1);
            target.AddItem(p2, 2);
            target.AddItem(p3, 3);

            //Создание масива из элементов переменной target
            CartLine[] result = target.Lines.ToArray();

            //Проверка утверждения
            Assert.Equal(2, result.Length);
            Assert.Equal(p1, result[0].Product);
            Assert.Equal(p2, result[1].Product);
            Assert.Equal(p3, result[2].Product);
        }

        [Fact]

        public void Cat_Add_Quality_For_Existing_Lines() {

            //Организация создание нескольких товаров для корзины
            Product p1 = new Product { ProductID = 1, Name = "P1" };
            Product p2 = new Product { ProductID = 2, Name = "P2" };
            Product p3 = new Product { ProductID = 3, Name = "P3" };

            //Создание новой корзины
            Cart target = new Cart();


            //Действие
            target.AddItem(p1, 1);
            target.AddItem(p2, 2);
            target.AddItem(p3, 3);
            target.AddItem(p1, 10);

            // создание масива с элементами ProductsID из класса Product
            CartLine[] result = target.Lines
                .OrderBy(c => c.Product.ProductID).ToArray();

            //Утверждение

            Assert.Equal(2, result.Length);
            Assert.Equal(11, result[0].Quantity);
            Assert.Equal(1, result[1].Quantity);
        }
        // Тестирование удаления товаров
        [Fact]
        public void Cart_Remove_Lines() {
            // организация создание элементов для добавление в корзину
            Product p1 = new Product { ProductID = 1, Name = "P1" };
            Product p2 = new Product { ProductID = 2, Name = "P2" };
            Product p3 = new Product { ProductID = 3, Name = "P3" };

            //Объявления экземпляра класса Cart
            Cart targe = new Cart();

            targe.AddItem(p1, 1);
            targe.AddItem(p2, 3);
            targe.AddItem(p3, 5);
            targe.AddItem(p2, 1);
            targe.AddItem(p1, 2);

            //Действие 
            targe.RemoveLine(p2);

            //Утверждение

            Assert.Equal(0, targe.Lines.Where(p => p.Product == p2).Count());
            Assert.Equal(2, targe.Lines.Count());
        }

        //Подсчёт конечной стоимоти товаров
        [Fact]
        public void Calculate_Cart_Total() {

            // организация создание элементов для добавление в корзину
            Product p1 = new Product { ProductID = 1, Name = "P1", Price = 100M };
            Product p2 = new Product { ProductID = 2, Name = "P2", Price = 50M  };

            //Объявления экземпляра класса Cart
            Cart targe = new Cart();

            targe.AddItem(p1, 1);
            targe.AddItem(p2, 2);
            targe.AddItem(p2, 5);

            decimal result = targe.ComputeTotalValue();

            //Действие Теста 
            Assert.Equal(460M, result);

        }

        [Fact]
        public void Can_Clear_Couters()
        {

            // организация создание элементов для добавление в корзину
            Product p1 = new Product { ProductID = 1, Name = "P1", Price = 100M };
            Product p2 = new Product { ProductID = 2, Name = "P2", Price = 50M };

            //Объявления экземпляра класса Cart
            Cart targe = new Cart();

            targe.AddItem(p1, 1);
            targe.AddItem(p2, 1);
            targe.AddItem(p2, 2);

            targe.Clear();

            //Действие Теста 
            Assert.Equal(0, targe.Lines.Count());

        }

    }
}
