﻿using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.API
{
    public class CustomHttpHandler : DelegatingHandler
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

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
            try
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
                    try
                    {
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
                            else
                            {
                                logger.Error("Error refreshing token");
                                Store.Unset("user");
                                Store.Unset("refreshToken");
                                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                                {
                                    Content = new StringContent($"An unexpected error occurred: Error refreshing token")
                                };
                            }
                        }
                        else if (result.ContainsKey("error") && (result["error"] == 2 || result["error"] == 1))
                        {
                            logger.Error(result["message"]);
                            Store.Unset("user");
                            Store.Unset("refreshToken");
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                return response;
            }
            catch (HttpRequestException ex)
            {
                logger.Error(ex.Message);
                // Check if the URL is blocked or unreachable
                if (ex.Message.Contains("Name or service not known") || ex.Message.Contains("No such host is known"))
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway)
                    {
                        Content = new StringContent("Snaptrude API URL is blocked or unreachable: " + ex.Message)
                    };
                }
                else if (ex.Message.Contains("Connection refused"))
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent("The connection to the Snaptrude API was refused: " + ex.Message)
                    };
                }
                else
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent("Network error occurred: " + ex.Message)
                    };
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent($"An unexpected error occurred: {ex.Message}")
                };
            }
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
