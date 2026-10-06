using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Controllers
{
    
    public class HomeController : ControllerBase
    {
        public IActionResult Index()
        {
            return Content("Home/Index");
        }

        public IActionResult PageOne()
        {
            return Content("Home/One");
        }

        [HttpGet]
        public IActionResult PageTwo()
        {
            return Content("(GET) Home/Two");
        }

        [HttpPost]
        public IActionResult PageTwo(int id)
        {
            return Content($"(POST) Home/Two: {id}");
        }

    }
}
