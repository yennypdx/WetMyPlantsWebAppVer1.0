/*
 * Last Author:             Andy Horn
 * Last Modified:           03/06/2019
 *
 * Notes:                   These tests use the test database hosted on Andy's AWS account. They do not interfere with the
 *                          actual database hosted on Carter's Azure account.
 */

using System;
using System.Data.SqlClient;
using DBHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DbHelper.Test
{
    [TestClass]
    public class DbHelperTest
    {
        private readonly DBHelper.DbHelper _db;
        private readonly string _connectionString = "Data Source=wetmyplants-test.c9yldqomj91e.us-west-2.rds.amazonaws.com,1433;Initial Catalog=WetMyPlantsTest;User ID=wetmyplants;Password=GR33nThumb;";
        private readonly string email = "test@test.test";
        private readonly string password = "password";
        private readonly string phone = "1234567890";
        private readonly string firstName = "Test";
        private readonly string lastName = "User";

        private readonly string speciesOneLatinName = "Testicus speciesus";
        private readonly string speciesOneCommonName = "Test Species One";
        private readonly double speciesOneWaterMax = 8.00;
        private readonly double speciesOneWaterMin = 4.00;
        private readonly double speciesOneLightMax = 6.00;
        private readonly double speciesOneLightMin = 3.00;

        private readonly string speciesTwoLatinName = "Testicus speciesus two";
        private readonly string speciesTwoCommonName = "Test Species Two";
        private readonly double speciesTwoWaterMax = 5.00;
        private readonly double speciesTwoWaterMin = 1.99;
        private readonly double speciesTwoLightMax = 9.99;
        private readonly double speciesTwoLightMin = 5.80;

        private readonly string plantOneAlias = "Alfredo";
        private readonly double plantOneCurrentLight = 7.50;
        private readonly double plantOneCurrentWater = 3.99;
        private readonly string plantOneId = "C4:C7:8D:6A:50:E8";
      
        private readonly string plantTwoAlias = "Mr. Biggles";
        private readonly double plantTwoCurrentLight = 3.50;
        private readonly double plantTwoCurrentWater = 8.00;
        private readonly string plantTwoId = "C4:C7:8D:6A:50:E5";

        public DbHelperTest()
        {
            _db = GetDb();
        }


        private DBHelper.DbHelper GetDb()
        {
            return new DBHelper.DbHelper(_connectionString);
        }

        [TestInitialize]
        public void Init()
        {
            _db.CreateNewUser(firstName, lastName, phone, email, password); // only added here after CreateNewUser was tested
            _db.CreateNewSpecies(speciesOneCommonName, speciesOneLatinName, speciesOneWaterMax, speciesOneWaterMin, speciesOneLightMax, speciesOneLightMin);
            _db.CreateNewSpecies(speciesTwoCommonName, speciesTwoLatinName, speciesTwoWaterMax, speciesTwoWaterMin, speciesTwoLightMax, speciesTwoLightMin);

            var species = _db.GetAllSpecies();
            var idOne = species[0].Id;
            var idTwo = species[1].Id;
            


            _db.CreateNewPlant(plantOneId, idOne, plantOneAlias, plantOneCurrentWater, plantOneCurrentLight);
            _db.CreateNewPlant(plantTwoId, idTwo, plantTwoAlias, plantTwoCurrentWater, plantTwoCurrentLight);

            _db.RegisterPlantToUser(_db.FindPlantsByNickname(plantOneAlias)[0], _db.FindUser(email: email));
            _db.RegisterPlantToUser(_db.FindPlantsByNickname(plantTwoAlias)[0], _db.FindUser(email: email));
        }

        [TestCleanup]
        public void Dispose()
        {
            var users = _db.GetAllUsers();
            users?.ForEach(i => _db.DeleteUser(i.Email));

            var plants = _db.GetAllPlants();
            plants?.ForEach(i => _db.DeletePlant(i.Id));

            var species = _db.GetAllSpecies();
            species?.ForEach(i => _db.DeleteSpecies(i.Id));
        }

        [TestMethod]
        public void DbHelperCreateNewUserTest()
        {
            _db.DeleteUser(email);
            var result = _db.CreateNewUser(firstName, lastName, phone, email, password);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperCreateNewUserEmailCollisionTest()
        {
            var result =
                _db.CreateNewUser("test", "test", "phone", email, "pwd"); // Create a user with the same email address.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperDeleteUserTest()
        {
            var result = _db.DeleteUser(email);
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
            var user = _db.FindUser(email);

            Assert.IsNotNull(user);
        }

        [TestMethod]
        public void DbHelperFindUserByEmailTestFail()
        {
            var result = _db.FindUser("other@email.com");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void DbHelperFindUserByIdTest()
        {
            var id = _db.FindUser(email).Id;
            var user = _db.FindUser(id);

            Assert.IsNotNull(user);
            Assert.AreEqual(_db.FindUser(email).Id, user.Id);
        }

        [TestMethod]
        public void DbHelperUpdateUserEmailTest()
        {
            const string newEmail = "new@email.test";
            var user = _db.FindUser(email);
            user.Email = newEmail;
            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);
            Assert.AreEqual(newEmail, _db.FindUser(user.Id).Email);
        }

        [TestMethod]
        public void DbHelperUpdateUserFirstNameTest()
        {
            const string newFirstName = "NewFirstName";
            var user = _db.FindUser(email);
            user.FirstName = newFirstName;
            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);

            Assert.AreEqual(newFirstName, _db.FindUser(user.Id).FirstName);
        }

        [TestMethod]
        public void DbHelperUpdateUserLastNameTest()
        {
            const string newLastName = "NewLastName";
            var user = _db.FindUser(email);
            user.LastName = newLastName;
            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);
            Assert.AreEqual(newLastName, _db.FindUser(user.Id).LastName);
        }

        [TestMethod]
        public void DbHelperUpdateUserPhoneNumberTest()
        {
            const string newPhone = "1112223333";
            var user = _db.FindUser(email);
            user.Phone = newPhone;
            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);
            Assert.AreEqual(newPhone, _db.FindUser(user.Id).Phone);
        }

        [TestMethod]
        public void DbHelperResetUserPasswordTest()
        {
            _db.ResetPassword(email, "NewPassword");

            var result = _db.AuthenticateUser(email, "NewPassword");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperAuthenticateUserSuccessTest()
        {
            var result = _db.AuthenticateUser(email, password);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperAuthenticateUserInvalidPasswordTest()
        {
            var result = _db.AuthenticateUser(email, "WrongPassword");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperLoginAndGetTokenTest()
        {
            var result = _db.LoginAndGetToken(email, password);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void DbHelperLoginAndGetTokenInvalidPasswordTest()
        {
            var result = _db.LoginAndGetToken(email, "WrongPassword");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void DbHelperRemoveErroneousTokensTest()
        {
            var originalToken = _db.LoginAndGetToken(email, password);

            var db = new SqlConnection(_connectionString);
            var userId = _db.FindUser(email).Id;
            db.Open();
            for (var i = 0; i < 10; i++)
            {
                var testQuery = "INSERT INTO Tokens (UserID, Token, Expiry) " +
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

            var currentToken = _db.LoginAndGetToken(email, password); // this should erase all tokens and create one new one.

            Assert.AreNotEqual(originalToken, currentToken);
        }

        [TestMethod]
        public void DbHelperRemoveExpiredTokenTest()
        {
            var originalToken = _db.LoginAndGetToken(email, password);

            // to adequately test an expired token, we must connect to the database and manually set a token's expiration date
            // for this test, I have chosen to set TODAY as the expiration date.
            var db = new SqlConnection(_connectionString); // manually connect to the test database
            var userId = _db.FindUser(email)?.Id; // find the test user's ID

            var today = DateTime.Today; // get today's date

            var query = $"UPDATE Tokens SET Expiry = '{today.ToString("G")}' WHERE UserID = {userId};"; // set the user's token's expiration date to today

            // execute the sql query
            var cmd = new SqlCommand(query, db);
            db.Open();
            cmd.ExecuteNonQuery();
            db.Close();

            var currentToken = _db.LoginAndGetToken(email, password); // get the current (new) token

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
            var result = _db.CreateNewSpecies("Test Species", "Testicus specei", 7.99, 2.34, 3.59, 2.00);

            Assert.IsTrue(result != 0);
        }

        [TestMethod]
        public void DbHelperGetAllSpeciesTest()
        {
            var result = _db.GetAllSpecies();

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void DbHelperGetPlantSpeciesByLatinNameTest()
        {
            var result = _db.FindSpecies(latinName: speciesOneLatinName);

            Assert.AreEqual(speciesOneCommonName, result.CommonName);
        }

        [TestMethod]
        public void DbHelperGetPlantSpeciesByCommonNameTest()
        {
            var result = _db.FindSpecies(commonName: speciesOneCommonName);

            Assert.AreEqual(speciesOneLatinName, result.LatinName);
        }

        [TestMethod]
        public void DbHelperGetPlantSpeciesByIdTest()
        {
            var id = _db.FindSpecies(latinName: speciesOneLatinName).Id; // get the id

            var result = _db.FindSpecies(id); // use the id to find the plant

            Assert.AreEqual(speciesOneLatinName, result.LatinName); // compare plant data
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesLatinNameTest()
        {
            var species = _db.FindSpecies(commonName: speciesOneCommonName);
            species.LatinName = "New latin name";
            _db.UpdateSpecies(species); // update with a new latin name

            var result = _db.FindSpecies(species.Id).LatinName; // get the species' latin name from the database

            Assert.AreEqual("New latin name", result); // the species should have the new latin name
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesCommonNameTest()
        {
            var species = _db.FindSpecies(commonName: speciesOneCommonName); // get the id
            species.CommonName = "New common name";
            _db.UpdateSpecies(species); // update with a new common name

            var result = _db.FindSpecies(species.Id).CommonName; // get the species' common name using its id

            Assert.AreEqual("New common name", result); // the species should have the new common name
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesWaterMaxTest()
        {
            var species = _db.FindSpecies(commonName: speciesOneCommonName);
            species.WaterMax = 10.00;
            _db.UpdateSpecies(species);

            var result = _db.FindSpecies(species.Id).WaterMax;

            Assert.AreEqual(10.00, result);
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesWaterMinTest()
        {
            var species = _db.FindSpecies(commonName: speciesOneCommonName);
            species.WaterMin = -1.00;
            _db.UpdateSpecies(species);

            var result = _db.FindSpecies(species.Id).WaterMin;

            Assert.AreEqual(-1.00, result);
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesLightMaxTest()
        {
            var species = _db.FindSpecies(latinName: speciesOneLatinName);
            species.LightMax = 10.00;
            _db.UpdateSpecies(species);

            var result = _db.FindSpecies(species.Id).LightMax;

            Assert.AreEqual(10.00, result);
        }

        [TestMethod]
        public void DbHelperUpdateSpeciesLightMinTest()
        {
            var species = _db.FindSpecies(latinName: speciesOneLatinName);
            species.LightMin = -1.00;
            _db.UpdateSpecies(species);

            var result = _db.FindSpecies(species.Id).LightMin;

            Assert.AreEqual(-1.00, result);
        }

        [TestMethod]
        public void DbHelperDeleteSpeciesTest()
        {
            var testSpeciesCommonName = "TEST SPECIES";
            var testSpeciesLatinName = "TEST SPECIES LATIN NAME";
            var testSpeciesWaterMax = 10.00;
            var testSpeciesWaterMin = 1.00;
            var testSpeciesLightMax = 10.00;
            var testSpeciesLightMin = 1.00;

            _db.CreateNewSpecies(testSpeciesCommonName, testSpeciesLatinName, testSpeciesWaterMax, testSpeciesWaterMin,
                testSpeciesLightMax, testSpeciesLightMin);

            var id = _db.FindSpecies(latinName: testSpeciesLatinName).Id; // get the id
            _db.DeleteSpecies(id); // delete the species from the database

            var result = _db.FindSpecies(id); // ensure the species is really gone

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
            var id = _db.GetAllSpecies()[0].Id;
            var pid = "C4:7C:8D:6A:51:23";
            var result = _db.CreateNewPlant(pid, id, plantOneAlias, plantOneCurrentWater, plantOneCurrentLight);

            Assert.IsTrue(result != false);
        }

        [TestMethod]
        public void DbHelperGetAllPlantsTest()
        {
            var result = _db.GetAllPlants();

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void DbHelperGetPlantByNicknameTest()
        {
            var result = _db.FindPlantsByNickname(plantOneAlias)[0]; // find a plant using a nickname

            Assert.AreEqual(plantOneAlias, result.Nickname); // should return the same plant
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
            var id = _db.FindPlantsByNickname(plantOneAlias)[0].Id; // get the id of a specific plant
            var result = _db.FindPlant(id); // use the id to get the plant from the database

            Assert.AreEqual(id, result.Id); // ensure they are the same plant based on the id
        }

      /*  [TestMethod]
        public void DbHelperUpdatePlantSpeciesTest()
        {
            var plants = _db.GetAllPlants();
            var plant = plants[0];

            plant.SpeciesId = plants[1].SpeciesId;

            _db.UpdatePlant(plant);

            var result = _db.FindPlant(plant.Id);

            Assert.AreEqual(result.SpeciesId, plants[1].SpeciesId);
        }

        [TestMethod]
        public void DbHelperUpdatePlantNicknameTest()
        {
            var plant = _db.FindPlantsByNickname(plantOneAlias)[0];
            plant.Nickname = "New nickname";
            _db.UpdatePlant(plant); // update with a new nickname

            var result = _db.FindPlant(plant.Id).Nickname; // get the plant from the database using the new nickname

            Assert.AreEqual("New nickname", result); // the plant should have the new nickname
        }

        [TestMethod]
        public void DbHelperUpdatePlantCurrentWaterTest()
        {
            var plant = _db.FindPlantsByNickname(plantOneAlias)[0]; // get the id
            plant.CurrentWater = -1.00;
            _db.UpdatePlant(plant); // update the with a new water level

            var result = _db.FindPlant(plant.Id).CurrentWater; // get the plant's water level from the database

            Assert.AreEqual(-1.00, result);
        }

        [TestMethod]
        public void DbHelperUpdatePlantCurrentLightTest()
        {
            var plant = _db.FindPlantsByNickname(plantOneAlias)[0]; // get the id
            plant.CurrentLight = -1.00;
            _db.UpdatePlant(plant); // update with a new light level

            var result = _db.FindPlant(plant.Id).CurrentLight; // get the plant's light level from the database

            Assert.AreEqual(-1.00, result);
        }
        */
        [TestMethod]
        public void DbHelperDeletePlantTest()
        {
            var id = _db.FindPlantsByNickname(plantOneAlias)[0].Id; // get the id
            _db.DeletePlant(id); // delete the plant

            var result = _db.FindPlant(id); // ensure the plant is gone

            Assert.IsNull(result);
        }
    }
};
