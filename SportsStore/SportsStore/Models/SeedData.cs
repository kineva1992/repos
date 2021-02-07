//using System.Linq;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;

//namespace SportsStore.Models
//{
//    public static class SeedData
//    {
//        public static void EnsurePopulated(IApplicationBuilder app)
//        {
//            ApplicationDBContext context = app.ApplicationServices.
//                GetRequiredService<ApplicationDBContext>();
//            context.Database.Migrate();
//            if (!context.Products.Any())
//            {
//                context.Products.AddRange(
//                    new Product
//                    {
//                        Name = "Kayak",
//                        Discription = "A boat for one price",
//                        Category = "Watersports",
//                        Price = 275
//                    },

//                    new Product
//                    {
//                        Name = "Lifejacet",
//                        Discription = "Protective and fishionenable",
//                        Category = "Watersports",
//                        Price = 48.98M
//                    },

//                    new Product
//                    {
//                        Name = "Soccer ball",
//                        Discription = "FIFA-approved size and weight",
//                        Category = "Soccer",
//                        Price = 19.50M
//                    },

//                    new Product
//                    {
//                        Name = "Corner Flags",
//                        Discription = "Give your playing field а professional touch",
//                        Category = "Soccer",
//                        Price = 34.95M
//                    },

//                    new Product
//                    {
//                        Name = "Stadium",
//                        Discription = "Flat-packed 35, 000-seat stadium",
//                        Category = "Soccer",
//                        Price = 79500
//                    },

//                    new Product
//                    {
//                        Name = "Thinking Сар",
//                        Discription = "Improve brain efficiency Ьу 75i",
//                        Category = "Chess",
//                        Price = 16
//                    },

//                    new Product
//                    {
//                        Name = "Unsteady Chair",
//                        Discription = "Secretly give your opponent а disadvantage",
//                        Category = "Chess",
//                        Price = 29.95m
//                    },
//                    new Product
//                    {
//                        Name = "Human Chess Board",
//                        Discription = "А fun game for the family",
//                        Category = "Chess",
//                        Price = 75
//                    },

//                    new Product
//                    {

//                        Name = "Bling-Bling King",
//                        Discription = "Gold-plated, diamond-studded King",
//                        Category = "Chess",
//                        Price = 1200
//                    }

//                    );

//                context.SaveChanges();
//            }
//        }
//    }
//}

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SportsStore.Models
{

    public static class SeedData
    {

        public static void EnsurePopulated(IApplicationBuilder app)
        {
            ApplicationDBContext context = app.ApplicationServices
                .GetRequiredService<ApplicationDBContext>();
            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product
                    {
                        Name = "Kayak",
                        Discription = "A boat for one person",
                        Category = "Watersports",
                        Price = 275
                    },
                    new Product
                    {
                        Name = "Lifejacket",
                        Discription = "Protective and fashionable",
                        Category = "Watersports",
                        Price = 48.95m
                    },
                    new Product
                    {
                        Name = "Soccer Ball",
                        Discription = "FIFA-approved size and weight",
                        Category = "Soccer",
                        Price = 19.50m
                    },
                    new Product
                    {
                        Name = "Corner Flags",
                        Discription = "Give your playing field a professional touch",
                        Category = "Soccer",
                        Price = 34.95m
                    },
                    new Product
                    {
                        Name = "Stadium",
                        Discription = "Flat-packed 35,000-seat stadium",
                        Category = "Soccer",
                        Price = 79500
                    },
                    new Product
                    {
                        Name = "Thinking Cap",
                        Discription = "Improve brain efficiency by 75%",
                        Category = "Chess",
                        Price = 16
                    },
                    new Product
                    {
                        Name = "Unsteady Chair",
                        Discription = "Secretly give your opponent a disadvantage",
                        Category = "Chess",
                        Price = 29.95m
                    },
                    new Product
                    {
                        Name = "Human Chess Board",
                        Discription = "A fun game for the family",
                        Category = "Chess",
                        Price = 75
                    },
                    new Product
                    {
                        Name = "Bling-Bling King",
                        Discription = "Gold-plated, diamond-studded King",
                        Category = "Chess",
                        Price = 1200
                    }
                );
                context.SaveChanges();
            }
        }
    }
}