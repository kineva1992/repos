using System.Linq;
using Microsoft.AspNetCore.Mvc;
using FirstCoreApplication.Models;
using System;

namespace FirstCoreApplication.Controllers
{
    public class HomeController : HelloBaseController
    {
        MobileContext db;
        public HomeController(MobileContext context)
        {
            db = context;
        }
        public IActionResult Index()
        {
            return Content("Запрос выполнен успешно");
        }

    }

  
}