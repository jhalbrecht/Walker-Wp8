using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Walker.Pcl.Model;
using Walker.Pcl.Service;

namespace Walker.Pcl.ViewModel
{
    public class AppHubViewModel : ViewModelBase
    {
        private AdpLogger _logger;
        private IWalkerDataService _walkerDataService;

        public AppHubViewModel(IWalkerDataService walkerDataService)
        {
            _logger = new AdpLogger();
            _logger.Log(this, "AppHubViewModel()");
            _walkerDataService = walkerDataService; 

            InitializeDefaults();
            Messenger.Default.Register<AppHubViewModel>(this, HandleSendVm);
        }

        private void HandleSendVm(AppHubViewModel obj)
        {
            _logger.Log(this, obj.StatusMessages);
            StatusMessages = obj.StatusMessages;
            DistanceMeters = obj.DistanceMeters;
            DistanceMiles = obj.DistanceMiles;
            ElapsedTime = obj.ElapsedTime; 

        }


        private void InitializeDefaults()
        {
            Activities = new ObservableCollection<Walk>();

            StatusMessages = "default status message from view model constructor";
            ElapsedTime = "00:00";
            DistanceMeters = "00:00";
        }



        private RelayCommand _toggleActivityCommand;

        /// <summary>
        /// Gets the ToggleActivityCommand.
        /// </summary>
        public RelayCommand ToggleActivityCommand
        {
            get
            {
                return _toggleActivityCommand
                    ?? (_toggleActivityCommand = new RelayCommand(
                                          () =>
                                          {
                                              _logger.Log(this, "_toggleActivityCommand: ");

                                              _walkerDataService.ToggleActivity(); 

                                          }));
            }
        }



        // public event PropertyChangedEventHandler PropertyChanged;

        public string StatusMessages { get; set; }
        public ObservableCollection<Walk> Activities { get; set; }
        // public string Duration { get; set; }
        public string DistanceMeters { get; set; }
        public string DistanceMiles { get; set; }
        public string ElapsedTime { get; set; }
    }
}
