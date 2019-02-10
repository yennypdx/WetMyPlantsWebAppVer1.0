using System.Web.Mvc;
using Models;
using WebApp.Models.AccountViewModels;
using WebApp.Models.HomeViewModels;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly DBHelper.DbHelper _db;

        public AccountController()
        {
            _db = new DBHelper.DbHelper();
        }

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ForgotUserPassword(ForgotPasswordViewModel uModel)
        {
            var result = _db.ForgotPassword(uModel.Email);
            return View("Login");
        }

        public ActionResult ResetPassword(ResetPasswordViewModel uModel)
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        //POST: Registration/Register
        [HttpPost]
        public ActionResult RegisterUser(RegistrationViewModel uModel)
        {
            var result = _db.CreateNewUser(
                uModel.FirstName,
                uModel.LastName,
                uModel.Phone,
                uModel.Email,
                uModel.Password);

            if (!result) return RedirectToAction("Register");

            var token = _db.LoginAndGetToken(uModel.Email, uModel.Password);

            if (token == null) return RedirectToAction("Register");

            // store token in a cookie here
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult LoginUser(LoginViewModel model)
        {
            var result = _db.LoginAndGetToken(model.Email, model.Password);
            
            if (result != null)
            {
                // store token as a cookie here
                return RedirectToAction("Index", "Home");
            }

            return View("Login");
        }
    }
}