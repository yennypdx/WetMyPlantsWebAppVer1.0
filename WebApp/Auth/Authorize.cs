using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DBHelper;
using Microsoft.ApplicationInsights.Web;
using Models;

namespace WebApp.Auth
{
    // Implement a subclass of the ActionFilterAttribute and IActionFilter
    // This provides us with four methods we can override:
    // 1) OnActionExecuting(ActionExecutingContext filterContext) -> Just before the action method is called
    // 2) OnActionExecuted(ActionExecutedContext filterContext) -> After the action method is called and before the result is executed (before view render)
    // 3) OnResultExecuting(ResultExecutingContext filterContext) -> Just before the result is executed (after all code has executed but before view render)
    // 4) OnResultExecuted(ResultExecutedContext filterContext) -> After the result is executed (after the view is rendered)

    // In each of these context objects we have access to the Session variable through filterContext.HttpContext.Session


    public class Authorize : ActionFilterAttribute, IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext) // before the action is executed
        {
            var token = filterContext.HttpContext.Session["Token"]?.ToString(); // Get the token from Session
            var userId = filterContext.HttpContext.Session["User"] != null      // Get the userId from Session
                ? Convert.ToInt32((filterContext.HttpContext.Session["User"] as User)?.Id)
                : -1;

            if (token != null && userId != -1) // If both the token and userId are valid, proceed to verify
            {
                var db = new DBHelper.DbHelper();
                if (!db.ValidateUserToken(userId, token)) // Validate the token with the userId
                {
                    filterContext.Result = new RedirectResult("/Account/Login");
                }
            }
            else // If one or both are invalid, redirect to login
            {
                filterContext.Result = new RedirectResult("/Account/Login");
            }
        }
    }
}