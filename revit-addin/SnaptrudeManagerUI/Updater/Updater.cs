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
using NLog;
using SnaptrudeManagerUI.API;

namespace SnaptrudeManagerUI
{
    public class Updater
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        private SparkleUpdater _sparkle;
        public static string UpdateVersion = "";

        private UpdateInfo _updateInfo;
        public Updater()
        {
            logger.Info("Initializing updater!");

            var uiPath48 = $@"C:\Users\ferna\source\repos\Snaptrude\snaptrudemanager\revit-addin\SnaptrudeManagerUI\bin\Debug\net48\SnaptrudeManagerUI.exe";
            var publicKey = "BNmliedOVk3IBj/5XSEdxGRW5ibo3fmGIkYHX9CRju4=";
            var awsRegion = "ap-south-1";
            string appCastURL = $"https://snaptrude-prod.data.s3.{awsRegion}.amazonaws.com/media/manager/appcast.xml";
            string reactUrl = Urls.Get("snaptrudeReactUrl");
            if (reactUrl != "https://app.snaptrude.com")
            {
                appCastURL = $"https://updatemanager.s3.{awsRegion}.amazonaws.com/appcast.xml";
            }
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
            logger.Info("Update Check Finished!");
            if (UpdateVersion == "")
            {
                App.OnLatestVersion?.Invoke();
            }
        }

        private void _sparkle_UpdateCheckStarted(object sender)
        {
            logger.Info("Update Check Started!");
        }

        private void _sparkle_DownloadCanceled(AppCastItem item, string path)
        {
            logger.Info("Download Canceled!");
        }

        private void _sparkle_DownloadFinished(AppCastItem item, string path)
        {
            logger.Info("Download Finished!");
            App.OnDownloadFinished?.Invoke();
        }

        private void _sparkle_DownloadStarted(AppCastItem item, string path)
        {
            logger.Info("Download Started!");
        }

        private void _sparkle_DownloadError(AppCastItem item, string path, Exception exception)
        {
            logger.Error($"Download Error! {path} : {exception}");
            App.OnDownloadError?.Invoke();
        }

        private void _sparkle_DownloadMadeProgress(object sender, AppCastItem item, NetSparkleUpdater.Events.ItemDownloadProgressEventArgs args)
        {
            App.OnProgressUpdate?.Invoke(args.ProgressPercentage, "Downloading update installer...");
        }

        private void _sparkle_UpdateDetected(object sender, NetSparkleUpdater.Events.UpdateDetectedEventArgs e)
        {
            logger.Info("Found an Update!");
            UpdateVersion = e.LatestVersion.Version;
            App.OnUpdateAvailable.Invoke();
            foreach (var appCastItem in e.AppCastItems)
            {
                if (appCastItem.IsCriticalUpdate)
                {
                    App.OnCriticalUpdateAvailable.Invoke();
                    break;
                }
            }
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
