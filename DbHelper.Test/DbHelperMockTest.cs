using DBHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace DbHelper.Test
{
    [TestClass]
    public class DbHelperMockTest
    {
        private readonly Moq.Mock<IDbHelper> _db;

        private const string FirstName = "Test";
        private const string LastName = "User";
        private const string Password = "password";
        private const string Email = "test@test.com";
        private const string Phone = "1234567890";

        public DbHelperMockTest()
        {
            _db = new Mock<IDbHelper>();
            var userDb = new List<User>();

            _db.Setup(d => d.CreateNewUser(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((
                    string fName,
                    string lName,
                    string p,
                    string e,
                    string pwd) =>
                {
                    if(userDb.Exists(u => u.Email == e))
                    {
                        return false;
                    }

                    userDb.Add(new User
                    {
                        FirstName = fName,
                        LastName = lName,
                        Email = e,
                        Hash = Crypto.HashPassword(pwd),
                        Phone = p
                    });
                    return true;
                });

            _db.Setup(d => d.AuthenticateUser(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string pwd) =>
                {
                    var user = userDb.FirstOrDefault(u => u.Email.Equals(email));
                    if(user == null)
                    {
                        return false;
                    }

                    return Crypto.ValidatePassword(pwd, user.Hash);
                });

            _db.Setup(d => d.DeleteUser(It.IsAny<string>()))
                .Returns((string email) =>
                {
                    var user = userDb.FirstOrDefault(u => u.Email.Equals(email));
                    if(user == null)
                    {
                        return false;
                    }

                    userDb.Remove(user);
                    return true;
                });

            _db.Setup(d => d.FindUserByEmail(It.IsAny<string>()))
                .Returns((string email) => { return userDb.FirstOrDefault(u => u.Email.Equals(email)); });
        }

        [TestInitialize]
        public void Init()
        {


            _db.Object.CreateNewUser(FirstName, LastName, Phone, Email, Password);
        }

        [TestCleanup]
        public void Dispose()
        {
            while(_db.Object.FindUserByEmail(Email) != null)
            {
                _db.Object.DeleteUser(Email);
            }
        }

        [TestMethod]
        public void DbHelperCreateNewUserTest()
        {
            _db.Object.DeleteUser(Email);
            var result = _db.Object.CreateNewUser(FirstName, LastName, Phone, Email, Password);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperCreateNewUserEmailCollisionTest()
        {
            var result = _db.Object.CreateNewUser("test", "test", "phone", Email, "pwd"); // Create a user with the same email address.
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperDeleteUserTest()
        {
            var result = _db.Object.DeleteUser(Email);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperDeleteNonExistentUserTest()
        {
            var result = _db.Object.DeleteUser("other@email.com");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DbHelperFindUserByEmailTest()
        {
            var user = _db.Object.FindUserByEmail(Email);

            Assert.IsNotNull(user);
        }

        [TestMethod]
        public void DbHelperFindUserByEmailTestFail()
        {
            var result = _db.Object.FindUserByEmail("other@email.com");
            
            Assert.IsNull(result);
        }

        //[TestMethod]
        //public void DbHelperUpdateUserEmailTest()
        //{
        //    //_db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

        //    var user = _db.FindUserByEmail(Email);
        //    user.Email = "newemail@test.com";

        //    var result = _db.UpdateUserByParam(user);

        //    Assert.IsTrue(result);
        //    Assert.AreEqual("newemail@test.com", _db.FindUserByEmail(Email).Email);
        //}

        //[TestMethod]
        //public void DbHelperUpdateUserFirstNameTest()
        //{
        //    //_db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

        //    var user = _db.FindUserByEmail(Email);
        //    user.FirstName = "NewFirstName";

        //    var result = _db.UpdateUserByParam(user);

        //    Assert.IsTrue(result);

        //    Assert.AreEqual("NewFirstName", _db.FindUserByEmail(Email).FirstName);
        //}

        //[TestMethod]
        //public void DbHelperUpdateUserLastNameTest()
        //{
        //    //_db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

        //    var user = _db.FindUserByEmail(Email);
        //    user.LastName = "NewLastName";

        //    var result = _db.UpdateUserByParam(user);

        //    Assert.IsTrue(result);
        //    Assert.AreEqual("NewLastName", _db.FindUserByEmail(Email).LastName);
        //}

        //[TestMethod]
        //public void DbHelperUpdateUserPhoneNumberTest()
        //{
        //    //_db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

        //    var user = _db.FindUserByEmail(Email);
        //    user.Phone = "1112223333";

        //    var result = _db.UpdateUserByParam(user);

        //    Assert.IsTrue(result);
        //    Assert.AreEqual("1112223333", _db.FindUserByEmail(Email).Phone);
        //}

        //[TestMethod]
        //public void DbHelperUpdateUserPasswordTest()
        //{
        //    //_db.CreateNewUser(FirstName, LastName, Phone, Email, Password);

        //    var login = _db.Login(Email, Password);

        //    _db.ResetPassword(Email, "NewPassword");

        //    var result = _db.Login(Email, "NewPassword");

        //    Assert.IsTrue(result);
        //}

        [TestMethod]
        public void DbHelperLoginSuccessTest()
        {
            var result = _db.Object.AuthenticateUser(Email, Password);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DbHelperLoginUnsuccessfulTest()
        {
            var result = _db.Object.AuthenticateUser(Email, "WrongPassword");

            Assert.IsFalse(result);
        }
    }
}
