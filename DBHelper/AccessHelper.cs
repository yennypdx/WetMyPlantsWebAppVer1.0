namespace DBHelper
{
    public static class AccessHelper
    {
        private static readonly string connectionString = @"Data Source=wetmyplants.database.windows.net;
                                                            Initial Catalog=WetMyPlants;User ID=wetmyplants;
                                                            Password=Gr33nThumb;Connect Timeout=30;Encrypt=True;
                                                            TrustServerCertificate=False;ApplicationIntent=ReadWrite;
                                                            MultiSubnetFailover=False";

        public static string GetDbConnectionString()
        {
            return connectionString;
        }
       
    }
}
