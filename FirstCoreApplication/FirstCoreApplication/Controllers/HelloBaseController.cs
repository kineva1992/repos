using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCoreApplication.Controllers
{
    public class HelloBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {

            if (context.HttpContext.Request.Headers.ContainsKey("User-Agent"))
            {
                // получаем заголовок User-Agent
                var useragent = context.HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
                // сравниваем его значение

                if (useragent.Contains("MSIE") || useragent.Contains("Trident"))
                {
                    context.Result = Content("Браузер Internet Explorel не поддерживается");
                }

                base.OnActionExecuting(context);
            }
        }
    }
}
