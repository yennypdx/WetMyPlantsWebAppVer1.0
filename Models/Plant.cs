namespace Models
{
    public class Plant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string  Species { get; set; }
        public double Sunlight { get; set; }
        public double Moisture { get; set; }
    }
}