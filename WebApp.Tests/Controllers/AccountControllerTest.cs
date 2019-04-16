using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
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
    }
}