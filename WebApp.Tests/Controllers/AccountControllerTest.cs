using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using DbHelper;
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
        private readonly Mock<HttpContextBase> _mockContext;
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
            var mockRequest = new Mock<HttpRequestBase>();

            _testUser = new User
            {
                Id = 0,
                Email = "test@user.com",
                FirstName = "Test",
                LastName = "Test",
                Password = "password",
                Hash = Crypto.HashPassword("password"),
                Phone = "1234567890",
                Plants = new List<string>()
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
                        Plants = new List<string>()
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

            _db.Setup(db => db.FindUser(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string token) =>
                {
                    if(email != null)
                        return _userList.FirstOrDefault(u => u.Email.Equals(email));
                    if(token != null)
                    {
                        if(_tokenDictionary.ContainsValue(token))
                        {
                            var result = _tokenDictionary.FirstOrDefault(k => k.Value.Equals(token));
                            var key = result.Key;
                            var user = _userList.FirstOrDefault(u => u.Id.Equals(key));
                            return user;
                        }
                    }

                    return null;
                });

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

                    string token;
                    if (_tokenDictionary.ContainsKey(user.Id))
                    {
                        token = _tokenDictionary[user.Id];
                    }
                    else
                    {
                        token = Crypto.HashPassword(DateTime.Today.ToLongDateString());
                        _tokenDictionary[user.Id] = token;
                    }

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

            _tokenDictionary.Add(_testUser.Id, Crypto.HashPassword(DateTime.Today.ToLongDateString()));

            // Set up the controller context for using server variables
            _accountController = new AccountController(_db.Object);
            var requestContext = new RequestContext {HttpContext = _mockContext.Object, RouteData = new RouteData()};
            var helper = new UrlHelper(requestContext);

            var controllerContext = new ControllerContext(_mockContext.Object, new RouteData(), _accountController);
            _accountController.Url = helper;
            _accountController.ControllerContext = controllerContext;

            _mockContext.Setup(x => x.Request).Returns(mockRequest.Object);
            _mockContext.SetupGet(c => c.Session["User"]).Returns(_testUser);
            _mockContext.SetupGet(c => c.Session["Token"]).Returns(_tokenDictionary[_testUser.Id]);

            mockRequest.SetupGet(c => c.Url).Returns(new Uri("https://wetmyplants.azurewebsites.net"));
        }

        [TestInitialize]
        public void Initialize()
        {
            _userList.Clear();
            _userList.Add(_testUser);

            _resetCodeDictionary.Clear();

            _tokenDictionary.Clear();
            _tokenDictionary.Add(_testUser.Id, Crypto.HashPassword(DateTime.Today.ToLongDateString()));
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

        [TestMethod]
        public void AccountController_ResetUserPasswordSuccess()
        {
            var newPassword = "newPassword";
            var model = new ResetPasswordViewModel
            {
                Email = _testUser.Email,
                Password = newPassword,
                ConfirmPassword = newPassword
            };

            var result = _accountController.ResetUserPassword(model) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Result does not contain a redirect to an action");
            Assert.AreEqual("Login", result.RouteValues["action"], "Result did not redirect to Login");
            Assert.IsTrue(_db.Object.AuthenticateUser(_testUser.Email, newPassword), "Unable to authenticate with the new password");
        }

        [TestMethod]
        public void AccountController_ResetUserPasswordInvalidConfirm()
        {
            var newPassword = "newPassword";
            var model = new ResetPasswordViewModel
            {
                Email = _testUser.Email,
                Password = newPassword,
                ConfirmPassword = "notNewPassword"
            };

            var result = _accountController.ResetUserPassword(model) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Result does not contain a redirect to an action");
            Assert.AreEqual("Login", result.RouteValues["action"], "Result did not redirect to Login");
            Assert.IsFalse(_db.Object.AuthenticateUser(_testUser.Email, newPassword), "Password was changed with a non-matching ConfirmPassword");
        }

        [TestMethod]
        public void AccountController_MyAccountAuthorized_ReturnsCorrectModelTest()
        {
            var result = _accountController.MyAccount() as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Model);

            var model = result.Model as MyAccountViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(_testUser.Id, model.Id);
        }

        [TestMethod]
        public void AccountController_LogoutTest()
        {
            _mockContext.Setup(c => c.Session.Abandon()); // Catch the call, do nothing

            var result = _accountController.Logout() as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result was null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Result does not contain a redirect to an action");
            Assert.AreEqual("Login", result.RouteValues["action"], "Result did not redirect to Login");
        }

        [TestMethod]
        public void AccountController_RegisterNewUserSuccess()
        {
            var newUser = new RegistrationViewModel
            {
                Email = "new@test.user",
                Password = "password",
                ConfirmPassword = "password",
                FirstName = "Test",
                LastName = "Test",
                Phone = "1234567890"
            };

            var result = _accountController.RegisterUser(newUser) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result was null");
            Assert.IsTrue(_userList.Exists(u => u.Email.Equals(newUser.Email)), "User does not exist in list");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Does not return to an action");
            Assert.AreEqual("Index", result.RouteValues["action"], "Did not redirect to Index");
        }

        [TestMethod]
        public void AccountController_RegisterUser_UserAlreadyExists()
        {
            var model = new RegistrationViewModel
            {
                Email = _testUser.Email,
                Password = "password",
                ConfirmPassword = "password",
                FirstName = "Test",
                LastName = "Test",
                Phone = "1234567890"
            };

            var result = _accountController.RegisterUser(model) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Does not redirect to an action");
            Assert.AreEqual("Register", result.RouteValues["action"], "Did not redirect to Register");
        }

        [TestMethod]
        public void AccountController_LoginUserSuccess()
        {
            var model = new LoginViewModel
            {
                Email = _testUser.Email,
                Password = _testUser.Password,
                RememberMe = false
            };

            var result = _accountController.LoginUser(model) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Did not redirect to an action");
            Assert.AreEqual("Index", result.RouteValues["action"], "Did not redirect to Index");
        }

        [TestMethod]
        public void AccountController_LoginUserInvalidPassword()
        {
            var model = new LoginViewModel
            {
                Email = _testUser.Email,
                Password = "wrongPassword",
                RememberMe = false
            };

            var result = _accountController.LoginUser(model) as ViewResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.AreEqual("Login", result.ViewName, "Did not return the Login view");
        }

        [TestMethod]
        public void AccountController_LoginUserInvalidEmail()
        {
            var model = new LoginViewModel
            {
                Email = "wrong@email.fail",
                Password = _testUser.Password,
                RememberMe = false
            };

            var result = _accountController.LoginUser(model) as ViewResult;

            Assert.IsNotNull(result, "Result was null");
            Assert.AreEqual("Login", result.ViewName, "Did not return the Login view");
        }

        [TestMethod]
        public void AccountController_UpdateUserSuccess()
        {
            var model = new MyAccountViewModel
            {
                Id = _testUser.Id,
                Email = "new@test.email",
                FirstName = "Update",
                LastName = "Update",
                Phone = "0009998888"
            };

            var result = _accountController.UpdateUser(model) as RedirectToRouteResult;
            var update = _userList.FirstOrDefault(u => u.Id.Equals(_testUser.Id));

            Assert.IsNotNull(update, "Updated user not found");
            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Result did not contain redirect to an action");
            Assert.AreEqual("MyAccount", result.RouteValues["action"], "Did not redirect to MyAccount");
            Assert.IsTrue(model.Id.Equals(update.Id) && model.Email.Equals(update.Email)
                          && model.FirstName.Equals(update.FirstName)
                          && model.LastName.Equals(update.LastName)
                          && model.Phone.Equals(update.Phone), "User wasn't updated correctly");
        }

        [TestMethod]
        public void AccountController_UpdateUserInvalidId()
        {
            var model = new MyAccountViewModel
            {
                Id = 100,
                Email = "new@test.emal",
                FirstName = "Update",
                LastName = "Update",
                Phone = "0987654321"
            };

            var result = _accountController.UpdateUser(model) as RedirectToRouteResult;
            var update = _userList.FirstOrDefault(u => u.Id.Equals(model.Id));

            Assert.IsNull(update, "Invalid user added to the database");
            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Result does not contain redirect to action");
            Assert.AreEqual("MyAccount", result.RouteValues["action"], "Result did not redirect to MyAccount");
        }

        [TestMethod]
        public void AccountController_DeleteUserLoadView()
        {
            var result = _accountController.DeleteUser() as ViewResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.AreEqual("DeleteUser", result.ViewName, "Did not load DeleteUser view");
            Assert.IsNotNull(result.Model);
        }

        [TestMethod]
        public void AccountController_ConfirmDeletion_Success()
        {
            var model = new DeleteUserViewModel
            {
                Email = _testUser.Email,
                Password = _testUser.Password
            };

            var result = _accountController.ConfirmDeletion(model) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Does not contain a redirect to an action");
            Assert.AreEqual("Login", result.RouteValues["action"], "Did not redirect to Login");
            Assert.IsFalse(_userList.Exists(u => u.Email.Equals(_testUser.Email)), "User list still contains user");
        }

        [TestMethod]
        public void AccountController_ConfirmDeletion_UnauthenticatedUser()
        {
            var model = new DeleteUserViewModel
            {
                Email = _testUser.Email,
                Password = "wrongPassword"
            };

            var result = _accountController.ConfirmDeletion(model) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Does not redirect to an action");
            Assert.AreEqual("DeleteUser", result.RouteValues["action"], "Did not redirect to DeleteUser");
        }

        [TestMethod]
        public void AccountController_ChangePassword_ViewLoad()
        {
            var result = _accountController.ChangePassword(_testUser.Email) as ViewResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.AreEqual("ChangePassword", result.ViewName, "Did not return ChangePassword view");
            Assert.IsNotNull(result.Model, "Model is null");

            var model = (ChangePasswordViewModel)result.Model;

            Assert.AreEqual(_testUser.Email, model.Email, "View model email does not match user");
        }

        [TestMethod]
        public void AccountController_ConfirmPasswordChange_AuthorizedSuccess()
        {
            var model = new ChangePasswordViewModel
            {
                Email = _testUser.Email,
                Password = _testUser.Password,
                NewPassword = "NewPassword"
            };

            var result = _accountController.ConfirmPasswordChange(model) as RedirectToRouteResult;

            Assert.IsNotNull(result, "Result is null");
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Result does not redirect to an action");
            Assert.AreEqual("MyAccount", result.RouteValues["action"], "Did not redirect to MyAccount");
            Assert.IsTrue(Crypto.ValidatePassword("NewPassword", _userList.FirstOrDefault(u => u.Email.Equals(_testUser.Email))?.Hash), "Password did not update");
        }

        [TestMethod]
        public void AccountController_ConfirmPasswordChange_UnauthenticatedUser()
        {
            var model = new ChangePasswordViewModel
            {
                Email = _testUser.Email,
                Password = "wrongPassword",
                NewPassword = "NewPassword"
            };

            var result = _accountController.ConfirmPasswordChange(model) as RedirectToRouteResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.RouteValues.ContainsKey("action"), "Does not redirect to an action");
            Assert.AreEqual("ChangePassword", result.RouteValues["action"], "Did not redirect to ChangePassword");
            Assert.IsTrue(Crypto.ValidatePassword(_testUser.Password, _userList.FirstOrDefault(u => u.Email.Equals(_testUser.Email))?.Hash), "Password changed");
        }
    }
}