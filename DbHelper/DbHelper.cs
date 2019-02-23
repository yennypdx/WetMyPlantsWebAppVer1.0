using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DbHelper;
using Models;

namespace DBHelper
{
    public enum UserColumns
    {
        Id,
        FirstName,
        LastName,
        Hash,
        Phone,
        Email
    }

    internal enum TokenColumns
    {
        Id,
        UserId,
        Token,
        Expiry
    }

    public class DbHelper : IDbHelper
    {
        private readonly string _connectionString;
        private readonly SqlConnection _dbConnection;

        public DbHelper(string connectionString = null)
        {
            _connectionString = connectionString ?? AccessHelper.GetDbConnectionString();
            _dbConnection = new SqlConnection(_connectionString);
        }

        // Open() makes it easier to use the SqlConnection without worrying about
        // its current state. It will always be open when you need it.
        private SqlConnection Open()
        {
            // Need to reset the connection string anytime the connection closes.
            // The using() statements in the helper methods below will close the
            // connection once the block's execution completes.
            _dbConnection.ConnectionString = _connectionString;
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();
            return _dbConnection;
        }

        // RunScalar() executes the SQL scalar command and returns a single value,
        // typically an ID or a count.
        private string RunScalar(string queryString)
        {
            var sqlCommand = new SqlCommand(queryString, _dbConnection);

            using(Open())
            {
                var result = sqlCommand.ExecuteScalar()?.ToString();
                return result;
            }
        }

        // RunReader executes a SQL query command, returning a collection of data.
        private DataTableReader RunReader(string queryString)
        {
            var sqlCommand = new SqlCommand(queryString, _dbConnection);
            var adapter = new SqlDataAdapter(sqlCommand);
            var dataSet = new DataSet();

            using(Open())
            {
                adapter.Fill(dataSet);
                return dataSet.CreateDataReader();
            }
        }

        // RunNonQuery executes a SQL command that does not return a query value, but
        // will return the number of rows affected by the action.
        private bool RunNonQuery(string queryString)
        {
            var sqlCommand = new SqlCommand(queryString, _dbConnection);

            try
            {
                using(Open())
                {
                    var result = sqlCommand.ExecuteNonQuery();
                    return result != 0;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Exception: {e}");
                return false;
            }
        }

        // Queries the SQL database for a single user and all its data,
        // returns a filled User object
        public User FindUserByEmail(string email)
        {
            var queryString = $"SELECT * FROM Users WHERE Email = '{email}';";

            var result = RunReader(queryString);

            // If there are no results to the query, the result will not contain
            // any rows, and attempting to read it will throw an exception.
            return result.Read()
                ? BuildUserFromDataReader(result)
                : null;
        }

        public List<User> GetAll()
        {
            var query = "SELECT * FROM Users";
            var results = RunReader(query);
            var users = new List<User>();
            while (results.Read())
                users.Add(BuildUserFromDataReader(results));
            return users;
        }

        public User FindUserById(int id)
        {
            var query = $"SELECT * FROM Users WHERE UserID = {id};";
            var result = RunReader(query);
            return result.Read()
                ? BuildUserFromDataReader(result)
                : null;
        }

        private static User BuildUserFromDataReader(DataTableReader reader)
        {
            //reader.Read();
            return new User
            {
                Id = reader.GetInt32((int) UserColumns.Id),
                FirstName = reader.GetString((int) UserColumns.FirstName),
                LastName = reader.GetString((int) UserColumns.LastName),
                Email = reader.GetString((int) UserColumns.Email),
                Phone = reader.GetString((int) UserColumns.Phone),
                Hash = reader.IsDBNull((int) UserColumns.Hash) 
                    ? ""
                    : reader.GetString((int) UserColumns.Hash)
            };
        }

        // Inserts a new tuple into the User table containing all the user's values,
        // returns whether or not the insertion was successful
        public bool CreateNewUser(string firstName, string lastName, string phone, string email, string password)
        {
            // Do not create the user if a user with the same email
            // already exists.
            if (FindUserByEmail(email) != null) return false;

            // Never store the password directly, always use its hash.
            var passwordHash = Crypto.HashPassword(password);

            var queryString = "INSERT INTO Users (FirstName, LastName, Phone, Email, Hash) " +
                              $"VALUES ('{firstName}'" +
                              $", '{lastName}'" +
                              $", '{phone}'" +
                              $", '{email}'" +
                              $", '{passwordHash}');";

            var result = RunNonQuery(queryString);
            return result;
        }

        // Deletes a user (tuple) from the database
        public bool DeleteUser(string email)
        {
            var id = FindUserByEmail(email)?.Id;
            if (id == null) return false;

            var tokenQuery = $"DELETE FROM Tokens WHERE UserID = {id};";
            RunNonQuery(tokenQuery);

            var userQuery = $"DELETE FROM Users WHERE Email = '{email}';";
            return RunNonQuery(userQuery);
        }

        public bool UpdateUserByParam(string email, UserColumns col, string newValue)
        {
            var id = FindUserByEmail(email)?.Id;
            var query = $"UPDATE Users SET {col.ToString()} = '{newValue}' WHERE UserID = {id};";
            return RunNonQuery(query);
        }


        public bool UpdateUser(User update)
        {
            if (FindUserById(update.Id) == null) return false;

            var query = "UPDATE Users SET " +
                        $"FirstName = '{update.FirstName}', " +
                        $"LastName = '{update.LastName}', " +
                        $"Email = '{update.Email}', " +
                        $"Phone = '{update.Phone}' " +
                        $"WHERE UserId = {update.Id};";

            return RunNonQuery(query);
        }

        public bool AuthenticateUser(string email, string password)
        {
            // get the user's hash for comparison
            var userHash = FindUserByEmail(email)?.Hash;
            // validate the given password
            return Crypto.ValidatePassword(password, userHash);
        }

        public string LoginAndGetToken(string email, string password)
        {
            // authenticate the email and password combo
            // if invalid, return nothing
            if (!AuthenticateUser(email, password))
                return null;

            // if valid, get the user's id
            var userId = FindUserByEmail(email)?.Id;
            if (userId == null) throw new DataException("Unable to find user");
            // use the id to get the valid token
            return GetUserToken(Convert.ToInt32(userId));
        }

        public bool ResetPassword(string email, string newPassword)
        {
            // find the user and get their ID
            var id = FindUserByEmail(email)?.Id;
            // if no user (and no ID) was found, return false
            if (id == null) throw new DataException("Unable to find user");

            // otherwise, generate a new hash from the given password and update
            // the field in the Users table
            var newHash = Crypto.HashPassword(newPassword);
            var query = $"UPDATE Users SET Hash = '{newHash}' WHERE UserID = {id};";
            return RunNonQuery(query);
        }

        private string GetUserToken(int id)
        {
            // verify that a corresponding user exists
            var user = FindUserById(id);
            if (user == null) throw new DataException("Unable to find user");

            // make sure there are zero (0) or one (1) tokens for any user,
            // if there are two (2) or more, this is an error.
            // correct this error by erasing all tokens associated
            // with that user and generating one (1) replacement token.
            var numTokens = RunScalar($"SELECT COUNT(*) FROM Tokens WHERE UserId = '{user.Id}';");
            if (numTokens != null && Convert.ToInt32(numTokens) != 1)
                DeleteUserToken(id);

            // get the token for this user
            var query = $"SELECT * FROM Tokens WHERE UserID = '{id}';";
            var result = RunReader(query);

            // if no token exists, generate a new one
            if (!result.HasRows)
                return GenerateNewTokenForUser(id);

            // otherwise, get the token and its expiration date
            result.Read();
            var expiry = result.GetDateTime((int)TokenColumns.Expiry).ToString("ddMMyyyy");
            var token = result.GetString((int)TokenColumns.Token);

            // if the token has expired, generate a new one
            // otherwise return the valid token
            return ToDateTime(expiry) > DateTime.Today 
                ? GenerateNewTokenForUser(id) 
                : token;
        }

        private string GenerateNewTokenForUser(int id)
        {
            // remove any old tokens, as we should only have one per user
            DeleteUserToken(id);
            // generate a new token based on the current date and time
            var newToken = GenerateNewToken();
            // set the new token
            SetTokenForUser(id, newToken);
            return newToken;
        }

        private bool DeleteUserToken(int userId)
        {
            var query = $"DELETE FROM Tokens WHERE UserID = {userId};";
            return RunNonQuery(query);
        }

        private static string GenerateNewToken()
        {
            // generate a new token based on the current date and time
            var dateString = DateTime.Today.Ticks.ToString();
            return Crypto.HashPassword(dateString);
        }

        private bool SetTokenForUser(int userId, string token)
        {
            // set an expiration date for two weeks in the future
            var expiry = DateTime.Today;
            expiry = expiry.AddDays(14);
            var query = $"INSERT INTO Tokens (UserID, Token, Expiry) VALUES ({userId}, '{token}', '{expiry}');";
            return RunNonQuery(query);
        }

        private static DateTime ToDateTime(string dateTime)
        {
            // convert a datetime string to a DateTime object
            // datetime string expected in ddmmyyyy format
            if (!dateTime.Length.Equals(8)) throw new ArgumentException("dateTime string must be in format ddmmyyyy");
            return new DateTime()
                    .AddYears((Convert.ToInt32(dateTime.Substring(4, 4))))
                    .AddMonths(Convert.ToInt32(dateTime.Substring(2, 2)))
                    .AddDays(Convert.ToDouble(dateTime.Substring(0, 2)));
        }
    }
}