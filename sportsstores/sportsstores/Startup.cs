using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using sportsstores.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace sportsstores
{
    public class Startup
    {
        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //Подключение к базе данных
            services.AddDbContext<ApplicationDbContext>(options => 
            options.UseSqlServer(Configuration["Data:SportStoreProducts:ConnectionString"]));
            //Строка подключения к IdentityFramework
            services.AddDbContext<AppIndentityDbContext>(options => 
            options.UseSqlServer(Configuration["Data:SportStoreIdentity:ConnectionString"]));
            //Подключение сервисов IndentityFramework
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppIndentityDbContext>()
                .AddDefaultTokenProviders();

            services.AddTransient<IProductRepository,EFProductRepository>();
            //Передача данных классу SessionCart методу GetCart 
            services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));
            //Каждый раз когданеобходим 
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //Регистрация службы IOrderRepository
            services.AddTransient<IOrderRepository, EFOrderRepository>();
            //Добавление поддержки MVC
            services.AddMvc();
            //Добавление возможности кешировать данные
            services.AddMemoryCache();
            //Добавление возможности Сесиии
            services.AddSession();  
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseStatusCodePages();
            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();
            app.UseMvc(routers => {
                routers.MapRoute(
                    name: null,
                    template:"{category}/Page{productPage:int}",
                    defaults: new {controller = "Product", action = "List" }
                    );
                routers.MapRoute(
                    name: null,
                    template: "Page{productPage:int}",
                    defaults: new {controller = "Product", action = "List", productPage = 1 }
                    ); 
                routers.MapRoute(
                    name:   null,
                    template:"{controller}",
                    defaults: new {controller = "Product", action = "List", productPage = 1});
                routers.MapRoute(
                    name:null,
                    template:"",
                    defaults: new {controller = "Product", action = "List", productPage = 1 });
                routers.MapRoute(
                    name: null,
                    template: "{controller=Product}/{action=List}/{id?}");  
                });

            SeedData.EnsurePopulated(app);
            IdentitySeedData.EnsurePopulated(app);
        }
    }
}
