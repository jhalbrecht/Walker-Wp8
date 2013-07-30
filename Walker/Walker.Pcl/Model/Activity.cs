using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Device.Location;

namespace Walker.Pcl.Model
{
    public class Activity
    {
        public Activity()
        {
            this.Start = DateTime.Now;
        }
        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }
        public string ActivityType { get; set; }
        public double Distance { get; set; }    
        //public GeoCoordinate Begin { get; set; }
        //public GeoCoordinate End { get; set; }
        public string GpxFileName { get; set; }
    }
}
