using Models;

namespace DBHelper
{
    public interface IDbHelper
    {
        bool CreateNewUser(string firstName, string lastName, string phone, string email, string password);
        User FindUserByEmail(string email);
        bool AuthenticateUser(string email, string password);
        string LoginAndGetToken(string email, string password);
        bool DeleteUser(string email);
        bool ResetPassword(string email, string newPassword);
        bool UpdateUser(User user);
    }
}