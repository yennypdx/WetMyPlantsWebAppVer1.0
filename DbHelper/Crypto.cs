// Andrew Horn
// 0/15/2019
//
// Crypto class to provide secure cryptography
// and authentication by hashing and verifying passwords.
// Utilizes the BCrypt.Net package to provide the methods.
// Information found on https://cmatskas.com/a-simple-net-password-hashing-implementation-using-bcrypt/


namespace DbHelper
{
    public static class Crypto
    {
        private static string GetNewSalt()
        {
            return BCrypt.Net.BCrypt.GenerateSalt(10);
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, GetNewSalt());
        }

        public static bool ValidatePassword(string plainTextPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
        }
    }
}