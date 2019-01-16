using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApp.Controllers
{
    public class RegistrationController : Controller
    {
        // GET: Registration
        //public ActionResult Index()
        //{
        //    return View();
        //}
        public string RegUser()
        {
            return "Registration page coming soon.";
        }

        // POST: Registration/Create
        [HttpPost]
        public ActionResult SubmitUser(string usernm, string mail, string passwd, string phone)
        {
            try
            {
                // TODO: connect to submittion method in dataprovider
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
