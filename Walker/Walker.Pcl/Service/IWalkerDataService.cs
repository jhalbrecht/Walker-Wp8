using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Walker.Pcl.Model;

namespace Walker.Pcl.Service
{
    public interface IWalkerDataService
    {
        // Task<bool> CreateGpxFile(List<System.Device.Location.GeoCoordinate> geos);

        void ToggleActivity();
        Task<ObservableCollection<Walk>> GetActivitiesAsync();
    }
}
