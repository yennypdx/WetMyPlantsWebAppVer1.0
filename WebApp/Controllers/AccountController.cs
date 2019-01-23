using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DBHelper;
using WebApp.Models;
using WebApp.Models.AccountViewModels;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly DBHelper.DbHelper _db;

        public AccountController()
        {
            _db = new DBHelper.DbHelper();
        }
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        public ActionResult Registration()
        {
            return View();
        }

        //POST: Registration/Register
        [HttpPost]
        public ActionResult Register(RegistrationViewModel uModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var fname = uModel.FirstName;
                    var lname = uModel.LastName;
                    var phn = uModel.Phone;
                    var email = uModel.Email;
                    var pass = uModel.Password;

                    _db.CreateNewUser(fname, lname, phn, email, pass);
                    return View("Register");
                }
                else
                {
                    return View();
                }
            }
            catch
            {
                return View();
            }
        }

        
    }
}