using System.Collections.Generic;

namespace CosmicWorks.Models
{
    public class Location
    {
        public string type { get; set; }
        public List<float> coordinates { get; set; }
    }
}