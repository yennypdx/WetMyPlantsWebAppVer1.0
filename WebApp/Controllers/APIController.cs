using System;
using System.Linq;
using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;
using DBHelper;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using WebApp.Models.AccountViewModels;
using Models;

namespace WebApp.Controllers
{
    [RoutePrefix("api")]
    public class ApiController : Controller
    {
        private readonly IDbHelper _db;

        /* HELPER FUNCTIONS */
        /* Jsonify takes a string and packages it as a JSON object under the "content" key */
        private JsonResult Jsonify(string content) => Json($"{{ content: '{content}' }}");

        /* BadRequest takes a string or JSON object and returns it along with a 500 (BadRequest) status code */
        private ActionResult BadRequest(string content) => BadRequest(Jsonify(content));

        private ActionResult BadRequest(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.BadRequest, content.Data.ToString());

        /* Ok takes a string or JSON object and returns it along with a 200 (OK) status code */
        private ActionResult Ok(string content) => Ok(Jsonify(content));

        private ActionResult Ok(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.OK, content.Data.ToString());

        /* CTOR receives the DbHelper through Dependency Injection */
        public ApiController(IDbHelper db) => _db = db;

        /* SendGrid >> helper method Android style */
        static public async Task SendPasswordResetEmail(string email)
        {
            /* System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY"); */
            string apiKey = "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";
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                /*
                 * TODO:
                 * 1. Create a helper method that will generate random six digits as temp key
                 * 2. Pair that six digits with the email and push to db
                 * 3. Send out the six digit via sendGrid
                 */
            };

            msg.AddTo(new EmailAddress(email, "user"));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
        }

        /* GET: api/ */
        /* Test function that returns "hello world!" when you navigate to the /api URI */
        public string Index()
        {
            return "hello world!";
        }

        /* Create new user in db >> return a TOKEN */
        [HttpPost, Route("user/register")]
        public ActionResult RegisterUser(RegistrationViewModel model)
        {
            var token = "";
            //var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (!ModelState.IsValid) return BadRequest("Invalid registration model");

            if (_db.CreateNewUser(model.FirstName, model.LastName, model.Phone, model.Email, model.Password))
            {
                token = _db.LoginAndGetToken(model.Email, model.Password);
            }
            else
            {
                return BadRequest("User already exists");
            }

            return token != null ? Jsonify(token) : BadRequest("Registration failed");
        }

        /* Authenticate user >> return a TOKEN */
        [HttpPost, Route("login")]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid login model");

            var token = _db.LoginAndGetToken(model.Email, model.Password);

            return token != null ? Jsonify(token) : BadRequest("Invalid login");
        }

        // Delete a user from db with ID >> Return OK
        [HttpDelete, Route("user/delete/{id}")]
        public ActionResult DeleteUser(int id)
        {
            //var users = _db.GetAllUsers();
            //var user = users?.FirstOrDefault(u => u.Id.Equals(id));
            var user = _db.FindUser(id);
            if (user == null) return BadRequest("Could not find user " + id);

            var result = _db.DeleteUser(user.Email);

            return result ? Ok("User deleted") : BadRequest("Error deleting user " + id);
        }

        /* INSECURE! >> return User list */
        [HttpGet, Route("users/all")]
        public JsonResult GetAllUsers()
        {
            return Json(_db.GetAllUsers(), JsonRequestBehavior.AllowGet);
        }

        /* User update their password >> Return OK */
        [HttpPost, Route("forgotpass/sg/{token}")]
        public ActionResult ForgotUserPasswordViaEmail(ForgotPasswordViewModel model)
        {
            var result = _db.FindUser(model.Email);
            if (result == null)
            {
                return BadRequest("Could not find user " + model);
            }

            var resetCode = Crypto.HashPassword(DateTime.Today.ToString());
            //TODO: Store the reset code in the DB
            _db.SetResetCode(result.Id, resetCode);

            //TODO: modify SendPasswordResetEmail() method
            SendPasswordResetEmail(model.Email).Wait();
            return Ok("Success");
        }

        /* User update their password >> Return OK */
        [HttpPost, Route("forgotpass/rmq/{token}")]
        public ActionResult ForgotUserPasswordViaText(ForgotPasswordViewModel model)
        {
            var result = _db.FindUser(model.Email);
            if (result == null)
            {
                return BadRequest("Could not find user " + model);
            }

            var resetCode = Crypto.HashPassword(DateTime.Today.ToString());
            //TODO: Store the reset code in the DB
            _db.SetResetCode(result.Id, resetCode);

            //TODO: modify SendPasswordResetEmail() method
            //SendPasswordResetText()
            return Ok("Success");
        }

        /* Sumbitting pin to get token and access to dashboard */
        [HttpPost]
        [Route("submit/{pin}")]
        public ActionResult SubmitUserPin(String userPin, String userEmail)
        {
            //TODO: utilize ValidateResetCode() to get the confirmation

            return Ok("Success");
        }

        /* Getting single account data >> return User list as JsonObject */
        [HttpGet]
        [Route("user/{token}")]
        public JsonResult GetUserDetail(String inToken)
        {
            //TODO: create find user method with token as param
            var result = _db.FindUser(inToken);
            return Json(new { content = result });
        }

        /* Updating user information on DB >> Return OK */
        [HttpPut]
        [Route("user/update")]
        public ActionResult UpdateAccountInfo(User model)
        {
            if (_db.FindUser(model.Id) != null && _db.UpdateUser(model))
            {
                Session["User"] = model;
                return Ok("Success");
            }
            else
            {
                return BadRequest("Update failed");
            }
        }

        /* Get list of plant from a user which holds the token*/
        /*[HttpGet]
        [Route("plant/{token}")]
        public JsonResult GetPlantListFromUser()
        {
            //return JsonObject of plant list
            return Json(new {plantList});
        }*/

    }
}