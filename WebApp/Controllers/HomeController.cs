using System.Collections.Generic;
using System.Web.Mvc;
using DBHelper;
using Models;
using WebApp.Models.HomeViewModels;
using DbHelper = DBHelper.DbHelper;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDbHelper _helper;

        public HomeController(IDbHelper helper)
        {
            _helper = helper;
        }

        public HomeController() => _helper = new DBHelper.DbHelper();
        public ActionResult Index(User user)
        {
            //if (user != null && ViewBag.Token != null)
            //{
                var model = new DashboardViewModel();
                model.User = user;
            model.Plants = new List<Plant>();
            //var u = _helper.FindUserByEmail("test@test.test");
            //model.User = u;
                //model.Plants = new List<Plant>
                //{
                //    new Plant
                //    {
                //        Name = "Test Plant",
                //        Alias = "Lil' Testy",
                //        Id = 1,
                //        Moisture = 0.0876,
                //        Sunlight = 0.0556,
                //        Species = "Planticus unrealus"
                //    }
                //};

            // ViewBag.Token = token;
            ViewBag.User = user;
                return View(model);
            //}
            return View();
        }
    }
}