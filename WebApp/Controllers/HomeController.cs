using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DBHelper;
using Models;
using WebApp.Models.HomeViewModels;
using DbHelper = DBHelper.DbHelper;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDbHelper _db;

        // DbHelper injected
        public HomeController(IDbHelper db) => _db = db;

        //public HomeController() => _helper = new DBHelper.DbHelper();
        public ActionResult Index(User user)
        {
            if (user == null)
                return RedirectToAction("Login", "Account");

            var plants = _db.GetPlantsForUser(user.Id);

            var model = new DashboardViewModel {User = user, Plants = plants?? new List<Plant>()};

            //ViewBag.User = user;

            Session["User"] = user;
            return View(model);
        }
    }
}