using System.Web.Mvc;
using Models;
using WebApp.Models.AccountViewModels;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Http;
using WebApp.Models.HomeViewModels;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly DBHelper.IDbHelper _db;

        // Inject Dependency
        public AccountController(DBHelper.IDbHelper db) => _db = db;

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

        public ActionResult ResetPassword()
        {
            return View();
        }
        [HttpGet]
        public ActionResult ForgotUserPassword(ForgotPasswordViewModel uModel)
        {
             var result = _db.FindUser(uModel.Email);
            
            if (result.Email != null && result.Email == uModel.Email)
            {
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = result.Id, code = GetHashCode() }, protocol: Request.Url.Scheme);
                SendPasswordResetEmail(uModel.Email, callbackUrl).Wait();
                return View("Login");
            }
            else
            {
                //return Content("<script language= 'javascript' type='text/javascript'>alert ('User not Fonud '); </script>");
                return View("ForgotPassword");
            }
            
        }

        static public async Task SendPasswordResetEmail(string email, string urlString)
        {
            string apiKey = "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";//System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var client = new SendGridClient(apiKey);            
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("resetpassword@wetmyplants.com", "WetMyPlants Team"),
                Subject = "Reset Password",
                PlainTextContent = "Please click on this link to reset your password: " + "http://wetmyplants.azurewebsites.net/Account/ResetPassword",
                HtmlContent = "<strong>Please click on this link to reset your password: </strong><a href=\"" + urlString + "\" > wetmyplants.azurewebsites.net/Account/ResetPassword</a>"
            };
            msg.AddTo(new EmailAddress(email, "user"));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
        }

        [HttpPost]
        public ActionResult ResetUserPassword(ResetPasswordViewModel uModel)
        {
            //string url = Request.Url.AbsolutePath;
            //string[] UrlParts = url.Split('/');
            //int id = System.Int32.Parse(url);
            // int id = (int)RouteData.Values["userId"];
            if(uModel.Password == uModel.ConfirmPassword)
            {
                _db.ResetPassword(uModel.Email, uModel.Password);
            }
            
            return View("Login");
        }

        public ActionResult Register()
        {
            return View();
        }

        public ActionResult MyAccount()
        {
            var user = Session["User"];
            return View(user);
        }

        //POST: Account/RegisterUser
        [HttpPost]
        public ActionResult RegisterUser(RegistrationViewModel uModel)
        {
            // CreateNewUser will be refactored to return the ID of the newly created user
            var result = _db.CreateNewUser(
                uModel.FirstName,
                uModel.LastName,
                uModel.Phone,
                uModel.Email,
                uModel.Password);

            if (!result) return RedirectToAction("Register");

            var token = _db.LoginAndGetToken(uModel.Email, uModel.Password);

            if (token == null) return RedirectToAction("Register");

            //ViewBag.User = result;
            ViewBag.Token = token;

            // set the session
            var user = _db.FindUser(uModel.Email);
            //Session["User"] = _db.FindUserByEmail(uModel.Email);
            Session["User"] = user;

            return RedirectToAction("Index", "Home", user);
        }

        [HttpPost]
        public ActionResult LoginUser(LoginViewModel model)
        {
            // Merge errors for name change from result to token!
            var token = _db.LoginAndGetToken(model.Email, model.Password);

            if (token != null)
            {
                var user = _db.FindUser(model.Email);
                ViewBag.User = user;
                ViewBag.Token = token;

                // set the session 
                Session["User"] = user;
                return RedirectToAction("Index", "Home"); // since the user object is stored in Session here,
                // it will be pulled out of Session in the HomeController's Index method
                //return RedirectToAction("Index", "Home", user);
            }

            return View("Login");
        }

        [HttpPost]
        public ActionResult UpdateUser(User user)
        {
            // update the session
            Session["User"] = user;
            _db.UpdateUser(user);
            return RedirectToAction("MyAccount", "Account", user);
        }
    }
}