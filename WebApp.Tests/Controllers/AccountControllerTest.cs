﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using DbHelper;
using DBHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Moq;
using WebApp.Controllers;
using WebApp.Models.AccountViewModels;

namespace WebApp.Tests.Controllers
{
    [TestClass]
    public class AccountControllerTest
    {
        // Is this okay? Constructor takes a database
        //private readonly DBHelper.IDbHelper db = new DBHelper.DbHelper();
        private HttpContextBase _context;
        private HttpRequestBase _request;
        private Mock<HttpContextBase> _mockContext;
        private Mock<HttpRequestBase> _mockRequest;
        private ControllerContext _controllerContext;
        private readonly Mock<IDbHelper> _db;
        private readonly AccountController _accountController;
        private readonly Dictionary<int, string> _resetCodeDictionary;
        private readonly Dictionary<int, string> _tokenDictionary;
        private readonly List<User> _userList;
        private readonly User _testUser;


        public AccountControllerTest()
        {
            _resetCodeDictionary = new Dictionary<int, string>();
            _tokenDictionary = new Dictionary<int, string>();
            _userList = new List<User>();

            _mockContext = new Mock<HttpContextBase>();
            _mockRequest = new Mock<HttpRequestBase>();
            _mockContext.Setup(x => x.Request).Returns(_mockRequest.Object);
            _mockRequest.SetupGet(c => c.Url).Returns(new Uri("https://wetmyplants.azurewebsites.net"));
            var requestContext = new RequestContext();
            requestContext.HttpContext = _mockContext.Object;
            requestContext.RouteData = new RouteData();
            UrlHelper helper = new UrlHelper(requestContext);

            _testUser = new User
            {
                Id = 0,
                Email = "test@user.com",
                FirstName = "Test",
                LastName = "Test",
                Password = "password",
                Hash = Crypto.HashPassword("password"),
                Phone = "1234567890",
                Plants = new List<int>()
            };

            _db = new Mock<IDbHelper>();

            _db.Setup(db => db.SetResetCode(It.IsAny<int>(), It.IsAny<string>()))
                .Callback((int userId, string resetCode) => _resetCodeDictionary.Add(userId, resetCode));

            _db.Setup(db => db.ValidateResetCode(It.IsAny<int>(), It.IsAny<string>()))
                .Returns((int userId, string resetCode) => _resetCodeDictionary[userId] == resetCode);

            _db.Setup(db => db.DeleteResetCode(It.IsAny<int>()))
                .Callback((int userId) => { _resetCodeDictionary.Remove(userId); });

            _db.Setup(db => db.CreateNewUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string firstName, string lastName, string phone, string email, string password) =>
                {
                    if (_userList.Exists(u => u.Email.Equals(email))) return false;

                    var newUser = new User
                    {
                        Id = _userList.Count + 1,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        Password = password,
                        Hash = Crypto.HashPassword(password),
                        Phone = phone,
                        Plants = new List<int>()
                    };

                    _userList.Add(newUser);
                    return true;
                });

            _db.Setup(db => db.DeleteUser(It.IsAny<string>()))
                .Returns((string email) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Email.Equals(email));
                    return user != null && _userList.Remove(user);
                });

            _db.Setup(db => db.FindUser(It.IsAny<int>()))
                .Returns((int userId) => _userList.FirstOrDefault(u => u.Id.Equals(userId)));

            _db.Setup(db => db.FindUser(It.IsAny<string>()))
                .Returns((string email) => _userList.FirstOrDefault(u => u.Email.Equals(email)));

            _db.Setup(db => db.AuthenticateUser(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string password) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Email.Equals(email));
                    return user != null && Crypto.ValidatePassword(password, user.Hash);
                });

            _db.Setup(db => db.LoginAndGetToken(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string password) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Email.Equals(email));

                    if (user == null) return null;

                    if (!Crypto.ValidatePassword(password, user.Hash)) return null;

                    var token = _tokenDictionary[user.Id];

                    if(token != null)
                        return token;

                    token = Crypto.HashPassword(DateTime.Today.ToLongDateString());
                    _tokenDictionary[user.Id] = token;

                    return token;
                });

            _db.Setup(db => db.ResetPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string newPassword) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Email.Equals(email));

                    if (user == null) return false;

                    _userList.Remove(user);

                    user.Password = newPassword;
                    user.Hash = Crypto.HashPassword(newPassword);

                    _userList.Add(user);
                    return true;
                });

            _db.Setup(db => db.UpdateUser(It.IsAny<User>()))
                .Returns((User update) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Id.Equals(update.Id));

                    if (user == null) return false;

                    _userList.Remove(user);
                    _userList.Add(update);

                    return true;
                });

            _accountController = new AccountController(_db.Object);
            _accountController.Url = helper;
            // Setup the context (HttpContext and HttpResult objects)
            _controllerContext = new ControllerContext(_mockContext.Object, new RouteData(), _accountController);
            _accountController.ControllerContext = _controllerContext;
        }

        [TestInitialize]
        public void Initialize()
        {
            _userList.Clear();
            _userList.Add(_testUser);

            _resetCodeDictionary.Clear();

            _tokenDictionary.Clear();
        }

        [TestMethod]
        public void Login_ShouldReturnTheLoginViewModel()
        {
            //var controller = new AccountController(db);
            var result = _accountController.Login() as ViewResult;
            if (result == null) Assert.Fail("Login ViewResult was null");

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            //Assert.AreEqual(string.Empty, result.ViewName);
        }

        [TestMethod]
        public void Register_ShouldReturnTheRegistrationViewModel()
        {
            //var controller = new AccountController(db);
            var result = _accountController.Register() as ViewResult;
            //var model = result.Model as RegistrationViewModel;
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            //Assert.AreEqual(string.Empty, result.ViewName);
        }

        [TestMethod]
        public void ForgotPassword_ShouldReturnTheForgotPasswordViewModel()
        {
            //var controller = new AccountController(db);
            var result = _accountController.ForgotPassword() as ViewResult;
            //var model = result.Model as ForgotPasswordViewModel;
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            //Assert.AreEqual(string.Empty, result.ViewName);
        }

        [TestMethod]
        public void AccountController_ResetPasswordSuccess()
        {
            var code = "TestResetCode1234567890";
            _resetCodeDictionary[_testUser.Id] = code;

            var result = _accountController.ResetPassword(_testUser.Id, code) as ViewResult;
            if (result == null) Assert.Fail();

            var model = result.Model as ResetPasswordViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(_testUser.Email, model.Email);
        }

        [TestMethod]
        public void AccountController_ResetPasswordInvalidCodeFail()
        {
            var code = "TestResetCode1234567890";
            var wrongCode = "0987654321edoCteseRtseT";

            _resetCodeDictionary[_testUser.Id] = code;

            var result = _accountController.ResetPassword(_testUser.Id, wrongCode) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result was not null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Result does not contain action redirect");
            Assert.AreEqual("Login", result.RouteValues["action"], "Result did not redirect to Login");
        }

        [TestMethod]
        public void AccountController_ResetPasswordDeletesResetCode()
        {
            var code = "TestResetCode1234567890";
            _resetCodeDictionary.Add(_testUser.Id, code);

            _accountController.ResetPassword(_testUser.Id, code);

            Assert.IsFalse(_resetCodeDictionary.ContainsKey(_testUser.Id));
        }

        [TestMethod]
        public void AccountController_ForgotUserPasswordInvalidUser()
        {
            var model = new ForgotPasswordViewModel
            {
                Email = "nonexistant@email.com"
            };

            var result = _accountController.ForgotUserPassword(model) as RedirectResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.AreEqual("ForgotPassword", result.Url, "Result does not redirect to ForgotPassword");
        }

        [TestMethod]
        public void AccountController_ForgotUserPasswordSetsResetCode()
        {

            var prevCodes = _resetCodeDictionary.ToList();
            var model = new ForgotPasswordViewModel
            {
                Email = _testUser.Email
            };

            _accountController.ForgotUserPassword(model);

            var curCodes = _resetCodeDictionary.ToList();

            Assert.AreNotEqual(prevCodes, curCodes, "Codes are the same");
            Assert.AreEqual(prevCodes.Count + 1, curCodes.Count, "One new code was not added");
        }
    }
}