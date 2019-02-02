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
        public ActionResult Index()
        {
            var model = new DashboardViewModel();

            var user = _helper.FindUserByEmail("test@test.test");
            model.User = user;
            model.Plants = new List<Plant>
            {
                new Plant
                {
                    Name = "Test Plant",
                    Alias = "Lil' Testy",
                    Id = 1,
                    Moisture = 0.0876,
                    Sunlight = 0.0556,
                    Species = "Planticus unrealus"
                }
            };

            return View(model);
        }
    }
}