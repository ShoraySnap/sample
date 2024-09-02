using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace TrudeCommon.Utils
{
    public static class FileUtils
    {
        static readonly string FOLDER_NAME =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\snaptrude-manager";

        static readonly string TEMP_PATH =
            $"{FOLDER_NAME}\\temp\\";
        static Logger logger = LogManager.GetCurrentClassLogger();


        public const string DATA_FNAME = "upload_data";
        public const string MATERIAL_FNAME = "material_data";
        public const string LOG_FNAME = "log";
        public const string ANALYTICS_FNAME = "analytics";

        public static bool Initialized = false;
        public static void Initialize()
        {
            if(Initialized) return;
            try
            {
                InitDirectories();
                Initialized = true;
            }
            catch(Exception ex)
            {
                logger.Error("Error initializing FileUtils... {0}", ex.Message);
                Initialized = false;
            }
        }
        private static void RecreateTempDirectory(string dirPath)
        {
            if(!Directory.Exists(dirPath))
            {
                logger.Trace($"Creating temp directory : ${dirPath}");
                try
                {
                    Directory.CreateDirectory(dirPath);
                }
                catch(Exception e)
                {
                    logger.Error($"Could not create : {dirPath}. ERROR: {e.Message}");
                }
            }
        }

        private static void InitDirectories()
        {
            RecreateTempDirectory(TEMP_PATH);
        }
        public static void SaveCommonTempFile(string filename, byte[] data)
        {
            if(!Initialized)
            {
                logger.Warn("FileUtils not initialized!");
                return;
            }
            string finalPath = Path.Combine(TEMP_PATH, filename);

            try
            {
                File.WriteAllBytes(finalPath, data);
            }
            catch (Exception e)
            {
                logger.Error($"Could not save temp file : {finalPath}. ERROR: {e.Message}");
            }
        }

        public static byte[] GetCommonTempFile(string filename)
        {
            if(!Initialized)
            {
                logger.Warn("FileUtils not initialized!");
                return new byte[0];
            }
            string finalPath = Path.Combine(TEMP_PATH, filename);

            try
            {
                var data = File.ReadAllBytes(finalPath);
                return data;
            }
            catch (Exception e)
            {
                logger.Error($"Could not get temp file : {finalPath}. ERROR: {e.Message}");
                return new byte[0];
            }

        }
    }
}
