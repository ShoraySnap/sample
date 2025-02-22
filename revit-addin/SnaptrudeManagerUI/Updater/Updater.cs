﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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
        public string UpdateVersion = "";
        public string DownloadsPath = "";
        public string DownloadedFilePath = "";
        public bool CriticalUpdateFound = false;

        private static readonly Guid DownloadsFolderGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        private UpdateInfo _updateInfo;
        public Updater()
        {
            logger.Info("Initializing updater!");

            var publicKey = "AVtJw/dp/mJhd4tEdUlIexV/Ut72lVrFhGUBX1oT/vU=";
            var awsRegion = "ap-south-1";
            string appCastURL = $"https://snaptrude-prod.data.s3.{awsRegion}.amazonaws.com/media/manager/appcast.xml";
            string reactUrl = Urls.Get("snaptrudeReactUrl");
            if (reactUrl != "https://app.snaptrude.com")
            {
                awsRegion = "us-east-2";
                appCastURL = $"https://updatemanager.s3.{awsRegion}.amazonaws.com/AutomatedDeployTest/appcast.xml";
            }

            //appCastURL = $"https://updatemanager.s3.{awsRegion}.amazonaws.com/appcast.xml";

            try
            {
                IntPtr pszPath = IntPtr.Zero;
                int hr = SHGetKnownFolderPath(DownloadsFolderGuid, 0, IntPtr.Zero, out pszPath);
                DownloadsPath = Marshal.PtrToStringAuto(pszPath);
            }
            catch (Exception)
            {
                DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
            }

            _sparkle = new SparkleUpdater(appCastURL, new Ed25519Checker(SecurityMode.Strict, publicKey))
            {
                UIFactory = null,
                SecurityProtocolType = System.Net.SecurityProtocolType.Tls12,
                UserInteractionMode = UserInteractionMode.DownloadNoInstall,
            };

            if (DownloadsPath != "")
            {
                _sparkle.TmpDownloadFilePath = DownloadsPath;
            }

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
            DownloadedFilePath = path;
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
            CriticalUpdateFound = IsCriticalUpdate(e.LatestVersion);
        }

        public bool IsCriticalUpdate(AppCastItem latestVersion)
        {
            int updateMajorVersion = int.Parse(latestVersion.Version.Substring(0,1));
            int intalledMajorVersion = int.Parse(latestVersion.AppVersionInstalled.Substring(0, 1));
            return updateMajorVersion > intalledMajorVersion;
        }

        public async Task<bool> IsUpdateAvailable()
        {
            _updateInfo = await _sparkle.CheckForUpdatesAtUserRequest();
            if (_updateInfo != null)
            {
                return _updateInfo.Status == UpdateStatus.UpdateAvailable;
            }
            return false;
        }

    }
}
