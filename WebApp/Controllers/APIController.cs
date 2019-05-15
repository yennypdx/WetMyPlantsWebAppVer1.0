using System;
using System.Net;
using System.Web.Mvc;
using DbHelper;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using WebApp.Models.AccountViewModels;
using Models;
using System.Linq;

namespace WebApp.Controllers
{
    [RoutePrefix("api")]
    public class ApiController : Controller
    {
        private readonly IDbHelper _db;

        /* CTOR receives the DbHelper through Dependency Injection */
        public ApiController(IDbHelper db) => _db = db;

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
        [HttpPost]
        [Route("user/register")]
        public ActionResult RegisterUser(RegistrationViewModel model)
        {
            var token = string.Empty;
            if (!ModelState.IsValid) return BadRequest("Invalid registration model");

            if (_db.CreateNewUser(model.FirstName, model.LastName, model.Phone, model.Email, model.Password)){
                token = _db.LoginAndGetToken(model.Email, model.Password);
            } else {
                return BadRequest("User already exists");
            }

            return token != null ? Jsonify(token) : BadRequest("Registration failed");
        }

        /* Authenticate user >> return a TOKEN */
        [HttpPost]
        [Route("login")]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid login model");

            var token = _db.LoginAndGetToken(model.Email, model.Password);

            return token != null ? Jsonify(token) : BadRequest("Invalid login");
        }

        /* Delete a user from db with ID >> Return OK (currently not implemented) */
        [HttpDelete]
        [Route("user/delete/{token}/")]
        public ActionResult DeleteUser(string token)
        {
            //var users = _db.GetAllUsers();
            //var user = users?.FirstOrDefault(u => u.Id.Equals(id));
            var user = _db.FindUser(null, token);
            if (user == null) return BadRequest("Could not find user");

            var result = _db.DeleteUser(user.Email);

            return result ? Ok("User deleted") : BadRequest("Error deleting user");
        }

        /* INSECURE! >> return User list  (currently not implemented) */
        [HttpGet]
        [Route("users/all")]
        public JsonResult GetAllUsers()
        {
            return Json(_db.GetAllUsers(), JsonRequestBehavior.AllowGet);
        }

        /* User update their password >> Return OK */
        [HttpPost]
        [Route("forgotpass/sg/{token}/")]
        public ActionResult ForgotUserPasswordViaEmail(ForgotPasswordViewModel model)
        {
            var result = _db.FindUser(email: model.Email);
            if (result == null)
            {
                return BadRequest("Could not find user " + model);
            }

            var resetCode = Crypto.HashPassword(DateTime.Today.ToLongDateString());
            //TODO: Store the reset code in the DB
            _db.SetResetCode(result.Id, resetCode);

            //TODO: modify SendPasswordResetEmail() method
            SendPasswordResetEmail(model.Email).Wait();
            return Ok("Success");
        }

        /* User update their password >> Return OK */
        [HttpPost]
        [Route("forgotpass/rmq/{token}/")]
        public ActionResult ForgotUserPasswordViaText(ForgotPasswordViewModel model)
        {
            var result = _db.FindUser(email: model.Email);
            if (result == null)
            {
                return BadRequest("Could not find user " + model);
            }

            var resetCode = Crypto.HashPassword(DateTime.Today.ToLongDateString());
            //TODO: Store the reset code in the DB
            _db.SetResetCode(result.Id, resetCode);

            //TODO: modify SendPasswordResetEmail() method
            //SendPasswordResetText()
            return Ok("Success");
        }

        /* Sumbitting pin to get token and access to dashboard */
        [HttpPost]
        [Route("submit/{pin}/")]
        public ActionResult SubmitUserPin(String userPin, String userEmail)
        {
            var user = _db.FindUser(email: userEmail);
            return user != null && _db.ValidateResetCode(user.Id, userPin)
                ? Ok("Success")
                : BadRequest("Invalid PIN or email");
        }

        /* Getting single account data >> return single USER Object */
        [HttpGet]
        [Route("user/{token}/")]
        public JsonResult GetUserDetail(String token)
        {
            var user = _db.FindUser(token: token);

            if (user == null)
                BadRequest("User not found.");

            return Json(user, JsonRequestBehavior.AllowGet);
        }

        /* Updating user information on DB >> Return OK */
        [HttpPatch]
        [Route("user/update/{token}/")]
        public ActionResult UpdateAccountInfo(String token, User model)
        {
            if (_db.FindUser(model.Id) != null && _db.UpdateUser(model))
            {
                Session["User"] = model;
                return Ok("Success");

            } else {
                return BadRequest("Update failed");
            }
        }
        
        /* Updating user PASSWORD on DB (user is logged in) >> Return OK */
        [HttpPatch]
        [Route("newpass/in")]
        public ActionResult UpdatePasswordAfterLoggedIn(ResetPasswordViewModel model)
        {
            if (model.Password == model.ConfirmPassword){
                _db.ResetPassword(model.Email, model.Password);
            }
            else{
                return BadRequest("Update failed");
            }

            return Ok("Success");
        }


        [HttpGet, Route("plant/{token}/")]
        public JsonResult GetPlantsList(string token)
        {
            var user = _db.FindUser(token: token);
            var plants = _db.GetPlantsForUser(user.Id);

            // Select only ID and Nickname from the list of plants
            var plantListOfIdAndNickname = plants.Select(plant => new Plant { Id = plant.Id, Nickname = plant.Nickname });

            // return list of all plants including nickname and id 
            return Json(plantListOfIdAndNickname, JsonRequestBehavior.AllowGet);
        }


        [HttpPut, Route("plant/edit/{token}/")]
        public ActionResult EditPlant(string token, Plant updatedPlant)
        {
            // Check that user exists and plant exists for user?
            var user = _db.FindUser(token: token);
            if (user == null) return BadRequest("Invalid token ");

            // Get all the plants for the user where the Id matches updatedPlant ID
            var userPlantsWithSpecifiedId = _db.GetPlantsForUser(user.Id).Where(plant => plant.Id.Equals(updatedPlant.Id)).ToList();

            // Verify the plant exists for that user
            if (userPlantsWithSpecifiedId.Count() <= 0) return BadRequest("Plant does not exists for spcified user " + token + " " + updatedPlant.Id);

            // If count is greather than one, there are multiple plants with the same Id -- this should not happen
            if (userPlantsWithSpecifiedId.Count() > 1) return BadRequest("Multiple plants exist with the same Id" + updatedPlant.Id);

            var result = _db.UpdatePlant(updatedPlant);

            return result ? Ok("Plant updated") : BadRequest("Error updating plant: " + updatedPlant.Id);
        }

       
        [HttpDelete, Route("plant/del/{token}/")]
        public ActionResult DeletePlant(string token, string plantId)
        {
            // Check that the user exists
            var user = _db.FindUser(token: token);
            if (user == null) return BadRequest("Invalid token");

            // Check that plant exists for that user
            var result = _db.DeletePlant(plantId);
            return result ? Ok("Plant deleted") : BadRequest("Error deleting plant: " + plantId);
        }

       
    }
}