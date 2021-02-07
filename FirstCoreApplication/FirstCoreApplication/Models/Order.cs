using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCoreApplication.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string User { set; get; }
        public string Address { set; get; }
        public string ContactPhone { get; set; }


        public int PhoneId { get; set; }
        public Phone Phone { get; set; }

    }
}
