using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static sportsstores.Models.Cart;

namespace sportsstores.Models
{
    public class Order
    {
        [BindNever]
        public int OrderID { get; set; }
        [BindNever]
        public ICollection<CartLine> Lines { get; set; }
        public string Name { get; set; }
        [Required(ErrorMessage = "Введите ваше имя")]
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string Line3 { get; set; }
        [Required(ErrorMessage = "Введите название вашего города")]
        public string City { get; set; }
        [Required(ErrorMessage = "Введите название вашего штата")]
        public string State { get; set; }
        public string Zip { get; set; }
        [Required(ErrorMessage = "Введите название вашей страны")]
        public string Country { get; set; }
        public bool GiftWrap { get; set; }

    }
}
