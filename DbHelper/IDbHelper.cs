using System;
using System.Collections.Generic;
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

        bool AddNewSpecies(string commonName, string latinName, double waterMax, double waterMin, double lightMax,
            double lightMin);
        Species FindSpeciesByLatinName(string latinName);
        Species FindSpeciesByCommonName(string commonName);
        Species FindSpeciesById(int id);
        bool UpdateSpecies(int id, SpeciesColumns property, string newValue);
        bool DeleteSpecies(int id);

        bool AddNewPlant(int id, string nickname, double currentWater, double currentLight);
        List<Plant> FindPlantsByNickname(string nickname);
        Plant FindPlantById(int id);
        bool UpdatePlant(int id, PlantColumns property, string newValue);
        bool DeletePlant(int id);
    }
}