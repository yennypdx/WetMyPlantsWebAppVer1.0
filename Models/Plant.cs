namespace Models
{
    public class Plant
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
        public int  SpeciesId { get; set; }
        public double CurrentLight { get; set; }
        public double CurrentWater { get; set; }
    }
}