using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApp.Models.HubViewModels
{
    public class HubViewModel
    {
        public List<Hub> Hubs { get; set; } = new List<Hub>();
    }
}