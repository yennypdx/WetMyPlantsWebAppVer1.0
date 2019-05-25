using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Models;

namespace DbHelper
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

    public enum SpeciesColumns
    {
        Id,
        CommonName,
        LatinName,
        WaterMax,
        WaterMin,
        LightMax,
        LightMin
    }

    public enum PlantColumns
    {
        Id,
        Nickname,        
        CurrentWater,
        CurrentLight,
        SpeciesId,
        LightTracker,
        UpdateTime
    }

    public enum ResponseTypes
    {
        LowWater,
        HighWater,
        LowLight,
        HighLight
    }

    public enum HubColumns
    {
        Id, Address, UserId, CurrentPower
    }

    public class DbHelper : IDbHelper
    {
        private readonly string _connectionString;
        private readonly SqlConnection _dbConnection;

        public DbHelper(string connectionString = null)
        {
            _connectionString = connectionString ?? AccessHelper.GetDbConnectionString();
        }

        // Open() makes it easier to use the SqlConnection without worrying about
        // its current state. It will always be open when you need it.
        private SqlConnection Open()
        {
            // Need to reset the connection string anytime the connection closes.
            // The using() statements in the helper methods below will close the
            // connection once the block's execution completes.
            if (_dbConnection.State == ConnectionState.Closed)
            {
                _dbConnection.ConnectionString = _connectionString;
            }
            if (_dbConnection.State != ConnectionState.Open)
            {
                _dbConnection.Open();
            }
            return _dbConnection;
        }

        // RunScalar() executes the SQL scalar command and returns a single value,
        // typically an ID or a count.
        private string RunScalar(string queryString)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var sqlCommand = new SqlCommand(queryString, connection);
                sqlCommand.CommandTimeout = 0;
                var result = sqlCommand.ExecuteScalar()?.ToString();
                connection.Close();
                return result;
            }
        }

        // RunReader executes a SQL query command, returning a collection of data.
        private DataTableReader RunReader(string queryString)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var sqlCommand = new SqlCommand(queryString, connection);
                sqlCommand.CommandTimeout = 0;
                var adapter = new SqlDataAdapter(sqlCommand);
                var dataSet = new DataSet();

                adapter.Fill(dataSet);
                connection.Close();
                return dataSet.CreateDataReader();
            }
        }

        // RunNonQuery executes a SQL command that does not return a query value, but
        // will return the number of rows affected by the action.
        private bool RunNonQuery(string queryString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var sqlCommand = new SqlCommand(queryString, connection);
                    sqlCommand.CommandTimeout = 0;

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
        public User FindUser(string email = null, string token = null)
        {
            if (email != null)
            {
                var queryString = $"SELECT * FROM Users WHERE Email = '{email}';";

                var result = RunReader(queryString);

                // If there are no results to the query, the result will not contain
                // any rows, and attempting to read it will throw an exception.
                return result.Read()
                    ? BuildUserFromDataReader(result)
                    : null;
            }

            if (token != null)
            {
                var tokenQuery = $"SELECT UserId FROM Tokens WHERE Token = '{token}'";

                var idResult = RunScalar(tokenQuery);

                if (idResult == null)
                    return null;

                var id = Convert.ToInt32(idResult);

                var queryString = $"SELECT * FROM Users WHERE UserID = {id};";

                var result = RunReader(queryString);

                // If there are no results to the query, the result will not contain
                // any rows, and attempting to read it will throw an exception.
                return result.Read()
                    ? BuildUserFromDataReader(result)
                    : null;
            }

            return null;
        }

        public List<User> GetAllUsers()
        {
            var query = "SELECT * FROM Users";
            var results = RunReader(query);
            var users = new List<User>();
            while (results.Read())
                users.Add(BuildUserFromDataReader(results));
            return users;
        }

        public User FindUser(int id)
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

        private static Species BuildSpeciesFromDataReader(DataTableReader reader)
        {
            try
            {
                var species = new Species
                {
                    Id = reader.GetInt32((int) SpeciesColumns.Id),
                    LatinName = reader.GetString((int) SpeciesColumns.LatinName),
                    CommonName = reader.GetString((int) SpeciesColumns.CommonName),
                    LightMax = reader.GetDouble((int) SpeciesColumns.LightMax),
                    LightMin = reader.GetDouble((int) SpeciesColumns.LightMin),
                    WaterMax = reader.GetDouble((int) SpeciesColumns.WaterMax),
                    WaterMin = reader.GetDouble((int) SpeciesColumns.WaterMin)
                };
                return species;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Plant BuildPlantFromDataReader(DataTableReader reader)
        {
            try
            {
                var plant = new Plant
                {
                    Id = reader.GetString((int) PlantColumns.Id),
                    Nickname = reader.GetString((int) PlantColumns.Nickname),
                    CurrentLight = reader.GetDouble((int) PlantColumns.CurrentLight),
                    CurrentWater = reader.GetDouble((int) PlantColumns.CurrentWater),
                    SpeciesId = reader.GetInt32((int)PlantColumns.SpeciesId),
                    LightTracker = reader.GetInt32((int) PlantColumns.LightTracker),
                    UpdateTime = reader.GetInt32((int) PlantColumns.UpdateTime)
                };
                return plant;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Inserts a new tuple into the User table containing all the user's values,
        // returns whether or not the insertion was successful
        public bool CreateNewUser(string firstName, string lastName, string phone, string email, string password)
        {
            // Do not create the user if a user with the same email
            // already exists.
            if (FindUser(email: email) != null) return false;

            // Never store the password directly, always use its hash.
            var passwordHash = Crypto.HashPassword(password);

            var queryString = "INSERT INTO Users (FirstName, LastName, Phone, Email, Hash) " +
                              $"VALUES ('{firstName}'" +
                              $", '{lastName}'" +
                              $", '{phone}'" +
                              $", '{email}'" +
                              $", '{passwordHash}');";

            var result = RunNonQuery(queryString);

            if (result)
            {
                var user = FindUser(email: email);
                CreateNotificationPreferences(user.Id);
            }

            return result;
        }

        // Deletes a user (tuple) from the database
        public bool DeleteUser(string email)
        {
            var id = FindUser(email: email)?.Id;
            if (id == null) return false;

            var tokenQuery = $"DELETE FROM Tokens WHERE UserID = {id};";
            var unlinkPlantsQuery = $"DELETE FROM UserPlants WHERE UserID = {id};";
            var removeLinkedPlantsQuery = "DELETE FROM Plants WHERE PlantID NOT IN (SELECT PlantID FROM UserPlants);";
            var userQuery = $"DELETE FROM Users WHERE Email = '{email}';";

            RunNonQuery(tokenQuery);
            RunNonQuery(unlinkPlantsQuery);
            RunNonQuery(removeLinkedPlantsQuery);
            DeleteNotificationPreferences(Convert.ToInt32(id));
            
            return RunNonQuery(userQuery);
        }
        
        public bool UpdateUser(User update)
        {
            if (FindUser(update.Id) == null) return false;

            var query = "UPDATE Users SET " +
                        $"FirstName = '{update.FirstName}', " +
                        $"LastName = '{update.LastName}', " +
                        $"Email = '{update.Email}', " +
                        $"Phone = '{update.Phone}' " +
                        $"WHERE UserId = {update.Id};";

            return RunNonQuery(query);
        }

        public void SetResetCode(int id, string resetCode)
        {
            if (FindUser(id) != null)
            {
                var query = $"INSERT INTO ResetCodes (UserId, Code) VALUES ({id}, '{resetCode}')";

                RunNonQuery(query);
            }
        }

        public bool ValidateResetCode(int userId, string resetCode)
        {
            var query = $"SELECT UserId FROM ResetCodes WHERE Code = '{resetCode}'";
            var id = RunScalar(query);

            return userId.ToString().Equals(id);
        }

        public void DeleteResetCode(int userId)
        {
            var query = $"DELETE FROM ResetCodes WHERE UserId = {userId}";
            RunNonQuery(query);
        }

        public void SetEmailNotificationPreference(int userId, bool setting)
        {
            int set = setting ? 1 : 0;
            var query = $"UPDATE Preferences SET Email = {set} WHERE UserID = {userId}";
            RunNonQuery(query);
        }

        public void SetPhoneNotificationPreference(int userId, bool setting)
        {
            int set = setting ? 1 : 0;
            var query = $"UPDATE Preferences SET Phone = {set} WHERE UserID = {userId};";
            RunNonQuery(query);
        }

        public Dictionary<string, bool> GetNotificationPreferences(int userId)
        {
            var dict = new Dictionary<string, bool>();
            var phoneQuery = $"SELECT Phone FROM Preferences WHERE UserID = {userId};";
            var emailQuery = $"SELECT Email FROM Preferences WHERE UserID = {userId};";

            var phoneResult = RunScalar(phoneQuery);
            var emailResult = RunScalar(emailQuery);

            dict["Phone"] = phoneResult != null && Convert.ToInt32(phoneResult) == 1;
            dict["Email"] = emailResult != null && Convert.ToInt32(emailResult) == 1;

            return dict;
        }

        private void CreateNotificationPreferences(int userId)
        {
            var query = $"INSERT INTO Preferences (UserID, Email, Phone) VALUES ({userId}, 1, 1);";
            RunNonQuery(query);
        }

        private void DeleteNotificationPreferences(int userId)
        {
            var query = $"DELETE FROM Preferences WHERE UserID = {userId};";
            RunNonQuery(query);
        }

        public string GetNotificationResponseMessage(ResponseTypes type)
        {
            Random rnd = new Random();
            int responeseId = rnd.Next(1, 5);
            switch (type)
            {
                case ResponseTypes.HighLight:
                    {
                        var query = $"SELECT ResponseMsg FROM HighLightResponses WHERE ResponseID = {responeseId};";

                        var reader = RunReader(query);

                        if (!reader.HasRows) return null;

                        reader.Read();

                        string result = reader.GetString(0);

                        return result;                      
                    }
                case ResponseTypes.LowLight:
                    {
                        var query = $"SELECT ResponseMsg FROM LowLightResponses WHERE ResponseID = {responeseId};";

                        var reader = RunReader(query);

                        if (!reader.HasRows) return null;

                        reader.Read();

                        string result = reader.GetString(0);

                        return result;
                    }
                case ResponseTypes.HighWater:
                    {
                        var query = $"SELECT ResponseMsg FROM HighWaterResponses WHERE ResponseID = {responeseId};";

                        var reader = RunReader(query);

                        if (!reader.HasRows) return null;

                        reader.Read();

                        string result = reader.GetString(0);

                        return result;
                    }
                case ResponseTypes.LowWater:
                    {
                        var query = $"SELECT ResponseMsg FROM LowWaterResponses WHERE ResponseID = {responeseId};";

                        var reader = RunReader(query);

                        if (!reader.HasRows) return null;

                        reader.Read();

                        string result = reader.GetString(0);

                        return result;
                    }
                default: break;

            }
            throw new Exception("Error in GetNotificationResponce, DbHelper");
        }

        public int CreateNewSpecies(string commonName, string latinName, double waterMax = 0, double waterMin = 0, double lightMax = 0,
            double lightMin = 0)
        {
            var query = "INSERT INTO Species (CommonName, LatinName, WaterMax, WaterMin, LightMax, LightMin) VALUES " +
                        $"('{commonName}', '{latinName}', {waterMax}, {waterMin}, {lightMax}, {lightMin}) " +
                        "SELECT SCOPE_IDENTITY();";

            var result = RunScalar(query);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public List<Species> GetAllSpecies()
        {
            var query = "SELECT * FROM Species;";
            var reader = RunReader(query);

            if (!reader.HasRows) return null;

            var list = new List<Species>();

            while (reader.Read())
            {
                list.Add(BuildSpeciesFromDataReader(reader));
            }

            return list.OrderBy(s => s.LatinName).ToList();
        }

        public Species FindSpecies(string commonName = null, string latinName = null)
        {
            if(commonName != null)
            {
                var query = $"SELECT * FROM Species WHERE CommonName = '{commonName}';";

                var reader = RunReader(query);

                if (!reader.HasRows) return null;

                reader.Read();
                var species = BuildSpeciesFromDataReader(reader);

                return species;
            }
            if (latinName != null)
            {
                var query = $"SELECT * FROM Species WHERE LatinName = '{latinName}';";

                var reader = RunReader(query);

                if (!reader.HasRows) return null;
                reader.Read();

                var species = BuildSpeciesFromDataReader(reader);

                return species;
            }
            //something went wrong, return null
            return null;
        }

        /*
        public Species FindSpecies(string commonName)
        {
            var query = $"SELECT * FROM Species WHERE CommonName = '{commonName}';";

            var reader = RunReader(query);

            if (!reader.HasRows) return null;

            reader.Read();
            var species = BuildSpeciesFromDataReader(reader);

            return species;
        }
        */

        public Species FindSpecies(int id)
        {
            var query = $"SELECT * FROM Species WHERE SpeciesID = {id};";

            var reader = RunReader(query);

            if (!reader.HasRows) return null;

            reader.Read();
            var species = BuildSpeciesFromDataReader(reader);

            return species;
        }

        public bool UpdateSpecies(Species update)
        {
            if (FindSpecies(update.Id) == null) return false;

            var query = "UPDATE Species SET " +
                        $"LatinName = '{update.LatinName}', " +
                        $"CommonName = '{update.CommonName}', " +
                        $"WaterMax = {update.WaterMax}, " +
                        $"WaterMin = {update.WaterMin}, " +
                        $"LightMax = {update.LightMax}, " +
                        $"LightMin = {update.LightMin} " +
                        $"WHERE SpeciesID = {update.Id};";

            var result = RunNonQuery(query);

            return result;
        }

        public bool DeleteSpecies(int id)
        {
            var query = $"DELETE FROM Species WHERE SpeciesID = {id};";

            var result = RunNonQuery(query);

            return result;
        }

        public bool CreateNewPlant(string plantId, int speciesId, string nickname, double currentWater = 0, double currentLight = 0)
        {
            var query = "INSERT INTO Plants (PlantId, SpeciesID, Nickname, CurrentWater, CurrentLight)" +
                        $"VALUES ('{plantId}', {speciesId}, '{nickname}', {currentWater}, {currentLight}) " +
                        "SELECT SCOPE_IDENTITY();";

            var result = RunNonQuery(query);

            return result;
        }

        public List<Plant> GetAllPlants()
        {
            var query = "SELECT * FROM Plants;";
            var reader = RunReader(query);

            if(!reader.HasRows)
                return null;

            var list = new List<Plant>();

            while (reader.Read())
                list.Add(BuildPlantFromDataReader(reader));

            return list;
        }

        public List<Plant> GetPlantsForUser(int id)
        {
            var plantIdQuery = $"SELECT PlantID FROM UserPlants WHERE UserID = {id};";

            var plantIds = RunReader(plantIdQuery);

            if (!plantIds.HasRows) return null;
            
            var plants = new List<Plant>();

            while (plantIds.Read())
                plants.Add(FindPlant(plantIds.GetString(0)));

            return plants;
        }

        public bool RegisterPlantToUser(Plant plant, User user)
        {
            var plantId = plant.Id;

            var query = $"INSERT INTO UserPlants(PlantID, UserID) VALUES ('{plantId}', {user.Id});";

            var result = RunNonQuery(query);

            return result;
        }

        public List<Plant> FindPlantsByNickname(string nickname)
        {
            var query = $"SELECT * FROM Plants WHERE Nickname = '{nickname}';";

            var reader = RunReader(query);

            if (!reader.HasRows) return null;

            var list = new List<Plant>();
            while (reader.Read())
                list.Add(BuildPlantFromDataReader(reader));

            return list;
        }

        public Plant FindPlant(string id)
        {
            var query = $"SELECT * FROM Plants WHERE PlantID = '{id}';";

            var result = RunReader(query);

            if (!result.HasRows) return null;

            result.Read();
            var plant = BuildPlantFromDataReader(result);

            return plant;
        }

        public User FindPlantUser(string id)
        {
            var userIdQuery = $"SELECT UserID FROM UserPlants WHERE PlantID = '{id}';";

            var userId = RunReader(userIdQuery);

            if (!userId.HasRows) return null;

            userId.Read();

            var user = FindUser(userId.GetInt32(0));
            
            return user;
        }

        public bool UpdatePlant(Plant update)
        {
            var query = "UPDATE Plants SET " +
                        $"SpeciesID = {update.SpeciesId}, " +
                        $"Nickname = '{update.Nickname}', " +
                        $"CurrentWater = {update.CurrentWater}, " +
                        $"CurrentLight = {update.CurrentLight}, " +
                        $"UpdateTime = {update.UpdateTime}, " +
                        $"LightTracker = {update.LightTracker} " +
                        $"WHERE PlantID = '{update.Id}';";

            var result = RunNonQuery(query);

            return result;
        }

        public bool DeletePlant(string id)
        {
            var query = $"DELETE FROM Plants WHERE PlantID = '{id}';";
            var unlinkPlantQuery = $"DELETE FROM UserPlants WHERE PlantID = '{id}';";

            RunNonQuery(unlinkPlantQuery);
            var result = RunNonQuery(query);

            return result;
        }

        public bool AuthenticateUser(string email, string password)
        {
            // get the user's hash for comparison
            var userHash = FindUser(email: email)?.Hash;
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
            var userId = FindUser(email: email)?.Id;
            if (userId == null) throw new DataException("Unable to find user");
            // use the id to get the valid token
            return GetUserToken(Convert.ToInt32(userId));
        }

        public bool ValidateUserToken(int userId, string token)
        {
            return GetUserToken(userId) == token;
        }

        public bool ResetPassword(string email, string newPassword)
        {
            // find the user and get their ID
            var id = FindUser(email: email)?.Id;
            // if no user (and no ID) was found, return false
            if (id == null) throw new DataException("Unable to find user");

            // otherwise, generate a new hash from the given password and update
            // the field in the Users table
            var newHash = Crypto.HashPassword(newPassword);
            var query = $"UPDATE Users SET Hash = '{newHash}' WHERE UserID = {id};";
            return RunNonQuery(query);
        }

        public int CreateHub(Hub hub)
        {
            var query = $"INSERT INTO Hubs (HubAddress, UserId, CurrentPower)" +
                $"VALUES ('{hub.Address}', {hub.UserId}, {hub.CurrentPower}) " +
                $"SELECT SCOPE_IDENTITY();";

            var id = RunScalar(query);

            return id != null
                ? Convert.ToInt32(id)
                : -1;
        }

        public bool DeleteHub(int id)
        {
            var query = $"DELETE FROM Hubs WHERE HubId = {id};";

            var result = RunNonQuery(query);
            return result;
        }

        public Hub GetHub(int id)
        {
            var query = $"SELECT * FROM Hubs WHERE HubId = {id};";

            var reader = RunReader(query);

            if (reader != null)
            {
                reader.Read();
                var hub = BuildHub(reader);
                return hub;
            }

            return null;
        }

        public List<Hub> GetHubList(int userId)
        {
            var query = $"SELECT * FROM Hubs WHERE UserId = {userId};";

            var reader = RunReader(query);

            if (reader != null)
            {
                var hubList = new List<Hub>();
                while (reader.Read())
                {
                    hubList.Add(BuildHub(reader));
                }

                return hubList;
            }

            return null;
        }

        public List<Hub> GetAllHubs()
        {
            var query = $"SELECT * FROM Hubs";

            var reader = RunReader(query);

            if(reader != null)
            {
                var hubList = new List<Hub>();
                while(reader.Read())
                {
                    hubList.Add(BuildHub(reader));
                }

                return hubList;
            }

            return null;
        }

        private static Hub BuildHub(DataTableReader reader)
        {
            try
            {
                return new Hub
                {
                    Id = reader.GetInt32((int)HubColumns.Id),
                    Address = reader.GetString((int)HubColumns.Address),
                    UserId = reader.GetInt32((int)HubColumns.UserId),
                    CurrentPower = reader.GetDouble((int)HubColumns.CurrentPower)
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetUserToken(int id)
        {
            // verify that a corresponding user exists
            var user = FindUser(id);
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
            var expiry = result.GetDateTime((int) TokenColumns.Expiry);
            var token = result.GetString((int)TokenColumns.Token);

            // if the token has expired, generate a new one
            // otherwise return the valid token
            return DateTime.Compare((expiry), DateTime.Today) <= 0
                ? GenerateNewTokenForUser(id) 
                : token;
        }

        private string GenerateNewTokenForUser(int id)
        {
            // remove any old tokens, as we should only have one per user
            DeleteUserToken(id);
            // generate a new token based on the current date and time
            var newToken = Crypto.GenerateToken();
            // set the new token
            SetTokenForUser(id, newToken);
            return newToken;
        }

        private void DeleteUserToken(int userId)
        {
            var query = $"DELETE FROM Tokens WHERE UserID = {userId};";
            RunNonQuery(query);
        }

        private void SetTokenForUser(int userId, string token)
        {
            // set an expiration date for two weeks in the future
            var expiry = DateTime.Today;
            expiry = expiry.AddDays(14);
            var query = $"INSERT INTO Tokens (UserID, Token, Expiry) VALUES ({userId}, '{token}', '{expiry}');";
            RunNonQuery(query);
        }
    }
}