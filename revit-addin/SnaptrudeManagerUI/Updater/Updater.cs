using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;

namespace SnaptrudeManagerUI
{
    public class Updater
    {
        private SparkleUpdater _sparkle;
        public static string UpdateVersion = "";

        private UpdateInfo _updateInfo;
        public Updater()
        {
            var uiPath48 = $@"C:\Users\ferna\source\repos\Snaptrude\snaptrudemanager\revit-addin\SnaptrudeManagerUI\bin\Debug\net48\SnaptrudeManagerUI.exe";
            var publicKey = "6xpMewsNUkGqnyQUMMoO9O/0Pb7uIDuD2jsrAGq+en8=";
            string appCastURL = "https://updatemanager.s3.us-east-2.amazonaws.com/appcast.xml";
            _sparkle = new SparkleUpdater(appCastURL, new Ed25519Checker(SecurityMode.Strict, publicKey), uiPath48)
            {
                UIFactory = null,
                SecurityProtocolType = System.Net.SecurityProtocolType.Tls12,
                UserInteractionMode = UserInteractionMode.DownloadAndInstall
            };
            PrepareCallbacks();
            App.OnStartDownload += StartDownload;
            App.OnCancelDownload += CancelDownload;
        }

        private void CancelDownload()
        {
            _sparkle.CancelFileDownload();
        }

        private async void StartDownload()
        {
            _sparkle.DownloadStarted -= _sparkle_DownloadStarted;
            _sparkle.DownloadHadError -= _sparkle_DownloadError;
            _sparkle.DownloadMadeProgress -= _sparkle_DownloadMadeProgress;
            _sparkle.DownloadFinished -= _sparkle_DownloadFinished;
            _sparkle.DownloadCanceled -= _sparkle_DownloadCanceled;

            _sparkle.DownloadStarted += _sparkle_DownloadStarted;
            _sparkle.DownloadHadError += _sparkle_DownloadError;
            _sparkle.DownloadMadeProgress += _sparkle_DownloadMadeProgress;
            _sparkle.DownloadFinished += _sparkle_DownloadFinished;
            _sparkle.DownloadCanceled += _sparkle_DownloadCanceled;

            await _sparkle.InitAndBeginDownload(_updateInfo.Updates.First());
        }

        private void PrepareCallbacks()
        {
            _sparkle.UpdateCheckStarted += _sparkle_UpdateCheckStarted;
            _sparkle.UpdateCheckFinished += _sparkle_UpdateCheckFinished;
            _sparkle.UpdateDetected += _sparkle_UpdateDetected;
        }

        private void _sparkle_UpdateCheckFinished(object sender, UpdateStatus status)
        {
            Debug.WriteLine("Update Check Finished!");
            if (UpdateVersion == "")
            {
                App.OnLatestVersion?.Invoke();
            }
        }

        private void _sparkle_UpdateCheckStarted(object sender)
        {
            Debug.WriteLine("Update Check Started!");
        }

        private void _sparkle_DownloadCanceled(AppCastItem item, string path)
        {
            Debug.WriteLine("Download Canceled!");
        }

        private void _sparkle_DownloadFinished(AppCastItem item, string path)
        {
            Debug.WriteLine("Download Finished!");
            App.OnDownloadFinished?.Invoke();
        }

        private void _sparkle_DownloadStarted(AppCastItem item, string path)
        {
            Debug.WriteLine("Download Started!");
        }

        private void _sparkle_DownloadError(AppCastItem item, string path, Exception exception)
        {
            //MessageBox.Show($"Download Error! {path} : {exception}");
            App.OnDownloadError?.Invoke();
        }

        private void _sparkle_DownloadMadeProgress(object sender, AppCastItem item, NetSparkleUpdater.Events.ItemDownloadProgressEventArgs args)
        {
            Debug.WriteLine("Download Making Progress!");
            App.OnProgressUpdate?.Invoke(args.ProgressPercentage, "Downloading update installer...");
        }

        private void _sparkle_UpdateDetected(object sender, NetSparkleUpdater.Events.UpdateDetectedEventArgs e)
        {
            Debug.WriteLine("we found an update");
            UpdateVersion = e.LatestVersion.Version;
            App.OnUpdateAvailable.Invoke();
        }

        public async Task<bool> IsUpdateAvailable()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;

            _updateInfo = await _sparkle.CheckForUpdatesAtUserRequest();
            if (_updateInfo != null)
            {
                return _updateInfo.Status == UpdateStatus.UpdateAvailable;
            }
            return false;
        }

        private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
