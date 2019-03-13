using System.Web.Mvc;

namespace WebApp.Controllers
{
    public class PlantController : Controller
    {
        // GET: Plant
        public ActionResult Index()
        {
            return View();
        }
    }
}