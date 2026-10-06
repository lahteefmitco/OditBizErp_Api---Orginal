using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using MictcoWebService.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace MictcoWebService.Authentication
{
    public class ClientSessonFilter: ActionFilterAttribute
    {
        //public void OnActionExecuting(ActionExecutingContext filterContext)
        //{

        //    if (filterContext.HttpContext.Session != null)
        //    {
        //        var id = filterContext.HttpContext.Session["Id"];
        //    }

        //    filterContext.Result = new ForbidResult("Client Not Login");
        //}


        //public override void OnActionExecuting(ActionExecutingContext filterContext)
        //{
        //    if (filterContext.HttpContext.Session != null)
        //    {
        //        HttpContext.filterContext.Session["userid"]

        //        filterContext.Result = new ForbidResult("Client Not Login");
        //    }
        //}

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }
}
