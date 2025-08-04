using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LevittUI.Models;

namespace LevittUI.Services
{
    public interface IHomeAutomationService
    {
        Task<bool> LoginAsync(string username, string password);
        Task<List<Room>> GetRoomsAsync();
        Task<bool> SetAirConditioningAsync(int roomId, bool isOn);
        Task<bool> SetTargetTemperatureAsync(int roomId, double temperature);
        Task<bool> SetBlindPositionAsync(int roomId, BlindPosition position);
        Task<bool> SetHouseBlindsAsync(string command); // "UP" or "DOWN" commands
        Task LogoutAsync();
        bool IsLoggedIn { get; }
    }

    public class HomeAutomationService : IHomeAutomationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeAutomationService> _logger;
        private readonly IConfigurationService _configurationService;
        private string _sessionId = string.Empty;
        private readonly string _baseUrl;

        // Based on the actual setup:
        // 5 rooms for temperature monitoring (3 bedrooms + 1 living room + 1 hallway)
        // 1 A/C control for the whole house (status: 1377, control: 1083)
        // 1 blinds control for the whole house
        private readonly Dictionary<int, Room> _roomMappings = new()
        {
            {
                1, new Room
                {
                    Id = 1,
                    Name = "Living Room",
                    TemperatureSensorId = 1391,
                    TargetTempSensorId = 775,
                    AcControlId = 1083,  // Shared A/C control for whole house
                    BlindControlId = 816 // Shared blind control for whole house (need to confirm)
                }
            },
            {
                2, new Room
                {
                    Id = 2,
                    Name = "Room 1",
                    TemperatureSensorId = 1398,
                    TargetTempSensorId = 816,
                    AcControlId = 1083,  // Same A/C control (whole house)
                    BlindControlId = 816 // Same blind control (whole house)
                }
            },
            {
                3, new Room
                {
                    Id = 3,
                    Name = "Room 2",
                    TemperatureSensorId = 1405,
                    TargetTempSensorId = 857,
                    AcControlId = 1083,  // Same A/C control (whole house)
                    BlindControlId = 816 // Same blind control (whole house)
                }
            },
            {
                4, new Room
                {
                    Id = 4,
                    Name = "Room 3",
                    TemperatureSensorId = 1412,
                    TargetTempSensorId = 898,
                    AcControlId = 1083,  // Same A/C control (whole house)
                    BlindControlId = 816 // Same blind control (whole house)
                }
            },
            {
                5, new Room
                {
                    Id = 5,
                    Name = "Hallway",
                    TemperatureSensorId = 1419,
                    TargetTempSensorId = 940,
                    AcControlId = 1083,  // Same A/C control (whole house)
                    BlindControlId = 816 // Same blind control (whole house)
                }
            }
        };

        // A/C Status Sensor ID (returns "Encendido"/"Apagado")
        private const int AC_STATUS_SENSOR_ID = 1377;

        public bool IsLoggedIn => !string.IsNullOrEmpty(_sessionId);

        public HomeAutomationService(HttpClient httpClient, ILogger<HomeAutomationService> logger, IConfigurationService configurationService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configurationService = configurationService;
            _baseUrl = $"http://{_configurationService.ServerAddress}";
            
            // Configure HttpClient timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                // Based on Fiddler trace, the system uses main.app with section=auth for authentication
                // First, get the main page to get a session
                var mainPageResponse = await _httpClient.GetAsync($"{_baseUrl}/main.app");
                if (!mainPageResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to access main page");
                    return false;
                }

                // Extract session ID from response headers or content
                var sessionId = ExtractSessionId(mainPageResponse);
                if (string.IsNullOrEmpty(sessionId))
                {
                    // Generate a session ID if we can't extract one
                    sessionId = Guid.NewGuid().ToString();
                }

                // Now try to authenticate using the auth section
                // Based on Fiddler trace: main.app?SessionId=...&section=auth
                var authUrl = $"{_baseUrl}/main.app?SessionId={sessionId}&section=auth";
                var authResponse = await _httpClient.GetAsync(authUrl);
                
                if (!authResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to access auth section");
                    return false;
                }

                // Try to POST credentials to the auth endpoint
                // Based on the HTML form: action="/main.app?SessionId=...&section=auth" method="post"
                // Form fields are: user (text) and pwd (password)
                var loginData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("user", username),
                    new KeyValuePair<string, string>("pwd", password),
                    new KeyValuePair<string, string>("login", "Conectar") // Submit button value
                });

                var loginUrl = $"{_baseUrl}/main.app?SessionId={sessionId}&section=auth";
                var loginResponse = await _httpClient.PostAsync(loginUrl, loginData);
                
                if (loginResponse.IsSuccessStatusCode)
                {
                    // Check if we got a new session ID from the login response
                    var newSessionId = ExtractSessionId(loginResponse);
                    if (!string.IsNullOrEmpty(newSessionId))
                    {
                        _sessionId = newSessionId;
                    }
                    else
                    {
                        _sessionId = sessionId;
                    }
                    
                    _logger.LogInformation("Login successful with session: {SessionId}", _sessionId);
                    
                    // Update HttpClient default headers to include the session cookie
                    _httpClient.DefaultRequestHeaders.Remove("Cookie");
                    _httpClient.DefaultRequestHeaders.Add("Cookie", $"SessionId={_sessionId}; Path=/");
                    
                    return true;
                }
                else
                {
                    _logger.LogError("Login failed with status: {StatusCode}", loginResponse.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return false;
            }
        }

        private string ExtractSessionId(HttpResponseMessage response)
        {
            // Try to extract session ID from Set-Cookie header
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    if (cookie.Contains("SessionId="))
                    {
                        var startIndex = cookie.IndexOf("SessionId=") + "SessionId=".Length;
                        var endIndex = cookie.IndexOf(';', startIndex);
                        if (endIndex == -1) endIndex = cookie.Length;
                        return cookie.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }
            
            return string.Empty;
        }

        public async Task<List<Room>> GetRoomsAsync()
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            try
            {
                var rooms = new List<Room>();

                // Get AC status for all rooms (shared A/C system)
                var acStatus = await GetDataPointAsync(AC_STATUS_SENSOR_ID);
                bool isAcOn = false;
                if (acStatus?.Value != null)
                {
                    isAcOn = acStatus.Value.ToLower() == "encendido" || acStatus.Value == "1";
                }

                foreach (var roomMapping in _roomMappings.Values)
                {
                    var room = new Room
                    {
                        Id = roomMapping.Id,
                        Name = roomMapping.Name,
                        TemperatureSensorId = roomMapping.TemperatureSensorId,
                        TargetTempSensorId = roomMapping.TargetTempSensorId,
                        AcControlId = roomMapping.AcControlId,
                        BlindControlId = roomMapping.BlindControlId
                    };

                    // Get current temperature
                    var currentTemp = await GetDataPointAsync(room.TemperatureSensorId);
                    if (currentTemp?.Value != null && double.TryParse(currentTemp.Value, out var temp))
                    {
                        room.CurrentTemperature = temp;
                    }
                    else
                    {
                        room.CurrentTemperature = double.NaN; // Will be displayed as "--"
                    }

                    // Get target temperature
                    var targetTemp = await GetDataPointAsync(room.TargetTempSensorId);
                    if (targetTemp?.Value != null && double.TryParse(targetTemp.Value, out var target))
                    {
                        room.TargetTemperature = target;
                    }
                    else
                    {
                        room.TargetTemperature = double.NaN; // Will be displayed as "--"
                    }

                    // Set AC status (same for all rooms since it's house-wide)
                    room.IsAcOn = isAcOn;

                    // Get blind position
                    var blindStatus = await GetDataPointAsync(room.BlindControlId);
                    if (blindStatus?.Value != null)
                    {
                        room.BlindPosition = ParseBlindPosition(blindStatus.Value);
                    }
                    else
                    {
                        room.BlindPosition = BlindPosition.Unknown; // Will need to add this enum value
                    }

                    room.LastUpdated = DateTime.Now;
                    rooms.Add(room);
                }

                return rooms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get rooms data");
                return new List<Room>();
            }
        }

        private async Task<DataPointResponse?> GetDataPointAsync(int plantItemId)
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var url = $"{_baseUrl}/ajax.app?SessionId={_sessionId}&service=getDp&plantItemId={plantItemId}&_={timestamp}";
                
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get data point {PlantItemId} - HTTP {StatusCode}", plantItemId, response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Data point {PlantItemId} response: {Content}", plantItemId, content);
                
                try
                {
                    // Parse the JSON response
                    // Expected format: {"service":"getDp","plantItemId":"1391","value":"Access denied","unit":"Web"}
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (jsonResponse.TryGetProperty("value", out var valueElement))
                    {
                        var value = valueElement.GetString();
                        
                        // Check for access denied or other error conditions
                        if (value == "Access denied" || string.IsNullOrEmpty(value))
                        {
                            _logger.LogWarning("Access denied or empty value for plant item {PlantItemId}", plantItemId);
                            return new DataPointResponse
                            {
                                PlantItemId = plantItemId,
                                Value = null, // This will be handled as "--" in the UI
                                Timestamp = DateTime.Now,
                                Status = "Access denied"
                            };
                        }
                        
                        // Extract unit if available
                        string? unit = null;
                        if (jsonResponse.TryGetProperty("unit", out var unitElement))
                        {
                            unit = unitElement.GetString();
                        }
                        
                        return new DataPointResponse
                        {
                            PlantItemId = plantItemId,
                            Value = value,
                            Unit = unit,
                            Timestamp = DateTime.Now,
                            Status = "OK"
                        };
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON response for plant item {PlantItemId}: {Content}", plantItemId, content);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data point {PlantItemId}", plantItemId);
                return null;
            }
        }

        private string GetMockValue(int plantItemId)
        {
            // Generate realistic mock values for testing
            var random = new Random();
            
            // Temperature sensors (assume IDs in 1300+ range are temperature)
            if (plantItemId >= 1300)
            {
                return (20 + random.NextDouble() * 10).ToString("F1"); // 20-30°C
            }
            
            // Lower IDs might be controls (0/1)
            if (plantItemId < 1000)
            {
                return random.Next(2).ToString(); // 0 or 1
            }
            
            return random.Next(100).ToString();
        }

        private BlindPosition ParseBlindPosition(string? value)
        {
            return value switch
            {
                "0" => BlindPosition.Up,
                "1" => BlindPosition.Down,
                "2" => BlindPosition.Partial,
                _ => BlindPosition.Unknown
            };
        }

        public async Task<bool> SetAirConditioningAsync(int roomId, bool isOn)
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            try
            {
                // A/C control is house-wide, so roomId doesn't matter
                // Use dialog.app endpoint with multipart form data
                return await SendAcCommandAsync(isOn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set AC for room {RoomId}", roomId);
                return false;
            }
        }

        private async Task<bool> SendAcCommandAsync(bool isOn)
        {
            try
            {
                _logger.LogInformation("A/C Control: Attempting to {Action} A/C", isOn ? "turn on" : "turn off");

                // Step 1: Initialize dialog wait with GET request to main.app
                var waitUrl = $"{_baseUrl}/main.app?SessionId={_sessionId}&section=dialog&action=wait&id=1083";
                _logger.LogDebug("A/C Control: Step 1 - GET {WaitUrl}", waitUrl);

                var waitRequest = new HttpRequestMessage(HttpMethod.Get, waitUrl);
                waitRequest.Headers.Add("Referer", $"{_baseUrl}/main.app?SessionId={_sessionId}&section=auth");

                var waitResponse = await _httpClient.SendAsync(waitRequest);
                if (!waitResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("A/C Control: Dialog wait failed with status {StatusCode}", waitResponse.StatusCode);
                    return false;
                }

                var waitContent = await waitResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("A/C Control: Wait response content length: {Length}", waitContent.Length);

                // Step 2: Load dialog form with GET request to dialog.app
                var dialogUrl = $"{_baseUrl}/dialog.app?SessionId={_sessionId}&action=new&id=1083";
                _logger.LogDebug("A/C Control: Step 2 - GET {DialogUrl}", dialogUrl);

                var dialogRequest = new HttpRequestMessage(HttpMethod.Get, dialogUrl);
                dialogRequest.Headers.Add("Referer", waitUrl);

                var dialogResponse = await _httpClient.SendAsync(dialogRequest);
                if (!dialogResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("A/C Control: Dialog load failed with status {StatusCode}", dialogResponse.StatusCode);
                    return false;
                }

                var dialogContent = await dialogResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("A/C Control: Dialog response content length: {Length}", dialogContent.Length);

                // Step 3: Submit the command via POST
                var postUrl = $"{_baseUrl}/dialog.app?SessionId={_sessionId}";
                _logger.LogDebug("A/C Control: Step 3 - POST {PostUrl}", postUrl);

                // Create form data manually to match browser format exactly
                var boundary = "----WebKitFormBoundary" + Guid.NewGuid().ToString("N")[..16];
                var formDataBuilder = new StringBuilder();
                
                // Add form fields in the exact format the browser uses
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"action\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine("update");
                
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"DpDescription\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine("COzwValME8");
                
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"id\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine("1083");
                
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"value\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine(isOn ? "1" : "2");
                
                formDataBuilder.AppendLine($"--{boundary}--");

                var formDataString = formDataBuilder.ToString();
                var formContent = new StringContent(formDataString, System.Text.Encoding.UTF8);
                formContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data")
                {
                    Parameters = { new System.Net.Http.Headers.NameValueHeaderValue("boundary", boundary) }
                };

                var postRequest = new HttpRequestMessage(HttpMethod.Post, postUrl)
                {
                    Content = formContent
                };
                postRequest.Headers.Add("Referer", dialogUrl);

                _logger.LogDebug("A/C Control: Sending form data with boundary: {Boundary}", boundary);
                _logger.LogDebug("A/C Control: Form data: {Content}", formDataString);

                var postResponse = await _httpClient.SendAsync(postRequest);
                var responseContent = await postResponse.Content.ReadAsStringAsync();

                _logger.LogDebug("A/C Control: POST response status: {StatusCode}", postResponse.StatusCode);
                _logger.LogDebug("A/C Control: POST response content: {Content}", responseContent.Length > 500 
                    ? responseContent.Substring(0, 500) + "..." : responseContent);

                if (postResponse.IsSuccessStatusCode)
                {
                    // Check if response contains success indicator (cleanupDialog function call)
                    if (responseContent.Contains("cleanupDialog"))
                    {
                        _logger.LogInformation("A/C Control: Command sent successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("A/C Control: Command failed - no cleanup dialog found in response");
                        return false;
                    }
                }

                _logger.LogError("A/C Control: POST request failed with status {StatusCode}", postResponse.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A/C Control Error");
                return false;
            }
        }

        private void AddHiddenFormFields(string htmlContent, MultipartFormDataContent formData)
        {
            try
            {
                // Simple regex to find hidden input fields
                var hiddenInputPattern = @"<input[^>]*type\s*=\s*[""']hidden[""'][^>]*>";
                var namePattern = @"name\s*=\s*[""']([^""']*)[""']";
                var valuePattern = @"value\s*=\s*[""']([^""']*)[""']";

                var hiddenInputs = System.Text.RegularExpressions.Regex.Matches(htmlContent, hiddenInputPattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match input in hiddenInputs)
                {
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(input.Value, namePattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var valueMatch = System.Text.RegularExpressions.Regex.Match(input.Value, valuePattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (nameMatch.Success && valueMatch.Success)
                    {
                        var fieldName = nameMatch.Groups[1].Value;
                        var fieldValue = valueMatch.Groups[1].Value;
                        
                        // Skip fields we're already setting explicitly
                        if (fieldName != "action" && fieldName != "DpDescription" && fieldName != "id" && fieldName != "value")
                        {
                            formData.Add(new StringContent(fieldValue), fieldName);
                            _logger.LogDebug("Added hidden field: {Name} = {Value}", fieldName, fieldValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse hidden form fields");
                // Continue without hidden fields - not critical
            }
        }

        public async Task<bool> SetTargetTemperatureAsync(int roomId, double temperature)
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            try
            {
                if (!_roomMappings.TryGetValue(roomId, out var room))
                    return false;

                return await SendCommandAsync(room.TargetTempSensorId, temperature.ToString("F1"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set target temperature for room {RoomId}", roomId);
                return false;
            }
        }

        public async Task<bool> SetHouseBlindsAsync(string command)
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            if (command != "UP" && command != "DOWN")
                throw new ArgumentException("Command must be 'UP' or 'DOWN'", nameof(command));

            try
            {
                var isUp = command == "UP";
                return await SendBlindsCommandAsync(isUp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send house blinds command {Command}", command);
                return false;
            }
        }

        private async Task<bool> SendBlindsCommandAsync(bool isUp)
        {
            try
            {
                _logger.LogInformation("Blinds Control: Attempting to move blinds {Direction}", isUp ? "up" : "down");

                // Use the correct dialog ID for blinds
                var dialogId = "1032";

                // Step 1: Initialize dialog wait with GET request to main.app
                var waitUrl = $"{_baseUrl}/main.app?SessionId={_sessionId}&section=dialog&action=wait&id={dialogId}";
                _logger.LogDebug("Blinds Control: Step 1 - GET {WaitUrl}", waitUrl);

                var waitRequest = new HttpRequestMessage(HttpMethod.Get, waitUrl);
                waitRequest.Headers.Add("Referer", $"{_baseUrl}/main.app?SessionId={_sessionId}&section=auth");

                var waitResponse = await _httpClient.SendAsync(waitRequest);
                if (!waitResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Blinds Control: Dialog wait failed with status {StatusCode}", waitResponse.StatusCode);
                    return false;
                }

                // Step 2: Load dialog form with GET request to dialog.app
                var dialogUrl = $"{_baseUrl}/dialog.app?SessionId={_sessionId}&action=new&id={dialogId}";
                _logger.LogDebug("Blinds Control: Step 2 - GET {DialogUrl}", dialogUrl);

                var dialogRequest = new HttpRequestMessage(HttpMethod.Get, dialogUrl);
                dialogRequest.Headers.Add("Referer", waitUrl);

                var dialogResponse = await _httpClient.SendAsync(dialogRequest);
                if (!dialogResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Blinds Control: Dialog load failed with status {StatusCode}", dialogResponse.StatusCode);
                    return false;
                }

                var dialogContent = await dialogResponse.Content.ReadAsStringAsync();
                _logger.LogDebug("Blinds Control: Dialog response content length: {Length}", dialogContent.Length);

                // Step 3: Submit the command via POST
                var postUrl = $"{_baseUrl}/dialog.app?SessionId={_sessionId}";
                _logger.LogDebug("Blinds Control: Step 3 - POST {PostUrl}", postUrl);

                // Create form data matching browser format (same as A/C control)
                var boundary = Guid.NewGuid().ToString();
                var formDataBuilder = new StringBuilder();
                
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"action\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine("update");
                
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"DpDescription\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine("COzwValME8");
                
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"id\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine(dialogId);
                
                formDataBuilder.AppendLine($"--{boundary}");
                formDataBuilder.AppendLine("Content-Disposition: form-data; name=\"value\"");
                formDataBuilder.AppendLine();
                formDataBuilder.AppendLine(isUp ? "1" : "2"); // 1 = UP, 2 = DOWN
                
                formDataBuilder.AppendLine($"--{boundary}--");

                var formDataString = formDataBuilder.ToString();
                var formContent = new StringContent(formDataString, System.Text.Encoding.UTF8);
                formContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data")
                {
                    Parameters = { new System.Net.Http.Headers.NameValueHeaderValue("boundary", boundary) }
                };

                var postRequest = new HttpRequestMessage(HttpMethod.Post, postUrl)
                {
                    Content = formContent
                };
                postRequest.Headers.Add("Referer", dialogUrl);

                _logger.LogDebug("Blinds Control: Sending form data with boundary: {Boundary}", boundary);
                _logger.LogDebug("Blinds Control: Form data: {Content}", formDataString);

                var postResponse = await _httpClient.SendAsync(postRequest);
                var responseContent = await postResponse.Content.ReadAsStringAsync();

                _logger.LogDebug("Blinds Control: POST response status: {StatusCode}", postResponse.StatusCode);
                _logger.LogDebug("Blinds Control: POST response content: {Content}", 
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

                if (postResponse.IsSuccessStatusCode)
                {
                    // Check if response contains success indicator (cleanupDialog function call)
                    if (responseContent.Contains("cleanupDialog"))
                    {
                        _logger.LogInformation("Blinds Control: Command sent successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Blinds Control: Command failed - no cleanup dialog found in response");
                        return false;
                    }
                }

                _logger.LogError("Blinds Control: POST request failed with status {StatusCode}", postResponse.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blinds Control Error");
                return false;
            }
        }

        public async Task<bool> SetBlindPositionAsync(int roomId, BlindPosition position)
        {
            if (!IsLoggedIn)
                throw new InvalidOperationException("Not logged in");

            try
            {
                if (!_roomMappings.TryGetValue(roomId, out var room))
                    return false;

                var value = position switch
                {
                    BlindPosition.Up => "0",
                    BlindPosition.Down => "1",
                    BlindPosition.Partial => "2",
                    _ => "0"
                };

                return await SendCommandAsync(room.BlindControlId, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set blind position for room {RoomId}", roomId);
                return false;
            }
        }

        private async Task<bool> SendCommandAsync(int plantItemId, string value)
        {
            try
            {
                // This would need to be determined from actual API analysis
                // The command endpoint might be different
                var commandData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("sessionId", _sessionId),
                    new KeyValuePair<string, string>("plantItemId", plantItemId.ToString()),
                    new KeyValuePair<string, string>("value", value)
                });

                var response = await _httpClient.PostAsync($"{_baseUrl}/ajax.app", commandData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command to plant item {PlantItemId}", plantItemId);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                if (IsLoggedIn)
                {
                    await _httpClient.GetAsync($"{_baseUrl}/logout?sessionId={_sessionId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
            finally
            {
                _sessionId = string.Empty;
            }
        }
    }
}
