using System.Configuration;
using System.Data;
using System.Windows;
using SnaptrudeManagerUI.Stores;
using System.Windows.Threading;
using SnaptrudeManagerUI.ViewModels;
using NLog;
using SnaptrudeManagerUI.IPC;
using TrudeCommon.Logging;
using TrudeCommon.Events;
using TrudeCommon.DataChannel;
using System.Collections.Concurrent;

namespace SnaptrudeManagerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        static DataChannel? dataChannel = null;
        private DispatcherTimer timer = new DispatcherTimer();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LogsConfig.Initialize("ManagerUI");

            //WPFTODO: CHECKFORUPDATES
            var currentVersion = "2.1";
            var updateVersion = "2.1";


            NavigationStore navigationStore = NavigationStore.Instance;
            MainWindowViewModel.Instance.ConfigMainWindowViewModel(navigationStore, currentVersion, updateVersion, true);
            if (currentVersion != updateVersion)
                navigationStore.CurrentViewModel = ViewModelCreator.CreateUpdateAvailableViewModel();
            else
                navigationStore.CurrentViewModel = ViewModelCreator.CreateLoginViewModel();
            logger.Info("UI Initialized!");

            SetupDataChannels();
            SetupEvents();

            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.MANAGER_UI_OPEN);

            Application.Current.Dispatcher.Hooks.OperationCompleted += ProcessEventQueue;

        }

        private void ProcessEventQueue(object? sender, EventArgs e)
        {
            ConcurrentQueue<TRUDE_EVENT> eventQueue = TrudeEventSystem.Instance.GetQueue();
            while(!eventQueue.IsEmpty)
            {
                if(eventQueue.TryDequeue(out TRUDE_EVENT eventType))
                {
                    logger.Info("Processing event from main queue: {0}", TrudeEventUtils.GetEventName(eventType));
                    switch (eventType)
                    {
                        case TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D:
                            {
                                LogManager.GetCurrentClassLogger().Info("View changed to 3D in Revit.");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MainWindowViewModel.Instance.IsActiveView3D = true;
                                });
                            }
                            break;
                        case TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER:
                            {
                                LogManager.GetCurrentClassLogger().Info("View changed to not 3D in Revit.");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MainWindowViewModel.Instance.IsActiveView3D = false;
                                });
                            }
                            break;
                        case TRUDE_EVENT.REVIT_CLOSED:
                            {
                                logger.Info("Revit closed, closing UI...");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Application.Current.Shutdown();
                                });
                            }
                            break;
                        case TRUDE_EVENT.DATA_FROM_PLUGIN:
                            {
                                logger.Info("Got data incoming from plugin!");
                                string? data = dataChannel?.ReadDataAsString();
                                logger.Info("data : \"{0}\"", data?.ToString().Trim());
                            }
                            break;
                    }
                }
            }

        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            logger.Info("UI Shutdown!");
            TrudeEventEmitter.EmitEvent(TRUDE_EVENT.MANAGER_UI_CLOSE);
            LogsConfig.Shutdown();
        }

        private void SetupDataChannels()
        {
            dataChannel = new DataChannel("MMF_1", 2048);
        }

        private void SetupEvents()
        {
            TrudeEventSystem.Instance.Init();
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_3D);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_PLUGIN_VIEW_OTHER);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.DATA_FROM_PLUGIN);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_CLOSED);
            TrudeEventSystem.Instance.SubscribeToEvent(TRUDE_EVENT.REVIT_CLOSED);

            TrudeEventSystem.Instance.Start();

            //SETUP EVENT QUEUE PROCESSING
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Start();
            timer.Tick += ProcessEventQueue;
        }
    }

}
