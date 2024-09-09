using System.Configuration;
using System.Data;
using System.Windows;
using SnaptrudeManagerUI.Stores;
using System.Windows.Threading;
using SnaptrudeManagerUI.ViewModels;
using SnaptrudeManagerUI.API;
using Microsoft.Win32;
using System.Reflection;
using Newtonsoft.Json;
using SnaptrudeManagerUI.Models;
using System.Web;
using NLog;
using SnaptrudeManagerUI.IPC;
using TrudeCommon.Logging;
using TrudeCommon.Events;
using TrudeCommon.DataTransfer;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Markup;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TrudeCommon.Utils;
using System.Text;
using TrudeCommon.Analytics;

namespace SnaptrudeManagerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public static DataTransferManager TransferManager;

        private DispatcherTimer timer = new DispatcherTimer();

        public static Action<int, string> OnProgressUpdate;
        public static Action OnSuccessfullLogin;
        public static Action OnFailedLogin;
        public static Action OnAbort;
        public static Action OnFailure;
        public static Action OnActivateView2D;
        public static Action OnActivateView3D;
        public static Action OnRvtOpened;
        public static Action OnRfaOpened;
        public static Action OnDocumentClosed;
        public static Action OnDocumentChanged;
        public static Action OnRevitClosed;
        public static Action OnUploadStart;
        public static Action<string> OnUploadIssue;

        public static Process RevitProcess;

        public static ProgressViewModel.ProgressViewType RetryUploadProgressType { get; internal set; }

        public static void RegisterProtocol()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + Constants.SNAPTRUDE_PROTOCOL))
            {
                key.SetValue(string.Empty, "URL:Snaptrude Manager");
                key.SetValue("URL Protocol", string.Empty);

                using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    string location = Assembly.GetExecutingAssembly().Location;
                    location = location.Replace(".dll", ".exe");
                    commandKey.SetValue(string.Empty, $"\"{location}\" \"%1\"");
                }
            }
        }

        protected async override void OnStartup(StartupEventArgs e)
        {
            RegisterProtocol();
            base.OnStartup(e);
            LogsConfig.Initialize("ManagerUI_" + Process.GetCurrentProcess().Id);

            FileUtils.Initialize();

            //WPFTODO: CHECKFORUPDATES
            var currentVersion = "4.0";
            var updateVersion = "4.0";

            string[] args = e.Args;
            int revitProcessId = 0;
            bool viewIs3D = false;
            bool isDocumentRvt = false;
            string fileName = "";
            bool isDocumentOpen = false;
            if (args.Any())
            {
                revitProcessId = int.Parse(args[0]);
                viewIs3D = bool.Parse(args[1]);
                isDocumentRvt = bool.Parse(args[2]);
                fileName = args[3];
                isDocumentOpen = true;
            }

            if (revitProcessId != 0)
            {
                RevitProcess = Process.GetProcessById(revitProcessId);
                RevitProcess.EnableRaisingEvents = true;
                RevitProcess.Exited += RevitProcess_Exited;
            }

            NavigationStore navigationStore = NavigationStore.Instance;
            MainWindowViewModel.Instance.ConfigMainWindowViewModel(navigationStore, currentVersion, updateVersion, viewIs3D, isDocumentRvt, isDocumentOpen, fileName);
            
            if (currentVersion != updateVersion)
                navigationStore.CurrentViewModel = ViewModelCreator.CreateUpdateAvailableViewModel();
            else
            {
                var isUserLoggedIn = await SnaptrudeRepo.CheckIfUserLoggedInAsync();
                MainWindowViewModel.Instance.IsLoaderVisible = false;
                string content = await isUserLoggedIn.Content.ReadAsStringAsync();
                if (content.Contains("Snaptrude API URL is blocked or unreachable") || content.Contains("The connection to the Snaptrude API was refused"))
                {
                    navigationStore.CurrentViewModel = ViewModelCreator.CreateAPIBlockedViewModel(content);
                }
                else if (content.Contains("Network error occurred"))
                {
                    navigationStore.CurrentViewModel = ViewModelCreator.CreateStartupInternetIssueWarningViewModel(content);
                }
                else if (Equals(Store.Get("userId"), "") || !isUserLoggedIn.IsSuccessStatusCode)
                {
                    navigationStore.CurrentViewModel = ViewModelCreator.CreateLoginViewModel();
                }
                else
                {
                    navigationStore.CurrentViewModel = ViewModelCreator.CreateHomeViewModel();
                }
            }

            // SnaptrudeService snaptrudeService = new SnaptrudeService();
            logger.Info("<<<UI Initialized!>>>");

            SetupDataChannels();
            SetupEvents();
            SetupStore();

            if (fileName != "")
            {
                UpdateNameAndFiletype(fileName, isDocumentRvt ? "rvt" : "rfa");
                UpdateButtonState(viewIs3D);
            }
            Application.Current.Dispatcher.Hooks.OperationCompleted += ProcessEventQueue;

        }

        private void RevitProcess_Exited(object sender, EventArgs e)
        {
            OnRevitClosed?.Invoke();
        }

        private void UpdateRevitProcess(int revitProcessId)
        {
            if (RevitProcess != null)
            {
                RevitProcess.Exited -= RevitProcess_Exited;
            }
            RevitProcess = Process.GetProcessById(revitProcessId);
            RevitProcess.EnableRaisingEvents = true;
            RevitProcess.Exited += RevitProcess_Exited;
        }

        private void SetupStore()
        {
            var accessToken = Store.Get("accessToken") as string;
            var refreshToken = Store.Get("refreshToken") as string;
            var fullname = Store.Get("fullname") as string;
            var userId = Store.Get("userId") as string;

            Store.Flush();
            Store.Set("accessToken", accessToken);
            Store.Set("refreshToken", refreshToken);
            Store.Set("fullname", fullname);
            Store.Set("userId", userId);
            Store.Save();
        }

        public static void UpdateNameAndFiletype(string projectName, string fileType)
        {
            Store.Set("projectName", projectName);
            Store.Set("fileType", fileType);
            Store.Save();
            MainWindowViewModel.Instance.ProjectFileName = $"{projectName}.{fileType}";
        }

        public static void UpdateButtonState(bool isView3D)
        {
            if (isView3D) OnActivateView3D?.Invoke();
            else OnActivateView2D?.Invoke();
        }

        private async void ProcessEventQueue(object sender, EventArgs e)
        {
            ConcurrentQueue<TRUDE_EVENT> eventQueue = TrudeEventSystem.Instance.GetQueue();
            while (!eventQueue.IsEmpty)
            {
                if (eventQueue.TryDequeue(out TRUDE_EVENT eventType))
                {
                    logger.Info("Processing event from main queue: {0}", TrudeEventUtils.GetEventName(eventType));
                    switch (eventType)
                    {
                        case TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D:
                            {
                                OnActivateView3D?.Invoke();
                                LogManager.GetCurrentClassLogger().Info("View changed to 3D in Revit.");
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER:
                            {
                                OnActivateView2D?.Invoke();
                                LogManager.GetCurrentClassLogger().Info("View changed to not 3D in Revit.");
                            }
                            break;
                        case TRUDE_EVENT.REVIT_CLOSED:
                            {
                                logger.Info("Revit closed, closing UI...");
                                // TODO: Loader / Dialog / Show message
                                OnRevitClosed?.Invoke();
                            }
                            break;
                        case TRUDE_EVENT.DATA_FROM_PLUGIN:
                            {
                                logger.Debug("Got data incoming from plugin!");
                                string data = TransferManager.ReadString(TRUDE_EVENT.DATA_FROM_PLUGIN);
                                logger.Debug("data : \"{0}\"", data);
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_PROJECTNAME_AND_FILETYPE:
                            {
                                logger.Info("Got data incoming to set projectname and filetype");
                                try
                                {
                                    string data = TransferManager.ReadString(TRUDE_EVENT.REVIT_PLUGIN_PROJECTNAME_AND_FILETYPE);
                                    logger.Info("data : \"{0}\"", data);
                                    Dictionary<string, string> parsedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                                    if (!Equals(Store.Get("projectName"), parsedData["projectName"]))
                                    {
                                        OnDocumentChanged?.Invoke();
                                    }
                                    Store.Set("projectName", parsedData["projectName"]);
                                    Store.Set("fileType", parsedData["fileType"]);
                                    Store.Save();
                                    MainWindowViewModel.Instance.ProjectFileName = $"{parsedData["projectName"]}.{parsedData["fileType"]}";
                                    if (Equals(parsedData["fileType"], "rvt"))
                                        OnRvtOpened?.Invoke();
                                    else
                                        OnRfaOpened?.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.Message);
                                }
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_SUCCESS:
                            {
                                logger.Info("Export finished, opening browser.");
                                try
                                {
                                    string floorkey = TransferManager.ReadString(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_SUCCESS).Trim();
                                    logger.Info("data : \"{0}\"", floorkey);

                                    await MainWindowViewModel.Instance.ProgressViewModel.FinishExport(floorkey);
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.Message);
                                }
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_FAILED:
                            {
                                logger.Error("Export failed.");
                                OnFailure?.Invoke();
                            }
                            break;
                        case TRUDE_EVENT.BROWSER_LOGIN_CREDENTIALS:
                            {
                                logger.Info("Got data incoming from browser!");
                                var backup = Store.GetData();
                                try
                                {
                                    string data = TransferManager.ReadString(TRUDE_EVENT.BROWSER_LOGIN_CREDENTIALS);
                                    logger.Info("data : \"{0}\"", data);
                                    Dictionary<string, string> userCredentialsModel = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                                    Store.SetAllAndSave(userCredentialsModel);

                                    if (!Store.isDataValid())
                                    {
                                        throw new Exception("Missing required data in login credentials.");
                                    }

                                    OnSuccessfullLogin?.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    Store.SetAllAndSave(backup);
                                    logger.Error(ex.Message);
                                    OnFailedLogin?.Invoke();
                                }
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_START:
                            {
                                logger.Info("Import to revit started!");
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_PROGRESS_UPDATE:
                            {
                                Current.MainWindow.Activate();
                                Current.MainWindow.Focus();
                                string data = TransferManager.ReadString(TRUDE_EVENT.REVIT_PLUGIN_PROGRESS_UPDATE);
                                string[] progressData = data.Split(';');
                                logger.Info("progress update {0}  {1}", progressData[0], progressData[1]);
                                if (progressData.Length >= 2)
                                {
                                    OnProgressUpdate?.Invoke(int.Parse(progressData[0]), progressData[1]);
                                }
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_SUCCESS:
                            {
                                logger.Info("Import to revit finished!");
                                if (Uploader.IsExportAnalyticsEnabled())
                                    await AnalyticsManager.CommitExportDataToAPI();
                                MainWindowViewModel.Instance.ProgressViewModel.FinishImportToRevit();
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_ABORTED:
                            {
                                logger.Info("Import to revit aborted!");
                                OnAbort?.Invoke();
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_FAILED:
                            {
                                logger.Error("Import to revit failed!");
                                OnFailure?.Invoke();
                            }
                            break;

                        case TRUDE_EVENT.REVIT_PLUGIN_DOCUMENT_OPENED:
                            {
                                logger.Info("Revit Document opened!");
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_DOCUMENT_CLOSED:
                            {
                                OnDocumentClosed?.Invoke();
                                logger.Info("Revit Document closed!");
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_ABORTED:
                            {
                                OnAbort?.Invoke();
                                logger.Info("Export to snaptrude aborted!");
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_UPDATE_REVIT_PROCESS_ID:
                            {
                                Current.MainWindow.Activate();
                                Current.MainWindow.Focus();
                                string data = TransferManager.ReadString(TRUDE_EVENT.REVIT_PLUGIN_UPDATE_REVIT_PROCESS_ID);
                                UpdateRevitProcess(int.Parse(data));
                                logger.Info("Changed Revit instance.");
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_REQUEST_UPLOAD_TO_SNAPTRUDE:
                            {
                                OnUploadStart?.Invoke();
                            }
                            break;
                    }
                }
            }

        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            logger.Info("Snaptrude Manager UI Shutting Down...");
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.MANAGER_UI_CLOSE);
            TrudeEventSystem.Instance.Shutdown();
            NavigationStore.Save();
            LogsConfig.Shutdown();
        }

        private void SetupDataChannels()
        {
            TransferManager = new DataTransferManager();
        }

        private void SetupEvents()
        {
            TrudeEventSystem.Instance.Init();

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.BROWSER_LOGIN_CREDENTIALS);

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.DATA_FROM_PLUGIN);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_PROJECTNAME_AND_FILETYPE);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_UPDATE_REVIT_PROCESS_ID);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_CLOSED);

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_START);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_PROGRESS_UPDATE);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_SUCCESS);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_ABORTED);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_FAILED);

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_SUCCESS);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_ABORTED);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_FAILED);

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_REQUEST_UPLOAD_TO_SNAPTRUDE);

            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_DOCUMENT_OPENED);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_DOCUMENT_CLOSED);
            TrudeEventSystem.Instance.Start();

            //SETUP EVENT QUEUE PROCESSING
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Start();
            timer.Tick += ProcessEventQueue;
        }
    }

}
