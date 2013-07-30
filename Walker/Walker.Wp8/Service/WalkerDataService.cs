using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using LiveSDKHelper;
using Microsoft.Live;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Walker.Model;
using Walker.Pcl.Model;
using Walker.Pcl.Service;
using Walker.ViewModels;
using Walker.Wp8;
using Windows.Devices.Geolocation;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Walker.Service
{
    public class WalkerDataService : IWalkerDataService
    {

        //public static MobileServiceClient MobileService = new MobileServiceClient(App.AmsUrl, App.AmsSecret);

        private IMobileServiceTable<Walk> walkTable = App.MobileService.GetTable<Walk>();
        private IMobileServiceTable<Sensor> sensorTable = App.MobileService.GetTable<Sensor>();
        private ObservableCollection<Walk> _activities;
        private WalkerAppSettings _walkerAppSettings;
        private AdpLogger _logger;
        //Geolocator geolocator = null;
        //bool tracking = false;
        //ProgressIndicator pi;
        //MapLayer PushpinMapLayer;

        const int MIN_ZOOM_LEVEL = 1;
        const int MAX_ZOOM_LEVEL = 20;
        const int MIN_ZOOMLEVEL_FOR_LANDMARKS = 16;

        private Walk _activity;
        private bool isActive = false;
        private GeoCoordinate _CurrentGeoCoordinate;
        private System.Device.Location.GeoCoordinate activityBegin;
        private System.Device.Location.GeoCoordinate activityEnd;
        private GeoCoordinate _PreviousGeoCoordinate;
        // private GeoCoordinateWatcher _geo;
        public GeoPositionAccuracy Accuracy = GeoPositionAccuracy.High;
        private string _mode = "Aerial";
        private double _CurrentDistance = 0;
        private double _PreviousDistance = 0;

        public ObservableCollection<Walk> _Trips;

        Geolocator geolocator = null;
        bool tracking = false;
        ProgressIndicator pi;
        MapLayer PushpinMapLayer;

        public DispatcherTimer activityTimer;
        TimeSpan _ElapsedTime;
        DateTime lastTime, startTime;

        // private TestViewModel TestVm;
        private Wp8AppHubViewModel Vm;
        private SensorsViewModel SensorVm;
        private int testInt = 0;
        private List<System.Device.Location.GeoCoordinate> Geos;
        private LiveConnectClient _client;
        private StreamSocket bluetoothSocket;
        string _receivedBuffer = "";
        public WalkerDataService()
        {
            _walkerAppSettings = new WalkerAppSettings();

            _activities = new ObservableCollection<Walk>();

            _logger = new AdpLogger();
            _logger.Log(this, "WalkerDataService()");

            //pi.IsVisible = true;
            //pi.IsIndeterminate = true;
            //pi.Text = "Resolving...";
            // SystemTray.SetProgressIndicator(this, pi);
            // SystemTray.SetProgressIndicator(Walker.View.AppHubView, pi);
            // fix bummer Messenger? Vm = SimpleIoc.Default.GetInstance<AppHubViewModel>(); 

            pi = new ProgressIndicator();
            pi.IsIndeterminate = true;
            pi.IsVisible = false;
            _Trips = new ObservableCollection<Walk>();

            // MvvmMap.ZoomLevel = 15;
            // fix TestVm = SimpleIoc.Default.GetInstance<TestViewModel>();
            Geos = new List<System.Device.Location.GeoCoordinate>();

            PhoneApplicationService phoneAppService = PhoneApplicationService.Current;
            phoneAppService.UserIdleDetectionMode = IdleDetectionMode.Disabled;

            LocationInitialize();
            TryBluetooth();
        }
        private async void TryBluetooth()
        {
            if (_walkerAppSettings.BlueToothEnabled)
            {
                PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
                var pairedDevices = await PeerFinder.FindAllPeersAsync();

                if (pairedDevices.Count == 0)
                {
                    _logger.Log(this, "TryBluetooth() ", "No paired devices found.");
                }
                else
                {
                    try
                    {
                        PeerInformation selectedDevice = pairedDevices[0]; //I'm  selecting the first one
                        bluetoothSocket = new StreamSocket();
                        await bluetoothSocket.ConnectAsync(selectedDevice.HostName, "1");
                        WaitForData(bluetoothSocket);
                        Write("ping"); //first ping to test connection
                    }
                    catch (Exception e)
                    {
                        _logger.Log(this, "TryBluetooth() threw exception: ", e.ToString());
                        // fix Vm.StatusMessages = "TryBluetooth() threw exception: ";
                    }
                }
            }
        }

        async private void Write(string str)
        {
            var dataBuffer = GetBufferFromByteArray(Encoding.UTF8.GetBytes(str + "|"));
            await bluetoothSocket.OutputStream.WriteAsync(dataBuffer);
        }

        private IBuffer GetBufferFromByteArray(byte[] package)
        {
            using (DataWriter dw = new DataWriter())
            {
                dw.WriteBytes(package);
                return dw.DetachBuffer();
            }
        }
        async private void WaitForData(StreamSocket socket)
        {
            try
            {
                byte[] bytes = new byte[5];
                await socket.InputStream.ReadAsync(bytes.AsBuffer(), 5, InputStreamOptions.Partial);
                bytes = bytes.TakeWhile((v, index) => bytes.Skip(index).Any(w => w != 0x00)).ToArray();
                string str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                if (str.Contains("|"))
                {
                    _receivedBuffer += str.Substring(0, str.IndexOf("|"));
                    DoSomethingWithReceivedString(_receivedBuffer);
                    _receivedBuffer = str.Substring(str.IndexOf("|") + 1);
                }
                else
                {
                    _receivedBuffer += str;
                }
            }
            catch
            {
                // TryConnect();
                TryBluetooth();
            }
            finally
            {
                WaitForData(socket);
            }
        }

        /// <summary>
        /// Process received bluetooth sensor data
        /// </summary>
        /// <param name="buffer"></param>
        private async void DoSomethingWithReceivedString(string buffer)
        {
            _logger.Log(this, "DoSomethingWithReceivedString(string buffer) : ", buffer);

            if (SensorVm == null)
                SensorVm = SimpleIoc.Default.GetInstance<SensorsViewModel>();

            if (buffer.Contains("Temperature"))
            {
                var foo = buffer.Split(':');
                SensorVm.Temperature = foo[1].Trim(); // .StatusMessages += buffer + " ";
                goto TheEnd;
            }

            if (buffer.Contains("Humidity"))
            {
                var foo = buffer.Split(':');
                SensorVm.Humidity = foo[1].Trim(); // .StatusMessages += buffer + " ";
                goto TheEnd;
            }

            if (buffer.Contains("Heart"))
            {
                var foo = buffer.Split(':');
                SensorVm.Heart = foo[1].Trim(); // .StatusMessages += buffer + " ";

                // persist sensor data to skyDrive if we're in an activity and Bluetooth is enabled in settings.
                if (isActive && _walkerAppSettings.BlueToothEnabled)
                {
                    Sensor sensor = new Sensor
                    {
                        ActivityStartTime = _activity.Start,
                        Temperature = Convert.ToDouble(SensorVm.Temperature),
                        Humidity = Convert.ToDouble(SensorVm.Humidity),
                        Heart = Convert.ToInt16(SensorVm.Heart)
                    };

                    try
                    {
                        _logger.Log(this, "sensorTable.InsertAsync(sensor)");
                        await sensorTable.InsertAsync(sensor);
                    }
                    catch (Exception e)
                    {
                        _logger.Log(this, "sensorTable.InsertAsync(sensor) exception: ", e.ToString());
                    }
                }
                goto TheEnd;
            }
            else
            {
                Vm.StatusMessages += buffer;
            }

            //if (buffer == "ping")
        //{
        //    MessageBox.Show("Rountrip took " + DateTime.Now.Subtract(_pingSentTime).TotalMilliseconds + "MS");
        //}
        //else
        //{
        //    MessageBox.Show(buffer);
        //}

            // if (SensorVm == null)
        TheEnd:
            ;
        }

        #region Location stuff

        /// <summary>
        /// Initialize geo location
        /// </summary>
        private void LocationInitialize()
        {
            // Reinitialize the GeoCoordinateWatcher
            // _geo = new GeoCoordinateWatcher(Accuracy) { MovementThreshold = 10 }; // jha previously 20

            _CurrentGeoCoordinate = new GeoCoordinate(34.2572, -118.6003); // roughly chatsworth
            _PreviousGeoCoordinate = _CurrentGeoCoordinate;

            geolocator = new Geolocator();
            geolocator.DesiredAccuracy = PositionAccuracy.High;
            // geolocator.MovementThreshold = 50; // The units are meters.
            geolocator.MovementThreshold = 10; // The units are meters.

            geolocator.StatusChanged -= geolocator_StatusChanged;
            geolocator.PositionChanged -= geolocator_PositionChanged;
            geolocator.StatusChanged += geolocator_StatusChanged;
            geolocator.PositionChanged += geolocator_PositionChanged;
        }
        void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            string status = "";

            switch (args.Status)
            {
                case PositionStatus.Disabled:
                    // the application does not have the right capability or the location master switch is off
                    status = "location is disabled in phone settings";
                    break;
                case PositionStatus.Initializing:
                    // the geolocator started the tracking operation
                    status = "initializing";
                    break;
                case PositionStatus.NoData:
                    // the location service was not able to acquire the location
                    status = "no data";
                    break;
                case PositionStatus.Ready:
                    // the location service is generating geopositions as specified by the tracking parameters
                    status = "ready";
                    break;
                case PositionStatus.NotAvailable:
                    status = "not available";
                    // not used in WindowsPhone, Windows desktop uses this value to signal that there is no hardware capable to acquire location information
                    break;
                case PositionStatus.NotInitialized:
                    // the initial state of the geolocator, once the tracking operation is stopped by the user the geolocator moves back to this state
                    break;
            }
            // update debug log in view model with geo location status change messages.
            Messenger.Default.Send<string>(status);
        }

        /// <summary>
        /// update view model and bound .xaml with geo cordinates, elapsed time and distance traveled. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            // Unfortunately, the Location API works with Windows.Devices.Geolocation.Geocoordinate objeccts
            // and the Maps controls use System.Device.Location.GeoCoordinate objects so we have to do a
            // conversion before we do anything with it on the map
            GeoCoordinate geoCoordinate = new GeoCoordinate()
            {
                Altitude = args.Position.Coordinate.Altitude.HasValue ? args.Position.Coordinate.Altitude.Value : 0.0,
                Course = args.Position.Coordinate.Heading.HasValue ? args.Position.Coordinate.Heading.Value : 0.0,
                HorizontalAccuracy = args.Position.Coordinate.Accuracy,
                Latitude = args.Position.Coordinate.Latitude,
                Longitude = args.Position.Coordinate.Longitude,
                Speed = args.Position.Coordinate.Speed.HasValue ? args.Position.Coordinate.Speed.Value : 0.0,
                VerticalAccuracy = args.Position.Coordinate.AltitudeAccuracy.HasValue ? args.Position.Coordinate.AltitudeAccuracy.Value : 0.0
            };

            Geos.Add(geoCoordinate); // save these pups to create a .gpx file...
            _CurrentGeoCoordinate = geoCoordinate;

            if (isActive)
            {
                _CurrentDistance += _PreviousGeoCoordinate.GetDistanceTo(_CurrentGeoCoordinate);

            }
            else
            {
                _PreviousGeoCoordinate = _CurrentGeoCoordinate;
            }

            _PreviousGeoCoordinate = _CurrentGeoCoordinate;

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                //TestVm.Hi = testInt++.ToString();
                //TestVm.GeoCoordinate = geoCoordinate;
                if (Vm == null)
                    Vm = SimpleIoc.Default.GetInstance<Wp8AppHubViewModel>();
                Vm.GeoCoordinate = geoCoordinate;
                Vm.DistanceMeters = _CurrentDistance.ToString("F2");
                Vm.DistanceMiles = (999.99).ToString();
                Vm.StatusMessages += string.Format("\nlat: {0} lon: {1}", geoCoordinate.Latitude.ToString(), geoCoordinate.Longitude.ToString());
            });

            // Dispatcher.BeginInvoke(() =>
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                // Draw a pushpin
                DrawPushpin(geoCoordinate);
                AddBluePoint(geoCoordinate);
                // Show progress indicator while map resolves to new position
                pi.IsVisible = true;
                pi.IsIndeterminate = true;
                pi.Text = "Resolving location...";
                // fix SystemTray.SetProgressIndicator(this, pi);
            });
        }

        private void AddBluePoint(GeoCoordinate coord)
        {
            MapOverlay overlay = new MapOverlay
            {
                GeoCoordinate = coord,
                //GeoCoordinate = MvvmMap.Center,
                Content = new Ellipse
                {
                    Fill = new SolidColorBrush(Colors.Blue),
                    Width = 15,
                    Height = 15
                }
            };
            MapLayer layer = new MapLayer();
            layer.Add(overlay);
            Messenger.Default.Send<MapLayer>(layer);
        }

        private void Jpin()
        {
            // fix UserLocationMarker marker = (UserLocationMarker)this.FindName("UserLocationMarker");
            // fix marker.GeoCoordinate = MvvmMap.Center;

            // fix Pushpin pushpin = (Pushpin)this.FindName("MyPushpin");
            // fix pushpin.GeoCoordinate = new GeoCoordinate(30.712474, -132.32691);
        }

        /// <summary>
        /// Persist to Azure if enabled in settings. 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private async Task PersistIt(string fileName)
        {
            if (Vm == null)
                Vm = SimpleIoc.Default.GetInstance<Wp8AppHubViewModel>();
            Vm.Activities.Add(_activity);

            if (_walkerAppSettings.AzureEnabled)
            {
                try
                {
                    _activity.GpxFileName = fileName;
                    await walkTable.InsertAsync(_activity);
                    _logger.Log(this, "walkTable.InsertAsync(_activity); ", "apparently worked");

                }
                catch (Exception e)
                {
                    _logger.Log(this, "walkTable.InsertAsync(_activity); ", e.ToString());
                }
            }
        }

        /// <summary>
        /// Start timer and initialize for start of new activity.
        /// </summary>
        private void StartTimer()
        {
            activityTimer = new DispatcherTimer();
            activityTimer.Interval = TimeSpan.FromSeconds(1);
            activityTimer.Tick += OnTimerTick;
            activityTimer.Start();
            lastTime = DateTime.Now;
            startTime = DateTime.Now;
        }

        /// <summary>
        /// Update view model and counters to update current elapsed time and distance travled. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnTimerTick(Object sender, EventArgs args)
        {
            if (Vm == null)
                Vm = SimpleIoc.Default.GetInstance<Wp8AppHubViewModel>();

            _ElapsedTime = DateTime.Now - _activity.Start;
            Vm.ElapsedTime = _ElapsedTime.ToString(@"hh\:mm\:ss");
            Vm.DistanceMeters = _CurrentDistance.ToString("0\\,000");
            Vm.DistanceMiles = ((_CurrentDistance * 3.281) / 5280).ToString("0\\,000");
            //Messenger.Default.Send<AppHubViewModel>(Vm);
        }

        private void MapResolveCompleted(object sender, MapResolveCompletedEventArgs e)
        {
            // Hide progress indicator
            pi.IsVisible = false;
            pi.IsIndeterminate = false;
        }

        private void DrawPushpin(GeoCoordinate coord)
        {
            var aPushpin = CreatePushpinObject();

            //Creating a MapOverlay and adding the Pushpin to it.
            MapOverlay MyOverlay = new MapOverlay();
            MyOverlay.Content = aPushpin;
            MyOverlay.GeoCoordinate = coord;
            MyOverlay.PositionOrigin = new Point(0, 0.5);
        }

        private Grid CreatePushpinObject()
        {
            //Creating a Grid element.
            Grid MyGrid = new Grid();
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.Background = new SolidColorBrush(Colors.Transparent);

            //Creating a Rectangle
            Rectangle MyRectangle = new Rectangle();
            MyRectangle.Fill = new SolidColorBrush(Colors.Black);
            MyRectangle.Height = 20;
            MyRectangle.Width = 20;
            MyRectangle.SetValue(Grid.RowProperty, 0);
            MyRectangle.SetValue(Grid.ColumnProperty, 0);

            //Adding the Rectangle to the Grid
            MyGrid.Children.Add(MyRectangle);

            //Creating a Polygon
            Polygon MyPolygon = new Polygon();
            MyPolygon.Points.Add(new Point(2, 0));
            MyPolygon.Points.Add(new Point(22, 0));
            MyPolygon.Points.Add(new Point(2, 40));
            MyPolygon.Stroke = new SolidColorBrush(Colors.Black);
            MyPolygon.Fill = new SolidColorBrush(Colors.Black);
            MyPolygon.SetValue(Grid.RowProperty, 1);
            MyPolygon.SetValue(Grid.ColumnProperty, 0);

            //Adding the Polygon to the Grid
            MyGrid.Children.Add(MyPolygon);
            return MyGrid;
        }


        #endregion

        private async Task<bool> LoadData()
        {
            return true;
        }

        /// <summary>
        /// Main loop for Activity tracking. 
        ///     Start and stops an activity.
        ///     Persists to 
        ///         - SkyDrive 
        ///         - Azure
        ///         - .gpx file viewer
        ///             as determined by SettingsView.xaml
        /// </summary>
        public async void ToggleActivity()
        {
            if (Vm == null)
                Vm = SimpleIoc.Default.GetInstance<Wp8AppHubViewModel>();

            if (isActive)
            {
                Vm.StatusMessages += "ToggleActivity Stop";
                activityTimer.Stop();
                isActive = false;
                _activity.Stop = DateTime.Now;
                activityEnd = _CurrentGeoCoordinate;
                TimeSpan ts = _activity.Stop - _activity.Start;
                var distance = (activityEnd).GetDistanceTo(activityBegin);
                _activity.Distance = distance;
                var distanceInFeet = distance * 3.281;
                var distanceInMiles = distanceInFeet / 5280;

                if (Geos.Count > 0)
                {
                    string skyDriveFileName = "Walker";
                    skyDriveFileName += DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
                    skyDriveFileName += ".gpx";
                    _logger.Log(this, skyDriveFileName);

                    byte[] bytes = Encoding.UTF8.GetBytes(ToGpx());
                    MemoryStream gpxMemoryStream = new MemoryStream(bytes);

                    try
                    {
                        if (_client == null)
                        {
                            // _client = new LiveConnectClient(e.Session);
                            _client = App.LiveConnectClient;
                        }
                        _logger.Log(this, "_client.UploadAsync(MeDetails.TopLevelSkyDriveFolder, skyDriveFileName, gpxMemoryStream,");

                        var result = await _client.UploadAsync(MeDetails.TopLevelSkyDriveFolder, skyDriveFileName, gpxMemoryStream,
                                                OverwriteOption.Overwrite);
                        _logger.Log(this, "Upload to skyDrive apparently worked!", result.ToString());
                        // var success = await Windows.System.Launcher.LaunchFileAsync(ms);
                        // var success = await Windows.System.Launcher.  LaunchFileAsync(ms);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(this, "Upload to skyDrive exception: ", ex.ToString());
                    }
                    gpxMemoryStream.Dispose();
                    if (_walkerAppSettings.GpxEnabled)
                        LaunchGpxFileAssociation(skyDriveFileName);

                    PersistIt(skyDriveFileName);
                }

                Vm.StatusMessages = string.Empty;
                Vm.StatusMessages += String.Format("Walked: {0:0000.00} meters ({2:00.000} miles) in {1} seconds.\n", distance, ts.ToString(), distanceInMiles);
                Vm.StatusMessages += String.Format("(new calc) Walked: {0:0000.00} meters \n", _CurrentDistance);
                _CurrentDistance = 0;
            }
            else
            {
                Vm.StatusMessages += "ToggleActivity Start";
                StartTimer();
                isActive = true;
                _activity = new Walk();
                _activity.Start = DateTime.Now;
                activityBegin = _CurrentGeoCoordinate;
                
                Geos = new List<GeoCoordinate>();   // clear Geos list as we're starting a new activity track.
                Geos.Add(_CurrentGeoCoordinate);    // Begin activity with current location. 
            }
        }

        /// <summary>
        /// Get activities from Azure
        /// </summary>
        /// <returns></returns>
        public async Task<ObservableCollection<Walk>> GetActivitiesAsync()
        {
            try
            {
                _logger.Log(this, "GetActivitiesAsync");
                return await walkTable.ToCollectionAsync();
            }
            catch (Exception e)
            {
                _logger.Log(this, "GetActivitiesAsync threw exception: ", e.ToString());
                return null;
            }
        }
        /// <summary>
        /// Launc .gpx file association. Pick #wp8 app to view .gpx file
        /// </summary>
        /// <param name="fileName"></param>
        private async void LaunchGpxFileAssociation(string fileName)
        {
            // Access local storage.
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;

            // Save the file in local storage
            StorageFile file = await local.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            if (file != null)
            {
                string userContent = ToGpx();
                if (!String.IsNullOrEmpty(userContent))
                {
                    using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                    {
                        using (DataWriter dataWriter = new DataWriter(transaction.Stream))
                        {
                            dataWriter.WriteString(userContent);
                            transaction.Stream.Size = await dataWriter.StoreAsync(); // reset stream size to override the file
                            await transaction.CommitAsync();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("The text box is empty, please write something and then click 'save' again.");
                }
            }
            StorageFile bqfile = await local.GetFileAsync(fileName);

            // Launch the bug query file.
            Windows.System.Launcher.LaunchFileAsync(bqfile);
        }

        /// <summary>
        /// Create a GPX representation of the track data.
        /// </summary>
        /// <returns>GPX data. (UTF-16 encoded)</returns>
        public string ToGpx()
        {
            const string gpx = "http://www.topografix.com/GPX/1/1";
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb))
            {
                writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");

                writer.WriteStartElement("gpx", gpx);
                writer.WriteAttributeString("version", "1.1");
                writer.WriteAttributeString("creator", "Walker");

                writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

                //Bounds b = CalculateBounds();
                //writer.WriteStartElement("bounds", gpx);
                //writer.WriteAttributeString("minlat", XmlConvert.ToString(b.MinLat));
                //writer.WriteAttributeString("maxlat", XmlConvert.ToString(b.MaxLat));
                //writer.WriteAttributeString("minlon", XmlConvert.ToString(b.MinLon));
                //writer.WriteAttributeString("maxlon", XmlConvert.ToString(b.MaxLon));

                writer.WriteStartElement("trk", gpx);
                writer.WriteStartElement("name", gpx);
                // writer.WriteValue(Name);
                writer.WriteValue("TrackName");
                writer.WriteEndElement();

                writer.WriteStartElement("trkseg", gpx);
                // foreach (TrackPoint p in Points)
                foreach (var p in Geos)
                {
                    writer.WriteStartElement("trkpt", gpx);
                    writer.WriteAttributeString("lat", XmlConvert.ToString(p.Latitude));
                    writer.WriteAttributeString("lon", XmlConvert.ToString(p.Longitude));
                    writer.WriteStartElement("ele", gpx);
                    writer.WriteValue(XmlConvert.ToString(p.Altitude));
                    writer.WriteEndElement();
                    // writer.WriteStartElement("time", gpx);
                    // writer.WriteValue(p. Time.ToString("s") + "Z");
                    // writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
            return sb.ToString();
        }
    }
}
