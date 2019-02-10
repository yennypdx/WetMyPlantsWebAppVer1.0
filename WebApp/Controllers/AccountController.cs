using System.Web.Mvc;
using WebApp.Models.AccountViewModels;

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
            //try
            //{
            //    if (ModelState.IsValid)
            //    {
            //        var fname = uModel.FirstName;
            //        var lname = uModel.LastName;
            //        var phn = uModel.Phone;
            //        var email = uModel.Email;
            //        var pass = uModel.Password;

            //        _db.CreateNewUser(fname, lname, phn, email, pass);
            //        return View("Register");
            //    }
            //    else
            //    {
            //        return View();
            //    }
            //}
            //catch
            //{
            //    return View();
            //}
            return View("Login");
        }

        [HttpPost]
        public ActionResult LoginUser(LoginViewModel model)
        {
            var result = _db.Login(model.Email, model.Password);

            return View("Login");
        }
    }
}