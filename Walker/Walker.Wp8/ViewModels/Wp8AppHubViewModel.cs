using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Phone.Maps.Controls;
using Walker.Model;
using Walker.Pcl.Model;
using Walker.Pcl.Service;
using Walker.Pcl.ViewModel;
using Walker.Service;
using Windows.Devices.Geolocation;
using Microsoft.Phone.Maps.Toolkit;

// http://www.geekchamp.com/articles/implementing-wp7-toggleimagecontrol-from-the-ground-up-part2

namespace Walker.ViewModels
{
    public class Wp8AppHubViewModel : ViewModelBase
    {
        // private System.Device.Location.GeoCoordinate GeoCoordinate;
        private AdpLogger _logger;
        private WalkerDataService _walkerDataService;
        private readonly INavigationService _navigationService;
        private WalkerAppSettings _walkerAppSettings;

        // public TestViewModel(IWalkerDataService walkerDataService)
        public Wp8AppHubViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _walkerDataService = new WalkerDataService(); 
            _logger = new AdpLogger();
            GeoCoordinate = new System.Device.Location.GeoCoordinate();
            // Messenger.Default.Register<Wp8AppHubViewModel>(this, HandleMessengerMessage);
            Messenger.Default.Register<string>(this, HandleMessengerStringMessage);
            ConfigureMapModes();
            _walkerAppSettings = new WalkerAppSettings();
            if (_walkerAppSettings.AzureEnabled)
                DoIt();
            else
            Activities = new ObservableCollection<Walk>();
            ElapsedTime = "n/a";
        }
        private void ConfigureMapModes()
        {
            Modes = new ObservableCollection<MapMode>
                {
                  new MapMode {CartographicMode = MapCartographicMode.Road, Name = "Road"},
                  new MapMode {CartographicMode = MapCartographicMode.Aerial, Name = "Aerial"},
                  new MapMode {CartographicMode = MapCartographicMode.Hybrid, Name = "Hybrid"},
                  new MapMode {CartographicMode = MapCartographicMode.Terrain, Name = "Terrain"}
                };
            SelectedMode = Modes[3];
        }

        private void HandleMessengerMessage(Wp8AppHubViewModel obj)
        {
            StatusMessages = obj.StatusMessages; 
        }
        private void HandleMessengerStringMessage(string str)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => StatusMessages += str);
        }

        private async Task DoIt()
        {
            Activities = new ObservableCollection<Walk>(); 
            Activities = await _walkerDataService.GetActivitiesAsync(); 
        }

        /// <summary>
        /// The <see cref="GeoCoordinate" /> property's name.
        /// </summary>
        public const string GeoCoordinatePropertyName = "GeoCoordinate";

        private GeoCoordinate _GeoCoordinate = null;

        /// <summary>
        /// Sets and gets the GeoCoordinate property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public GeoCoordinate GeoCoordinate
        {
            get
            {
                return _GeoCoordinate;
            }

            set
            {
                if (_GeoCoordinate == value)
                {
                    return;
                }

                RaisePropertyChanging(() => GeoCoordinate);
                _GeoCoordinate = value;
                RaisePropertyChanged(() => GeoCoordinate);
            }
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

        private RelayCommand _settingsViewCommand;

        /// <summary>
        /// Gets the SettingsViewCommand.
        /// </summary>
        public RelayCommand SettingsViewCommand
        {
            get
            {
                return _settingsViewCommand
                    ?? (_settingsViewCommand = new RelayCommand(
                                          () => _navigationService.Navigate("View/SettingsView", null)));
            }
        }

        public string StatusMessages { get; set; }
        public ObservableCollection<Walk> Activities { get; set; }
        // public string Duration { get; set; }
        public string DistanceMeters { get; set; }
        public string DistanceMiles { get; set; }
        public string ElapsedTime { get; set; }
        public ObservableCollection<MapMode> Modes { get; set; }
        public MapMode SelectedMode { get; set; }
        public ObservableCollection<MapLayer> MapLayers { get; set; }
        public string Temperature { get; set; }
        public string Humidity { get; set; }
        public string Heart { get; set; }
    }
}
