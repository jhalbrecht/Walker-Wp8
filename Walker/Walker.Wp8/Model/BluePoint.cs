using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Maps.Controls;

namespace Walker.Model
{
    public class BluePoint 
    {
        public GeoCoordinate GeoCoordinate { get; set; }
        public Content
    }
}


        //private void AddBluePoint(GeoCoordinate coord)
        //{
        //    MapOverlay overlay = new MapOverlay
        //    {
        //        GeoCoordinate = coord,
        //        //GeoCoordinate = MvvmMap.Center,
        //        Content = new Ellipse
        //        {
        //            Fill = new SolidColorBrush(Colors.Blue),
        //            Width = 15,
        //            Height = 15
        //        }
        //    };
        //    MapLayer layer = new MapLayer();
        //    layer.Add(overlay);

        //    MvvmMap.Layers.Add(layer);
        //}
