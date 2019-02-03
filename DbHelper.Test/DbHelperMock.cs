using System.Collections.Generic;
using System.Linq;
using DBHelper;
using Models;
using Moq;

namespace DbHelper.Test
{
    public class DbHelperMock
    {
        private readonly Moq.Mock<IDbHelper> _m;

        public IDbHelper Mock => _m.Object;

        public DbHelperMock()
        {
            _m = new Mock<IDbHelper>();
            var userDb = new List<User>();

            _m.Setup(d => d.CreateNewUser(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((
                    string fName,
                    string lName,
                    string p,
                    string e,
                    string pwd) =>
                {
                    if(userDb.Exists(u => u.Email == e))
                        return false;

                    userDb.Add(new User
                    {
                        FirstName = fName,
                        LastName = lName,
                        Email = e,
                        Hash = Crypto.HashPassword(pwd),
                        Phone = p
                    });
                    return true;
                });

            _m.Setup(d => d.AuthenticateUser(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string email, string pwd) =>
                {
                    var user = userDb.FirstOrDefault(u => u.Email.Equals(email));
                    if(user == null)
                        return false;
                    return Crypto.ValidatePassword(pwd, user.Hash);
                });

            _m.Setup(d => d.DeleteUser(It.IsAny<string>()))
                .Returns((string email) =>
                {
                    var user = userDb.FirstOrDefault(u => u.Email.Equals(email));
                    if(user == null)
                        return false;
                    userDb.Remove(user);
                    return true;
                });

            _m.Setup(d => d.FindUserByEmail(It.IsAny<string>()))
                .Returns((string email) => { return userDb.FirstOrDefault(u => u.Email.Equals(email)); });
        }
    }
}