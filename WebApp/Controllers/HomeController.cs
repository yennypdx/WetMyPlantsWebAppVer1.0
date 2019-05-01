using System.Collections.Generic;
using System.Web.Mvc;
using DBHelper;
using Models;
using WebApp.Auth;
using WebApp.Models;
using WebApp.Models.HomeViewModels;

namespace WebApp.Controllers
{
    public class HomeController : AuthController
    {
        //protected readonly IDbHelper Db;

        // DbHelper injected
        public HomeController(IDbHelper db) : base(db) { }

        [AuthorizeUser]
        public ActionResult Index()
        {
            // The AuthorizeUser attribute will ensure only a valid user can access this method,
            // therefore, we know that Session carries a user object; no need to check.
            if (!(Session["User"] is User user)) return RedirectToAction("Login", "Account");

            var plants = Db.GetPlantsForUser(user.Id);

            if (plants != null)
                plants.ForEach(p => user.Plants.Add(p.Id));
            else user.Plants = new List<string>();

            var model = new DashboardViewModel
            {
                User = user,
                Plants = plants
            };

            return View(model);
        }
    }
}