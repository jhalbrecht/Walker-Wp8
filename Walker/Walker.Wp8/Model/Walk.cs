using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Walker.Pcl.Model;

namespace Walker.Model
{
    public class Walk : Activity
    {
        public GeoCoordinate Begin { get; set; }
        public GeoCoordinate End { get; set; }

    }
}
