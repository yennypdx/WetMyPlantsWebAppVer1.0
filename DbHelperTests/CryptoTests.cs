using DbHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbHelperTests
{
    [TestClass]
    public class CryptoTests
    {
        [TestMethod]
        public void CryptoTestGeneratePasswordHash()
        {
            var hash = Crypto.HashPassword("password");

            Assert.IsNotNull(hash);
        }

        [TestMethod]
        public void CryptoTestVerifyHashedPassword()
        {
            const string password = "password";
            var hash = Crypto.HashPassword(password);

            var isCorrect = Crypto.ValidatePassword(password, hash);
            Assert.IsTrue(isCorrect);
        }

        [TestMethod]
        public void CryptoTestVerifyBadPassword()
        {
            const string correctPassword = "password";
            const string incorrectPassword = "notPassword";

            var hash = Crypto.HashPassword(correctPassword);

            var isCorrect = Crypto.ValidatePassword(incorrectPassword, hash);
            Assert.IsFalse(isCorrect);
        }
    }
}
