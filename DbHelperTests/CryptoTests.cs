using System;
using DbHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbHelperTests
{
    [TestClass]
    public class CryptoTests
    {
        [TestMethod]
        public void CryptoTestGetNewSalt()
        {
            var salt = Crypto.GetNewSalt();

            Assert.IsNotNull(salt);
        }

        [TestMethod]
        public void CryptoTestGeneratePasswordHashAndSalt()
        {
            var salt = Crypto.GetNewSalt();
            var hash = Crypto.HashPassword("password", salt);

            Assert.IsNotNull(hash);
        }

        [TestMethod]
        public void CryptoTestGeneratePasswordHashOnly()
        {
            var hash = Crypto.HashPassword("password");

            Assert.IsNotNull(hash);
        }

        [TestMethod]
        public void CryptoTestVerifyHashedPasswordWithSalt()
        {
            var password = "password";
            var salt = Crypto.GetNewSalt();
            var hash = Crypto.HashPassword(password, salt);

            var isCorrect = Crypto.ValidatePassword(password, hash);
            Assert.IsTrue(isCorrect);
        }

        [TestMethod]
        public void CryptoTestVerifyHashPasswordNoSalt()
        {
            var password = "password";
            var hash = Crypto.HashPassword(password);

            var isCorrect = Crypto.ValidatePassword(password, hash);
            Assert.IsTrue(isCorrect);
        }

        [TestMethod]
        public void CryptoTestVerifyBadPassword()
        {
            var correctPassword = "password";
            var incorrectPassword = "notPassword";

            var hash = Crypto.HashPassword(correctPassword);

            var isCorrect = Crypto.ValidatePassword(incorrectPassword, hash);
            Assert.IsFalse(isCorrect);
        }
    }
}
