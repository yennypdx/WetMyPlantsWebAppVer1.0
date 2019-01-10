using DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessTests
{
    [TestClass()]
    public class AuthenticationProviderTests
    {
        private const string Password = "password";

        [TestMethod]
        public void GenerateSaltTest()
        {
            var salt = AuthenticationProvider.GenerateSalt();
            Assert.IsNotNull(salt);
        }

        [TestMethod]
        public void HashPasswordTest()
        {
            var salt = AuthenticationProvider.GenerateSalt();
            var hash = AuthenticationProvider.HashPassword(Password + salt);
            Assert.AreNotEqual(Password + salt, hash);
        }

        [TestMethod]
        public void VerifyPasswordTest()
        {
            var hash = AuthenticationProvider.HashPassword(Password);
            var result = AuthenticationProvider.VerifyPassword(Password, hash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyPasswordWithSaltTest()
        {
            var salt = AuthenticationProvider.GenerateSalt();
            var hash = AuthenticationProvider.HashPassword(Password + salt);
            var result = AuthenticationProvider.VerifyPassword(Password + salt, hash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyPasswordMissingSaltTest()
        {
            var salt = AuthenticationProvider.GenerateSalt();
            var hash = AuthenticationProvider.HashPassword(Password + salt);
            var result = AuthenticationProvider.VerifyPassword(Password, hash);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyPasswordWithSaltWrongPasswordTest()
        { 
            const string incorrectPassword = "Password";
            var salt = AuthenticationProvider.GenerateSalt();
            var hash = AuthenticationProvider.HashPassword(Password + salt);
            var result = AuthenticationProvider.VerifyPassword(incorrectPassword + salt, hash);
            Assert.IsFalse(result);
        }
    }
}