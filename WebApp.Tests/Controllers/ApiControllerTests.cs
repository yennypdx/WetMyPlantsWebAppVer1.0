using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DBHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApp.Controllers;
using WebApp.Models.AccountViewModels;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace WebApp.Tests.Controllers
{
    [TestClass]
    public class ApiControllerTests
    {
        // This method based on a StackOverflow answer found at https://stackoverflow.com/questions/1269713/unit-tests-on-mvc-validation/3353125#3353125
        // Thank you to Giles Smith for providing the answer!
        // Since we are passing models directly into the controller, it skips the standard model binding validation process;
        // This ValidateModel method mimics the validation process that happens during binding.
        private void ValidateModel(object model)
        {
            var context = new ValidationContext(model, null, null);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results);

            foreach (var x in results)
                _api.ModelState.AddModelError(x.MemberNames.FirstOrDefault() ?? string.Empty, x.ErrorMessage);
        }
        private readonly ApiController _api;

        public ApiControllerTests()
        {
            _api = new ApiController(new DBHelper.DbHelper(AccessHelper.GetTestDbConnectionString()));
        }

        [TestMethod]
        public void ApiTestGetHelloWorld()
        {
            var msg = _api.Index();

            Assert.AreEqual("hello world!", msg);
        }

        [TestMethod]
        public void ApiTestLoginFailInvalidModel()
        {
            var model = new LoginViewModel();
            ValidateModel(model);
            
            var result = _api.Login(model) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("Invalid login model", result?.StatusDescription);
        }

        [TestMethod]
        public void ApiTestLoginFailInvalidPassword()
        {
            var model = new LoginViewModel
            {
                Email = "test@test.test",
                Password = "wrongPassword",
                RememberMe = false
            };

            ValidateModel(model);

            var result = _api.Login(model) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("Invalid login", result?.StatusDescription);
        }
    }
}
