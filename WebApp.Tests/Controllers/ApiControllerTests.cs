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
        private List<User> _userList;
        private List<Plant> _plantList;
        private List<Species> _speciesList;
        private Dictionary<int, string> _tokenTable;
        private Dictionary<int, int> _userPlantTable;
        private Dictionary<int, string> _resetCodeTable;

        private User _testUser;
        private Plant _testPlant;
        private Species _testSpecies;

        public ApiControllerTests()
        {
            _testUser = new User
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Test",
                Email = "test@email.com",
                Password = "password",
                Hash = Crypto.HashPassword(DateTime.Today.ToLongDateString()),
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

            _userList = new List<User>();
            _plantList = new List<Plant>();
            _speciesList = new List<Species>();
            _tokenTable = new Dictionary<int, string>();
            _userPlantTable = new Dictionary<int, int>();
            _resetCodeTable = new Dictionary<int, string>();

            _tokenTable.Add(_testUser.Id, Crypto.HashPassword(DateTime.Today.ToLongDateString()));
            _userPlantTable.Add(_testUser.Id, _testPlant.Id);

            _dbMock = new Mock<IDbHelper>();
            //_api = new ApiController(new DBHelper.DbHelper(AccessHelper.GetTestDbConnectionString()));
        }

        [TestInitialize]
        public void Init()
        {
            _api.RegisterUser(_registrationViewModel);
        }

        [TestCleanup]
        public void Dispose()
        {
            var data = _api.GetAllUsers();
            var list = (List<User>) data.Data;
            list.ForEach(u => _api.DeleteUser(u.Id));
        }

        [TestMethod]
        public void ApiTestGetHelloWorld()
        {
            var msg = _api.Index();

            Assert.AreEqual("hello world!", msg);
        }

        [TestMethod]
        public void ApiTestGetAllUsers()
        {
            var userList = _api.GetAllUsers().Data as List<User>;

            Assert.IsNotNull(userList);
            Assert.AreNotEqual(0, userList.Count);
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
                Email = _registrationViewModel.Email,
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
                Email = "test@test.test",
                Password = "password",
                RememberMe = false
            };
            ValidateModel(model);

            var result = _api.Login(model) as HttpStatusCodeResult;
            
            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), result?.StatusCode);
            Assert.IsNotNull(Json.Decode(result?.StatusDescription)["content"]);
        }

        [TestMethod]
        public void ApiTestRegisterFailUserAlreadyExists()
        {
            var result = _api.RegisterUser(_registrationViewModel) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.BadRequest), result?.StatusCode);
            Assert.AreEqual("Unable to register user", Json.Decode(result?.StatusDescription)["content"]);
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

            var result = _api.RegisterUser(newUser) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), result?.StatusCode);
            Assert.IsNotNull(Json.Decode(result?.StatusDescription)["id"]);
        }

        [TestMethod]
        public void ApiTestDeleteUserSuccess()
        {
            var id = ((List<User>)_api.GetAllUsers().Data)[0].Id;
            var deleteResult = _api.DeleteUser(id) as HttpStatusCodeResult;

            Assert.AreEqual(Convert.ToInt32(HttpStatusCode.OK), deleteResult?.StatusCode);
            Assert.AreEqual("User deleted", Json.Decode(deleteResult?.StatusDescription)["content"]);
        }
    }
}