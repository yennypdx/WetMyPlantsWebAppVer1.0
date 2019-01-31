using System.Web.Mvc;
using WebApp.Models.HomeViewModels;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(new DashboardViewModel());
        }
    }
}