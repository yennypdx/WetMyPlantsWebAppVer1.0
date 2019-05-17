using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using DbHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Moq;
using WebApp.Controllers;
using WebApp.Models.AccountViewModels;
using Crypto = DbHelper.Crypto;
using HttpStatusCodeResult = System.Web.Mvc.HttpStatusCodeResult;
using JsonResult = System.Web.Mvc.JsonResult;
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

        /*************************************************************************************************/

        private readonly ApiController _api;
        private readonly List<User> _userList;
        private readonly List<Plant> _plantList;
        private readonly List<Species> _speciesList;
        private readonly Dictionary<int, string> _tokenTable;
        private readonly Dictionary<int, string> _userPlantTable;
        private readonly Dictionary<int, string> _resetCodeTable;

        private readonly User _testUser;
        private readonly Plant _testPlant;
        private readonly Species _testSpecies;

        private readonly Mock<HttpContextBase> _mockContext;


        public ApiControllerTests()
        {
            _mockContext = new Mock<HttpContextBase>();
            var mockRequest = new Mock<HttpRequestBase>();
            // Set up test objects
            _testUser = new User
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Test",
                Email = "test@email.com",
                Password = "password",
                Hash = Crypto.HashPassword("password"),
                Phone = "1234567890",
                Plants = new List<string>()
            };
            _testSpecies = new Species
            {
                Id = 1,
                CommonName = "Test",
                LatinName = "Test",
                LightMax = 100,
                LightMin = 0,
                WaterMax = 100,
                WaterMin = 0
            };
            _testPlant = new Plant
            {
                Id = "C4:7C:8D:6A:51:E9",
                Nickname = "Test",
                CurrentLight = 50,
                CurrentWater = 50,
                SpeciesId = _testSpecies.Id
            };

            // Initialize lists and dictionaries
            _userList = new List<User>();
            _plantList = new List<Plant>();
            _speciesList = new List<Species>();
            _tokenTable = new Dictionary<int, string>();
            _userPlantTable = new Dictionary<int, string>();
            _resetCodeTable = new Dictionary<int, string>();

            // Database Moq Setup
            var dbMock = new Mock<IDbHelper>();

            // bool CreateNewUser(string firstName, string lastName, string phone, string email, string password)
            dbMock.Setup(db => db.CreateNewUser(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string firstName, string lastName, string phone, string email, string password) =>
                {
                    if (_userList.Exists(u => u.Email.Equals(email)))
                        return false;

                    var user = new User
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

                    _userList.Add(user);
                    return true;
                });

            dbMock.Setup(db => db.LoginAndGetToken(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string password) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Email.Equals(email));

                    if (user == null) return null;

                    string token;
                    if (_tokenTable.ContainsKey(user.Id))
                        token = _tokenTable[user.Id];
                    else
                    {
                        token = Crypto.HashPassword(DateTime.Today.ToLongDateString());
                        _tokenTable.Add(user.Id, token);
                    }

                    return Crypto.ValidatePassword(password, user.Hash)
                        ? token
                        : null;
                });

            dbMock.Setup(db => db.FindUser(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string token) =>
                {
                    if (email != null)
                        return _userList.FirstOrDefault(u => u.Email.Equals(email));
                    if (token != null)
                    {
                        if (_tokenTable.ContainsValue(token))
                        {
                            var result = _tokenTable.FirstOrDefault(k => k.Value.Equals(token));
                            var key = result.Key;
                            var user = _userList.FirstOrDefault(u => u.Id.Equals(key));
                            return user;
                        }
                    }

                    return null;
                });
            dbMock.Setup(db => db.FindUser(It.IsAny<int>()))
                .Returns((int id) => { return _userList.FirstOrDefault(u => u.Id.Equals(id)); });

            dbMock.Setup(db => db.DeleteUser(It.IsAny<string>()))
                .Returns((string email) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Email.Equals(email));

                    return user != null && _userList.Remove(user);
                });

            dbMock.Setup(db => db.UpdateUser(It.IsAny<User>()))
                .Returns((User update) =>
                {
                    //if (!_userList.Exists(u => u.Id.Equals(update.Id)))
                    if (_userList.FirstOrDefault(u => u.Id.Equals(update.Id)) == null)
                        return false;

                    _userList.Remove(_userList.First(u => u.Id.Equals(update.Id)));
                    _userList.Add(update);
                    return true;
                });

            dbMock.Setup(db => db.SetResetCode(It.IsAny<int>(), It.IsAny<string>()))
                .Callback((int userId, string code) =>
                {
                    if (!_resetCodeTable.ContainsKey(userId))
                        _resetCodeTable.Add(userId, code);
                });

            dbMock.Setup(db => db.ValidateResetCode(It.IsAny<int>(), It.IsAny<string>()))
                .Returns((int userId, string code) => _resetCodeTable.ContainsKey(userId)
                                                      && _resetCodeTable[userId] == code);

            _api = new ApiController(dbMock.Object);

            var requestContext = new RequestContext { HttpContext = _mockContext.Object, RouteData = new RouteData() };
            var helper = new UrlHelper(requestContext);

            var controllerContext = new ControllerContext(_mockContext.Object, new RouteData(), _api);
            _api.Url = helper;
            _api.ControllerContext = controllerContext;

            _tokenTable.Add(_testUser.Id, Crypto.HashPassword(DateTime.Today.ToLongDateString()));

            _mockContext.Setup(x => x.Request).Returns(mockRequest.Object);
            _mockContext.SetupGet(c => c.Session["User"]).Returns(_testUser);
            _mockContext.SetupGet(c => c.Session["Token"]).Returns(_tokenTable[_testUser.Id]);

            mockRequest.SetupGet(c => c.Url).Returns(new Uri("https://wetmyplants.azurewebsites.net"));
        }

        [TestInitialize]
        public void Init()
        {
            _userList.Clear();
            _userList.Add(_testUser);

            _plantList.Clear();
            _plantList.Add(_testPlant);

            _speciesList.Clear();
            _speciesList.Add(_testSpecies);

            _tokenTable.Clear();
            _tokenTable.Add(_testUser.Id, Crypto.HashPassword(DateTime.Today.ToLongDateString()));

            _resetCodeTable.Clear();

            _userPlantTable.Clear();
            _userPlantTable.Add(_testUser.Id, _testPlant.Id);
        }

        [TestMethod]
        public void ApiController_GetHelloWorld()
        {
            var msg = _api.Index();

            Assert.AreEqual("hello world!", msg);
        }

        [TestMethod]
        public void ApiController_Login_InvalidModel()
        {
            var model = new LoginViewModel();
            ValidateModel(model);
            
            var result = _api.Login(model) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("Invalid login model", Json.Decode(result?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiController_Login_InvalidPassword()
        {
            var model = new LoginViewModel
            {
                Email = _testUser.Email,
                Password = "wrongPassword",
                RememberMe = false
            };

            ValidateModel(model);

            var result = _api.Login(model) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("Invalid login", Json.Decode(result?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiController_Login()
        {
            var model = new LoginViewModel
            {
                Email = _testUser.Email,
                Password = _testUser.Password,
                RememberMe = false
            };
            ValidateModel(model);

            var result = _api.Login(model) as JsonResult;
            if (result == null) Assert.Fail("Result was null");

            var data = Json.Decode(result.Data.ToString());
            if (data == null) Assert.Fail("Result data was null");

            try
            {
                var token = data["content"];
                Assert.AreEqual(_tokenTable[_testUser.Id], token, "Token did not match");
            }
            catch (Exception)
            {
                Assert.Fail("Data did not carry expected content");
            }            
        }

        [TestMethod]
        public void ApiController_Register_UserAlreadyExists()
        {
            var registerModel = new RegistrationViewModel
            {
                FirstName = "New",
                LastName = "User",
                Email = _testUser.Email,
                Password = "password",
                ConfirmPassword = "password",
                Phone = "1234567890"
            };

            var result = _api.RegisterUser(registerModel) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("User already exists", Json.Decode(result?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiController_Register_InvalidModel()
        {
            var model = new RegistrationViewModel();
            ValidateModel(model);

            var result = _api.RegisterUser(model) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("Invalid registration model", Json.Decode(result?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiController_Register()
        {
            var newUser = new RegistrationViewModel
            {
                Email = "new@user.test",
                Password = "password",
                ConfirmPassword = "password",
                FirstName = "New",
                LastName = "User",
                Phone = "1234567890"
            };
            ValidateModel(newUser);

            var result = _api.RegisterUser(newUser) as JsonResult;
            if (result == null) Assert.Fail("Result is null");

            var data = Json.Decode(result.Data.ToString());
            if (data == null) Assert.Fail("Data is null");

            try
            {
                var token = data["content"];
                var user = _userList.FirstOrDefault(u => u.Email.Equals(newUser.Email));
                Assert.IsNotNull(user);
                Assert.AreEqual(_tokenTable[user.Id], token, "Token did not match");
            }
            catch (Exception)
            {
                Assert.Fail("Data did not contain expected content");
            }
        }

        [TestMethod]
        public void ApiController_DeleteUser()
        {
            //var deleteResult = _api.DeleteUser(_testUser.Id) as HttpStatusCodeResult;

            //Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), deleteResult?.StatusCode);
            //Assert.AreEqual("User deleted", Json.Decode(deleteResult?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiController_DeleteUser_InvalidId()
        {
            //var id = _testUser.Id + 111;

            //var result = _api.DeleteUser(id) as HttpStatusCodeResult;
            //if (result == null) Assert.Fail("Result was null");

            //Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result.StatusCode, "Status was not 500 BAD REQUEST");
        }

        [TestMethod]
        public void ApiController_ForgotUserPasswordViaEmailTest()
        {
            var model = new ForgotPasswordViewModel
            {
                Email = _testUser.Email
            };

            var result = _api.ForgotUserPasswordViaEmail(model) as HttpStatusCodeResult;
            if (result == null) Assert.Fail("Result was null");

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), result.StatusCode, "Result was not 200 OK");
        }

        [TestMethod]
        public void ApiController_ForgotUserPasswordViaEmail_InvalidUser()
        {
            var model = new ForgotPasswordViewModel
            {
                Email = "invalid@email.domain"
            };

            var result = _api.ForgotUserPasswordViaEmail(model) as HttpStatusCodeResult;
            if (result == null) Assert.Fail("Result was null");

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result.StatusCode, "Status was not 400 BAD REQUEST");
        }

        [TestMethod]
        public void ApiController_ForgotUserPasswordViaText()
        {
            var model = new ForgotPasswordViewModel
            {
                Email = _testUser.Email
            };

            var result = _api.ForgotUserPasswordViaText(model) as HttpStatusCodeResult;
            if (result == null) Assert.Fail("Result was null");

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), result.StatusCode, "Result was not 200 OK");
        }

        [TestMethod]
        public void ApiController_ForgotUserPasswordViaText_InvalidUser()
        {
            var model = new ForgotPasswordViewModel
            {
                Email = "invalid@email.domain"
            };

            var result = _api.ForgotUserPasswordViaText(model) as HttpStatusCodeResult;
            if(result == null)
                Assert.Fail("Result was null");

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result.StatusCode, "Status was not 400 BAD REQUEST");
        }

        [TestMethod]
        public void ApiController_SubmitUserPin()
        {
            var pin = "12345";
            _resetCodeTable.Add(_testUser.Id, pin);

            var result = _api.SubmitUserPin(pin, _testUser.Email) as HttpStatusCodeResult;
            if (result == null) Assert.Fail("Result was null");

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), result.StatusCode, "Status was not 200 OK");
        }

        [TestMethod]
        public void ApiController_SubmitUserPin_InvalidPin()
        {
            var pin = "12345";
            var wrongPin = "09876";
            _resetCodeTable.Add(_testUser.Id, pin);

            var result = _api.SubmitUserPin(wrongPin, _testUser.Email) as HttpStatusCodeResult;
            if (result == null) Assert.Fail("Result was null");

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result.StatusCode, "Status was not 500 BAD REQUEST");
        }

        [TestMethod]
        public void ApiController_SubmitUserPin_InvalidEmail()
        {
            var pin = "12345";
            _resetCodeTable.Add(_testUser.Id, pin);

            var result = _api.SubmitUserPin(pin, "wrong@email.com") as HttpStatusCodeResult;
            if (result == null) Assert.Fail("Result was null");

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result.StatusCode, "Status was not 500 BAD REQUEST");
        }

        [TestMethod]
        public void ApiController_GetUserDetail()
        {
            var token = _tokenTable.FirstOrDefault(t => t.Key.Equals(_testUser.Id)).Value;
            var result = _api.GetUserDetail(token);
            //if (result == null) Assert.Fail("Result was null");
            var data = result.Data.ToString();

            //var returned = Json.Decode<User>(data);
            //if (returned == null) Assert.Fail("Data was null");

            Assert.AreEqual(data, "Models.User", "User returned did not have matching value");
        }

        [TestMethod]
        public void ApiController_GetUserDetail_InvalidToken()
        {
            var token = "123456789";

            var result = _api.GetUserDetail(token);
            //if (result == null) Assert.Fail("Result was null");
            //var returned = Json.Decode(result.Data.ToString())["content"];

            Assert.IsNull(result, "Returned null");
        }

        [TestMethod]
        public void ApiController_UpdateAccountInfo()
        {
            var token = "12345";
            //var model = _testUser;
            //model.Email = "new@email.address";
            var model = new User
            {
                Id = _testUser.Id,
                FirstName = _testUser.FirstName,
                LastName = _testUser.LastName,
                Phone = _testUser.Phone,
                Email = "new@email.address",
                Hash = _testUser.Hash,
                Password = _testUser.Password,
                Plants = _testUser.Plants
            };

            var result = _api.UpdateAccountInfo(token, model) as HttpStatusCodeResult;
            if (result == null)
                Assert.Fail("Result was null");

            //Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), result.StatusCode, "Status was not 200 OK");
        }

        [TestMethod]
        public void ApiController_UpdateAccountInfo_InvalidUserModel()
        {
            //var model = _testUser;
            var model = new User
            {
                Id = _testUser.Id + 111,
                Email = "new@email.address",
                FirstName = _testUser.FirstName,
                LastName = _testUser.LastName,
                Hash = _testUser.Hash,
                Password = _testUser.Password,
                Phone = _testUser.Phone,
                Plants = _testUser.Plants
            };

            //model.Email = "new@email.address";
            //model.Id = _testUser.Id + 111;

            //var result = _api.UpdateAccountInfo(model) as HttpStatusCodeResult;
            //if (result == null) Assert.Fail("Result is null");

            //Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result.StatusCode, "Status was not 500 BAD REQUEST");
        }
    }
}