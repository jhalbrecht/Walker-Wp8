using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Walker.Pcl.ViewModel;
using Walker.Pcl.Model;
using Walker.Pcl.Service;
using GalaSoft.MvvmLight;
using Walker.ViewModels;

namespace Walker.Design
{
    public class FakeAppHubViewModel : ViewModelBase  // Walker.Pcl.ViewModel.AppHubViewModel
    // public class FakeAppHubViewModel : Wp8AppHubViewModel // ViewModelBase  // Walker.Pcl.ViewModel.AppHubViewModel
    {
        private IWalkerDataService fakeWalkerDataService;
        //public FakeAppHubViewModel(IWalkerDataService walkerDataService)
        //    : base(walkerDataService)
        //{
        //    Duration = "00.00";
        //}
        public FakeAppHubViewModel()
        {
            ElapsedTime = "12.34";
            DistanceMeters = "23.45";
            DistanceMiles = "34.56";
            StatusMessages = "From FakeAppHubViewModel constructor";
            ElapsedTime = "45.67";
        }
        public string StatusMessages { get; set; }
        // public string Duration { get; set; }
        public string DistanceMeters { get; set; }
        public string DistanceMiles { get; set; }
        public string ElapsedTime { get; set; }

    }
}