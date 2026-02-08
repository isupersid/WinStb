# Troubleshooting Authentication Issues

## Common Authentication Errors and Solutions

### 1. "The format of value '...' is invalid" ✅ FIXED

**Error Message:**
```
Exception thrown: 'System.FormatException' in System.Net.Http.dll
Authentication error: The format of value 'ver: 2 rev: 250 Safari/533.3' is invalid.
```

**Cause:** HTTP header validation in .NET was too strict for the MAG box User-Agent format.

**Solution:** ✅ Fixed by using `TryAddWithoutValidation()` and removing spaces around colons.

**What changed:**
- Old: `ver: 2 rev: 250` → New: `ver:2 rev:250`
- Using `TryAddWithoutValidation()` instead of `Add()`

---

### 2. Network Connection Errors

**Error Messages:**
- "Network error: Unable to connect to the remote server"
- "Network error: No such host is known"
- "Network error: The connection timed out"

**Possible Causes:**

1. **Wrong Portal URL**
   ```
   ❌ http://example.com
   ✅ http://example.com/stalker_portal
   ```
   The URL should point to the Stalker Portal installation.

2. **Server is Down**
   - Test in browser: Open `http://your-portal.com/stalker_portal/server/load.php`
   - Should see some response (even error is OK, means server is reachable)

3. **Firewall Blocking**
   - Windows Firewall may block the app
   - Check: `Windows Security → Firewall & network protection`
   - Allow WinStb through firewall

4. **VPN/Proxy Issues**
   - Some IPTV portals block VPN connections
   - Try disabling VPN temporarily

**Solutions:**
```
✅ Verify URL format includes /stalker_portal
✅ Test URL in web browser first
✅ Check firewall settings
✅ Disable VPN if using one
✅ Try HTTPS instead of HTTP (or vice versa)
```

---

### 3. Portal Errors (Server Rejects Connection)

**Error Messages:**
- "Portal error: Subscription expired"
- "Portal error: Access denied"
- "Portal error: MAC address not found"
- "Portal error: Too many connections"

**Causes & Solutions:**

#### "Subscription expired"
```
❌ Your subscription with the IPTV provider has ended
✅ Contact your provider to renew
✅ Verify subscription status with provider
```

#### "MAC address not found"
```
❌ MAC address not whitelisted by provider
✅ Contact provider to add your MAC: 00:1A:79:XX:XX:XX
✅ Verify you're using the exact MAC they have on file
✅ Generate a new MAC in the app and send to provider
```

#### "Too many connections"
```
❌ MAC address already connected from another device/location
✅ Close other apps/devices using same MAC
✅ Wait 5-10 minutes for session to expire
✅ Contact provider to reset connection
```

#### "Access denied"
```
❌ General authentication failure
✅ Verify MAC address format: 00:1A:79:XX:XX:XX
✅ Check if MAC starts with 00:1A:79 (required by most portals)
✅ Try a different MAC address from same range
```

---

### 4. Invalid Response Errors

**Error Messages:**
- "Invalid response format: ..."
- "No token received from portal"

**Possible Causes:**

1. **Portal returned HTML instead of JSON**
   - Server might be showing an error page
   - Portal URL might be incorrect

2. **Portal requires additional parameters**
   - Some portals need Serial Number
   - Some portals need Device ID

**Solutions:**

#### Check raw response (View Output in Visual Studio)
```
1. Run app in Debug mode (F5)
2. View → Output Window
3. Select "Debug" from dropdown
4. Look for lines like:
   Handshake URL: ...
   Handshake response: ...
```

#### Example good response:
```json
{"js":{"token":"ABC123...", "random":"xyz"}}
```

#### Example bad response:
```html
<html><body>Error 404</body></html>
```

If you see HTML, the URL is wrong.

---

### 5. SSL/Certificate Errors

**Error Messages:**
- "The SSL connection could not be established"
- "The remote certificate is invalid"

**Causes:**
- Portal uses self-signed SSL certificate
- Portal has expired certificate

**Solutions:**
```
✅ App already disables certificate validation
✅ Try using HTTP instead of HTTPS
✅ Check if portal URL requires specific protocol
```

**Current code (already implemented):**
```csharp
ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
```
This accepts all certificates, including self-signed ones.

---

## Debugging Steps

### Step 1: Enable Debug Output

**In Visual Studio:**
1. Press `F5` to run in Debug mode
2. `View → Output Window` (Ctrl+Alt+O)
3. Select "Debug" from dropdown
4. Try connecting to portal
5. Look for these debug lines:

```
Handshake URL: http://portal.com/stalker_portal/server/load.php?type=stb&action=handshake&JsHttpRequest=1-xml
Headers configured - MAC: 00:1A:79:XX:XX:XX
Handshake response: {"js":{"token":"..."}}
Token received: ABC123...
Profile response: {"js":{...}}
```

### Step 2: Test Portal URL Manually

**In web browser:**
```
http://your-portal.com/stalker_portal/server/load.php?type=stb&action=handshake
```

**Expected response:**
- Some JSON response (even error is OK)
- Means server is reachable

**If you see:**
- 404 Not Found → Wrong URL
- Connection timeout → Server down or blocked
- HTML page → Wrong path

### Step 3: Verify MAC Address Format

**Required format:**
```
00:1A:79:XX:XX:XX
```

**Rules:**
- ✅ Must start with `00:1A:79`
- ✅ Exactly 6 pairs of hex digits
- ✅ Separated by colons
- ❌ Don't use other formats like `00-1A-79-XX-XX-XX`

**To generate new MAC in app:**
1. Edit profile
2. Clear MAC address field
3. Save
4. App will auto-generate valid MAC

### Step 4: Test with Minimal Profile

**Create test profile:**
```
Name: Test
Portal URL: http://your-portal.com/stalker_portal
MAC Address: (leave empty for auto-generate)
STB Type: MAG254
Serial: (leave empty)
Device ID: (leave empty)
```

Don't set optional fields initially. Add them only if provider requires.

### Step 5: Compare with Working Client

**If you have Android stbemu app working:**
1. Note the exact URL format it uses
2. Note the exact MAC address
3. Copy these to WinStb
4. Should work identically

---

## Advanced Debugging

### View Full HTTP Request

Add this to `StalkerPortalClient.cs` after `ConfigureHeaders()`:

```csharp
// Log all headers
foreach (var header in _httpClient.DefaultRequestHeaders)
{
    System.Diagnostics.Debug.WriteLine($"Header: {header.Key} = {string.Join(", ", header.Value)}");
}
```

### Test with Fiddler/Wireshark

**Using Fiddler:**
1. Download Fiddler (free HTTP debugger)
2. Run Fiddler
3. Run WinStb
4. Try connecting to portal
5. See exact HTTP requests in Fiddler

Look for:
- Request URL
- Request headers
- Response status code
- Response body

### Test from Command Line

**Using curl:**
```bash
curl -H "Cookie: mac=00:1A:79:XX:XX:XX; stb_lang=en; timezone=UTC" \
     -H "User-Agent: Mozilla/5.0 (QtEmbedded; U; Linux; C) AppleWebKit/533.3 (KHTML, like Gecko) MAG200 stbapp ver:2 rev:250 Safari/533.3" \
     "http://your-portal.com/stalker_portal/server/load.php?type=stb&action=handshake&JsHttpRequest=1-xml"
```

Should return JSON with token if working.

---

## Common Portal URL Formats

### Standard Format
```
http://portal.example.com/stalker_portal
```

### With Port
```
http://portal.example.com:8080/stalker_portal
```

### Subdomain
```
http://stb.example.com/stalker_portal
```

### With Path
```
http://example.com/iptv/stalker_portal
```

### HTTPS
```
https://secure.example.com/stalker_portal
```

**Note:** The app automatically appends `/stalker_portal` if not present in URL.

---

## Provider-Specific Issues

### Issue: Provider requires Serial Number

**Symptoms:**
- Connects but no channels load
- "Device not found" errors

**Solution:**
```
1. Edit profile
2. Set Serial Number (e.g., "SN123456789")
3. Set Device ID (e.g., "DEV123456")
4. Save and reconnect
```

**Note:** These bind the MAC to specific device IDs. Once set, must use same values on reconnection.

### Issue: Provider uses non-standard endpoints

**Symptoms:**
- URL format different from standard
- Uses `/portal.php` instead of `/server/load.php`

**Current code:**
```csharp
var url = $"{baseUrl}/server/load.php?type={type}&action={action}...";
```

**May need to change to:**
```csharp
var url = $"{baseUrl}/portal.php?type={type}&action={action}...";
```

Contact me if you need to support different endpoint formats.

---

## Still Having Issues?

### Provide Debug Information

If still stuck, provide:

1. **Debug output** (from Output Window)
   ```
   Handshake URL: ...
   Headers configured - MAC: ...
   Handshake response: ...
   ```

2. **Error message** (exact text)

3. **Portal URL format** (can obscure domain)
   ```
   Example: http://xxx.xxx.xxx/stalker_portal
   ```

4. **Provider info** (if known)
   - Do they support stbemu?
   - What STB type do they recommend?

5. **Test with curl** (if possible)
   - Does curl command work?

### Contact Provider

Questions to ask your IPTV provider:

1. "What is the exact Stalker Portal URL?"
2. "Is my MAC address whitelisted: 00:1A:79:XX:XX:XX?"
3. "What STB type should I use? (MAG254, MAG322, etc.)"
4. "Do you require Serial Number or Device ID?"
5. "How many concurrent connections are allowed?"
6. "Is my subscription active?"

---

## Quick Fixes Checklist

```
☐ Portal URL ends with /stalker_portal
☐ MAC address starts with 00:1A:79
☐ MAC address is whitelisted with provider
☐ Subscription is active
☐ No other devices using same MAC
☐ Firewall allows WinStb
☐ Not using VPN (or provider allows VPN)
☐ Portal URL accessible in browser
☐ Using correct protocol (HTTP vs HTTPS)
☐ App is up to date (latest version)
```

If all checked and still fails, it's likely a provider-side issue.
