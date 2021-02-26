using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Carwash.Controllers
{
    public class HomeController : Controller
    {

        //GET index page
        public IActionResult Index()
        {
            return View();
        }
    }
}
