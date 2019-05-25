using System.Collections.Generic;
using Models;

namespace WebApp.Models.HomeViewModels
{
    public class DashboardViewModel
    {
        public User User { get; set; } = null;
        public List<Plant> Plants { get; set; } = new List<Plant>();
        public List<Hub> Hubs { get; set; } = new List<Hub>();
    }
}