// Andy Horn
// 01/09/2019
// AuthenticationProvider.cs
//
// Provides a wrapper for the System.Web.Helpers.Crypto class from .Net Framework
// The methods are used to create and verify passwords utilizing a stored 'salt' value
// The 'salt' is a randomly generated string of characters that are added to the
// original password string before it is all cryptographically hashed into a
// seemingly random string of characters.
// 
// In order to hack a password, the hacker would have to know, not only the password,
// but the salt value as well. This adds an extra layer of security to the password.

using System.Web.Helpers;

namespace DataAccess
{
    public static class AuthenticationProvider
    {
        public static string GenerateSalt()
        {
            return Crypto.GenerateSalt();
        }

        public static string HashPassword(string password)
        {
            return Crypto.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            var result = Crypto.VerifyHashedPassword(hash, password);
            return result;
        }
    }
}
