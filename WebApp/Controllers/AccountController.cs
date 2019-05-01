
using System;
using System.Web.Mvc;
using Models;
using WebApp.Models.AccountViewModels;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Http;
using DbHelper;
using WebApp.Auth;
using WebApp.Models.HomeViewModels;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        public readonly DBHelper.IDbHelper Db;

        // Inject Dependency
        public AccountController(DBHelper.IDbHelper db) => Db = db;

        public ActionResult Login()
        {
            return View();
        }
        
        public ActionResult ForgotPassword()
        {
            return View();
        }

        public ActionResult ResetPassword(int? userId, string code)
        {
            if (userId == null || code == null) return RedirectToAction("Login");

            if (Db.ValidateResetCode((int)userId, code))
            {
                Db.DeleteResetCode((int)userId);

                var model = new ResetPasswordViewModel()
                {
                    Email = (Db.FindUser((int)userId))?.Email
                };

                return View(model);
            }

            return RedirectToAction("Login");
        }
        [HttpPost]
        public ActionResult ForgotUserPassword(ForgotPasswordViewModel uModel)
        {
            var result = Db.FindUser(email: uModel.Email);

            if (result == null) return Redirect("ForgotPassword");

            var resetCode = Crypto.HashPassword(DateTime.Today.ToString());
            // TODO: Store the reset code in the DB
            Db.SetResetCode(result.Id, resetCode);

            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = result.Id, code = resetCode }, protocol: Request.Url.Scheme);

            SendPasswordResetEmail(uModel.Email, callbackUrl).Wait();
            return View("Login");
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
            if(uModel.Password == uModel.ConfirmPassword)
            {
                Db.ResetPassword(uModel.Email, uModel.Password);
            }
            
            return RedirectToAction("Login");
        }

        public ActionResult Register()
        {
            return View();
        }

        [AuthorizeUser]
        public ActionResult MyAccount()
        {
            var user = (User)Session["User"];

            // Convert user to ViewModel to pass to the View
            var userViewModel = new MyAccountViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Email = user.Email,
                Id = user.Id
            };

            return View(userViewModel);
        }

        public ActionResult Logout()
        {
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        //POST: Account/RegisterUser
        [HttpPost]
        public ActionResult RegisterUser(RegistrationViewModel uModel)
        {
            // CreateNewUser will be refactored to return the ID of the newly created user
            string token;

            if (Db.CreateNewUser(uModel.FirstName,
                    uModel.LastName, uModel.Phone, 
                    uModel.Email, uModel.Password) &&
                (token = Db.LoginAndGetToken(uModel.Email, uModel.Password)) != null)
            {
                Session["Token"] = token;
                Session["User"] = Db.FindUser(email: uModel.Email);
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Register");
        }

        [HttpPost]
        public ActionResult LoginUser(LoginViewModel model)
        {
            string token;
            User user;

            if ((token = Db.LoginAndGetToken(model.Email, model.Password)) != null
                && (user = Db.FindUser(email: model.Email)) != null)
            {
                Session["Token"] = token;
                Session["User"] = user;

                return RedirectToAction("Index", "Home");
            }

            return View("Login");
        }

        [AuthorizeUser]
        [HttpPost]
        public ActionResult UpdateUser(MyAccountViewModel model)
        {
            var message = "Error updating your account.";
            // Convert model to User to update database and session
            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Id = model.Id,
                Phone = model.Phone
            };

            if (Db.UpdateUser(user)) // if the update is successful
            {
                // update the session
                Session["User"] = user;

                // update the message
                message = "Updated!";

                // update the view model
                model = new MyAccountViewModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Id = user.Id,
                    Phone = user.Phone
                };
            }

            ViewBag.UpdateMessage = message;
            // Reload the view with the current model (updated or not)
            return RedirectToAction("MyAccount", "Account", model);
        }

        [AuthorizeUser]
        public ActionResult DeleteUser()
        {
            // The AuthorizeUser attribute will verify the user is valid, no need to check
            return View("DeleteUser", new DeleteUserViewModel
            {
                Email = (Session["User"] as User)?.Email
            });
        }

        [AuthorizeUser]
        public ActionResult ConfirmDeletion(DeleteUserViewModel model)
        {
            // Check that the user entered the correct password
            if (Db.AuthenticateUser(model.Email, model.Password))
            {
                // Delete the user
                Db.DeleteUser(model.Email);

                // Abandon the session to log the deleted user out
                Session.Abandon();
                return RedirectToAction("Login", "Account");
            }
            else
            {
                // Incorrect password -- return to delete user page with error message
                ViewBag.Error = "Incorrect Password";
                return RedirectToAction("DeleteUser", "Account", new { email = model.Email });
            }
        }

        [AuthorizeUser]
        public ActionResult ChangePassword(string email)
        {
            var user = (User)Session["User"];
            var model = new ChangePasswordViewModel
            {
                Email = user.Email
            };
            return View("ChangePassword", model);
        }
        [AuthorizeUser, HttpPost]
        public ActionResult ConfirmPasswordChange(ChangePasswordViewModel model)
        {
            if (Db.AuthenticateUser(model.Email, model.Password))
            {
                Db.ResetPassword(model.Email, model.NewPassword);

                // TODO: Output a message for successful password change

                return RedirectToAction("MyAccount", "Account");
            }
            else
            {
                // Incorrect password -- return to ConfirmPasswordChange page with error message
                TempData["Error"] = "Incorrect Password";
                return RedirectToAction("ChangePassword", "Account", model.Email);
            }
        }
     
    }
}