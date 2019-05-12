using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbHelper.Test
{
    [TestClass]
    public class CryptoTest
    {
        [TestMethod]
        public void CryptoGetHashedPasswordTest()
        {
            // plaintext password
            var pwd = "password";

            // hashed password
            var hash = Crypto.HashPassword(pwd);

            // make sure it doesn't just return the plaintext password
            Assert.AreNotEqual(pwd, hash);
        }

        [TestMethod]
        public void CryptoValidateCorrectPasswordTest()
        {
            // plaintext password
            var pwd = "password";

            // hashed password
            var hash = Crypto.HashPassword(pwd);

            // should return true, indicating the password is correct
            var isCorrect = Crypto.ValidatePassword(pwd, hash);
            Assert.IsTrue(isCorrect);
        }

        [TestMethod]
        public void CryptoValidateIncorrectPasswordFailTest()
        {
            // plaintext password
            var pwd = "password";

            // plaintext password that is invalid
            var incorrectPassword = "wrongPassword";

            // hashed password, using the correct password
            var hash = Crypto.HashPassword(pwd);

            // should return false, indicating the password used was incorrect
            var isCorrect = Crypto.ValidatePassword(incorrectPassword, hash);
            Assert.IsFalse(isCorrect);
        }

        [TestMethod]
        public void CryptoGenerateTokenTest()
        {
            var token = Crypto.GenerateToken();
            Assert.IsNotNull(token);
        }

        [TestMethod]
        public void CryptoSafeTokenTest()
        {
            var token = Crypto.GenerateToken();

            var isSafe = true;

            foreach (var t in token)
                if(t == '\\' || t == '/')
                    isSafe = false;

            Assert.IsTrue(isSafe, "Token contains a backslash");
        }
    }
}
