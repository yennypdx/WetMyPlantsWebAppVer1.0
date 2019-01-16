using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbHelper.Test
{
    [TestClass]
    public class CryptoTest
    {
        [TestMethod]
        public void CryptoGetHashedPasswordTest()
        {
            var pwd = "password";

            var hash = Crypto.HashPassword(pwd);

            Assert.AreNotEqual(pwd, hash);
        }

        [TestMethod]
        public void CryptoValidateCorrectPasswordTest()
        {
            var pwd = "password";
            var hash = Crypto.HashPassword(pwd);

            var isCorrect = Crypto.ValidatePassword(pwd, hash);
            Assert.IsTrue(isCorrect);
        }

        [TestMethod]
        public void CryptoValidateIncorrectPasswordFailTest()
        {
            var pwd = "password";
            var wrongPwd = "wrongPassword";

            var hash = Crypto.HashPassword(pwd);

            var isCorrect = Crypto.ValidatePassword(wrongPwd, hash);

            Assert.IsFalse(isCorrect);
        }
    }
}
