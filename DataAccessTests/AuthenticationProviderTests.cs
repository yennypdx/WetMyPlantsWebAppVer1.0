using DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessTests
{
    [TestClass()]
    public class AuthenticationProviderTests
    {
        [TestMethod()]
        public void GenerateSaltTest()
        {
            var salt = AuthenticationProvider.GenerateSalt();
            Assert.IsNotNull(salt);
        }

        [TestMethod()]
        public void HashPasswordTest()
        {
            var salt = AuthenticationProvider.GenerateSalt();
            var password = "password";
            var hash = AuthenticationProvider.HashPassword(password + salt);
            Assert.AreNotEqual(password + salt, hash);
        }

        [TestMethod()]
        public void VerifyPasswordTest()
        {
            var password = "password";
            var hash = AuthenticationProvider.HashPassword(password);
            var result = AuthenticationProvider.VerifyPassword(password, hash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyPasswordWithSaltTest()
        {
            var password = "password";
            var salt = AuthenticationProvider.GenerateSalt();
            var hash = AuthenticationProvider.HashPassword(password + salt);
            var result = AuthenticationProvider.VerifyPassword("password" + salt, hash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyPasswordMissingSaltTest()
        {
            var password = "password";
            var salt = AuthenticationProvider.GenerateSalt();
            var hash = AuthenticationProvider.HashPassword(password + salt);
            var result = AuthenticationProvider.VerifyPassword(password, hash);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyPasswordWithSaltWrongPasswordTest()
        {
            var password = "password";
            var incorrectPassword = "Password";
            var salt = AuthenticationProvider.GenerateSalt();
            var hash = AuthenticationProvider.HashPassword(password + salt);
            var result = AuthenticationProvider.VerifyPassword(incorrectPassword + salt, hash);
            Assert.IsFalse(result);
        }
    }
}