using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCoreApplication.Models
{
    public class SampleData
    {
        public static void Initialize(MobileContext context) {
            if (!context.Phones.Any()) {
                context.Phones.AddRange(
                    new Phone { 
                    Name = "iPhone",
                    Company = "Apple",
                    Price = 600
                    },

                    new Phone {
                    Name = "Samsung Galaxy Edge",
                    Company = "Samsung",
                    Price = 550
                    },

                    new Phone { 
                    Name = "Pixel 3",
                    Company = "Google",
                    Price = 500
                    }
                    );
                context.SaveChanges();
            }
        }

    }
}
