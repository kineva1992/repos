
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace sportsstores.Models
{
    public class AppIndentityDbContext : IdentityDbContext
    {
        public AppIndentityDbContext(DbContextOptions<AppIndentityDbContext> options) : base(options) { }

    }
}
