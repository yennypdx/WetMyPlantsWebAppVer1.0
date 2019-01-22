using System;
using System.Data;
using System.Data.SqlClient;
using DbHelper;
using Models;

namespace DBHelper
{
    enum UserColumns
    {
        Id,
        FirstName,
        LastName,
        Phone,
        Email,
        Hash
    }

    public class DbHelper
    {
        private readonly string _dbConnectionString = AccessHelper.GetDbConnectionString();
        private readonly SqlConnection _dbConnection;

        public DbHelper()
        {
            _dbConnection = new SqlConnection(_dbConnectionString);
        }

        // Open() makes it easier to use the SqlConnection without worrying about
        // its current state. It will always be open when you need it.
        private SqlConnection Open()
        {
            _dbConnection.ConnectionString = _dbConnectionString;
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();
            return _dbConnection;
        }

        private string RunScalar(string commandString)
        {
            var sqlCommand = new SqlCommand(commandString, _dbConnection);

            using(Open())
            {
                var result = sqlCommand.ExecuteScalar()?.ToString();
                return result;
            }
        }

        private DataTableReader RunReader(string connectionString)
        {
            var sqlCommand = new SqlCommand(connectionString, _dbConnection);
            var adapter = new SqlDataAdapter(sqlCommand);
            var dataSet = new DataSet();

            using(Open())
            {
                adapter.Fill(dataSet);
                return dataSet.CreateDataReader();
            }
        }

        private bool RunNonQuery(string connectionString)
        {
            var sqlCommand = new SqlCommand(connectionString, _dbConnection);

            try
            {
                using(Open())
                {
                    var result = sqlCommand.ExecuteNonQuery();
                    return result != 0;
                }
            }
            catch(Exception)
            {
                return false;
            }
        }

        private User FindUserByEmail(string email)
        {
            if (email == null) return null;

            var queryString = $"SELECT * FROM Users WHERE Email = '{email}';";

            var result = RunReader(queryString);

            if (!result.HasRows) return null;

            result.Read();
            var user = new User
            {
                Id = result.GetString((int) UserColumns.Id),
                FirstName = result.GetString((int) UserColumns.FirstName),
                LastName = result.GetString((int) UserColumns.LastName),
                Email = result.GetString((int) UserColumns.Email),
                Phone = result.GetString((int) UserColumns.Phone),
                Hash = result.GetString((int) UserColumns.Hash)
            };
            return user;
        }

        public bool CreateNewUser(string firstName, string lastName, string phone, string email, string password)
        {
            if (FindUserByEmail(email) != null) return false;

            var passwordHash = Crypto.HashPassword(password);

            var queryString = "INSERT INTO Users (FirstName, LastName, Phone, Email, Hash) " +
                              $"VALUES ({firstName}, {lastName}, {phone}, {email}, {passwordHash})";

            var result = RunNonQuery(queryString);
            return result;
        }
    }
}