// Andrew Horn
// 0/15/2019
//
// Crypto class to provide secure cryptography
// and authentication by hashing and verifying passwords.
// Utilizes the BCrypt.Net package to provide the methods.
// Information found on https://cmatskas.com/a-simple-net-password-hashing-implementation-using-bcrypt/

using DevOne.Security.Cryptography.BCrypt;

namespace DbHelper
{
    public static class Crypto
    {
        private static string GetNewSalt()
        {
            return BCryptHelper.GenerateSalt(10);
        }

        public static string HashPassword(string password)
        {
            return BCryptHelper.HashPassword(password, GetNewSalt());
        }

        public static bool ValidatePassword(string plainTextPassword, string hashedPassword)
        {
            return BCryptHelper.CheckPassword(plainTextPassword, hashedPassword);
        }
    }
}