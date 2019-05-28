
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
using WebApp.Models;
using WebApp.Helpers;

namespace WebApp.Controllers
{
    public class AccountController : AuthController
    {
        //public readonly DBHelper.IDbHelper Db;

        // Inject Dependency
        public AccountController(IDbHelper db) : base(db) {}

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

            var resetCode = Crypto.GeneratePin().ToString();
            Db.SetResetCode(result.Id, resetCode);

            if (uModel.Sms)
            {
                SmsService.SendSms(result.Phone, $"Here is your password reset PIN: {resetCode}");
                var userId = Db.FindUser(email: result.Email)?.Id;

                var model = new PinViewModel
                {
                    UserId = (int)userId
                };

                return View("Pin", model);
            }
            else
            {
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = result.Id, code = resetCode }, protocol: Request?.Url?.Scheme);

                SendPasswordResetEmail(uModel.Email, callbackUrl);
                return View("Login");
            }
        }

        private async Task SendPasswordResetEmail(string email, string urlString)
        {
            /*
            string apiKey = "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";//System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var client = new SendGridClient(apiKey);            
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("resetpassword@wetmyplants.com", "WetMyPlants Team"),
                Subject = "Reset Password",
                PlainTextContent = "Please click on this link to reset your password: " + urlString,
                HtmlContent = "<strong>Please click on this link to reset your password: </strong><a href=\"" + urlString + "\" > wetmyplants.azurewebsites.net/Account/ResetPassword</a>"
            };
            msg.AddTo(new EmailAddress(email, "user"));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
            */
            var emailMessage = new EmailService
            {
                Destination = email,
                PlainTextContent = $"Please click on this link to reset your password: {urlString}",
                HtmlContent = $"<strong>Please click <a href={urlString}>here</a> to reset your password.",
                Subject = "Reset Password"
            };

            emailMessage.Send();
        }

        [HttpPost]
        public ActionResult ResetUserPassword(ResetPasswordViewModel uModel)
        {
            if(uModel.Password == uModel.ConfirmPassword)
            {
                var user = Db.FindUser(email: uModel.Email);
                if(user == null)
                    return RedirectToAction("Login");

                Db.ResetPassword(uModel.Email, uModel.Password);
                Db.DeleteResetCode(user.Id);
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
            //var update = ViewData["Update"];

            var preferences = Db.GetNotificationPreferences(user.Id);

            // Convert user to ViewModel to pass to the View
            var userViewModel = new MyAccountViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Email = user.Email,
                Id = user.Id,
                NotifyEmail = preferences["Email"],
                NotifyPhone = preferences["Phone"]
            };

            if (TempData["Update"] != null && (string) TempData["Update"] == "true")
                ViewBag.Update = "true";

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
            // Convert model to User to update database
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
                Db.SetEmailNotificationPreference(user.Id, model.NotifyEmail);
                Db.SetPhoneNotificationPreference(user.Id, model.NotifyPhone);

                // if update was successful, reload the account page to refresh the data
                TempData["Update"] = "true";
                return RedirectToAction("MyAccount");
            }

            // if the update was unsuccessful, reload the view with an error message
            ViewBag.Update = "false";
            return View("MyAccount", model);
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