using System;
using System.Collections.Generic;
using Models;

namespace DBHelper
{
    public interface IDbHelper
    {
        bool CreateNewUser(string firstName, string lastName, string phone, string email, string password);
        List<User> GetAllUsers();
        User FindUserByEmail(string email);
        User FindUserById(int id);
        bool AuthenticateUser(string email, string password);
        string LoginAndGetToken(string email, string password);
        bool DeleteUser(string email);
        bool ResetPassword(string email, string newPassword);
        bool UpdateUser(User user);

        bool CreateNewSpecies(string commonName, string latinName, double waterMax, double waterMin, double lightMax,
            double lightMin);

        List<Species> GetAllSpecies();
        Species FindSpeciesByLatinName(string latinName);
        Species FindSpeciesByCommonName(string commonName);
        Species FindSpeciesById(int id);
        bool UpdateSpecies(Species update);
        bool DeleteSpecies(int id);

        bool CreateNewPlant(int speciesId, string nickname, double currentWater, double currentLight);
        List<Plant> GetAllPlants();
        List<Plant> FindPlantsByNickname(string nickname);
        Plant FindPlantById(int id);
        bool UpdatePlant(Plant update);
        bool DeletePlant(int id);
    }
}