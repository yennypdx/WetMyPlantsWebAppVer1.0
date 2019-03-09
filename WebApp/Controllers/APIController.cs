using System.Linq;
using System.Net;
using System.Web.Mvc;
using DBHelper;
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
    }
}