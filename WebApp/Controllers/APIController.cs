using System.Linq;
using System.Net;
using System.Web.Mvc;
using DBHelper;
using Models;
using WebApp.Models.AccountViewModels;

namespace WebApp.Controllers
{
    [RoutePrefix("api")]
    public class ApiController : Controller
    {
        private readonly IDbHelper _db;

        /* HELPER FUNCTIONS */

        // Jsonify takes a string and packages it as a JSON object under the "content" key.
        private JsonResult Jsonify(string content) => Json($"{{ content: '{content}' }}");

        // BadRequest takes a string or JSON object and returns it along with a 500 (BadRequest) status code
        private ActionResult BadRequest(string content) => BadRequest(Jsonify(content));
        private ActionResult BadRequest(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.BadRequest, content.Data.ToString());

        // Ok takes a string or JSON object and returns it along with a 200 (OK) status code
        private ActionResult Ok(string content) => Ok(Jsonify(content));
        private ActionResult Ok(JsonResult content) => 
            new HttpStatusCodeResult(HttpStatusCode.OK, content.Data.ToString());

        // CTOR receives the DbHelper through Dependency Injection
        public ApiController(IDbHelper db) => _db = db;

        // GET: api/
        // Test function that returns "hello world!" when you navigate to the /api URI
        public string Index()
        {
            return "hello world!";
        }

        // POST: api/user/register
        // POST requests to this URI containing RegistrationViewModel data will create a new user
        [HttpPost, Route("user/register")]
        public ActionResult RegisterUser(RegistrationViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid registration model");

            if (!_db.CreateNewUser(model.FirstName, model.LastName, model.Phone, model.Email, model.Password))
                return BadRequest("Unable to register user");

            var id = _db.GetAllUsers().Where(u => u.Email.Equals(model.Email)).ToArray()[0].Id;

            return Ok(Json($"{{ id : '{id}' }}"));
        }

        // POST: api/login
        // POST requests at this URI containing a LoginViewModel will authenticate the user
        // and return the user's token
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid login model");

            var token = _db.LoginAndGetToken(model.Email, model.Password);

            return token != null ? Ok(token) : BadRequest("Invalid login");
        }

        // DELETE: api/user/delete/id
        // DELETE requests at this URI will delete a user from the database based on their ID
        [HttpDelete, Route("user/delete/{id}")]
        public ActionResult DeleteUser(int id)
        {
            var users = _db.GetAllUsers();
            var user = users?.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null) return BadRequest("Could not find user " + id);

            var result = _db.DeleteUser(user.Email);

            return result ? Ok("User deleted") : BadRequest("Error deleting user " + id);
        }

        // GET: api/users/all
        // INSECURE!!! GET requests at this URI will return a JSON object containing all User objects in the database
        [HttpGet, Route("users/all")]
        public JsonResult GetAllUsers()
        {
            return Json(_db.GetAllUsers(), JsonRequestBehavior.AllowGet);
        }


        [HttpGet, Route("plant/{token}")]
        public JsonResult GetPlantsList(string token)
        {
            var user = _db.FindUser(token);
            var plants = _db.GetPlantsForUser(user.Id);

            // Select only ID and Nickname from the list of plants
            var plantListOfIdAndNickname = plants.Select(plant => new Plant { Id = plant.Id, Nickname = plant.Nickname });
            
            // return list of all plants including nickname and id 
            return Json(plantListOfIdAndNickname, JsonRequestBehavior.AllowGet);
        }


        [HttpPut, Route("plant/edit/{token}")]
        public ActionResult EditPlant(string token, Plant updatedPlant)
        {
            // Check that user exists and plant exists for user?
            var user = _db.FindUser(token);
            if (user == null) return BadRequest("Could not find user " + token);
    
            // Get all the plants for the user where the Id matches updatedPlant ID
            var userPlantsWithSpecifiedId = _db.GetPlantsForUser(user.Id).Where(plant => plant.Id.Equals(updatedPlant.Id)).ToList();

            // Verify the plant exists for that user
            if (userPlantsWithSpecifiedId.Count() <= 0) return BadRequest("Plant does not exists for spcified user " + token + " " + updatedPlant.Id);

            // If count is greather than one, there are multiple plants with the same Id -- this should not happen
            if (userPlantsWithSpecifiedId.Count() > 1) return BadRequest("Multiple plants exist with the same Id" + updatedPlant.Id);

            var result = _db.UpdatePlant(updatedPlant);

            return result ? Ok("Plant updated") : BadRequest("Error updating plant: " + updatedPlant.Id);
        }

        // TODO: Route, Http
        [HttpDelete, Route("plant/del/{token}")]
        public ActionResult DeletePlant(string token, int plantId)
        {
            // Check that the user exists
            var user = _db.FindUser(token);
            if (user == null) return BadRequest("Could not find user " + token);

            // Check that plant exists for that user

            var result = _db.DeletePlant(plantId);
            return result ? Ok("Plant deleted") : BadRequest("Error deleting plant: " + plantId);
        }
    }
}