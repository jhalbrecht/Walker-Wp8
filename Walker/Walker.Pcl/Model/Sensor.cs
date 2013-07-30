using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Walker.Pcl.Model
{
    public class Sensor
    {
        public  int Id { get; set; }
        public DateTime ActivityStartTime { get; set; } // The Activity->Walk:StartTime should be unique across all activities.
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public int Heart { get; set; }
    }
}
