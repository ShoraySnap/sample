using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
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
                        TextureProperties textureProps = new TextureProperties(
                            (string)diffuseTexture["url"],
                            (double)diffuseTexture["uScale"],
                            (double)diffuseTexture["vScale"],
                            (double)diffuseTexture["uOffset"],
                            (double)diffuseTexture["vOffset"],
                            (double)diffuseTexture["wAng"]
                            );

                        string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Configs.CUSTOM_FAMILY_DIRECTORY, "resourceFile", "fetchedTextures");
                        string savedPath = DownloadTexture(textureProps.TexturePath, name, baseDir);

                        if (savedPath != "")
                        {
                            textureProps.TexturePath = savedPath;
                            MaterialOperations.MaterialOperations.CreateMaterial(GlobalVariables.Document, name, textureProps, alpha);
                        }
                    }
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Error");
            }
        }

        public static string DownloadTexture(string url, string filename, string path, bool overwrite = false)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string extension = Path.GetExtension(url);
                    string fullFilename = $"{filename}{extension}";
                    string fullPath = Path.Combine(path, fullFilename);
                    if (File.Exists(fullPath) && !overwrite)
                    {
                        Console.WriteLine("Texture already exists at: " + fullPath);
                        System.Diagnostics.Debug.WriteLine("Texture already exists at: " + fullPath);
                        return fullPath;
                    }
                    client.DownloadFile(new Uri(url), fullPath);
                    Console.WriteLine("Texture downloaded successfully at: " + fullPath);
                    System.Diagnostics.Debug.WriteLine("Texture downloaded successfully at: " + fullPath);
                    return fullPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while downloading the texture: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("An error occurred while downloading the texture: " + ex.Message);
                    return "";
                }
            }
        }

    }

}