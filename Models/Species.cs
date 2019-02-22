namespace Models
{
    public class Species
    {
        public int Id { get; set; }
        public string LatinName { get; set; }
        public string CommonName { get; set; }
        public double WaterMax { get; set; }
        public double WaterMin { get; set; }
        public double LightMax { get; set; }
        public double LightMin { get; set; }
    }
}