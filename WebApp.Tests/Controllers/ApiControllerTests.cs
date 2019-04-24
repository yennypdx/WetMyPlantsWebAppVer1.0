using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;
using DBHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Moq;
using WebApp.Controllers;
using WebApp.Models.AccountViewModels;
using Crypto = DbHelper.Crypto;
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
        private readonly Mock<IDbHelper> _dbMock;
        private readonly List<User> _userList;
        private readonly List<Plant> _plantList;
        private readonly List<Species> _speciesList;
        private readonly Dictionary<int, string> _tokenTable;
        private readonly Dictionary<int, int> _userPlantTable;
        private readonly Dictionary<int, string> _resetCodeTable;

        private readonly User _testUser;
        private readonly Plant _testPlant;
        private readonly Species _testSpecies;

        public ApiControllerTests()
        {
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
                Plants = new List<int>()
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
                Id = 1,
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
            _userPlantTable = new Dictionary<int, int>();
            _resetCodeTable = new Dictionary<int, string>();

            // Database Moq Setup
            _dbMock = new Mock<IDbHelper>();

            // bool CreateNewUser(string firstName, string lastName, string phone, string email, string password)
            _dbMock.Setup(db => db.CreateNewUser(
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
                        Plants = new List<int>()
                    };

                    _userList.Add(user);
                    return true;
                });

            _dbMock.Setup(db => db.LoginAndGetToken(It.IsAny<string>(), It.IsAny<string>()))
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

            _dbMock.Setup(db => db.FindUser(It.IsAny<string>()))
                .Returns((string email) => { return _userList.FirstOrDefault(u => u.Email.Equals(email)); });
            _dbMock.Setup(db => db.FindUser(It.IsAny<int>()))
                .Returns((int id) => { return _userList.FirstOrDefault(u => u.Id.Equals(id)); });

            _dbMock.Setup(db => db.DeleteUser(It.IsAny<string>()))
                .Returns((string email) =>
                {
                    var user = _userList.FirstOrDefault(u => u.Email.Equals(email));

                    return user != null && _userList.Remove(user);
                });

            _api = new ApiController(_dbMock.Object);
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
            Assert.AreEqual("Invalid login model", Json.Decode(result?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiTestLoginFailInvalidPassword()
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
        public void ApiTestLoginSuccessRetrievesToken()
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
        public void ApiTestRegisterFailUserAlreadyExists()
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
        public void ApiTestRegisterUserFailInvalidModel()
        {
            var model = new RegistrationViewModel();
            ValidateModel(model);

            var result = _api.RegisterUser(model) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("Invalid registration model", Json.Decode(result?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiTestRegistrationSuccess()
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
        public void ApiTestDeleteUserSuccess()
        {
            var deleteResult = _api.DeleteUser(_testUser.Id) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), deleteResult?.StatusCode);
            Assert.AreEqual("User deleted", Json.Decode(deleteResult?.StatusDescription)["content"]);
        }
    }
}