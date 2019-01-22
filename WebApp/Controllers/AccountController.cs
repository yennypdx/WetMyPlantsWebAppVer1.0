using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp.Models;
using WebApp.Models.AccountViewModels;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
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

                    SubmitUserDataToDb(fname, lname, phn, email, pass);
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

        public void SubmitUserDataToDb(string inFname, string inLname, string inPhn, string inEmail, string inPass)
        {
            //TODO: get the connection from DBHelper
            var connectionStr = "";
            //TODO: reconfirm the sequence of the attributes in the table
            var queryInsertDataToUserDb = "INSERT INTO Users (FirstName, LastName, Password, Email, Password) " +
                                          "VALUES (@fname, @lname, @phn, @mail, @pass)";

            using (var connection = new SqlConnection(connectionStr))
            {
                var commandUserDb = new SqlCommand(queryInsertDataToUserDb, connection);
                commandUserDb.Parameters.AddWithValue("@fname", inFname);
                commandUserDb.Parameters.AddWithValue("@lname", inLname);
                commandUserDb.Parameters.AddWithValue("@phn", inPhn);
                commandUserDb.Parameters.AddWithValue("@mail", inEmail);
                commandUserDb.Parameters.AddWithValue("@pass", inPass);

                try
                {
                    connection.Open();
                    commandUserDb.ExecuteNonQuery();
                }
                catch (SqlException sqlEx)
                {
                    sqlEx.Errors.ToString();
                }
            }
        }
    }
}