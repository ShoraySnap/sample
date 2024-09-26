using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TrudeCommon.Utils
{
    internal static class Util
    {
        internal static string GetUniqueHash(string name, int length)
        {
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string guid = name;

            string combined = string.Join("", new string[] {timestamp, guid});

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hashString.Substring(0, length);
            }
        }

    }
}
