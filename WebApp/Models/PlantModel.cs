using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApp.Models
{
    public class PlantModel
    {
        public long Id { get; set; }
        public long TypeId { get; set; }
        public string NickName { get; set; }
        public ulong LightVariable { get; set; }
        public ulong WaterVariable { get; set; }
    }
}