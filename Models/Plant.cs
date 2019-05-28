namespace Models
{
    public class Plant
    {
        public string Id { get; set; }
        public string Nickname { get; set; }       
        public double CurrentLight { get; set; }
        public double CurrentWater { get; set; }
        public int SpeciesId { get; set; }
        public int LightTracker { get; set; }
        public int UpdateTime { get; set; }
    }
}