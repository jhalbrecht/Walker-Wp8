using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace Walker.ViewModels
{
    public class SensorsViewModel : ViewModelBase
    {
        public SensorsViewModel()
        {
            Temperature = "n/a";
            Humidity = "n/a";
            Heart = "n/a";
        }

        public string Temperature { get; set; }
        public string Humidity { get; set; }
        public string Heart { get; set; }
    }
}
