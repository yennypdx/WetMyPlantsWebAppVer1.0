using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp.Models;

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
        public ActionResult RegUser(UserModel uModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    ViewData["FirstName"] = uModel.FirstName;
                    ViewData["LastName"] = uModel.LastName;
                    ViewData["Phone"] = uModel.Phone;
                    ViewData["Email"] = uModel.Email;
                    ViewData["ConfirmPassword"] = uModel.Password;

                    SubmitUserDataToDb(uModel);
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

        public void SubmitUserDataToDb(UserModel uModel)
        {
            //TODO: get the connection from DBHelper
            var connectionStr = "";
            //TODO: reconfirm the sequence of the attributes in the table
            var queryInsertDataToUserDb = "INSERT INTO Users (FirstName, LastName, Password, Email, Password) " +
                                          "VALUES (@fname, @lname, @phn, @mail, @pass)";

            using (var connection = new SqlConnection(connectionStr))
            {
                var commandUserDb = new SqlCommand(queryInsertDataToUserDb, connection);
                commandUserDb.Parameters.AddWithValue("@fname", uModel.FirstName);
                commandUserDb.Parameters.AddWithValue("@lname", uModel.LastName);
                commandUserDb.Parameters.AddWithValue("@phn", uModel.Phone);
                commandUserDb.Parameters.AddWithValue("@mail", uModel.Email);
                commandUserDb.Parameters.AddWithValue("@pass", uModel.Password);

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