using DBHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DbHelper.Test
{

    [TestClass]
    public class DbHelperTest
    {
        private readonly DBHelper.DbHelper _db;

        private const string FirstName = "Test";
        private const string LastName = "User";
        private const string Password = "password";
        private const string Email = "test@test.com";
        private const string Phone = "1234567890";

        public DbHelperTest()
        {
            _db = GetDb();
        }

        private static DBHelper.DbHelper GetDb()
        {
            return new DBHelper.DbHelper();
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
        public void DbHelperUpdateUserEmailTest()
        {
            const string newEmail = "new@email.test";

            var result = _db.UpdateUser(Email, UserColumns.Email, newEmail);

            Assert.IsTrue(result);
            Assert.AreEqual(newEmail, _db.FindUserByEmail(newEmail).Email);
        }

        [TestMethod]
        public void DbHelperUpdateUserFirstNameTest()
        {
            const string newFirstName = "NewFirstName";
            var result = _db.UpdateUser(Email, UserColumns.FirstName, newFirstName);

            Assert.IsTrue(result);

            Assert.AreEqual(newFirstName, _db.FindUserByEmail(Email).FirstName);
        }

        [TestMethod]
        public void DbHelperUpdateUserLastNameTest()
        {
            const string newLastName = "NewLastName";

            var result = _db.UpdateUser(Email, UserColumns.LastName, newLastName);

            Assert.IsTrue(result);
            Assert.AreEqual(newLastName, _db.FindUserByEmail(Email).LastName);
        }

        [TestMethod]
        public void DbHelperUpdateUserPhoneNumberTest()
        {
            const string newPhone = "1112223333";

            var result = _db.UpdateUser(Email, UserColumns.Phone, newPhone);

            Assert.IsTrue(result);
            Assert.AreEqual(newPhone, _db.FindUserByEmail(Email).Phone);
        }

        [TestMethod]
        public void DbHelperUpdateUserPasswordTest()
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
    }
};