using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Hash { get; set; }
        public string Password { get; set; }
        public List<string> Plants { get; set; } = new List<string>(); //if things break horribly, put this back to int
    }
}
