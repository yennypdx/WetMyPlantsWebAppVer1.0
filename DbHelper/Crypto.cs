// Andrew Horn
// 01/19/2019
//
// Crypto class to provide secure cryptography
// and authentication by hashing and verifying passwords.
// Utilizes the BCrypt.Net package to provide the methods.
// Information found on https://cmatskas.com/a-simple-net-password-hashing-implementation-using-bcrypt/

using System;
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
            if (plaintextPassword == null || hashedPassword == null) return false;
            return BCryptHelper.CheckPassword(plaintextPassword, hashedPassword);
        }

        public static string GenerateToken()
        {
            // allocate a new string
            var str = string.Empty;
            // create a new random number generator
            var r = new Random(Convert.ToInt32(DateTime.Now.Millisecond));

            // we will generate a string of 100 random alphabetic characters
            for (var i = 0; i < 100; i++)
            {
                // each character has a 50% chance of being uppercase vs. lowercase
                if (r.Next(1, 4) <= 2)
                    // lowercase
                    str += Convert.ToChar(r.Next(97, 122));
                else
                {
                    // uppercase
                    str += Convert.ToChar(r.Next(65, 90));
                }
            }

            // return the character string
            return str;
        }

        // GeneratePin returns a random 6 digit number to be used to reset password on Android
        public static int GeneratePin()
        {
            Random rand = new Random();
            return rand.Next(100000, 999999);
        }
    }
}