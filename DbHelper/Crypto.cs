// Andrew Horn
// 01/19/2019
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
            // returns a new salt; anything over 12 is stronger, but slower
            return BCryptHelper.GenerateSalt(12);
        }

        public static string HashPassword(string plaintextPassword)
        {
            // will generate a new salt with every hash (never re-use a salt)
            return BCryptHelper.HashPassword(plaintextPassword, GetNewSalt());
        }

        public static bool ValidatePassword(string plaintextPassword, string hashedPassword)
        {
            // can validate a hashed password + salt without knowing the salt
            return BCryptHelper.CheckPassword(plaintextPassword, hashedPassword);
        }
    }
}