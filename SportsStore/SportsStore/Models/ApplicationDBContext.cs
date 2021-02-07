using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace SportsStore.Models
{
    public class ApplicationDBContext: DbContext{
        public DbSet<Product> Products { set; get; }
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        
    }
}
