using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WinStb.Models;

namespace WinStb.Services
{
    public class StalkerPortalClient
    {
        private readonly HttpClient _httpClient;
        private Profile _currentProfile;
        private string _authToken;
        private int _requestId = 1;

        public StalkerPortalClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public string LastError { get; private set; }

        public async Task<bool> AuthenticateAsync(Profile profile)
        {
            _currentProfile = profile;
            LastError = null;

            try
            {
                // Step 1: Handshake to get auth token
                var handshakeUrl = BuildUrl("handshake", "stb");
                System.Diagnostics.Debug.WriteLine($"Handshake URL: {handshakeUrl}");

                ConfigureHeaders();
                System.Diagnostics.Debug.WriteLine($"Headers configured - MAC: {profile.MacAddress}");

                var response = await _httpClient.GetStringAsync(handshakeUrl);
                System.Diagnostics.Debug.WriteLine($"Handshake response: {response}");

                var jsonResponse = JObject.Parse(response);

                // Check for error in response
                var error = jsonResponse["js"]?["error"]?.ToString();
                if (!string.IsNullOrEmpty(error))
                {
                    // Replace the inner 'msg' variable name with a different name to avoid CS0136
                    var errorMsg = jsonResponse["js"]?["msg"]?.ToString() ?? error;
                    LastError = $"Portal error: {errorMsg}";
                    System.Diagnostics.Debug.WriteLine($"Portal returned error: {errorMsg}");
                    return false;
                }

                _authToken = jsonResponse["js"]?["token"]?.ToString();

                if (string.IsNullOrEmpty(_authToken))
                {
                    LastError = "No token received from portal. Response may be invalid.";
                    System.Diagnostics.Debug.WriteLine($"No token in response: {response}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"Token received: {_authToken}");

                // Step 2: Get profile to verify connection
                // Include device parameters if provided
                var profileParams = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(_currentProfile.SerialNumber))
                {
                    profileParams["sn"] = _currentProfile.SerialNumber;
                    System.Diagnostics.Debug.WriteLine($"Sending Serial Number: {_currentProfile.SerialNumber}");
                }

                if (!string.IsNullOrEmpty(_currentProfile.DeviceId))
                {
                    profileParams["device_id"] = _currentProfile.DeviceId;
                    System.Diagnostics.Debug.WriteLine($"Sending Device ID: {_currentProfile.DeviceId}");
                }

                if (!string.IsNullOrEmpty(_currentProfile.DeviceId2))
                {
                    profileParams["device_id2"] = _currentProfile.DeviceId2;
                    System.Diagnostics.Debug.WriteLine($"Sending Device ID2: {_currentProfile.DeviceId2}");
                }

                if (!string.IsNullOrEmpty(_currentProfile.Signature))
                {
                    profileParams["signature"] = _currentProfile.Signature;
                    System.Diagnostics.Debug.WriteLine($"Sending Signature: {_currentProfile.Signature}");
                }

                var profileUrl = BuildUrl("get_profile", "stb", profileParams.Count > 0 ? profileParams : null);
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var profileResponse = await _httpClient.GetStringAsync(profileUrl);
                System.Diagnostics.Debug.WriteLine($"Profile response received (length: {profileResponse.Length})");

                // Check if profile response indicates an error
                // Note: Don't confuse user's "status" field with API error status
                var profileJson = JObject.Parse(profileResponse);

                // Check for explicit error message (device conflict, etc.)
                var msg = profileJson["js"]?["msg"]?.ToString();
                if (!string.IsNullOrEmpty(msg) &&
                    (msg.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
                     msg.Contains("mismatch", StringComparison.OrdinalIgnoreCase)))
                {
                    LastError = $"Device registration issue: {msg}";
                    System.Diagnostics.Debug.WriteLine($"Profile error: {msg}");
                    return false;
                }

                // Check if we got valid profile data (has an id field)
                var profileId = profileJson["js"]?["id"]?.ToString();
                if (string.IsNullOrEmpty(profileId))
                {
                    LastError = "Invalid profile response - no user ID returned";
                    System.Diagnostics.Debug.WriteLine("Profile error: No ID in response");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"Profile loaded successfully - User ID: {profileId}");
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"Network error: {ex.Message}";
                if (ex.InnerException != null)
                    LastError += $" ({ex.InnerException.Message})";
                System.Diagnostics.Debug.WriteLine($"HTTP error: {LastError}");
                return false;
            }
            catch (JsonException ex)
            {
                LastError = $"Invalid response format: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"JSON parse error: {LastError}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"Unexpected error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        private void ConfigureHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();

            // Standard MAG set-top box user agent
            // Note: Removed spaces around colons to comply with HTTP header format
            var userAgent = "Mozilla/5.0 (QtEmbedded; U; Linux; C) AppleWebKit/533.3 (KHTML, like Gecko) MAG200 stbapp ver:2 rev:250 Safari/533.3";
            var xUserAgent = $"Model: {_currentProfile.StbType}; Link: Ethernet";

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Agent", xUserAgent);

            // Cookie with MAC address and other settings
            var cookie = $"mac={_currentProfile.MacAddress}; stb_lang=en; timezone={_currentProfile.TimeZone}";
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookie);
        }

        private string BuildUrl(string action, string type, Dictionary<string, string> parameters = null)
        {
            if (_currentProfile == null)
                throw new InvalidOperationException("Profile not set");

            var baseUrl = _currentProfile.PortalUrl.TrimEnd('/');
            if (!baseUrl.EndsWith("stalker_portal", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = $"{baseUrl}/stalker_portal";
            }

            var url = $"{baseUrl}/server/load.php?type={type}&action={action}&JsHttpRequest={_requestId++}-xml";

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
                }
            }

            return url;
        }

        public async Task<List<Genre>> GetGenresAsync()
        {
            try
            {
                var url = BuildUrl("get_genres", "itv");
                var response = await _httpClient.GetStringAsync(url);
                var jsonResponse = JObject.Parse(response);

                var genres = jsonResponse["js"]?.ToObject<List<Genre>>();
                return genres ?? new List<Genre>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get genres error: {ex.Message}");
                return new List<Genre>();
            }
        }

        public async Task<List<Channel>> GetChannelsAsync(string genreId = null, int page = 1)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "p", page.ToString() },
                    { "sortby", "number" },
                    { "fav", "0" },
                    { "hd", "0" }
                };

                if (!string.IsNullOrEmpty(genreId) && genreId != "*")
                {
                    parameters["genre"] = genreId;
                }

                var url = BuildUrl("get_ordered_list", "itv", parameters);
                var response = await _httpClient.GetStringAsync(url);
                var jsonResponse = JObject.Parse(response);

                var data = jsonResponse["js"]?["data"];
                if (data == null)
                    return new List<Channel>();

                var channels = new List<Channel>();
                foreach (var item in data)
                {
                    channels.Add(new Channel
                    {
                        Id = item["id"]?.ToString(),
                        Name = item["name"]?.ToString(),
                        Number = item["number"]?.ToString(),
                        Cmd = item["cmd"]?.ToString(),
                        Logo = item["logo"]?.ToString(),
                        Hd = item["hd"]?.ToObject<int?>(),
                        Lock = item["lock"]?.ToObject<int?>(),
                        Fav = item["fav"]?.ToObject<int?>(),
                        GenreTitle = item["tv_genre_title"]?.ToString(),
                        HasArchive = item["has_archive"]?.ToObject<bool?>()
                    });
                }

                return channels;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get channels error: {ex.Message}");
                return new List<Channel>();
            }
        }

        public async Task<List<Channel>> GetAllChannelsAsync()
        {
            var allChannels = new List<Channel>();
            var page = 1;
            var hasMorePages = true;

            while (hasMorePages)
            {
                var channels = await GetChannelsAsync(null, page);
                if (channels.Count == 0)
                {
                    hasMorePages = false;
                }
                else
                {
                    allChannels.AddRange(channels);
                    page++;

                    // Stop if we got less than a full page (typically 14 items per page)
                    if (channels.Count < 14)
                    {
                        hasMorePages = false;
                    }
                }
            }

            return allChannels;
        }

        public async Task<string> CreateLinkAsync(string cmd, bool isVod = false)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "cmd", cmd },
                    { "series", "" },
                    { "forced_storage", "undefined" },
                    { "disable_ad", "0" },
                    { "download", "0" }
                };

                var type = isVod ? "vod" : "itv";
                var url = BuildUrl("create_link", type, parameters);
                var response = await _httpClient.GetStringAsync(url);
                var jsonResponse = JObject.Parse(response);

                var streamCmd = jsonResponse["js"]?["cmd"]?.ToString();

                // Extract the actual streaming URL from the command
                if (!string.IsNullOrEmpty(streamCmd))
                {
                    // Remove "ffmpeg " prefix if present
                    if (streamCmd.StartsWith("ffmpeg ", StringComparison.OrdinalIgnoreCase))
                    {
                        streamCmd = streamCmd.Substring(7).Trim();
                      }

                    // Some portals return the URL with quotes
                    streamCmd = streamCmd.Trim('"', '\'');
                }

                return streamCmd;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create link error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Genre>> GetVodCategoriesAsync()
        {
            try
            {
                var url = BuildUrl("get_categories", "vod");
                var response = await _httpClient.GetStringAsync(url);
                var jsonResponse = JObject.Parse(response);

                var categories = jsonResponse["js"]?.ToObject<List<Genre>>();
                return categories ?? new List<Genre>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get VOD categories error: {ex.Message}");
                return new List<Genre>();
            }
        }

        public async Task<List<VodItem>> GetVodItemsAsync(string categoryId = null, int page = 1)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "p", page.ToString() },
                    { "sortby", "added" },
                    { "fav", "0" },
                    { "not_ended", "0" }
                };

                if (!string.IsNullOrEmpty(categoryId) && categoryId != "*")
                {
                    parameters["category"] = categoryId;
                }

                var url = BuildUrl("get_ordered_list", "vod", parameters);
                var response = await _httpClient.GetStringAsync(url);
                var jsonResponse = JObject.Parse(response);

                var data = jsonResponse["js"]?["data"];
                if (data == null)
                    return new List<VodItem>();

                var items = new List<VodItem>();
                foreach (var item in data)
                {
                    items.Add(new VodItem
                    {
                        Id = item["id"]?.ToString(),
                        Name = item["name"]?.ToString(),
                        OriginalName = item["o_name"]?.ToString(),
                        Description = item["description"]?.ToString(),
                        Cmd = item["cmd"]?.ToString(),
                        Screenshot = item["screenshot_uri"]?.ToString(),
                        Year = item["year"]?.ToString(),
                        Director = item["director"]?.ToString(),
                        Actors = item["actors"]?.ToString(),
                        Rating = item["rating_imdb"]?.ToString(),
                        Duration = item["duration"]?.ToString(),
                        Hd = item["hd"]?.ToObject<int?>(),
                        Fav = item["fav"]?.ToObject<int?>(),
                        Category = item["category_title"]?.ToString()
                    });
                }

                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get VOD items error: {ex.Message}");
                return new List<VodItem>();
            }
        }

        public async Task SendWatchdogAsync()
        {
            try
            {
                var url = BuildUrl("watchdog", "watchdog");
                await _httpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Watchdog error: {ex.Message}");
            }
        }
    }
}
