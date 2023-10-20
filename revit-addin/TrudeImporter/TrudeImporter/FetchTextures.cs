using TrudeImporter;
using Newtonsoft.Json.Linq;
using System.IO;
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
                        name = name.Replace(" ", "_");
                        string url = (string)diffuseTexture["url"];
                        string path = Directory.GetCurrentDirectory();
                        path = Path.Combine(path, "fetchedTextures");
                        GlobalVariables.DownloadTexture(url, name, path);
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