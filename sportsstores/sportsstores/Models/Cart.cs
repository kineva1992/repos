using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sportsstores.Models
{
    public class Cart
    {
        private List<CartLine> LineCollection = new List<CartLine>();

        public virtual void AddItem(Product product, int quality) {
            CartLine line = LineCollection
                .Where(p => p.Product.ProductID == product.ProductID)
                .FirstOrDefault();
            if (line == null) {
                LineCollection.Add(new CartLine {
                Product = product,
                Quantity = quality
                });  
            }
            else{
                line.Quantity += quality;
            }
        }
        //Удаление из корзины
        public virtual void RemoveLine(Product product) =>
            LineCollection.RemoveAll(l => l.Product.ProductID == product.ProductID);
        //Подсчёт стоимости товаров
        public virtual decimal ComputeTotalValue() =>
            LineCollection.Sum(l => l.Product.Price * l.Quantity);
        //Очистить корзину
        public virtual void Clear() =>
            LineCollection.Clear();
        //Получение списка последовательного спика из CartLine
        public virtual IEnumerable<CartLine> Lines => LineCollection;

        //Класс для создании корзины
        public class CartLine{
            public int CartLineID { get; set; }
            public Product Product { get; set; }
            public int Quantity { get; set; }

}
    }
}
