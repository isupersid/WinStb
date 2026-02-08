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

        // Cache for channels and genres
        private List<Channel> _cachedChannels;
        private DateTime _channelsCacheTime;
        private List<Genre> _cachedGenres;
        private DateTime _genresCacheTime;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

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

            // Reset session state for fresh authentication
            _authToken = null;
            _requestId = 1;

            // Clear cached data when switching profiles
            ClearCache();

            try
            {
                // Step 1: Handshake to get auth token
                var handshakeUrl = BuildUrl("handshake", "stb");
                System.Diagnostics.Debug.WriteLine($"\n========== AUTHENTICATION ATTEMPT ==========");
                System.Diagnostics.Debug.WriteLine($"Handshake URL: {handshakeUrl}");

                ConfigureHeaders();
                System.Diagnostics.Debug.WriteLine($"Headers configured - MAC: {profile.MacAddress}");

                // Add a small delay to prevent rate limiting
                await System.Threading.Tasks.Task.Delay(500);

                var response = await _httpClient.GetStringAsync(handshakeUrl);
                System.Diagnostics.Debug.WriteLine($"Handshake response: {response}");

                var jsonResponse = JObject.Parse(response);

                // Check if js is an array (error case) or object (normal case)
                var jsToken = jsonResponse["js"];
                if (jsToken is JArray)
                {
                    LastError = "Portal authentication failed. This could be due to:\n" +
                               "- MAC address already in use\n" +
                               "- Portal blocking automated access\n" +
                               "- Invalid MAC address format\n" +
                               "- Server rate limiting\n\n" +
                               "Try waiting 30 seconds and authenticate again.";
                    System.Diagnostics.Debug.WriteLine("Portal returned js as array instead of object - authentication rejected");
                    System.Diagnostics.Debug.WriteLine("==========================================\n");
                    return false;
                }

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
            // Check cache first
            if (_cachedGenres != null && DateTime.Now - _genresCacheTime < _cacheExpiration)
            {
                System.Diagnostics.Debug.WriteLine("Returning cached genres");
                return _cachedGenres;
            }

            try
            {
                var url = BuildUrl("get_genres", "itv");
                System.Diagnostics.Debug.WriteLine($"\n========== GET GENRES REQUEST ==========");
                System.Diagnostics.Debug.WriteLine($"URL: {url}");

                var response = await _httpClient.GetStringAsync(url);
                System.Diagnostics.Debug.WriteLine($"\n========== GET GENRES RESPONSE ==========");
                System.Diagnostics.Debug.WriteLine($"Response Length: {response.Length} characters");
                System.Diagnostics.Debug.WriteLine($"Response Body:\n{response}");
                System.Diagnostics.Debug.WriteLine($"==========================================\n");

                var jsonResponse = JObject.Parse(response);

                var genres = jsonResponse["js"]?.ToObject<List<Genre>>();

                // Cache the result
                _cachedGenres = genres ?? new List<Genre>();
                _genresCacheTime = DateTime.Now;

                return _cachedGenres;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get genres error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<Genre>();
            }
        }

        public async Task<List<Channel>> GetChannelsAsync(string genreId = null, int page = 0)
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
                System.Diagnostics.Debug.WriteLine($"\n========== GET CHANNELS REQUEST ==========");
                System.Diagnostics.Debug.WriteLine($"URL: {url}");

                var response = await _httpClient.GetStringAsync(url);
                System.Diagnostics.Debug.WriteLine($"\n========== GET CHANNELS RESPONSE ==========");
                System.Diagnostics.Debug.WriteLine($"Response Length: {response.Length} characters");
                System.Diagnostics.Debug.WriteLine($"Response Body:\n{response}");
                System.Diagnostics.Debug.WriteLine($"==========================================\n");

                var jsonResponse = JObject.Parse(response);

                var data = jsonResponse["js"]?["data"];
                if (data == null)
                {
                    System.Diagnostics.Debug.WriteLine("No 'data' field in response");
                    return new List<Channel>();
                }

                // Check if data is an array or object
                var channels = new List<Channel>();

                if (data is JArray dataArray)
                {
                    // Data is an array - iterate through it
                    foreach (var item in dataArray)
                    {
                        if (item is JObject channelObj)
                        {
                            var logoUrl = channelObj["logo"]?.ToString()?.Trim();

                            // Validate logo URL - must be absolute URL or null
                            if (!string.IsNullOrWhiteSpace(logoUrl))
                            {
                                // Check if it's a valid absolute URI (http:// or https://)
                                if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out Uri uriResult) ||
                                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                                {
                                    logoUrl = null; // Invalid URL, use null instead
                                }
                            }
                            else
                            {
                                logoUrl = null; // Empty or whitespace, use null
                            }

                            channels.Add(new Channel
                            {
                                Id = channelObj["id"]?.ToString(),
                                Name = channelObj["name"]?.ToString(),
                                Number = channelObj["number"]?.ToString(),
                                Cmd = channelObj["cmd"]?.ToString(),
                                Logo = logoUrl,
                                Hd = channelObj["hd"]?.ToObject<int?>(),
                                Lock = channelObj["lock"]?.ToObject<int?>(),
                                Fav = channelObj["fav"]?.ToObject<int?>(),
                                GenreTitle = channelObj["tv_genre_title"]?.ToString(),
                                HasArchive = channelObj["has_archive"]?.ToObject<bool?>()
                            });
                        }
                    }
                }
                else if (data is JObject dataObj)
                {
                    // Data is an object - might be a dictionary with numeric keys
                    foreach (var prop in dataObj.Properties())
                    {
                        if (prop.Value is JObject channelObj)
                        {
                            var logoUrl = channelObj["logo"]?.ToString()?.Trim();

                            // Validate logo URL - must be absolute URL or null
                            if (!string.IsNullOrWhiteSpace(logoUrl))
                            {
                                // Check if it's a valid absolute URI (http:// or https://)
                                if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out Uri uriResult) ||
                                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                                {
                                    logoUrl = null; // Invalid URL, use null instead
                                }
                            }
                            else
                            {
                                logoUrl = null; // Empty or whitespace, use null
                            }

                            channels.Add(new Channel
                            {
                                Id = channelObj["id"]?.ToString(),
                                Name = channelObj["name"]?.ToString(),
                                Number = channelObj["number"]?.ToString(),
                                Cmd = channelObj["cmd"]?.ToString(),
                                Logo = logoUrl,
                                Hd = channelObj["hd"]?.ToObject<int?>(),
                                Lock = channelObj["lock"]?.ToObject<int?>(),
                                Fav = channelObj["fav"]?.ToObject<int?>(),
                                GenreTitle = channelObj["tv_genre_title"]?.ToString(),
                                HasArchive = channelObj["has_archive"]?.ToObject<bool?>()
                            });
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Parsed {channels.Count} channels");
                return channels;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\n========== GET CHANNELS ERROR ==========");
                System.Diagnostics.Debug.WriteLine($"Error Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Error Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"==========================================\n");
                return new List<Channel>();
            }
        }

        public async Task<List<Channel>> GetAllChannelsAsync(bool forceRefresh = false)
        {
            // Check cache first unless force refresh is requested
            if (!forceRefresh && _cachedChannels != null && DateTime.Now - _channelsCacheTime < _cacheExpiration)
            {
                System.Diagnostics.Debug.WriteLine($"Returning {_cachedChannels.Count} cached channels");
                return _cachedChannels;
            }

            System.Diagnostics.Debug.WriteLine("Fetching channels from API...");
            var allChannels = new List<Channel>();
            var page = 0;
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

            // Cache the result
            _cachedChannels = allChannels;
            _channelsCacheTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"Cached {_cachedChannels.Count} channels");

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

        public async Task<List<VodItem>> GetVodItemsAsync(string categoryId = null, int page = 0)
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
                System.Diagnostics.Debug.WriteLine($"\n========== GET VOD REQUEST ==========");
                System.Diagnostics.Debug.WriteLine($"URL: {url}");

                var response = await _httpClient.GetStringAsync(url);
                System.Diagnostics.Debug.WriteLine($"\n========== GET VOD RESPONSE ==========");
                System.Diagnostics.Debug.WriteLine($"Response Length: {response.Length} characters");
                System.Diagnostics.Debug.WriteLine($"Response Body:\n{response}");
                System.Diagnostics.Debug.WriteLine($"==========================================\n");

                var jsonResponse = JObject.Parse(response);

                var data = jsonResponse["js"]?["data"];
                if (data == null)
                {
                    System.Diagnostics.Debug.WriteLine("No 'data' field in VOD response");
                    return new List<VodItem>();
                }

                var items = new List<VodItem>();

                if (data is JArray dataArray)
                {
                    // Data is an array - iterate through it
                    foreach (var item in dataArray)
                    {
                        if (item is JObject vodObj)
                        {
                            var screenshotUrl = vodObj["screenshot_uri"]?.ToString()?.Trim();

                            // Validate screenshot URL - must be absolute URL or null
                            if (!string.IsNullOrWhiteSpace(screenshotUrl))
                            {
                                // Check if it's a valid absolute URI (http:// or https://)
                                if (!Uri.TryCreate(screenshotUrl, UriKind.Absolute, out Uri uriResult) ||
                                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                                {
                                    screenshotUrl = null; // Invalid URL, use null instead
                                }
                            }
                            else
                            {
                                screenshotUrl = null; // Empty or whitespace, use null
                            }

                            items.Add(new VodItem
                            {
                                Id = vodObj["id"]?.ToString(),
                                Name = vodObj["name"]?.ToString(),
                                OriginalName = vodObj["o_name"]?.ToString(),
                                Description = vodObj["description"]?.ToString(),
                                Cmd = vodObj["cmd"]?.ToString(),
                                Screenshot = screenshotUrl,
                                Year = vodObj["year"]?.ToString(),
                                Director = vodObj["director"]?.ToString(),
                                Actors = vodObj["actors"]?.ToString(),
                                Rating = vodObj["rating_imdb"]?.ToString(),
                                Duration = vodObj["duration"]?.ToString(),
                                Hd = vodObj["hd"]?.ToObject<int?>(),
                                Fav = vodObj["fav"]?.ToObject<int?>(),
                                Category = vodObj["category_title"]?.ToString()
                            });
                        }
                    }
                }
                else if (data is JObject dataObj)
                {
                    // Data is an object - might be a dictionary with numeric keys
                    foreach (var prop in dataObj.Properties())
                    {
                        if (prop.Value is JObject vodObj)
                        {
                            var screenshotUrl = vodObj["screenshot_uri"]?.ToString()?.Trim();

                            // Validate screenshot URL - must be absolute URL or null
                            if (!string.IsNullOrWhiteSpace(screenshotUrl))
                            {
                                // Check if it's a valid absolute URI (http:// or https://)
                                if (!Uri.TryCreate(screenshotUrl, UriKind.Absolute, out Uri uriResult) ||
                                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                                {
                                    screenshotUrl = null; // Invalid URL, use null instead
                                }
                            }
                            else
                            {
                                screenshotUrl = null; // Empty or whitespace, use null
                            }

                            items.Add(new VodItem
                            {
                                Id = vodObj["id"]?.ToString(),
                                Name = vodObj["name"]?.ToString(),
                                OriginalName = vodObj["o_name"]?.ToString(),
                                Description = vodObj["description"]?.ToString(),
                                Cmd = vodObj["cmd"]?.ToString(),
                                Screenshot = screenshotUrl,
                                Year = vodObj["year"]?.ToString(),
                                Director = vodObj["director"]?.ToString(),
                                Actors = vodObj["actors"]?.ToString(),
                                Rating = vodObj["rating_imdb"]?.ToString(),
                                Duration = vodObj["duration"]?.ToString(),
                                Hd = vodObj["hd"]?.ToObject<int?>(),
                                Fav = vodObj["fav"]?.ToObject<int?>(),
                                Category = vodObj["category_title"]?.ToString()
                            });
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Parsed {items.Count} VOD items");
                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\n========== GET VOD ERROR ==========");
                System.Diagnostics.Debug.WriteLine($"Error Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Error Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"==========================================\n");
                return new List<VodItem>();
            }
        }

        public void ClearCache()
        {
            System.Diagnostics.Debug.WriteLine("Clearing all cached data");
            _cachedChannels = null;
            _cachedGenres = null;
            _channelsCacheTime = DateTime.MinValue;
            _genresCacheTime = DateTime.MinValue;
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

        public async Task LogoutAsync()
        {
            try
            {
                if (_currentProfile == null)
                    return;

                System.Diagnostics.Debug.WriteLine("Attempting logout...");
                var url = BuildUrl("logout", "stb");
                await _httpClient.GetStringAsync(url);
                System.Diagnostics.Debug.WriteLine("Logout successful");

                _authToken = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error (non-critical): {ex.Message}");
            }
        }
    }
}
