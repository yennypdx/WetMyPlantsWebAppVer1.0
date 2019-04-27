using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DBHelper;
using Models;
using WebApp.Auth;
using WebApp.Models.HomeViewModels;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDbHelper _db;

        // DbHelper injected
        public HomeController(IDbHelper db) => _db = db;

        [AuthorizeUser]
        public ActionResult Index()
        {
            // The AuthorizeUser attribute will ensure only a valid user can access this method,
            // therefore, we know that Session carries a user object; no need to check.
            var user = Session["User"] as User;

            var model = new DashboardViewModel
            {
                User = user,
                Plants = _db.GetPlantsForUser(user.Id) ?? new List<Plant>()
            };

            return View(model);
        }
    }
}