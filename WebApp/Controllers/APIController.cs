using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using System.Web.Routing;
using DBHelper;
using WebApp.Models.AccountViewModels;
using DbHelper = DBHelper.DbHelper;

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

        // GET: Api
        public string Index()
        {
            return "hello world!";
        }

        // POST: api/register
        [HttpPost]
        public ActionResult Register(RegistrationViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid registration model");

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
            if (!ModelState.IsValid) return BadRequest("Invalid login model");

            var token = _db.LoginAndGetToken(model.Email, model.Password);

            if (token != null)
                return Ok(token);

            return BadRequest("Invalid login");
        }
    }
}