using System;
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
            while(_db.FindUserByEmail(Email) != null)
                _db.RemoveUser(Email);
        }

        [TestMethod]
        public void DbHelperCreateNewUserTest()
        {
            _db.RemoveUser(Email);
            var result = _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperCreateNewUserEmailCollisionTest()
        {
            _db.CreateNewUser("test", "test", "phone", Email, "pwd"); // Create a user with the same email address.
            var result = _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperDeleteUserTest()
        {
            if (_db.FindUserByEmail(Email) == null)
                _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var result = _db.RemoveUser(Email);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperDeleteNonExistentUserTest()
        {
            var result = _db.RemoveUser("other@email.com");

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
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var user = _db.FindUserByEmail(Email);
            user.Email = "newemail@test.com";

            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);
            Assert.AreEqual("newemail@test.com", _db.FindUserByEmail(Email).Email);
        }

        [TestMethod]
        public void DbHelperUpdateUserFirstNameTest()
        {
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var user = _db.FindUserByEmail(Email);
            user.FirstName = "NewFirstName";

            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);

            Assert.AreEqual("NewFirstName", _db.FindUserByEmail(Email).FirstName);
        }

        [TestMethod]
        public void DbHelperUpdateUserLastNameTest()
        {
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var user = _db.FindUserByEmail(Email);
            user.LastName = "NewLastName";

            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);
            Assert.AreEqual("NewLastName", _db.FindUserByEmail(Email).LastName);
        }

        [TestMethod]
        public void DbHelperUpdateUserPhoneNumberTest()
        {
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var user = _db.FindUserByEmail(Email);
            user.Phone = "1112223333";

            var result = _db.UpdateUser(user);

            Assert.IsTrue(result);
            Assert.AreEqual("1112223333", _db.FindUserByEmail(Email).Phone);
        }

        [TestMethod]
        public void DbHelperUpdateUserPasswordTest()
        {
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var login = _db.Login(Email, Password);

            _db.ResetPassword(Email, "NewPassword");

            var result = _db.Login(Email, "NewPassword");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperLoginSuccessTest()
        {
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var result = _db.Login(Email, Password);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperLoginUnsuccessfulTest()
        {
            _db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

            var result = _db.Login(Email, "WrongPassword");

            Assert.IsFalse(result);
        }
    }
}
