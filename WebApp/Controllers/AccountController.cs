using System.Web.Mvc;
using Models;
using WebApp.Models.AccountViewModels;
using System.Threading.Tasks;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;
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

        [HttpPost]
        public ActionResult ForgotUserPassword(ForgotPasswordViewModel uModel)
        {
             var result = _db.FindUserByEmail(uModel.Email);
            if (result.Email != null && result.Email == uModel.Email)
            {
                SendPasswordResetEmail(uModel.Email).Wait();
                return View("Login");
            }
            else
            {                
                return Content("<script language= 'javascript' type='text/javascript'>alert ('User not Fonud '); </script>");
            }
            
        }

        static public async Task SendPasswordResetEmail(string email)
        {
            string apiKey = "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";//System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("resetpassword@wetmyplants.com", "WetMyPlants Team"),
                Subject = "Reset Password",
                PlainTextContent = "Please click on this link to reset your password: " + "http://wetmyplants.azurewebsites.net/Account/ResetPassword",
                HtmlContent = "<strong>Please click on this link to reset your password: </strong><a href='http://wetmyplants.azurewebsites.net/Account/ResetPassword'> wetmyplants.azurewebsites.net/Account/ResetPassword</a>"
            };
            msg.AddTo(new EmailAddress(email, "user"));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
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