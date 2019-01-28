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
        private readonly string _connectionString = "Data Source=.;Initial Catalog=WetMyPlantsTest;Integrated Security=True;";

        private const string FirstName = "Test";
        private const string LastName = "User";
        private const string Password = "password";
        private const string Email = "test@test.com";
        private const string Phone = "1234567890";

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
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);
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
            _db.DeleteUser(Email);
            var result = _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperCreateNewUserEmailCollisionTest()
        {
            var result =
                _db.CreateNewUser("test", "test", "phone", Email, "pwd"); // Create a user with the same email address.
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperDeleteUserTest()
        {
            var result = _db.DeleteUser(Email);
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
            var user = _db.FindUserByEmail(Email);

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
            var id = _db.FindUserByEmail(Email).Id;
            var user = _db.FindUserById(id);

            Assert.IsNotNull(user);
            Assert.AreEqual(_db.FindUserByEmail(Email).Id, user.Id);
        }

        [TestMethod]
        public void DbHelperUpdateUserByParamEmailTest()
        {
            const string newEmail = "new@email.test";

            var result = _db.UpdateUserByParam(Email, UserColumns.Email, newEmail);

            Assert.IsTrue(result);
            Assert.AreEqual(newEmail, _db.FindUserByEmail(newEmail).Email);
        }

        [TestMethod]
        public void DbHelperUpdateUserEmailTest()
        {
            const string newEmail = "new@email.test";
            var user = _db.FindUserByEmail(Email);
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
            var result = _db.UpdateUserByParam(Email, UserColumns.FirstName, newFirstName);

            Assert.IsTrue(result);

            Assert.AreEqual(newFirstName, _db.FindUserByEmail(Email).FirstName);
        }

        [TestMethod]
        public void DbHelperUpdateUserFirstNameTest()
        {
            const string newFirstName = "NewFirstName";
            var user = _db.FindUserByEmail(Email);
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

            var result = _db.UpdateUserByParam(Email, UserColumns.LastName, newLastName);

            Assert.IsTrue(result);
            Assert.AreEqual(newLastName, _db.FindUserByEmail(Email).LastName);
        }

        [TestMethod]
        public void DbHelperUpdateUserLastNameTest()
        {
            const string newLastName = "NewLastName";
            var user = _db.FindUserByEmail(Email);
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

            var result = _db.UpdateUserByParam(Email, UserColumns.Phone, newPhone);

            Assert.IsTrue(result);
            Assert.AreEqual(newPhone, _db.FindUserByEmail(Email).Phone);
        }

        [TestMethod]
        public void DbHelperUpdateUserPhoneNumberTest()
        {
            const string newPhone = "1112223333";
            var user = _db.FindUserByEmail(Email);
            user.Phone = newPhone;
            var result = _db.UpdateUser(user);
            //var result = _db.UpdateUserByParam(Email, UserColumns.Phone, newPhone);

            Assert.IsTrue(result);
            Assert.AreEqual(newPhone, _db.FindUserById(user.Id).Phone);
        }

        [TestMethod]
        public void DbHelperResetUserPasswordTest()
        {
            _db.ResetPassword(Email, "NewPassword");

            var result = _db.AuthenticateUser(Email, "NewPassword");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperAuthenticateUserSuccessTest()
        {
            var result = _db.AuthenticateUser(Email, Password);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperAuthenticateUserInvalidPasswordTest()
        {
            var result = _db.AuthenticateUser(Email, "WrongPassword");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperLoginAndGetTokenTest()
        {
            var result = _db.LoginAndGetToken(Email, Password);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void DbHelperLoginAndGetTokenInvalidPasswordTest()
        {
            var result = _db.LoginAndGetToken(Email, "WrongPassword");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void DbHelperRemoveErroneousTokensTest()
        {
            var originalToken = _db.LoginAndGetToken(Email, Password);

            var db = new SqlConnection(_connectionString);
            var userId = _db.FindUserByEmail(Email).Id;
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

            var currentToken = _db.LoginAndGetToken(Email, Password); // this should erase all tokens and create one new one.

            Assert.AreNotEqual(originalToken, currentToken);
        }

        [TestMethod]
        public void DbHelperRemoveExpiredTokenTest()
        {
            var originalToken = _db.LoginAndGetToken(Email, Password);

            var db = new SqlConnection(_connectionString);
            var userId = _db.FindUserByEmail(Email)?.Id;

            var yesterday = DateTime.Today;
            var query = $"UPDATE Tokens SET Expiry = '{yesterday.ToString("G")}' WHERE UserID = {userId};";
            var cmd = new SqlCommand(query, db);
            db.Open();
            cmd.ExecuteNonQuery();
            db.Close();

            var currentToken = _db.LoginAndGetToken(Email, Password);

            Assert.AreNotEqual(originalToken, currentToken);
        }
    }
};