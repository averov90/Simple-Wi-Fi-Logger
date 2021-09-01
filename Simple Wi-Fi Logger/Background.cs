using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.LocalBroadcastManager.Content;
using Android.Net.Wifi;
using Android.Net.Wifi.Rtt;
using Android.Locations;
using Android.Content.PM;

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Simple_Wi_Fi_Logger {
    [Service]
    public class LoggingService : Service {
        public const string FORSERVICE_CHANNEL_ID = "averov90.Simple_WiFi_Logger";
        public const int FORSERVICE_NOTIFICATION_ID = 2000;
        public override IBinder OnBind(Intent intent) {
            return null;
        }

        AutoResetEvent write_event;

        LocalBroadcastManager localBroadcastManager;
        SettingsReceiver receiver;
        Thread service_thread;

        NotificationManager notificationManager = null;

        int sleep_delay;
        AutoResetEvent thread_working_sleeper_mloop;
        ManualResetEvent thread_working_sleeper_tiny;
        bool thread_working, split_file, use_separators;
        WifiManager wifiManager;

        Dictionary<string, wifiInfo> current_wifi_list = null;

        WifiRttManager rttManager = null;
        RttCallback rttCallback;

        LocationManager locationManager;
        MyLocationListener locationListener;

        CurrentLocation? estimated_location_gps;
        CurrentLocation? estimated_location_network;

        LogFile logfile;

        Handler activity_context;
        Mutex write_sync;

        string unavailible_rtt_error_message = "null";

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) {
            RegisterService();
            activity_context = new Handler(Looper.MainLooper);

            localBroadcastManager = LocalBroadcastManager.GetInstance(this);
            receiver = new SettingsReceiver(this);
            localBroadcastManager.RegisterReceiver(receiver, new IntentFilter("SERVICE_AMDMTS_ANY"));

            sleep_delay = intent.GetIntExtra("NEW_SLEEP_DELAY", 0);
            use_separators = intent.GetBooleanExtra("ENABLE_SEPARATORS", false);
            split_file = false;

            {
                wifiManager = (WifiManager)Application.Context.GetSystemService(WifiService);
                if (wifiManager.IsWifiEnabled) {
                    wifiManager.SetWifiEnabled(true);
                }

                var wifiReceiver = new WifiReceiver(this, wifiManager);
                Application.Context.RegisterReceiver(wifiReceiver, new IntentFilter(WifiManager.ScanResultsAvailableAction));
            }

            {
#pragma warning disable CS0618 // Type or member is obsolete
                string storage_path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath;
#pragma warning restore CS0618 // Type or member is obsolete
                if (storage_path.Contains("/Android")) {
                    storage_path = storage_path.Split("/Android")[0];
                }
                storage_path += "/Simple Wi-Fi Logger";
                if (!Directory.Exists(storage_path)) {
                    Directory.CreateDirectory(storage_path);
                }
                logfile = new LogFile(storage_path, this);
            }

            {
                locationListener = new MyLocationListener(this);
                locationManager = (LocationManager)GetSystemService(LocationService);
                locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 20, 0.1F, locationListener);
                locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 20, 0.1F, locationListener);
            }

            {
                if (intent.GetBooleanExtra("ENABLE_RTT", true)) {
                    unavailible_rtt_error_message = "unsupported";
                    if (PackageManager.HasSystemFeature(PackageManager.FeatureWifiRtt)) {

                        rttManager = (WifiRttManager)Application.Context.GetSystemService(WifiRttRangingService);
                        if (rttManager.IsAvailable) {
                            rttCallback = new RttCallback(this);
                        } else
                            rttManager = null;
                    }
                }
            }

            write_event = new AutoResetEvent(false);
            write_sync = new Mutex();

            thread_working_sleeper_mloop = new AutoResetEvent(false);
            thread_working_sleeper_tiny = new ManualResetEvent(false);
            thread_working = true;
            service_thread = new Thread(thread_proc);
            service_thread.Start();

            Toast.MakeText(Application.Context, "Logger started!", ToastLength.Long).Show();

            return StartCommandResult.Sticky;
        }

        private void RegisterService() {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var channel = new NotificationChannel(FORSERVICE_CHANNEL_ID, "Simple Wi-Fi Logger", NotificationImportance.Min) {
                Description = "Simple Wi-Fi Logger notifications"
            };

            notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);


            var notificationIntent = new Intent(Application.Context, typeof(MainActivity));
            var stackBuilder = TaskStackBuilder.Create(Application.Context);
            stackBuilder.AddNextIntentWithParentStack(notificationIntent);
            PendingIntent resultPendingIntent = stackBuilder.GetPendingIntent(0, PendingIntentFlags.UpdateCurrent);

            var notification = new Notification.Builder(Application.Context, FORSERVICE_CHANNEL_ID)
                .SetContentTitle("Simple Wi-Fi Logger")
                .SetContentText("Simple Wi-Fi logger service is RUNNING!")
                .SetSmallIcon(Resource.Drawable.appBar_icon)
                .SetContentIntent(resultPendingIntent)
                .SetOngoing(true)
                .Build();

            // Enlist this instance of the service as a foreground service, MUST CALL IN < 5 SECONDS ON RUNTIME
            StartForeground(FORSERVICE_NOTIFICATION_ID, notification);
        }

        public override void OnDestroy() {
            thread_working = false;
            thread_working_sleeper_tiny.Set();
            thread_working_sleeper_mloop.Set();

            locationManager.RemoveUpdates(locationListener);
            localBroadcastManager.UnregisterReceiver(receiver);

            if (notificationManager != null) notificationManager.DeleteNotificationChannel(FORSERVICE_CHANNEL_ID);
            base.OnDestroy();
        }

        void thread_proc() {
            while (thread_working) {
#pragma warning disable CS0618 // Type or member is obsolete
                while (!wifiManager.StartScan() && thread_working) {
                    Thread.Sleep(TimeSpan.Zero);
                    thread_working_sleeper_tiny.WaitOne(20);
                }

#pragma warning restore CS0618
                write_event.WaitOne();

                if (split_file) {
                    split_file = false;
                    logfile.NewFile();


                    activity_context.Post(() => {
                        Toast.MakeText(Application.Context, "Log splitted", ToastLength.Short).Show();
                    });
                }
                logfile.WriteData();

                //write_event.Reset(); - net need, event is autoreset
                thread_working_sleeper_mloop.WaitOne(sleep_delay);
            }
        }

        class LogFile : IDisposable {
            LoggingService context;

            ulong counter;
            string storage_path;
            StreamWriter file;

            public LogFile(string folder_path, LoggingService context) {
                this.context = context;

                string[] files = Directory.GetFiles(folder_path, "*.csv", SearchOption.TopDirectoryOnly);
                if (files.Length != 0) {
                    counter = files.Select((string src) => {
                        string[] parts = src.Split('_');
                        if (parts.Length > 1 && ulong.TryParse(parts[1], out ulong res)) {
                            return res;
                        }
                        return 0UL;
                    }).Max() + 1;
                }
                storage_path = folder_path + "/";
                
                file = new StreamWriter(storage_path + "log_" + counter + "_.csv", false);
                file.WriteLine("Date; " +
                     "BSSID; " +
                     "Level (dbm); " +
                     "Level-based distance (m); " +
                     "RTT distance (mm); " +
                     "Standart; " +
                     "Frequrency; " +
                     "SSID; " +
                     "Security; " +
                     "Passpoint venue name; " +
                     "Passpoint operators name; " +
                     "GPS latitude (deg); GPS longtitude (deg); GPS radial accuracy (m); " +
                     "GPS altitude (m, WGS84); GPS vertical accuracy (m); " +
                     "GPS speed (m/s); GPS speed accuracy (m/s); " +
                     "Network latitude (deg); Network longtitude (deg); Network radial accuracy (m); " +
                     "Network altitude (m, WGS84); Network vertical accuracy (m); " +
                     "Network speed (m/s); Network speed accuracy (m/s)");
                file.Flush();
            }

            public void NewFile() {
                file.Close();

                file = new StreamWriter(storage_path + "log_" + ++counter + "_.csv", false);
                file.WriteLine("Date; " +
                     "BSSID; " +
                     "Level (dbm); " +
                     "Level-based distance (m); " +
                     "RTT distance (mm); " +
                     "Standart; " +
                     "Frequrency; " +
                     "SSID; " +
                     "Security; " +
                     "Passpoint venue name; " +
                     "Passpoint operators name; " +
                     "GPS latitude (deg); GPS longtitude (deg); GPS radial accuracy (m); " +
                     "GPS altitude (m, WGS84); GPS vertical accuracy (m); " +
                     "GPS speed (m/s); GPS speed accuracy (m/s); " +
                     "Network latitude (deg); Network longtitude (deg); Network radial accuracy (m); " +
                     "Network altitude (m, WGS84); Network vertical accuracy (m); " +
                     "Network speed (m/s); Network speed accuracy (m/s)");
                file.Flush();
            }

            public void WriteData() {
                string datetime = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.f");

                if (context.use_separators) {
                    file.WriteLine(" - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ;" +
                        " - ; - ; - ;" +
                        " - ; - ;" +
                        " - ; - ;" +
                        " - ; - ; - ;" +
                        " - ; - ;" +
                        " - ; - ");
                }

                context.write_sync.WaitOne();
                if (context.estimated_location_gps.HasValue && context.estimated_location_network.HasValue) {
                    foreach (var item in context.current_wifi_list) {
                        file.WriteLine($"{datetime}; " +
                        $"{item.Key}; " +
                        $"{item.Value.level}; " +
                        $"{item.Value.LEVELdistance}; " +
                        $"{(item.Value.RTTdistance != null ? item.Value.RTTdistance.Value.ToString() : context.unavailible_rtt_error_message)}; " +
                        $"{item.Value.standard}; " +
                        $"{item.Value.main_frequrency}; " +
                        $"{item.Value.ssid}; " +
                        $"{item.Value.auth_info}; " +
                        $"{item.Value.passpointVenueName}; " +
                        $"{item.Value.passpointOperatorsName}; " +
                        $"{context.estimated_location_gps.Value.latitude}; {context.estimated_location_gps.Value.longtitude}; {context.estimated_location_gps.Value.horizontal_accuracy}; " +
                        $"{context.estimated_location_gps.Value.altitude}; {context.estimated_location_gps.Value.vertical_accuracy}; " +
                        $"{context.estimated_location_gps.Value.speed}; {context.estimated_location_gps.Value.speed_accuracy}; " +
                        $"{context.estimated_location_network.Value.latitude}; {context.estimated_location_network.Value.longtitude}; {context.estimated_location_network.Value.horizontal_accuracy}; " +
                        $"{context.estimated_location_network.Value.altitude}; {context.estimated_location_network.Value.vertical_accuracy}; " +
                        $"{context.estimated_location_network.Value.speed}; {context.estimated_location_network.Value.speed_accuracy}");
                    }
                } else if (context.estimated_location_network.HasValue) {
                    foreach (var item in context.current_wifi_list) {
                        file.WriteLine($"{datetime}; " +
                        $"{item.Key}; " +
                        $"{item.Value.level}; " +
                        $"{item.Value.LEVELdistance}; " +
                        $"{(item.Value.RTTdistance != null ? item.Value.RTTdistance.Value.ToString() : context.unavailible_rtt_error_message)}; " +
                        $"{item.Value.standard}; " +
                        $"{item.Value.main_frequrency}; " +
                        $"{item.Value.ssid}; " +
                        $"{item.Value.auth_info}; " +
                        $"{item.Value.passpointVenueName}; " +
                        $"{item.Value.passpointOperatorsName}; " +
                        "null; null; null; " +
                        "null; null; " +
                        "null; null; " +
                        $"{context.estimated_location_network.Value.latitude}; {context.estimated_location_network.Value.longtitude}; {context.estimated_location_network.Value.horizontal_accuracy}; " +
                        $"{context.estimated_location_network.Value.altitude}; {context.estimated_location_network.Value.vertical_accuracy}; " +
                        $"{context.estimated_location_network.Value.speed}; {context.estimated_location_network.Value.speed_accuracy}");
                    }
                } else if (context.estimated_location_gps.HasValue) {
                    foreach (var item in context.current_wifi_list) {
                        file.WriteLine($"{datetime}; " +
                        $"{item.Key}; " +
                        $"{item.Value.level}; " +
                        $"{item.Value.LEVELdistance}; " +
                        $"{(item.Value.RTTdistance != null ? item.Value.RTTdistance.Value.ToString() : context.unavailible_rtt_error_message)}; " +
                        $"{item.Value.standard}; " +
                        $"{item.Value.main_frequrency}; " +
                        $"{item.Value.ssid}; " +
                        $"{item.Value.auth_info}; " +
                        $"{item.Value.passpointVenueName}; " +
                        $"{item.Value.passpointOperatorsName}; " +
                        $"{context.estimated_location_gps.Value.latitude}; {context.estimated_location_gps.Value.longtitude}; {context.estimated_location_gps.Value.horizontal_accuracy}; " +
                        $"{context.estimated_location_gps.Value.altitude}; {context.estimated_location_gps.Value.vertical_accuracy}; " +
                        $"{context.estimated_location_gps.Value.speed}; {context.estimated_location_gps.Value.speed_accuracy}; " +
                        "null; null; null; " +
                        "null; null; " +
                        "null; null");
                    }
                } else {
                    foreach (var item in context.current_wifi_list) {
                        file.WriteLine($"{datetime}; " +
                        $"{item.Key}; " +
                        $"{item.Value.level}; " +
                        $"{item.Value.LEVELdistance}; " +
                        $"{(item.Value.RTTdistance != null ? item.Value.RTTdistance.Value.ToString() : context.unavailible_rtt_error_message)}; " +
                        $"{item.Value.standard}; " +
                        $"{item.Value.main_frequrency}; " +
                        $"{item.Value.ssid}; " +
                        $"{item.Value.auth_info}; " +
                        $"{item.Value.passpointVenueName}; " +
                        $"{item.Value.passpointOperatorsName}; " +
                        "null; null; null; " +
                        "null; null; " +
                        "null; null; " +
                        "null; null; null; " +
                        "null; null; " +
                        "null; null");
                    }
                }
                context.write_sync.ReleaseMutex();
                file.Flush();
            }

            public void Dispose() {
                file.Close();
            }
        }

        class SettingsReceiver : BroadcastReceiver { //MicroService ;)
            LoggingService context;
            public SettingsReceiver(LoggingService context) {
                this.context = context;
            }

            public override void OnReceive(Context ctx, Intent intent) {
                int tmp;
                if ((tmp = intent.GetIntExtra("NEW_SLEEP_DELAY", -1)) != -1) {
                    if (context.sleep_delay > 5000 && tmp > 5000) {
                        context.sleep_delay = tmp;
                        context.thread_working_sleeper_mloop.Set();
                    } else
                        context.sleep_delay = tmp;
                } else if (intent.GetBooleanExtra("LOGFILE_SPLIT", false)) {
                    context.split_file = true;
                } else if (intent.HasExtra("ENABLE_SEPARATORS")) {
                    context.use_separators = intent.GetBooleanExtra("ENABLE_SEPARATORS", false);
                }
            }
        }

        class wifiInfo {
            private const double DISTANCE_MHZ_M = 27.55;

            public string ssid, auth_info;
            public WifiStandard standard;
            public bool passpoint;
            public int main_frequrency, level;
            public int? RTTdistance;
            public float LEVELdistance;

            public string passpointVenueName, passpointOperatorsName;

            public wifiInfo(string ssid, string auth_info, int level, WifiStandard standard, int main_frequrency, bool passpoint, string passpointVenueName, string passpointOperatorsName) {
                this.ssid = ssid;
                this.auth_info = auth_info;
                this.level = level;
                this.standard = standard;
                this.main_frequrency = main_frequrency;
                this.passpoint = passpoint;
                this.passpointVenueName = passpointVenueName;
                this.passpointOperatorsName = passpointOperatorsName;
                
                RTTdistance = null;

                //10.0.pow((DISTANCE_MHZ_M - 20 * log10(frequency.toDouble()) + abs(level)) / 20.0)
                //From: https://github.com/VREMSoftwareDevelopment/WiFiAnalyzer/blob/master/app/src/main/kotlin/com/vrem/wifianalyzer/wifi/model/WiFiUtils.kt
                LEVELdistance = (float)Math.Pow(10, (DISTANCE_MHZ_M - 20 * Math.Log10(main_frequrency) - level) / 20.0);
            }
        }

        struct CurrentLocation {
            public double latitude, longtitude, altitude;
            public float horizontal_accuracy, vertical_accuracy;
            public float speed, speed_accuracy;
            public CurrentLocation(double latitude, double longtitude, double altitude, float horizontal_accuracy, float vertical_accuracy, float speed, float speed_accuracy) {
                this.latitude = latitude;
                this.longtitude = longtitude;
                this.altitude = altitude;
                this.horizontal_accuracy = horizontal_accuracy;
                this.vertical_accuracy = vertical_accuracy;
                this.speed = speed;
                this.speed_accuracy = speed_accuracy;
            }
        }

        class MyLocationListener : Java.Lang.Object, ILocationListener {
            LoggingService context;

            public MyLocationListener(LoggingService context) {
                this.context = context;
            }


            public void OnLocationChanged(Location loc) {
                context.write_sync.WaitOne();
                if (loc.Provider == LocationManager.GpsProvider) {
                    context.estimated_location_gps = new CurrentLocation(loc.Latitude, loc.Longitude, (loc.HasAltitude ? loc.Altitude : -1), (loc.HasAccuracy ? loc.Accuracy : -1), (loc.HasVerticalAccuracy ? loc.VerticalAccuracyMeters : -1), (loc.HasSpeed ? loc.Speed : -1), (loc.HasSpeedAccuracy ? loc.SpeedAccuracyMetersPerSecond : -1));
                } else /*if (loc.Provider == LocationManager.NetworkProvider)*/ {
                    context.estimated_location_network = new CurrentLocation(loc.Latitude, loc.Longitude, (loc.HasAltitude ? loc.Altitude : -1), (loc.HasAccuracy ? loc.Accuracy : -1), (loc.HasVerticalAccuracy ? loc.VerticalAccuracyMeters : -1), (loc.HasSpeed ? loc.Speed : -1), (loc.HasSpeedAccuracy ? loc.SpeedAccuracyMetersPerSecond : -1));
                }
                context.write_sync.ReleaseMutex();
            }

            public void OnProviderDisabled(string provider) { }

            public void OnProviderEnabled(string provider) { }

            public void OnStatusChanged(string provider, Availability status, Bundle extras) {
                if (status != Availability.Available) {
                    context.write_sync.WaitOne();
                    if (provider == LocationManager.GpsProvider) {
                        context.estimated_location_gps = null;
                    } else /*if (provider == LocationManager.NetworkProvider)*/ {
                        context.estimated_location_network = null;
                    }
                    context.write_sync.ReleaseMutex();
                }
            }
        }

        class RttCallback : RangingResultCallback {
            LoggingService context;
            public RttCallback(LoggingService context) {
                this.context = context;
            }

            public override void OnRangingFailure(RangingResultStatusCode code) {
                context.write_event.Set();
            }

            public override void OnRangingResults(IList<RangingResult> list) {
                foreach (var item in list) {
                    if (item.Status == RangingStatus.Success)
                        context.current_wifi_list[item.MacAddress.ToString().ToUpper()].RTTdistance = (item.DistanceMm > 0 ? item.DistanceMm : 0);
                }
                context.write_event.Set();
            }
        }

        class WifiReceiver : BroadcastReceiver {
            LoggingService context;
            WifiManager wifiManager;

            public WifiReceiver(LoggingService context, WifiManager wifiManager) {
                this.context = context;
                this.wifiManager = wifiManager;
            }

            public override void OnReceive(Context ctx, Intent intent) {
                context.current_wifi_list = new Dictionary<string, wifiInfo>(wifiManager.ScanResults.Select(p => new KeyValuePair<string, wifiInfo>(p.Bssid.ToUpper(), new wifiInfo(p.Ssid, p.Capabilities, p.Level, (Build.VERSION.SdkInt >= BuildVersionCodes.R ? (WifiStandard)p.WifiStandard : WifiStandard.Unknown), p.Frequency, p.IsPasspointNetwork, p.VenueName.ToString(), p.OperatorFriendlyName.ToString()))));

                if (context.rttManager != null) {
                   var request = new RangingRequest.Builder()
                        .AddAccessPoints(wifiManager.ScanResults)
                       .Build();

                    context.rttManager.StartRanging(request, context.Application.MainExecutor, context.rttCallback);
                } else {
                    context.write_event.Set();
                }
            }
        }
    }
}