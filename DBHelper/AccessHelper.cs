using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace DBHelper
{
    class AccessHelper
    {
        string connectionString = string.Empty;
        public AccessHelper()
        {
            try
            {
                connectionString = ConfigurationManager.ConnectionStrings["Data Source=wetmyplants.database.windows.net;Initial Catalog=WetMyPlants;User ID=wetmyplants;Password=********;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"].ConnectionString;
            }
            catch(Exception ex)
            {
                throw new Exception("Error in AccessHelper constructor" + ex.Message);
            }

        }
       
    }
}
