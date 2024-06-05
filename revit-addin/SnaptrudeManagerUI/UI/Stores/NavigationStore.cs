using Newtonsoft.Json;
using NLog;
using SnaptrudeManagerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.Stores
{
    public class NavigationStore
    {
        private static string filePath;
        private static Dictionary<string, string> data = new Dictionary<string, string>();
        private static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "snaptrude-manager");
        static Logger logger = LogManager.GetCurrentClassLogger(); 

        private static readonly object padlock = new object();
        private static NavigationStore instance;
        public static NavigationStore Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new NavigationStore();
                    }
                    return instance;
                }
            }
        }
        private NavigationStore()
        {
            filePath = Path.Combine(appDataPath, "userPreferences.json");

            if (File.Exists(filePath))
            {
                data = ParseDataFile(filePath);
            }
            else
            {
                CreateEmptyConfig();
            }
        }

        private ViewModelBase currentViewModel;

        public ViewModelBase CurrentViewModel
        {
            get => currentViewModel;
            set
            {
                currentViewModel = value;
                OnCurrentViewModelChanged();
            }
        }

        private static Dictionary<string, string> ParseDataFile(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                CreateEmptyConfig();
                return data;
            }
        }

        private static void CreateEmptyConfig()
        {
            data = new Dictionary<string, string>();
            Save();
        }

        public static object Get(string key)
        {
            data.TryGetValue(key, out var value);
            return value;
        }

        public static void Set(string key, string value)
        {
            data[key] = value;
        }

        public static void Unset(string key)
        {
            data.Remove(key);
        }

        public static void SetAllAndSave(Dictionary<string, string> dataObject)
        {
            data = dataObject;
            Save();
        }

        public static void Save()
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        public static Dictionary<string, string> GetData()
        {
            return new Dictionary<string, string>(data);
        }

        public static void Flush()
        {
            CreateEmptyConfig();
        }

        private void OnCurrentViewModelChanged()
        {
            CurrentViewModelChanged?.Invoke();
        }

        public event Action CurrentViewModelChanged;
    }
}
