using Newtonsoft.Json.Linq;
using System;
using System.IO;
using TrudeImporter;

namespace FetchTextures
{
    public class FetchTextures
    {
        public FetchTextures()
        {
            try
            {
                JArray materials = GlobalVariables.materials;
                foreach (JObject material in materials)
                {
                    if (material["diffuseTexture"] != null)
                    {
                        JObject diffuseTexture = (JObject)material["diffuseTexture"];
                        string name = (string)material["name"];
                        float alpha = (float)material["alpha"] * 100;
                        string url = (string)diffuseTexture["url"];
                        string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Configs.CUSTOM_FAMILY_DIRECTORY, "resourceFile", "fetchedTextures");
                        string savedPath = GlobalVariables.DownloadTexture(url, name, baseDir);

                        if (savedPath != "") GlobalVariables.CreateMaterial(GlobalVariables.Document, name, savedPath, alpha);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No texture to add");
                    }
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Error");
            }
        }
    }

}