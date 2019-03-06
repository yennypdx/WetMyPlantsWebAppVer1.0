using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;
using DBHelper;
using WebApp.Models.AccountViewModels;

namespace WebApp.Controllers
{
    public class ApiController : Controller
    {
        private readonly IDbHelper _db;

        private JsonResult BadRequest(string content) => BadRequest(Json(new {content}));
        private JsonResult BadRequest(JsonResult content)
        {
            Response.Clear();
            Response.StatusCode = (int) HttpStatusCode.BadRequest; // 400
            return Json(content);
        }

        private JsonResult Ok(string content) => Ok(Json(new {content}));
        private JsonResult Ok(JsonResult content)
        {
            Response.Clear();
            Response.StatusCode = (int) HttpStatusCode.OK; // 200
            return Json(content);
        }

        public ApiController(IDbHelper db) => _db = db;
        // GET: Api
        public string Index()
        {
            return "hello world!";
        }

        // POST: api/register
        [HttpPost]
        public ActionResult Register(RegistrationViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid registration data");

            if (!_db.CreateNewUser(model.FirstName, model.LastName, model.Phone, model.Email, model.Password))
                return BadRequest("Unable to register user");

            var loginViewModel = new LoginViewModel
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = false
            };

            return Login(loginViewModel);
        }

        // POST: api/login
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid login data");

            var token = _db.LoginAndGetToken(model.Email, model.Password);

            if (token != null)
                return Ok(token);

            return BadRequest("Invalid login, check email and password and try again");
        }
    }
}