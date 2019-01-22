using System.Web.Mvc;
using WebApp.Models.AccountViewModels;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        //public ActionResult Index()
        //{
        //    return View();
        //}

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LoginUser(LoginViewModel model)
        {
            return null;
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        public ActionResult Registration()
        {
            return View();
        }
    }
}