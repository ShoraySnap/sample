using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.API
{
    public class CustomHttpHandler : DelegatingHandler
    {
        private readonly string djangoUrl = Urls.Get("snaptrudeDjangoUrl");
        private readonly List<string> ignorePaths = new List<string>
        {
            "/register/",
            "/snaplogin/",
            "/refreshAccessToken/",
            "/sendResetPasswordMail/",
            "/resetPassword/"
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri.AbsolutePath;

            if (!ignorePaths.Contains(path))
            {
                var accessToken = Store.Get("accessToken")?.ToString();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Headers.Add("auth", "Bearer " + accessToken);
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);

                if (result.ContainsKey("error") && result.ContainsKey("isTokenExpired") && result["isTokenExpired"])
                {
                    // Handle token expired
                    var accessToken = Store.Get("accessToken") as string;
                    var refreshToken = Store.Get("refreshToken") as string;

                    var tokenResponse = await RefreshTokenAsync(accessToken, refreshToken);

                    if (tokenResponse != null && tokenResponse.IsSuccessStatusCode)
                    {
                        var tokenData = await tokenResponse.Content.ReadAsStringAsync();
                        var tokenResult = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenData);

                        if (tokenResult.ContainsKey("accessToken"))
                        {
                            // Update access token and retry the original request
                            //TODO: update config.json with refreshtoken, fullname and userId; and not just accessToken
                            Store.Set("accessToken", tokenResult["accessToken"]);
                            Store.Save();

                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult["accessToken"]);
                            return await base.SendAsync(request, cancellationToken);
                        }
                    }
                }
                else if (result.ContainsKey("error") && result["error"] == 2)
                {
                    // Handle specific error
                    Store.Unset("user");
                    Store.Unset("refreshToken");
                }
            }
            return response;
        }

        private async Task<HttpResponseMessage> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            using (var client = new HttpClient())
            {
                var formData = new Dictionary<string, string>
                {
                    { "accessToken", accessToken },
                    { "refreshToken", refreshToken }
                };
                var content = new FormUrlEncodedContent(formData);

                return await client.PostAsync($"{djangoUrl}/refreshAccessToken/", content);
            }
        }
    }

}
