/*
 * Last Author:             Andy Horn
 * Last Modified:           02/05/2019
 *
 * Notes:                   These tests use the test database hosted on Andy's AWS account. They do not interfere with the
 *                          actual database hosted on Carter's Azure account.
 */

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using DBHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DbHelper.Test
{

    internal class TestSpecies
    {
        public int Id { get; set; }
        public string CommonName { get; set; }
        public string LatinName { get; set; }
        public double WaterMax { get; set; }
        public double WaterMin { get; set; }
        public double LightMax { get; set; }
        public double LightMin { get; set; }
    }

    internal class TestPlant
    {
        public int Id { get; set; }
        public TestSpecies Species { get; set; }
        public string Nickname { get; set; }
        public double CurrentWater { get; set; }
        public double CurrentLight { get; set; }
    }

    internal class TestUser
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "Test";
        public string LastName { get; set; } = "User";
        public string Password { get; set; } = "password";
        public string Email { get; set; } = "test@test.test";
        public string Phone { get; set; } = "1234567890";
        public List<TestPlant> Plants { get; set; }
    }

    [TestClass]
    public class DbHelperTest
    {
        private readonly DBHelper.DbHelper _db;
        private readonly string _connectionString = "Data Source=wetmyplants-test.c9yldqomj91e.us-west-2.rds.amazonaws.com,1433;Initial Catalog=WetMyPlantsTest;User ID=wetmyplants;Password=GR33nThumb;";
        private TestUser _user;



        public DbHelperTest()
        {
            TestSpecies SpeciesOne = new TestSpecies()
            {
                CommonName = "Test Plant One",
                LatinName = "Planticus testus one",
                WaterMax = 6.00,
                WaterMin = 2.00,
                LightMax = 8.00,
                LightMin = 5.00
            };
            TestSpecies SpeciesTwo = new TestSpecies()
            {
                CommonName = "Test Plant Two",
                LatinName = "Planticus testus two",
                WaterMax = 3.00,
                WaterMin = 0.50,
                LightMax = 10.00,
                LightMin = 5.50
            };
            TestSpecies SpeciesThree = new TestSpecies()
            {
                CommonName = "Test Plant Three",
                LatinName = "Planticus testus three",
                WaterMax = 10.00,
                WaterMin = 8.80,
                LightMax = 3.50,
                LightMin = 0.00
            };

            _db = GetDb();
            _user = new TestUser()
            {
                Email = "test@test.test",
                FirstName = "Test",
                LastName = "User",
                Password = "password",
                Phone = "1234567890",
                Plants = new List<TestPlant>()
                {
                    new TestPlant()
                    {
                        Species = SpeciesOne,
                        CurrentLight = 7.65,
                        CurrentWater = 5.59,
                        Nickname = "Lil Jimmy"
                    },
                    new TestPlant()
                    {
                        Species = SpeciesTwo,
                        CurrentLight = 3.32,
                        CurrentWater = 1.29,
                        Nickname = "Ms. Nezbit"
                    },
                    new TestPlant()
                    {
                        Species = SpeciesThree,
                        CurrentLight = 2.20,
                        CurrentWater = 4.29,
                        Nickname = "Mr. Biggles"
                    },
                    new TestPlant()
                    {
                        Species = SpeciesOne,
                        CurrentLight = 2.23,
                        CurrentWater = 7.79,
                        Nickname = "Alexandria Ocasio-Cortez"
                    }
                }
            };
        }


        private DBHelper.DbHelper GetDb()
        {
            return new DBHelper.DbHelper(_connectionString);
        }

        [TestInitialize]
        public void Init()
        {
            _db.CreateNewUser(_user.FirstName, _user.LastName, _user.Phone, _user.Email, _user.Password); // only added here after CreateNewUser was tested
        }

        [TestCleanup]
        public void Dispose()
        {
            var list = _db.GetAll();
            list.ForEach(i => _db.DeleteUser(i.Email));
        }

        [TestMethod]
        public void DbHelperCreateNewUserTest()
        {
            _db.DeleteUser(_user.Email);
            var result = _db.CreateNewUser(_user.FirstName, _user.LastName, _user.Phone, _user.Email, _user.Password);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperCreateNewUserEmailCollisionTest()
        {
            var result =
                _db.CreateNewUser("test", "test", "phone", _user.Email, "pwd"); // Create a user with the same email address.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperDeleteUserTest()
        {
            var result = _db.DeleteUser(_user.Email);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperDeleteNonExistentUserTest()
        {
            var result = _db.DeleteUser("other@email.com");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperFindUserByEmailTest()
        {
            var user = _db.FindUserByEmail(_user.Email);

            Assert.IsNotNull(user);
        }

        [TestMethod]
        public void DbHelperFindUserByEmailTestFail()
        {
            var result = _db.FindUserByEmail("other@email.com");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void DbHelperFindUserByIdTest()
        {
            var id = _db.FindUserByEmail(_user.Email).Id;
            var user = _db.FindUserById(id);

            Assert.IsNotNull(user);
            Assert.AreEqual(_db.FindUserByEmail(_user.Email).Id, user.Id);
        }

        [TestMethod]
        public void DbHelperUpdateUserByParamEmailTest()
        {
            const string newEmail = "new@email.test";

            var result = _db.UpdateUserByParam(_user.Email, UserColumns.Email, newEmail);

            Assert.IsTrue(result);
            Assert.AreEqual(newEmail, _db.FindUserByEmail(newEmail).Email);
        }

        [TestMethod]
        public void DbHelperUpdateUserEmailTest()
        {
            const string newEmail = "new@email.test";
            var user = _db.FindUserByEmail(_user.Email);
            user.Email = newEmail;
            var result = _db.UpdateUser(user);
            //var result = _db.UpdateUserByParam(Email, UserColumns.Email, newEmail);

            Assert.IsTrue(result);
            Assert.AreEqual(newEmail, _db.FindUserById(user.Id).Email);
        }

        [TestMethod]
        public void DbHelperUpdateUserByParamFirstNameTest()
        {
            const string newFirstName = "NewFirstName";
            var result = _db.UpdateUserByParam(_user.Email, UserColumns.FirstName, newFirstName);

            Assert.IsTrue(result);

            Assert.AreEqual(newFirstName, _db.FindUserByEmail(_user.Email).FirstName);
        }

        [TestMethod]
        public void DbHelperUpdateUserFirstNameTest()
        {
            const string newFirstName = "NewFirstName";
            var user = _db.FindUserByEmail(_user.Email);
            user.FirstName = newFirstName;
            var result = _db.UpdateUser(user);
            //var result = _db.UpdateUserByParam(Email, UserColumns.FirstName, newFirstName);

            Assert.IsTrue(result);

            Assert.AreEqual(newFirstName, _db.FindUserById(user.Id).FirstName);
        }

        [TestMethod]
        public void DbHelperUpdateUserByParamLastNameTest()
        {
            const string newLastName = "NewLastName";

            var result = _db.UpdateUserByParam(_user.Email, UserColumns.LastName, newLastName);

            Assert.IsTrue(result);
            Assert.AreEqual(newLastName, _db.FindUserByEmail(_user.Email).LastName);
        }

        [TestMethod]
        public void DbHelperUpdateUserLastNameTest()
        {
            const string newLastName = "NewLastName";
            var user = _db.FindUserByEmail(_user.Email);
            user.LastName = newLastName;
            var result = _db.UpdateUser(user);
            //var result = _db.UpdateUserByParam(Email, UserColumns.LastName, newLastName);

            Assert.IsTrue(result);
            Assert.AreEqual(newLastName, _db.FindUserById(user.Id).LastName);
        }

        [TestMethod]
        public void DbHelperUpdateUserByParamPhoneNumberTest()
        {
            const string newPhone = "1112223333";

            var result = _db.UpdateUserByParam(_user.Email, UserColumns.Phone, newPhone);

            Assert.IsTrue(result);
            Assert.AreEqual(newPhone, _db.FindUserByEmail(_user.Email).Phone);
        }

        [TestMethod]
        public void DbHelperUpdateUserPhoneNumberTest()
        {
            const string newPhone = "1112223333";
            var user = _db.FindUserByEmail(_user.Email);
            user.Phone = newPhone;
            var result = _db.UpdateUser(user);
            //var result = _db.UpdateUserByParam(Email, UserColumns.Phone, newPhone);

            Assert.IsTrue(result);
            Assert.AreEqual(newPhone, _db.FindUserById(user.Id).Phone);
        }

        [TestMethod]
        public void DbHelperResetUserPasswordTest()
        {
            _db.ResetPassword(_user.Email, "NewPassword");

            var result = _db.AuthenticateUser(_user.Email, "NewPassword");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperAuthenticateUserSuccessTest()
        {
            var result = _db.AuthenticateUser(_user.Email, _user.Password);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperAuthenticateUserInvalidPasswordTest()
        {
            var result = _db.AuthenticateUser(_user.Email, "WrongPassword");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperLoginAndGetTokenTest()
        {
            var result = _db.LoginAndGetToken(_user.Email, _user.Password);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void DbHelperLoginAndGetTokenInvalidPasswordTest()
        {
            var result = _db.LoginAndGetToken(_user.Email, "WrongPassword");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void DbHelperRemoveErroneousTokensTest()
        {
            var originalToken = _db.LoginAndGetToken(_user.Email, _user.Password);

            var db = new SqlConnection(_connectionString);
            var userId = _db.FindUserByEmail(_user.Email).Id;
            db.Open();
            for (var i = 0; i < 10; i++)
            {
                var testQuery = $"INSERT INTO Tokens (UserID, Token, Expiry) " +
                                $"VALUES ({userId}, '{new Random().Next(100000000, 999999999)}', 01012000);";

                var testCommand = new SqlCommand(testQuery, db);
                testCommand.ExecuteNonQuery();
            }
            db.Close();

            var numTokensQuery = $"SELECT COUNT(*) FROM Tokens WHERE UserID = {userId};";
            db.ConnectionString = AccessHelper.GetDbConnectionString();
            db.Open();
            var cmd = new SqlCommand(numTokensQuery, db);
            var numTokens = cmd.ExecuteScalar().ToString();
            db.Close();
            
            if (Convert.ToInt32(numTokens) <= 1)
                Assert.IsFalse(false);

            var currentToken = _db.LoginAndGetToken(_user.Email, _user.Password); // this should erase all tokens and create one new one.

            Assert.AreNotEqual(originalToken, currentToken);
        }

        [TestMethod]
        public void DbHelperRemoveExpiredTokenTest()
        {
            var originalToken = _db.LoginAndGetToken(_user.Email, _user.Password);

            // to adequately test an expired token, we must connect to the database and manually set a token's expiration date
            // for this test, I have chosen to set TODAY as the expiration date.
            var db = new SqlConnection(_connectionString); // manually connect to the test database
            var userId = _db.FindUserByEmail(_user.Email)?.Id; // find the test user's ID

            var today = DateTime.Today; // get today's date
            var query = $"UPDATE Tokens SET Expiry = '{today.ToString("G")}' WHERE UserID = {userId};"; // set the user's token's expiration date to today

            // execute the sql query
            var cmd = new SqlCommand(query, db);
            db.Open();
            cmd.ExecuteNonQuery();
            db.Close();

            var currentToken = _db.LoginAndGetToken(_user.Email, _user.Password); // get the current (new) token

            Assert.AreNotEqual(originalToken, currentToken); // verify it is NEW and not the same as the original one
        }

        /*
         *******************************
         *  PLANT SPECIES TEST METHODS
         *******************************
         */

        [TestMethod]
        public void DbHelperAddPlantSpeciesTest()
        {
            // use DbHelper to register a new plant species
            // this method should return true if the query was successful, false otherwise
            var result = _db.AddNewSpecies(
                _user.Plants[0].Species.CommonName,
                _user.Plants[0].Species.LatinName,
                _user.Plants[0].Species.WaterMax,
                _user.Plants[0].Species.WaterMin,
                _user.Plants[0].Species.LightMax,
                _user.Plants[0].Species.LightMin);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperGetPlantSpeciesByLatinNameTest()
        {
            var result = _db.FindSpeciesByLatinName(_user.Plants[0].Species.LatinName).LatinName;
            var latin = _user.Plants[0].Species.LatinName;

            Assert.AreEqual(latin, result);
        }

        [TestMethod]
        public void DbHelperGetPlantSpeciesByCommonNameTest()
        {
            var result = _db.FindSpeciesByCommonName(_user.Plants[0].Species.CommonName).CommonName;
            var common = _user.Plants[0].Species.CommonName;

            Assert.AreEqual(common, result);
        }

        [TestMethod]
        public void DbHelperGetPlantSpeciesByIdTest()
        {
            var id = _db.FindSpeciesByLatinName(_user.Plants[0].Species.LatinName).Id; // get the id

            var result = _db.FindSpeciesById(id); // use the id to find the plant

            Assert.AreEqual(_user.Plants[0].Species.LatinName, result.LatinName); // compare plant data
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesLatinNameTest()
        {
            var id = _db.FindSpeciesByCommonName(_user.Plants[0].Species.CommonName).Id; // necessary to get the id for the next step
            _db.UpdateSpecies(id, SpeciesColumns.LatinName, "New latin name"); // update with a new latin name

            var result = _db.FindSpeciesById(id).LatinName; // get the species' latin name from the database

            Assert.AreEqual("New latin name", result); // the species should have the new latin name
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesCommonNameTest()
        {
            var id = _db.FindSpeciesByCommonName(_user.Plants[0].Species.CommonName).Id; // get the id
            _db.UpdateSpecies(id, SpeciesColumns.CommonName, "New common name"); // update with a new common name

            var result = _db.FindSpeciesById(id).CommonName; // get the species' common name using its id

            Assert.AreEqual("New common name", result); // the species should have the new common name
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesWaterMaxTest()
        {
            var id = _db.FindSpeciesByCommonName(_user.Plants[0].Species.CommonName).Id;
            _db.UpdateSpecies(id, SpeciesColumns.WaterMax, "10.00");

            var result = _db.FindSpeciesById(id).WaterMax;

            Assert.AreEqual(10.00, result);
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesWaterMinTest()
        {
            var id = _db.FindSpeciesByCommonName(_user.Plants[0].Species.CommonName).Id;
            _db.UpdateSpecies(id, SpeciesColumns.WaterMin, "-1.00");

            var result = _db.FindSpeciesById(id).WaterMin;

            Assert.AreEqual(-1.00, result);
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesLightMaxTest()
        {
            var id = _db.FindSpeciesByLatinName(_user.Plants[0].Species.LatinName).Id;
            _db.UpdateSpecies(id, SpeciesColumns.LightMax, "10.00");

            var result = _db.FindSpeciesById(id).LightMax;

            Assert.AreEqual(10.00, result);
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesLightMinTest()
        {
            var id = _db.FindSpeciesByLatinName(_user.Plants[0].Species.LatinName).Id;
            _db.UpdateSpecies(id, SpeciesColumns.LightMin, "-1.00");

            var result = _db.FindSpeciesById(id).LightMin;

            Assert.AreEqual(-1.00, result);
        }

        [TestMethod]
        public void DbHelperDeleteSpeciesTest()
        {
            var id = _db.FindSpeciesByLatinName(_user.Plants[0].Species.LatinName).Id; // get the id
            _db.DeleteSpecies(id); // delete the species from the database

            var result = _db.FindSpeciesById(id); // ensure the species is really gone

            Assert.IsNull(result);
        }

        /*
         ***********************
         *  PLANT TEST METHODS
         ***********************
         */


        [TestMethod]
        public void DbHelperAddPlantTest()
        {
            // use DbHelper to register a new plant
            // this method should return true if the query was successful, false otherwise
            var result = _db.AddNewPlant(
                _user.Plants[0].Species.Id,
                _user.Plants[0].Nickname,
                _user.Plants[0].CurrentWater,
                _user.Plants[0].CurrentLight);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperGetPlantByNicknameTest()
        {
            var result = _db.FindPlantsByNickname(_user.Plants[0].Nickname)[0]; // find a plant using a nickname

            Assert.AreEqual(_user.Plants[0].Nickname, result.Nickname; // should return the same plant
        }

        [TestMethod]
        public void DbHelperGetPlantsByNicknameEmptyTest()
        {
            var result = _db.FindPlantsByNickname("Invalid nickname"); // use an invalid nickname

            Assert.IsNull(result); // should return null
        }

        [TestMethod]
        public void DbHelperGetPlantByIdTest()
        {
            var id = _db.FindPlantsByNickname(_user.Plants[0].Nickname)[0].Id; // get the id of a specific plant
            var result = _db.FindPlantById(id); // use the id to get the plant from the database

            Assert.AreEqual(id, result.Id); // ensure they are the same plant based on the id
        }

        [TestMethod]
        public void DbHelperUpdatePlantSpeciesTest()
        {
            var id = _db.FindPlantsByNickname(_user.Plants[0].Nickname)[0].Id; // necessary to get the id for the next step
            _db.UpdatePlant(id, PlantColumns.SpeciesId, _user.Plants[1].Species.Id.ToString()); // update with a new species

            var result = _db.FindPlantById(id); // get the species from the database using the new latin name

            Assert.AreEqual(_user.Plants[1].Species.Id, result.Id); // the plant should have the new species
        }

        [TestMethod]
        public void DbHelperUpdatePlantNicknameTest()
        {
            var id = _db.FindPlantsByNickname(_user.Plants[0].Nickname)[0].Id; // get the id
            _db.UpdatePlant(id, PlantColumns.Nickname, "New nickname"); // update with a new nickname

            var result = _db.FindPlantById(id).Nickname; // get the plant from the database using the new nickname

            Assert.AreEqual("New nickname", result); // the plant should have the new nickname
        }

        [TestMethod]
        public void DbHelperUpdatePlantCurrentWaterTest()
        {
            var id = _db.FindPlantsByNickname(_user.Plants[0].Nickname)[0].Id; // get the id
            _db.UpdatePlant(id, PlantColumns.CurrentWater, "-1.00"); // update the with a new water level

            var result = _db.FindPlantById(id).CurrentWater; // get the plant's water level from the database

            Assert.AreEqual(-1.00, result);
        }

        [TestMethod]
        public void DbHelperUpdatePlantCurrentLightTest()
        {
            var id = _db.FindPlantsByNickname(_user.Plants[0].Nickname)[0].Id; // get the id
            _db.UpdatePlant(id, PlantColumns.CurrentLight, "-1.00"); // update with a new light level

            var result = _db.FindPlantById(id).CurrentLight; // get the plant's light level from the database

            Assert.AreEqual(-1.00, result);
        }

        [TestMethod]
        public void DbHelperDeletePlantTest()
        {
            var id = _db.FindPlantsByNickname(_user.Plants[0].Nickname)[0].Id; // get the id
            _db.DeletePlant(id); // delete the plant

            var result = _db.FindPlantById(id); // ensure the plant is gone

            Assert.IsNull(result);
        }
    }
};