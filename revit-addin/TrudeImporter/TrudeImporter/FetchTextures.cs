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
                        string name = SanitizeFilename((string)material["name"]);
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
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Validate and encode the URL
                url = ValidateAndEncodeUrl(url);
                if (string.IsNullOrEmpty(url))
                {
                    System.Diagnostics.Debug.WriteLine("Invalid URL provided.");
                    return "";
                }

                using (WebClient client = new WebClient())
                {
                    string extension = Path.GetExtension(url);
                    if (extension != ".jpg" && extension != ".png" && extension != ".bmp" && extension != ".jpeg")
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping file with unsupported extension: {extension}");
                        return "";
                    }
                    string fullFilename = $"{filename}{extension}";
                    string fullPath = Path.Combine(path, fullFilename);

                    if (File.Exists(fullPath) && !overwrite)
                    {
                        System.Diagnostics.Debug.WriteLine("Texture already exists at: " + fullPath);
                        return fullPath;
                    }

                    client.DownloadFile(new Uri(url), fullPath);
                    System.Diagnostics.Debug.WriteLine("Texture downloaded successfully at: " + fullPath);

                    return fullPath;
                }
            }
            catch (WebException ex)
            {
                System.Diagnostics.Debug.WriteLine("A WebException occurred while downloading the texture: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("The faulty url is: " + url);
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine("An ArgumentException occurred: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("The faulty url is: " + url);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("An unexpected error occurred: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("The faulty url is: " + url);
            }

            return "";
        }

        private static string ValidateAndEncodeUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri validatedUri))
            {
                return null;
            }

            if (validatedUri.Scheme != Uri.UriSchemeHttp && validatedUri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            return Uri.EscapeUriString(url);
        }
        private static string SanitizeFilename(string filename)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, '_');
            }
            return filename;
        }

    }

}