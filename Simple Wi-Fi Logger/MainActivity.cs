using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Android.Widget;
using Android.Content;
using AndroidX.LocalBroadcastManager.Content;
using Android.Content.PM;
using Android.Text.Method;

namespace Simple_Wi_Fi_Logger {
    public static class GlobalScope {
        public static bool service_enabled = false;
        public static Intent service_logging;
        public static int sleep_delay = 0;

        public static MainActivity current_activity = null;
        public static ulong current_index_;
        public static ulong current_index { 
            get { return current_index_; } 
            set {
                current_index_ = value;  
                if (current_activity != null) {
                    current_activity.OnCurrentIndexUpdate();
                }
            } 
        }
    }


    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        EditText textbox_delay;
        Button log_button;
        CheckBox enable_separators, enable_rtt;
        TextView current_index;
        
        LocalBroadcastManager localBroadcastManager;
        

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            localBroadcastManager = LocalBroadcastManager.GetInstance(this);

            log_button = (Button)FindViewById(Resource.Id.button1);
            log_button.Click += Log_button_Click;

            Button reset_button = (Button)FindViewById(Resource.Id.button2);
            reset_button.Click += Reset_button_Click;

            Button split_button = (Button)FindViewById(Resource.Id.button3);
            split_button.Click += Split_button_Click;

            textbox_delay = (EditText)FindViewById(Resource.Id.editText1);
            textbox_delay.TextChanged += Textbox_delay_TextChanged;

            enable_separators = (CheckBox)FindViewById(Resource.Id.checkBox1);
            enable_separators.CheckedChange += Enable_separators_CheckedChange;

            enable_rtt = (CheckBox)FindViewById(Resource.Id.checkBox2);

            current_index = (TextView)FindViewById(Resource.Id.textView7);

            TextView developer_link = (TextView)FindViewById(Resource.Id.textView2);
            developer_link.Click += Developers_link_Click;
            developer_link.SetHorizontallyScrolling(true);
            developer_link.MovementMethod = new ScrollingMovementMethod();

            TextView copy_folder = (TextView)FindViewById(Resource.Id.textView5);
            copy_folder.Text = GetLogFolder();
            copy_folder.Click += Copy_folder_Click;
            copy_folder.SetHorizontallyScrolling(true);
            copy_folder.MovementMethod = new ScrollingMovementMethod();

            copy_folder = (TextView)FindViewById(Resource.Id.textView4);
            copy_folder.Click += Copy_folder_Click;

            CheckAppPermissions();

            if (GlobalScope.service_enabled == true) {
                textbox_delay.Text = GlobalScope.sleep_delay.ToString();
                log_button.Text = GetText(Resource.String.main_button_stop);

                current_index.Text = GlobalScope.current_index.ToString();
            }
            GlobalScope.current_activity = this;
        }

        protected override void OnDestroy() {
            GlobalScope.current_activity = null;
            base.OnDestroy();
        }

        private void CheckAppPermissions() {
            if ((int)Build.VERSION.SdkInt < 23) {
                perms_ok = true;
                return;
            } else {
                var permissions = new List<string>();
                if (PackageManager.CheckPermission(Android.Manifest.Permission.AccessNetworkState, PackageName) != Permission.Granted) 
                    permissions.Add(Android.Manifest.Permission.AccessNetworkState);
                
                if (PackageManager.CheckPermission(Android.Manifest.Permission.AccessWifiState, PackageName) != Permission.Granted) 
                    permissions.Add(Android.Manifest.Permission.AccessWifiState);

                if (PackageManager.CheckPermission(Android.Manifest.Permission.ChangeWifiState, PackageName) != Permission.Granted)
                    permissions.Add(Android.Manifest.Permission.ChangeWifiState);

                if (PackageManager.CheckPermission(Android.Manifest.Permission.AccessCoarseLocation, PackageName) != Permission.Granted) 
                    permissions.Add(Android.Manifest.Permission.AccessCoarseLocation);
                
                if (PackageManager.CheckPermission(Android.Manifest.Permission.AccessFineLocation, PackageName) != Permission.Granted) 
                    permissions.Add(Android.Manifest.Permission.AccessFineLocation);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q && PackageManager.CheckPermission(Android.Manifest.Permission.AccessBackgroundLocation, PackageName) != Permission.Granted)
                    permissions.Add(Android.Manifest.Permission.AccessBackgroundLocation);

                if (PackageManager.CheckPermission(Android.Manifest.Permission.ReadExternalStorage, PackageName) != Permission.Granted)
                    permissions.Add(Android.Manifest.Permission.ReadExternalStorage);

                if (PackageManager.CheckPermission(Android.Manifest.Permission.WriteExternalStorage, PackageName) != Permission.Granted)
                    permissions.Add(Android.Manifest.Permission.WriteExternalStorage);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.P && PackageManager.CheckPermission(Android.Manifest.Permission.ForegroundService, PackageName) != Permission.Granted)
                    permissions.Add(Android.Manifest.Permission.ForegroundService);

                if (permissions.Count == 0) {
                    log_button.Text = GetText(Resource.String.main_button_start);
                    perms_ok = true;
                    return;
                }

                RequestPermissions(permissions.ToArray(), 1);
            }
        }

        bool perms_ok = false;
        private void Log_button_Click(object sender, EventArgs e) {
            if (perms_ok) {
                if (GlobalScope.service_enabled) {
                    GlobalScope.service_enabled = false;
                    log_button.Text = GetText(Resource.String.main_button_start);

                    StopService(GlobalScope.service_logging);
                    GlobalScope.service_logging = null;
                    current_index.Text = "-";
                } else {
                    GlobalScope.service_enabled = true;
                    log_button.Text = GetText(Resource.String.main_button_stop);

                    GlobalScope.service_logging = new Intent(this, typeof(LoggingService));
                    GlobalScope.service_logging.SetAction("SERVICE_AMDMTS_ANY");
                    GlobalScope.service_logging.PutExtra("NEW_SLEEP_DELAY", GlobalScope.sleep_delay * 1000);
                    GlobalScope.service_logging.PutExtra("ENABLE_SEPARATORS", enable_separators.Checked);
                    GlobalScope.service_logging.PutExtra("ENABLE_RTT", enable_rtt.Checked);
                    StartForegroundService(GlobalScope.service_logging);
                }
            } else {
                CheckAppPermissions();
            }
        }

        private string GetLogFolder() {
#pragma warning disable CS0618 // Type or member is obsolete
            string storage_path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath;
#pragma warning restore CS0618 // Type or member is obsolete
            if (storage_path.Contains("/Android")) {
                storage_path = storage_path.Split("/Android")[0];
            }
            storage_path += "/Simple Wi-Fi Logger";
            try {
                if (!Directory.Exists(storage_path)) {
                    Directory.CreateDirectory(storage_path);
                }
            } catch (Exception) { }
           
            return storage_path;
        }

        private void Copy_folder_Click(object sender, EventArgs e) {
            var clipboard = (ClipboardManager)GetSystemService(ClipboardService);
            clipboard.Text = GetLogFolder();

            Toast.MakeText(Application.Context, "Path to log storage folder copied to clipboard", ToastLength.Short).Show();
        }

        private void Developers_link_Click(object sender, EventArgs e) {
            var clipboard = (ClipboardManager)GetSystemService(ClipboardService);
            clipboard.Text = "https://github.com/averov90/Simple-Wi-Fi-Logger";

            Toast.MakeText(Application.Context, "Link to developer's page copied to clipboard", ToastLength.Short).Show();
        }

        private void Enable_separators_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e) {
            Intent intent = new Intent("SERVICE_AMDMTS_ANY");
            intent.PutExtra("ENABLE_SEPARATORS", e.IsChecked);
            localBroadcastManager.SendBroadcast(intent);
        }

        private void Split_button_Click(object sender, EventArgs e) {
            if (GlobalScope.service_enabled) {
                Intent intent = new Intent("SERVICE_AMDMTS_ANY");
                intent.PutExtra("LOGFILE_SPLIT", true);
                localBroadcastManager.SendBroadcast(intent);

                Toast.MakeText(Application.Context, "Log split command received", ToastLength.Short).Show();
            }
        }

        public void OnCurrentIndexUpdate() {
            current_index.Text = GlobalScope.current_index.ToString();
        }

        private void Reset_button_Click(object sender, EventArgs e) {
            textbox_delay.Text = "0";
        }

        private void Textbox_delay_TextChanged(object sender, Android.Text.TextChangedEventArgs e) {
            if (ushort.TryParse(textbox_delay.Text, out ushort temp)) {
                GlobalScope.sleep_delay = temp;

                Intent intent = new Intent("SERVICE_AMDMTS_ANY");
                intent.PutExtra("NEW_SLEEP_DELAY", GlobalScope.sleep_delay * 1000);
                localBroadcastManager.SendBroadcast(intent);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (grantResults.Any(p => p == Permission.Denied)) {
                string res = "Permissions denied:";
                for (int i = 0; i < permissions.Length; ++i) {
                    if (grantResults[i] == Permission.Denied)
                        res += "\n> " + permissions[i].Substring(permissions[i].LastIndexOf('.') + 1);
                }
                res += "\nYou must grant listed permissions to start work!\nVisit the developer's page for help.";

                log_button.Text = GetText(Resource.String.main_button_params);

                Toast.MakeText(Application.Context, res, ToastLength.Long).Show();
                //new Timer((object none) => { System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow(); }, null, 3000, Timeout.Infinite);
            } else {
                log_button.Text = GetText(Resource.String.main_button_start);
                perms_ok = true;
            }
                
        }


    }
}