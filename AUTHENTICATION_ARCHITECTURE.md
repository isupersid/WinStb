# Stalker Portal Authentication Architecture

## Overview

The Stalker Portal uses a **two-phase authentication system** based on MAC address identification and token-based authorization. It's designed to emulate physical Set-Top Box (STB) devices like MAG boxes.

---

## Authentication Flow Diagram

```
┌─────────────┐                                    ┌──────────────────┐
│   WinStb    │                                    │ Stalker Portal   │
│   Client    │                                    │     Server       │
└──────┬──────┘                                    └────────┬─────────┘
       │                                                    │
       │  1. HANDSHAKE REQUEST                             │
       │  ─────────────────────────────────────────────>   │
       │     GET /portal.php?type=stb&action=handshake    │
       │                                                    │
       │     Headers:                                      │
       │     • User-Agent: MAG200 browser                  │
       │     • X-User-Agent: Model: MAG254                 │
       │     • Cookie: mac=00:1A:79:XX:XX:XX               │
       │                                                    │
       │                        [Server validates MAC]     │
       │                        [Checks whitelist]         │
       │                        [Generates token]          │
       │                                                    │
       │  2. HANDSHAKE RESPONSE                            │
       │  <─────────────────────────────────────────────   │
       │     { "js": { "token": "ABC123..." } }            │
       │                                                    │
       │  [Store token in memory]                          │
       │                                                    │
       │  3. GET PROFILE REQUEST (Verify)                  │
       │  ─────────────────────────────────────────────>   │
       │     GET /portal.php?type=stb&action=get_profile  │
       │                                                    │
       │     Headers:                                      │
       │     • Authorization: Bearer ABC123...             │
       │     • Cookie: mac=00:1A:79:XX:XX:XX               │
       │                                                    │
       │                        [Verify token]             │
       │                        [Return profile data]      │
       │                                                    │
       │  4. PROFILE RESPONSE                              │
       │  <─────────────────────────────────────────────   │
       │     { "js": { profile data... } }                 │
       │                                                    │
       │  ✅ AUTHENTICATED                                 │
       │                                                    │
       │  5. ALL SUBSEQUENT API CALLS                      │
       │  ─────────────────────────────────────────────>   │
       │     Authorization: Bearer ABC123...               │
       │                                                    │
```

---

## Phase 1: Initial Handshake (MAC-Based Identification)

### Purpose
Identify the device using its MAC address and obtain an authentication token.

### Request Details

**Endpoint:**
```
GET http://portal.com/stalker_portal/server/load.php?type=stb&action=handshake&JsHttpRequest=1-xml
```

**Critical Headers:**

1. **User-Agent** - Identifies the browser/platform
   ```
   Mozilla/5.0 (QtEmbedded; U; Linux; C) AppleWebKit/533.3 (KHTML, like Gecko) MAG200 stbapp ver: 2 rev: 250 Safari/533.3
   ```
   - `QtEmbedded`: Embedded Qt framework (used by MAG boxes)
   - `MAG200`: Device model identifier
   - Tells server this is a legitimate STB device

2. **X-User-Agent** - Device-specific information
   ```
   Model: MAG254; Link: Ethernet
   ```
   - `Model`: Specific STB model (MAG254, MAG322, etc.)
   - `Link`: Connection type (Ethernet, WiFi)

3. **Cookie** - Contains the MAC address and settings
   ```
   mac=00:1A:79:XX:XX:XX; stb_lang=en; timezone=UTC
   ```
   - `mac`: **Primary identifier** - This is how the portal knows who you are
   - `stb_lang`: Interface language preference
   - `timezone`: User's timezone

### MAC Address Format

**Required Format:** `00:1A:79:XX:XX:XX`

**Why this format?**
- `00:1A:79` is the official OUI (Organizationally Unique Identifier) for **Infomir** (manufacturer of MAG boxes)
- Stalker Portal middleware recognizes this prefix as legitimate MAG devices
- Many portals **whitelist only this prefix** for security

**In the code:**
```csharp
// From Profile.cs - Auto-generates valid MAC
var random = new Random();
MacAddress = $"00:1A:79:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}";
```

### Server-Side Validation (What the Portal Does)

1. **Extracts MAC address** from Cookie header
2. **Checks MAC whitelist** - Is this MAC registered?
3. **Validates OUI prefix** - Is it a recognized device type?
4. **Checks subscription status** - Is the account active?
5. **Checks device limits** - Too many concurrent connections?
6. **Generates authentication token** if all checks pass

### Response Format

**Success:**
```json
{
  "js": {
    "token": "C00F7332ED272F00D5FD3E82F567A282",
    "random": "some_random_value"
  }
}
```

**Failure:**
```json
{
  "js": {
    "error": "Failed",
    "msg": "Subscription expired"
  }
}
```

---

## Phase 2: Token-Based Authorization (Bearer Auth)

### Purpose
Use the obtained token for all subsequent API calls (OAuth 2.0 Bearer Token pattern - RFC 6750).

### Token Characteristics

**Format:**
- 32-character hexadecimal string (in most implementations)
- Example: `C00F7332ED272F00D5FD3E82F567A282`

**Lifetime:**
- Typically **session-based** (valid until user disconnects)
- Some portals implement **time-based expiry** (e.g., 24 hours)
- Token invalidates on logout or session timeout

**Storage:**
- Kept in **memory only** during app runtime
- Not persisted to disk (security best practice)
- Must re-authenticate after app restart

### Authorization Header

**All API calls after handshake include:**
```
Authorization: Bearer C00F7332ED272F00D5FD3E82F567A282
```

**In the code:**
```csharp
// From StalkerPortalClient.cs
_httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
```

### Example Authenticated Request

**Get Channels:**
```
GET /portal.php?type=itv&action=get_ordered_list&JsHttpRequest=1-xml

Headers:
  Authorization: Bearer C00F7332ED272F00D5FD3E82F567A282
  Cookie: mac=00:1A:79:XX:XX:XX; stb_lang=en; timezone=UTC
  User-Agent: Mozilla/5.0 (QtEmbedded...) MAG200...
  X-User-Agent: Model: MAG254; Link: Ethernet
```

**Note:** The MAC address Cookie is **still included** in all requests even though we have a token. This is for:
1. Session continuity
2. Analytics/logging on the server
3. Device tracking

---

## Code Implementation

### 1. Authentication Method

**Location:** `WinStb\Services\StalkerPortalClient.cs`

```csharp
public async Task<bool> AuthenticateAsync(Profile profile)
{
    _currentProfile = profile;

    try
    {
        // PHASE 1: Handshake with MAC address
        var handshakeUrl = BuildUrl("handshake", "stb");

        // Configure headers with MAC address and device info
        ConfigureHeaders();

        // Send handshake request
        var response = await _httpClient.GetStringAsync(handshakeUrl);
        var jsonResponse = JObject.Parse(response);

        // Extract token from response
        _authToken = jsonResponse["js"]?["token"]?.ToString();

        if (string.IsNullOrEmpty(_authToken))
            return false;

        // PHASE 2: Verify with Bearer token
        var profileUrl = BuildUrl("get_profile", "stb");
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Verify connection works
        var profileResponse = await _httpClient.GetStringAsync(profileUrl);

        return !string.IsNullOrEmpty(profileResponse);
    }
    catch (Exception ex)
    {
        return false;
    }
}
```

### 2. Header Configuration

```csharp
private void ConfigureHeaders()
{
    _httpClient.DefaultRequestHeaders.Clear();

    // Emulate MAG200 browser
    var userAgent = "Mozilla/5.0 (QtEmbedded; U; Linux; C) " +
                    "AppleWebKit/533.3 (KHTML, like Gecko) " +
                    "MAG200 stbapp ver: 2 rev: 250 Safari/533.3";

    // Device model and connection
    var xUserAgent = $"Model: {_currentProfile.StbType}; Link: Ethernet";

    _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
    _httpClient.DefaultRequestHeaders.Add("X-User-Agent", xUserAgent);

    // MAC address in cookie
    var cookie = $"mac={_currentProfile.MacAddress}; stb_lang=en; timezone={_currentProfile.TimeZone}";
    _httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
}
```

### 3. URL Building

```csharp
private string BuildUrl(string action, string type, Dictionary<string, string> parameters = null)
{
    var baseUrl = _currentProfile.PortalUrl.TrimEnd('/');

    // Ensure path ends with /stalker_portal
    if (!baseUrl.EndsWith("stalker_portal"))
        baseUrl = $"{baseUrl}/stalker_portal";

    // Build URL with incrementing request ID
    var url = $"{baseUrl}/server/load.php?type={type}&action={action}&JsHttpRequest={_requestId++}-xml";

    // Add additional parameters
    if (parameters != null)
    {
        foreach (var param in parameters)
            url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
    }

    return url;
}
```

---

## Security Architecture

### 1. Device Identification Layer

**MAC Address as Primary Key:**
- MAC address = unique device identifier
- Service provider whitelists specific MAC addresses
- Each MAC = 1 subscription
- Changing MAC = different device = blocked (unless provider updates whitelist)

**Why MAC-based?**
- **Hardware-tied licensing** - Can't easily share accounts
- **Device limits** - Provider controls how many devices per account
- **Fraud prevention** - Can't create unlimited accounts
- **Session management** - Track which devices are active

### 2. Token Layer

**Bearer Token (OAuth 2.0):**
- Short-lived session credentials
- Prevents MAC address from being visible in every request body
- Can be invalidated server-side
- Reduces risk if token is intercepted (limited lifetime)

**Security Properties:**
- **Stateless** - Server doesn't need to store session state
- **Revocable** - Can invalidate tokens without changing MAC
- **Time-limited** - Automatic expiry reduces exposure window

### 3. Device Emulation Layer

**User-Agent Spoofing:**
- Makes the app appear as a legitimate MAG box
- Some portals **reject** requests from non-MAG user agents
- Bypass device-type restrictions

**Headers Checked:**
- `User-Agent`: Browser/device type
- `X-User-Agent`: Device model
- `Cookie`: MAC address

If any of these are missing or invalid, the portal may reject the request.

---

## Security Considerations & Vulnerabilities

### Current Weaknesses

1. **SSL Certificate Validation Disabled**
   ```csharp
   ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
   ```
   - **Why**: Many IPTV portals use self-signed certificates
   - **Risk**: Vulnerable to man-in-the-middle attacks
   - **Mitigation**: Could add option to enable/disable per profile

2. **Plain Text Storage**
   ```csharp
   // Profiles stored as JSON in ApplicationData
   await FileIO.WriteTextAsync(file, json);
   ```
   - **Risk**: MAC addresses and URLs visible in file system
   - **Mitigation**: Use Windows.Security.Credentials.PasswordVault

3. **Token in Memory Only**
   - **Good**: Not persisted to disk
   - **Bad**: Must re-authenticate on every app launch
   - **Could improve**: Secure token persistence with expiry

4. **No Rate Limiting**
   - App doesn't implement retry limits or exponential backoff
   - Could be blocked if too many failed auth attempts

### Provider-Side Security

**What Service Providers Do:**

1. **MAC Whitelist Management**
   - Only registered MACs can connect
   - Providers manually add/remove MACs
   - Usually limit to 1-3 devices per account

2. **Concurrent Connection Limits**
   - Detect if same MAC connects from multiple IPs
   - Block or throttle if exceeded

3. **Session Monitoring**
   - Track token usage patterns
   - Detect abnormal behavior (too many requests, geographic anomalies)

4. **Device Fingerprinting**
   - Check User-Agent consistency
   - Verify headers match expected device type
   - Some portals check additional device parameters (serial, device_id)

---

## Additional Device Parameters

### Optional Authentication Fields

Some portals support additional device identifiers:

```csharp
// From Profile.cs
public string SerialNumber { get; set; }      // STB serial number
public string DeviceId { get; set; }          // Unique device identifier
public string DeviceId2 { get; set; }         // Secondary device ID
public string Signature { get; set; }         // Device signature/hash
```

**How They Work:**
1. First connection: Client sends these parameters
2. Portal associates them with the MAC address
3. Future connections: Portal expects same values
4. **Lock-in effect**: Makes it harder for multiple clients to use same MAC

**Implementation:**
```csharp
// These would be sent in get_profile request
var parameters = new Dictionary<string, string>
{
    { "sn", profile.SerialNumber },
    { "device_id", profile.DeviceId },
    { "device_id2", profile.DeviceId2 },
    { "signature", profile.Signature }
};
```

Currently not implemented in the code, but the Profile model supports them for future use.

---

## Authentication State Management

### State Lifecycle

```
┌─────────────┐
│   App Start │
└──────┬──────┘
       │
       v
┌─────────────────┐
│ No Profile      │  ← User creates/selects profile
│ Not Connected   │
└──────┬──────────┘
       │ User clicks "Connect"
       v
┌──────────────────┐
│ Authenticating   │ ← Handshake + get_profile
│ (Loading)        │
└──────┬───────────┘
       │
       ├─ Success ──> ┌────────────────┐
       │              │ Connected      │
       │              │ Token stored   │
       │              │ Ready to browse│
       │              └────────┬───────┘
       │                       │
       │                       v
       │              ┌────────────────┐
       │              │ All API calls  │
       │              │ use Bearer     │
       │              │ token          │
       │              └────────┬───────┘
       │                       │
       │              Token expires or
       │              app closes
       │                       │
       │                       v
       │              ┌────────────────┐
       │              │ Disconnected   │
       │              └────────────────┘
       │
       └─ Failure ──> ┌────────────────┐
                      │ Connection     │
                      │ Failed         │
                      │ Show error     │
                      └────────────────┘
```

### Code: State Tracking

```csharp
// From MainViewModel.cs
public class MainViewModel : BaseViewModel
{
    private Profile _currentProfile;      // Currently selected profile
    private bool _isConnected;           // Authentication status
    private string _statusMessage;        // UI status display

    public Profile CurrentProfile { get; set; }
    public bool IsConnected { get; set; }
    public string StatusMessage { get; set; }

    public StalkerPortalClient PortalClient { get; }  // Holds the token
}
```

### Disconnection Scenarios

**Token becomes invalid when:**
1. User closes the app (token not persisted)
2. Server-side session expires (time-based)
3. Provider revokes MAC access (subscription ends)
4. Too many concurrent connections
5. Portal detects suspicious activity

**How app handles it:**
- All API calls check for authentication errors
- If token invalid: Show "Connection Failed"
- User must re-authenticate (click "Connect" again)

---

## Comparison to Other IPTV Authentication Methods

### M3U Playlist URLs

```
http://server.com:8000/get.php?username=user&password=pass&type=m3u
```

**Differences:**
- ✅ Simple: Just a URL with credentials
- ❌ No session management
- ❌ Credentials in every request
- ❌ No device binding

### Xtream Codes API

```
POST /player_api.php?username=user&password=pass&action=get_live_streams
```

**Differences:**
- ✅ Username/password authentication
- ✅ Token-based (similar)
- ❌ Not device-bound (easier to share)
- ❌ No MAC address requirement

### Stalker Portal (This App)

```
MAC + Token authentication
```

**Advantages:**
- ✅ Device-bound (harder to share)
- ✅ Hardware-level identification
- ✅ Provider controls exact devices
- ✅ Matches real STB behavior

**Disadvantages:**
- ❌ More complex authentication
- ❌ Requires MAC whitelisting by provider
- ❌ Less flexible for users

---

## Testing Authentication

### How to Test

1. **Valid Credentials:**
   ```
   Portal URL: http://yourprovider.com/stalker_portal
   MAC: 00:1A:79:AA:BB:CC (whitelisted by provider)
   Expected: Token returned, connection successful
   ```

2. **Invalid MAC:**
   ```
   MAC: 00:1A:79:XX:XX:XX (not whitelisted)
   Expected: "Subscription expired" or "Access denied"
   ```

3. **Wrong Portal URL:**
   ```
   Portal URL: http://wrong.com
   Expected: Connection timeout or 404 error
   ```

4. **Expired Subscription:**
   ```
   MAC: Valid but expired
   Expected: "Subscription expired" in response
   ```

### Debug Logging

Enable debug output to see authentication flow:
```csharp
// In StalkerPortalClient.cs - already included
System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
```

View in Visual Studio:
- `View → Output Window`
- Select "Debug" from dropdown
- See real-time authentication attempts

---

## Summary

### Authentication Flow (Simplified)
1. **User enters**: Portal URL + MAC address
2. **App sends**: Handshake request with MAC in Cookie
3. **Portal checks**: Is this MAC whitelisted?
4. **Portal returns**: Authentication token
5. **App uses**: Bearer token for all subsequent requests
6. **Portal validates**: Token on each request

### Key Components
- **MAC Address**: Primary identifier (hardware-level)
- **Bearer Token**: Session credentials (OAuth 2.0)
- **Headers**: Device emulation (User-Agent, X-User-Agent)
- **Cookies**: Persistent settings (MAC, language, timezone)

### Security Model
- **Two-factor**: MAC (what you have) + Token (what you know)
- **Time-limited**: Tokens expire
- **Device-bound**: Can't easily share accounts
- **Provider-controlled**: Whitelist management

This architecture balances security (device binding, tokens) with usability (automatic re-authentication, session management) while accurately emulating real MAG set-top box behavior.
