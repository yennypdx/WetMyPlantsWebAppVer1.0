namespace DBHelper
{
    public static class AccessHelper
    {
        private static readonly string connectionString = @"Data Source=wetmyplants.database.windows.net;Initial Catalog=WetMyPlants;User ID=wetmyplants;Password=Gr33nThumb;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        private static readonly string testConnectiongString =
            @"Data Source=wetmyplants-test.c9yldqomj91e.us-west-2.rds.amazonaws.com,1433;Initial Catalog=WetMyPlantsTest;User ID=wetmyplants;Password=GR33nThumb;";
        public static string GetDbConnectionString()
        {
            return connectionString;
        }

        public static string GetTestDbConnectionString()
        {
            return testConnectiongString;
        }
       
    }
}
