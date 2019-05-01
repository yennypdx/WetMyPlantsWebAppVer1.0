using System;
using System.Collections.Generic;
using Models;

namespace DBHelper
{
    public interface IDbHelper
    {
        bool CreateNewUser(string firstName, string lastName, string phone, string email, string password);
        List<User> GetAllUsers();
        User FindUser(string email = null, string token = null);
        User FindUser(int id);
        bool AuthenticateUser(string email, string password);
        string LoginAndGetToken(string email, string password);
        void SetResetCode(int userId, string resetCode);
        bool ValidateResetCode(int userId, string resetCode);
        void DeleteResetCode(int userId);
        bool ValidateUserToken(int userId, string token);
        bool DeleteUser(string email);
        bool ResetPassword(string email, string newPassword);
        bool UpdateUser(User user);

        int CreateNewSpecies(string commonName, string latinName, double waterMax, double waterMin, double lightMax,
            double lightMin);

        List<Species> GetAllSpecies();
        Species FindSpecies(string commonName, string latinName);
        Species FindSpecies(int id);
        bool UpdateSpecies(Species update);
        bool DeleteSpecies(int id);

        bool CreateNewPlant(string plantId, int speciesId, string nickname, double currentWater, double currentLight);
        bool RegisterPlantToUser(Plant plant, User user);
        List<Plant> GetAllPlants();
        List<Plant> GetPlantsForUser(int id);
        List<Plant> FindPlantsByNickname(string nickname);
        Plant FindPlant(string id);
        bool UpdatePlant(Plant update);
        bool DeletePlant(string id);
    }
}