#define WP8
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using LiveSDKHelper;
using Microsoft.Live;
using Microsoft.Live.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Walker.Pcl.Model;
using Walker.Pcl.ViewModel;
using Walker.ViewModels;
using Walker.Wp8;
using Windows.Devices.Geolocation;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;


// Nokia resources
//  https://projects.developer.nokia.com/mapexplorer/wiki/Implementation

// phone toolkit
// http://wp.qmatteoq.com/maps-in-windows-phone-8-and-phone-toolkit-a-winning-team-part-1/

// http://metronuggets.com/2013/06/25/belatedly-introducing-livesdkhelper/


namespace Walker.View
{
    public partial class AppHubView : PhoneApplicationPage
    {
        const int MIN_ZOOM_LEVEL = 1;
        const int MAX_ZOOM_LEVEL = 20;
        const int MIN_ZOOMLEVEL_FOR_LANDMARKS = 16;

        private AdpLogger _logger;

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

        // private Wp8AppHubViewModel TestVm;
        private int testInt = 0;

        private List<System.Device.Location.GeoCoordinate> Geos;

        private LiveConnectClient _client;

        private StreamSocket bluetoothSocket;
        string _receivedBuffer = "";

        public AppHubView()
        {
            _logger = new AdpLogger();
            _logger.Log(this, "AdpLogger()");
            InitializeComponent();
            pi = new ProgressIndicator();
            pi.IsIndeterminate = true;
            pi.IsVisible = false;
            _Trips = new ObservableCollection<Walk>();

            MvvmMap.ZoomLevel = 15;
            // TestVm = SimpleIoc.Default.GetInstance<Wp8AppHubViewModel>();
            Geos = new List<System.Device.Location.GeoCoordinate>();

            PhoneApplicationService phoneAppService = PhoneApplicationService.Current;
            phoneAppService.UserIdleDetectionMode = IdleDetectionMode.Disabled;


            Messenger.Default.Register<MapLayer>(this, HandleMapLayer);
        }

        private void HandleMapLayer(MapLayer obj)
        {
            MvvmMap.Layers.Add(obj);
        }

        //private async void TryBluetooth()
        //{
        //    PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
        //    var pairedDevices = await PeerFinder.FindAllPeersAsync();

        //    if (pairedDevices.Count == 0)
        //    {
        //        _logger.Log(this, "TryBluetooth() ", "No paired devices found.");
        //        Vm.StatusMessages = "No paired devices found.";
        //    }
        //    else
        //    {
        //        try
        //        {
        //            PeerInformation selectedDevice = pairedDevices[0];//I'm  selecting the first one
        //            bluetoothSocket = new StreamSocket();
        //            await bluetoothSocket.ConnectAsync(selectedDevice.HostName, "1");
        //            WaitForData(bluetoothSocket);
        //            Write("ping");//first ping to test connection
        //        }
        //        catch (Exception e)
        //        {
        //            _logger.Log(this, "TryBluetooth() threw exception: ", e.ToString());
        //            Vm.StatusMessages = "TryBluetooth() threw exception: ";
        //        }
        //    }
        //}

        //async private void Write(string str)
        //{
        //    var dataBuffer = GetBufferFromByteArray(Encoding.UTF8.GetBytes(str + "|"));
        //    await bluetoothSocket.OutputStream.WriteAsync(dataBuffer);
        //}

        //private IBuffer GetBufferFromByteArray(byte[] package)
        //{
        //    using (DataWriter dw = new DataWriter())
        //    {
        //        dw.WriteBytes(package);
        //        return dw.DetachBuffer();
        //    }
        //}

        //async private void WaitForData(StreamSocket socket)
        //{
        //    try
        //    {
        //        byte[] bytes = new byte[5];
        //        await socket.InputStream.ReadAsync(bytes.AsBuffer(), 5, InputStreamOptions.Partial);
        //        bytes = bytes.TakeWhile((v, index) => bytes.Skip(index).Any(w => w != 0x00)).ToArray();
        //        string str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        //        if (str.Contains("|"))
        //        {
        //            _receivedBuffer += str.Substring(0, str.IndexOf("|"));
        //            DoSomethingWithReceivedString(_receivedBuffer);
        //            _receivedBuffer = str.Substring(str.IndexOf("|") + 1);
        //        }
        //        else
        //        {
        //            _receivedBuffer += str;
        //        }
        //    }
        //    catch
        //    {
        //        // TryConnect();
        //        TryBluetooth();
        //    }
        //    finally
        //    {
        //        WaitForData(socket);
        //    }
        //}

        //private void DoSomethingWithReceivedString(string buffer)
        //{
        //    //if (buffer == "ping")
        //    //{
        //    //    MessageBox.Show("Rountrip took " + DateTime.Now.Subtract(_pingSentTime).TotalMilliseconds + "MS");
        //    //}
        //    //else
        //    //{
        //    //    MessageBox.Show(buffer);
        //    //}
        //    Vm.StatusMessages += buffer + " ";
        //    _logger.Log(this, buffer);

        //}

        /// <summary>
        /// Gets the view's ViewModel.
        /// </summary>
        public Wp8AppHubViewModel Vm
        {
            get
            {
                return (Wp8AppHubViewModel)DataContext;
            }
        }

        private void PhoneApplicationPageLoaded(object sender, RoutedEventArgs e)
        {
            // MvvmMap.Center = VmTest.GeoCoordinate; 
            //_Walk = new Walk();
            //LocationInitialize();
            //ButtonActivity.Foreground = new SolidColorBrush(Colors.Yellow);

            //TryBluetooth(); 
        }

        //private void LocationInitialize()
        //{
        //    // Reinitialize the GeoCoordinateWatcher
        //    // _geo = new GeoCoordinateWatcher(Accuracy) { MovementThreshold = 10 }; // jha previously 20

        //    _CurrentGeoCoordinate = new GeoCoordinate(34.2572, -118.6003); // roughly chatsworth
        //    _PreviousGeoCoordinate = _CurrentGeoCoordinate;

        //    geolocator = new Geolocator();
        //    geolocator.DesiredAccuracy = PositionAccuracy.High;
        //    // geolocator.MovementThreshold = 50; // The units are meters.
        //    geolocator.MovementThreshold = 10; // The units are meters.

        //    geolocator.StatusChanged -= geolocator_StatusChanged;
        //    geolocator.PositionChanged -= geolocator_PositionChanged;
        //    geolocator.StatusChanged += geolocator_StatusChanged;
        //    geolocator.PositionChanged += geolocator_PositionChanged;
        //}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //Creating a MapLayer and adding the MapOverlay to it
            //PushpinMapLayer = new MapLayer();
            //MyMap.Layers.Add(PushpinMapLayer);
            // Jpin(); 
            base.OnNavigatedTo(e);
        }

        //async private void ButtonActivity_Click(object sender, RoutedEventArgs e)
        //{


            //if (isActive)
            //{
            //    activityTimer.Stop();
            //    ButtonActivity.Foreground = new SolidColorBrush(Colors.Green);
            //    ButtonActivity.Content = "Start";
            //    isActive = false;
            //    _activity.Stop = DateTime.Now;
            //    activityEnd = _CurrentGeoCoordinate;
            //    TimeSpan ts = _activity.Stop - _activity.Start;
            //    var distance = (activityEnd).GetDistanceTo(activityBegin);
            //    _activity.Distance = distance;
            //    var distanceInFeet = distance * 3.281;
            //    var distanceInMiles = distanceInFeet / 5280;

            //    if (Geos.Count > 0)
            //    {
            //        string skyDriveFileName = "Walker";
            //        skyDriveFileName += DateTime.Now.ToString("yyyyMMddhhmmss");
            //        skyDriveFileName += ".gpx";
            //        _logger.Log(this, skyDriveFileName);


            //        byte[] bytes = Encoding.UTF8.GetBytes(ToGpx());
            //        MemoryStream gpxMemoryStream = new MemoryStream(bytes);

            //        try
            //        {
            //            var result = await _client.UploadAsync(MeDetails.TopLevelSkyDriveFolder, skyDriveFileName, gpxMemoryStream,
            //                                    OverwriteOption.Overwrite);
            //            _logger.Log(this, "Upload to skyDrive apparently worked!", result.ToString());
            //            // var success = await Windows.System.Launcher.LaunchFileAsync(ms);
            //            // var success = await Windows.System.Launcher.  LaunchFileAsync(ms);
            //        }
            //        catch (Exception ex)
            //        {
            //            _logger.Log(this, "Upload to skyDrive exception: ", ex.ToString());
            //        }
            //        gpxMemoryStream.Dispose();

            //        LaunchGpxFileAssociation();
            //    }

            //    Vm.StatusMessages = string.Empty;
            //    Vm.StatusMessages += String.Format("Walked: {0:0000.00} meters ({2:00.000} miles) in {1} seconds.\n", distance, ts.ToString(), distanceInMiles);
            //    Vm.StatusMessages += String.Format("(new calc) Walked: {0:0000.00} meters \n", _CurrentDistance);

            //    PersistIt();
            //    _CurrentDistance = 0;
            //    this.MyMap.ResolveCompleted -= MapResolveCompleted;
            //}
            //else
            //{
            //    StartTimer();
            //    this.MyMap.ZoomLevel = 17;
            //    ButtonActivity.Foreground = new SolidColorBrush(Colors.Red);
            //    ButtonActivity.Content = "Stop";
            //    isActive = true;
            //    _activity = new Walk();
            //    _activity.Start = DateTime.Now;
            //    activityBegin = _CurrentGeoCoordinate;
            //    this.MyMap.ResolveCompleted += MapResolveCompleted;
            //}
        //}

        //private async void LaunchGpxFileAssociation()
        //{
        //    // Access local storage.
        //    StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;

        //    // Save the file in local storage
        //    StorageFile file = await local.CreateFileAsync("Walker.gpx", CreationCollisionOption.ReplaceExisting);
        //    if (file != null)
        //    {
        //        string userContent = ToGpx();
        //        if (!String.IsNullOrEmpty(userContent))
        //        {
        //            using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
        //            {
        //                using (DataWriter dataWriter = new DataWriter(transaction.Stream))
        //                {
        //                    dataWriter.WriteString(userContent);
        //                    transaction.Stream.Size = await dataWriter.StoreAsync(); // reset stream size to override the file
        //                    await transaction.CommitAsync();
        //                }
        //            }
        //        }
        //        else
        //        {
        //            MessageBox.Show("The text box is empty, please write something and then click 'save' again.");
        //        }
        //    }

        //    // Access the bug query file.
        //    StorageFile bqfile = await local.GetFileAsync("Walker.gpx");

        //    // Launch the bug query file.
        //    Windows.System.Launcher.LaunchFileAsync(bqfile);
        //}

        ///// <summary>
        ///// Get a GPX representation of the track data.
        ///// </summary>
        ///// <returns>GPX data. (UTF-16 encoded)</returns>
        //public string ToGpx()
        //{
        //    const string gpx = "http://www.topografix.com/GPX/1/1";
        //    StringBuilder sb = new StringBuilder();
        //    using (XmlWriter writer = XmlWriter.Create(sb))
        //    {
        //        writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");

        //        writer.WriteStartElement("gpx", gpx);
        //        writer.WriteAttributeString("version", "1.1");
        //        writer.WriteAttributeString("creator", "Walker");

        //        writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
        //        writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

        //        //Bounds b = CalculateBounds();
        //        //writer.WriteStartElement("bounds", gpx);
        //        //writer.WriteAttributeString("minlat", XmlConvert.ToString(b.MinLat));
        //        //writer.WriteAttributeString("maxlat", XmlConvert.ToString(b.MaxLat));
        //        //writer.WriteAttributeString("minlon", XmlConvert.ToString(b.MinLon));
        //        //writer.WriteAttributeString("maxlon", XmlConvert.ToString(b.MaxLon));

        //        writer.WriteStartElement("trk", gpx);
        //        writer.WriteStartElement("name", gpx);
        //        // writer.WriteValue(Name);
        //        writer.WriteValue("TrackName");
        //        writer.WriteEndElement();

        //        writer.WriteStartElement("trkseg", gpx);
        //        // foreach (TrackPoint p in Points)
        //        foreach (var p in Geos)
        //        {
        //            writer.WriteStartElement("trkpt", gpx);
        //            writer.WriteAttributeString("lat", XmlConvert.ToString(p.Latitude));
        //            writer.WriteAttributeString("lon", XmlConvert.ToString(p.Longitude));
        //            writer.WriteStartElement("ele", gpx);
        //            writer.WriteValue(XmlConvert.ToString(p.Altitude));
        //            writer.WriteEndElement();
        //            // writer.WriteStartElement("time", gpx);
        //            // writer.WriteValue(p. Time.ToString("s") + "Z");
        //            // writer.WriteEndElement();
        //            writer.WriteEndElement();
        //        }

        //        writer.WriteEndElement();
        //    }
        //    return sb.ToString();
        //}

        //static string ReturnSummaryDataXml()
        //{
        //MemoryStream ms = new MemoryStream();

        //using (XmlWriter xmlWriter = XmlWriter.Create(ms))
        //{
        //    // TODO add style information? 
        //    xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
        //    xmlWriter.WriteStartElement("SummaryTemperatureData");
        //    xmlWriter.WriteAttributeString("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
        //    xmlWriter.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
        //    // xmlWriter.WriteElementString("DataLoggerDeviceName", std.DataLoggerDeviceName);
        //    // TODO would this fix the ToString("F2") ?? <xs:element name="startdate" type="xs:dateTime"/>

        //    //        //             new XAttribute("lat", cord.Latitude.ToString()),
        //    //        //             new XAttribute("lon", cord.Longitude.ToString()),

        //    foreach (var item in Geos)
        //    {

        //        xmlWriter.WriteElementString("lat", cord.Latitude.ToString());
        //    }


        //    //xmlWriter.WriteElementString("CurrentMeasuredTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
        //    //xmlWriter.WriteElementString("CurrentTemperature0", temperature0.ToString("F2"));
        //    //xmlWriter.WriteElementString("CurrentTemperature1", std.CurrentTemperature1.ToString("F2"));


        //    xmlWriter.WriteEndElement();
        //    xmlWriter.Flush();
        //    xmlWriter.Close();
        //}

        //byte[] byteArray = ms.ToArray();
        //char[] cc = UTF8Encoding.UTF8.GetChars(byteArray);
        //string str = new string(cc);
        // return str;
        //}

        //void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        //{
        //    string status = "";

        //    switch (args.Status)
        //    {
        //        case PositionStatus.Disabled:
        //            // the application does not have the right capability or the location master switch is off
        //            status = "location is disabled in phone settings";
        //            break;
        //        case PositionStatus.Initializing:
        //            // the geolocator started the tracking operation
        //            status = "initializing";
        //            break;
        //        case PositionStatus.NoData:
        //            // the location service was not able to acquire the location
        //            status = "no data";
        //            break;
        //        case PositionStatus.Ready:
        //            // the location service is generating geopositions as specified by the tracking parameters
        //            status = "ready";
        //            break;
        //        case PositionStatus.NotAvailable:
        //            status = "not available";
        //            // not used in WindowsPhone, Windows desktop uses this value to signal that there is no hardware capable to acquire location information
        //            break;
        //        case PositionStatus.NotInitialized:
        //            // the initial state of the geolocator, once the tracking operation is stopped by the user the geolocator moves back to this state

        //            break;
        //    }

        //    DispatcherHelper.CheckBeginInvokeOnUI(() => Vm.StatusMessages = status);
        //}

        //void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        //{
        //    // Unfortunately, the Location API works with Windows.Devices.Geolocation.Geocoordinate objeccts
        //    // and the Maps controls use System.Device.Location.GeoCoordinate objects so we have to do a
        //    // conversion before we do anything with it on the map
        //    GeoCoordinate geoCoordinate = new GeoCoordinate()
        //    {
        //        Altitude = args.Position.Coordinate.Altitude.HasValue ? args.Position.Coordinate.Altitude.Value : 0.0,
        //        Course = args.Position.Coordinate.Heading.HasValue ? args.Position.Coordinate.Heading.Value : 0.0,
        //        HorizontalAccuracy = args.Position.Coordinate.Accuracy,
        //        Latitude = args.Position.Coordinate.Latitude,
        //        Longitude = args.Position.Coordinate.Longitude,
        //        Speed = args.Position.Coordinate.Speed.HasValue ? args.Position.Coordinate.Speed.Value : 0.0,
        //        VerticalAccuracy = args.Position.Coordinate.AltitudeAccuracy.HasValue ? args.Position.Coordinate.AltitudeAccuracy.Value : 0.0
        //    };


        //    Geos.Add(geoCoordinate); // save these pups to create a .gpx file...


        //    _CurrentGeoCoordinate = geoCoordinate;

        //    DispatcherHelper.CheckBeginInvokeOnUI(() =>
        //        {
        //            // TestVm.Hi = testInt++.ToString();
        //            TestVm.GeoCoordinate = geoCoordinate;
        //        });

        //    Dispatcher.BeginInvoke(() =>
        //    {
        //        // Center the map on the new location
        //        this.MyMap.Center = geoCoordinate;

        //        //TestVm.Hi = testInt++.ToString();
        //        //TestVm.GeoCoordinate = positionCoord;

        //        //TestVm.GeoCoordinate = new GeoCoordinate()
        //        //{
        //        //    Altitude = args.Position.Coordinate.Altitude.HasValue ? args.Position.Coordinate.Altitude.Value : 0.0,
        //        //    Course = args.Position.Coordinate.Heading.HasValue ? args.Position.Coordinate.Heading.Value : 0.0,
        //        //    HorizontalAccuracy = args.Position.Coordinate.Accuracy,
        //        //    Latitude = args.Position.Coordinate.Latitude,
        //        //    Longitude = args.Position.Coordinate.Longitude,
        //        //    Speed = args.Position.Coordinate.Speed.HasValue ? args.Position.Coordinate.Speed.Value : 0.0,
        //        //    VerticalAccuracy = args.Position.Coordinate.AltitudeAccuracy.HasValue ? args.Position.Coordinate.AltitudeAccuracy.Value : 0.0
        //        //};

        //        //var foo = new GeoCoordinate(34.2572, -118.6003);
        //        //MvvmMap.Center = foo;

        //        // Draw a pushpin
        //        DrawPushpin(geoCoordinate);
        //        AddBluePoint(geoCoordinate);
        //        // Show progress indicator while map resolves to new position
        //        pi.IsVisible = true;
        //        pi.IsIndeterminate = true;
        //        pi.Text = "Resolving location...";
        //        SystemTray.SetProgressIndicator(this, pi);
        //    });
        //}

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

        //private void Jpin()
        //{
        //    UserLocationMarker marker = (UserLocationMarker)this.FindName("UserLocationMarker");
        //    marker.GeoCoordinate = MvvmMap.Center;

        //    Pushpin pushpin = (Pushpin)this.FindName("MyPushpin");
        //    pushpin.GeoCoordinate = new GeoCoordinate(30.712474, -132.32691);

        //}

        //private void PersistIt()
        //{
        //    Vm.Activities.Add(_activity);
        //}

        //private void StartTimer()
        //{
        //    activityTimer = new DispatcherTimer();
        //    activityTimer.Interval = TimeSpan.FromSeconds(1);
        //    activityTimer.Tick += OnTimerTick;
        //    activityTimer.Start();
        //    lastTime = DateTime.Now;
        //    startTime = DateTime.Now;
        //}

        //void OnTimerTick(Object sender, EventArgs args)
        //{
        //    _ElapsedTime = DateTime.Now - _activity.Start;
        //    Vm.ElapsedTime = _ElapsedTime.ToString(@"hh\:mm\:ss");
        //    Vm.DistanceMeters = _CurrentDistance.ToString("0\\,000");
        //    Vm.DistanceMiles = ((_CurrentDistance * 3.281) / 5280).ToString("0\\,000");
        //}

        //private void MapResolveCompleted(object sender, MapResolveCompletedEventArgs e)
        //{
        //    // Hide progress indicator
        //    pi.IsVisible = false;
        //    pi.IsIndeterminate = false;
        //    SystemTray.SetProgressIndicator(this, null);
        //}

        //private void DrawPushpin(GeoCoordinate coord)
        //{
        //    var aPushpin = CreatePushpinObject();

        //    //Creating a MapOverlay and adding the Pushpin to it.
        //    MapOverlay MyOverlay = new MapOverlay();
        //    MyOverlay.Content = aPushpin;
        //    MyOverlay.GeoCoordinate = coord;
        //    MyOverlay.PositionOrigin = new Point(0, 0.5);

        //    // Add the MapOverlay containing the pushpin to the MapLayer
        //    this.PushpinMapLayer.Add(MyOverlay);
        //}

        //private Grid CreatePushpinObject()
        //{
        //    //Creating a Grid element.
        //    Grid MyGrid = new Grid();
        //    MyGrid.RowDefinitions.Add(new RowDefinition());
        //    MyGrid.RowDefinitions.Add(new RowDefinition());
        //    MyGrid.Background = new SolidColorBrush(Colors.Transparent);

        //    //Creating a Rectangle
        //    Rectangle MyRectangle = new Rectangle();
        //    MyRectangle.Fill = new SolidColorBrush(Colors.Black);
        //    MyRectangle.Height = 20;
        //    MyRectangle.Width = 20;
        //    MyRectangle.SetValue(Grid.RowProperty, 0);
        //    MyRectangle.SetValue(Grid.ColumnProperty, 0);

        //    //Adding the Rectangle to the Grid
        //    MyGrid.Children.Add(MyRectangle);

        //    //Creating a Polygon
        //    Polygon MyPolygon = new Polygon();
        //    MyPolygon.Points.Add(new Point(2, 0));
        //    MyPolygon.Points.Add(new Point(22, 0));
        //    MyPolygon.Points.Add(new Point(2, 40));
        //    MyPolygon.Stroke = new SolidColorBrush(Colors.Black);
        //    MyPolygon.Fill = new SolidColorBrush(Colors.Black);
        //    MyPolygon.SetValue(Grid.RowProperty, 1);
        //    MyPolygon.SetValue(Grid.ColumnProperty, 0);

        //    //Adding the Polygon to the Grid
        //    MyGrid.Children.Add(MyPolygon);
        //    return MyGrid;
        //}

        private async void SignInButton_OnSessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            if (e.Error == null && e.Status == LiveConnectSessionStatus.Connected)
            {
                // _client = new LiveConnectClient(e.Session);
                App.LiveConnectClient = new LiveConnectClient(e.Session);

                try
                {
                    // var meResult = await _client.GetAsync(LiveSdkConstants.MyDetails);
                    var meResult = await App.LiveConnectClient.GetAsync(LiveSdkConstants.MyDetails);
                    var myDetails = JsonConvert.DeserializeObject<MeDetails>(meResult.RawResult);

                    SignedInAs.Text = string.Format("Signed in as {0}", myDetails.Name);
                }
                catch
                {
                    MessageBox.Show("Something went wrong with Live login");
                }
            }
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (MvvmMap.ZoomLevel < MAX_ZOOM_LEVEL)
            {
                MvvmMap.ZoomLevel++;
            }
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (MvvmMap.ZoomLevel > MIN_ZOOM_LEVEL)
            {
                MvvmMap.ZoomLevel--;
            }
        }

        private void Foo0()
        {


            //if (Geos.Count > 0)
            //{
            //    _logger.Log(this, "Geos.Count > 0");
            //    // _walkerDataService.CreateGpxFile(Geos); 


            //    XNamespace ns = "http://www.topografix.com/GPX/1/1";
            //    XNamespace xsiNs = "http://www.w3.org/2001/XMLSchema-instance";
            //    XDocument xDoc = new XDocument(
            //        new XDeclaration("1.0", "UTF-8", "no"),
            //        new XElement(ns + "gpx",
            //                     new XAttribute(XNamespace.Xmlns + "xsi", xsiNs),
            //                     new XAttribute(xsiNs + "schemaLocation",
            //                                    "http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd"),
            //                     new XAttribute("creator", "Walker"),
            //                     new XAttribute("version", "1.1")));
            //    new XElement(Geos.Select(datatype -> new XElement(G)
            //    Geos.Select

            //    //foreach (var cord in Geos)
            //    //{
            //        //_logger.Log(this,"");
            //        //xDoc.Add(
            //        //new XElement(ns + "wpt",
            //        //             new XAttribute("lat", cord.Latitude.ToString()),
            //        //             new XAttribute("lon", cord.Longitude.ToString()),
            //        //             new XElement(ns + "name", "test"),
            //        //             new XElement(ns + "sym", "Car")));
            //        //))
            //        //;
            //    // }

            //    var foo = 1; 
            //}
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            Vm.SettingsViewCommand.Execute(null); 
        }

        //void Foo1()
        //{

        //    ////byte[] response = Encoding.UTF8.GetBytes(ReturnSummaryDataXml());
        //    //string header = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-cache\r\nConnection: close\r\n\r\n";
        //    ////connection.Send(Encoding.UTF8.GetBytes(header));
        //    ////connection.Send(response);


        //    //MemoryStream ms = new MemoryStream();

        //    //using (XmlWriter xmlWriter = XmlWriter.Create(ms))
        //    //{
        //    //    XNamespace ns = "http://www.topografix.com/GPX/1/1";

        //    //    // TODO add style information? 
        //    //    xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
        //    //    xmlWriter.WriteStartElement("gpx", "http://www.topografix.com/GPX/1/1");
        //    //    xmlWriter.WriteAttributeString("version", "1.1");
        //    //    xmlWriter.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
        //    //    xmlWriter.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");


        //    //    // xmlWriter.WriteAttributeString("xmlns", "xsd", "http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd");

        //    //    // xmlWriter.WriteElementString("DataLoggerDeviceName", std.DataLoggerDeviceName);
        //    //    // TODO would this fix the ToString("F2") ?? <xs:element name="startdate" type="xs:dateTime"/>

        //    //    foreach (var cord in Geos)
        //    //    {
        //    //        xmlWriter.WriteElementString("lat", cord.Latitude.ToString());
        //    //        xmlWriter.WriteElementString("lon", cord.Longitude.ToString());
        //    //    }

        //    //    xmlWriter.WriteEndElement();
        //    //    xmlWriter.Flush();
        //    //    xmlWriter.Close();
        //    //}

        //    //byte[] byteArray = ms.ToArray();
        //    //char[] cc = UTF8Encoding.UTF8.GetChars(byteArray);
        //    //string str = new string(cc);
        //    //_logger.Log(this, "GPX ???? ", str);
        //}

        //private void activityButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (activityButton.IsChecked == true)
        //    {
        //        _logger.Log(this, "activityButton_Click",activityButton.IsChecked.ToString());

        //    }
        //    else
        //    {
        //        _logger.Log(this, "activityButton_Click", activityButton.IsChecked.ToString());
        //    }
        //}


    }
}