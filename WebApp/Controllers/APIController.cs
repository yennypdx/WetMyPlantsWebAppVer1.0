using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DBHelper;
using Models;
using WebApp.Models.AccountViewModels;

namespace WebApp.Controllers
{
    public class ApiController : Controller
    {
        private readonly IDbHelper _db;

        private JsonResult Jsonify(string content) => Json($"{{ content: '{content}' }}");
        private ActionResult BadRequest(string content) => BadRequest(Jsonify(content));
        private ActionResult BadRequest(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.BadRequest, content.Data.ToString());

        private ActionResult Ok(string content) => Ok(Jsonify(content));
        private ActionResult Ok(JsonResult content) => 
            new HttpStatusCodeResult(HttpStatusCode.OK, content.Data.ToString());

        public ApiController(IDbHelper db) => _db = db;

        // GET: api/
        public string Index()
        {
            return "hello world!";
        }

        // POST: api/user/register
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
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid login model");

            var token = _db.LoginAndGetToken(model.Email, model.Password);

            if (token != null)
                return Ok(token);

            return BadRequest("Invalid login");
        }

        // DELETE: api/user/delete/id
        [HttpDelete, Route("user/delete/{id}")]
        public ActionResult DeleteUser(int id)
        {
            var result = _db.DeleteUser(_db.GetAllUsers().Where(u => u.Id == id).ToList()[0].Email);

            return result ? Ok("User deleted") : BadRequest("Error deleting user " + id.ToString());
        }

        // GET: api/users/all
        [HttpGet, Route("users/all")]
        public List<User> GetAllUsers()
        {
            return _db.GetAllUsers();
        }
    }
}