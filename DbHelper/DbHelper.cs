using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using DbHelper;
using Models;
using System.Net.Http;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DBHelper
{
    internal enum UserColumns
    {
        Id,
        FirstName,
        LastName,
        Hash,
        Phone,
        Email
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
            // Need to reset the connection string anytime the connection closes.
            // The using() statements in the helper methods below will close the
            // connection once the block's execution completes.
            _dbConnection.ConnectionString = _dbConnectionString;
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();
            return _dbConnection;
        }

        // RunScalar() executes the SQL scalar command and returns a single value,
        // typically an ID or a count.
        private string RunScalar(string commandString)
        {
            var sqlCommand = new SqlCommand(commandString, _dbConnection);

            using(Open())
            {
                var result = sqlCommand.ExecuteScalar()?.ToString();
                return result;
            }
        }

        // RunReader executes a SQL query command, returning a collection of data.
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

        // RunNonQuery executes a SQL command that does not return a query value, but
        // will return the number of rows affected by the action.
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
            if (email == null) return null;

            var queryString = $"SELECT * FROM Users WHERE Email = '{email}';";

            var result = RunReader(queryString);

            // If there are no results to the query, the result will not contain
            // any rows, and attempting to read it will throw an exception.
            if (!result.HasRows) return null;

            result.Read(); // Move to the first (and only) record.
            var user = new User
            {
                Id = result.GetInt32((int) UserColumns.Id),
                FirstName = result.GetString((int) UserColumns.FirstName),
                LastName = result.GetString((int) UserColumns.LastName),
                Email = result.GetString((int) UserColumns.Email),
                Phone = result.GetString((int) UserColumns.Phone),
                Hash = result.GetString((int) UserColumns.Hash)
            };
            return user;
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
                              $"VALUES ('{firstName}', '{lastName}', '{phone}', '{email}', '{passwordHash}');";

            var result = RunNonQuery(queryString);
            return result;
        }

        // Deletes a user (tuple) from the database
        public bool RemoveUser(string email)
        {
            var removeString = $"DELETE FROM Users WHERE Email = '{email}';";

            var result = RunNonQuery(removeString);

            return result;
        }

        public bool UpdateUser(User user)
        {
            throw new NotImplementedException();
        }

        public bool Login(string email, string password)
        {
            //throw new NotImplementedException();
            var userHash = FindUserByEmail(email).Hash;

            var isValid = Crypto.ValidatePassword(password, userHash);

            return isValid;
        }

        public bool ForgotPassword(string email)
        {
            SendPasswordResetEmail(email).Wait();
            return true;
        }

        public bool ResetPassword(string email)
        {
           // SendPasswordResetEmail(email);
           
            return true;
        }

        //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
        static public async Task SendPasswordResetEmail(string email)
        {
            string apiKey = System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("resetpassword@wetmyplants.com", "WetMyPlants Team"),
                Subject = "Reset Password",
                PlainTextContent = "Please click on this link to reset your password: https://wetmyplants.azurewebsites.net/Account/ResetPassword",
                HtmlContent = "<strong>Please click on this link to reset your password: </strong><a href='https://wetmyplants.azurewebsites.net/Account/ResetPassword'></a>"
            };
            msg.AddTo(new EmailAddress(email, "user"));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
        }
    }
}