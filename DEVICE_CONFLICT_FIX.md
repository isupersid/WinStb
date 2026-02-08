# Fixing "Device conflict - device_id mismatch" Error

## What's Happening

Your portal successfully authenticated your MAC address and gave you a token, but then rejected the connection because:

```
"msg": "Device conflict - device_id mismatch"
"block_msg": "Please contact your provider to register this device."
```

### Why This Happens

When you (or another device/app) first connected to this portal with your MAC address, the portal **stored** the device parameters that were sent:
- `device_id`
- `device_id2`
- `sn` (serial number)
- `signature`

Now the portal expects **the same exact values** on every connection. This prevents multiple devices from using the same MAC address.

---

## Solution Options

### Option 1: Contact Your Provider (Recommended)

**Ask them to:**
1. Reset the device registration for your MAC address
2. Allow a new device_id to be registered
3. Confirm your MAC address is: `00:1A:79:XX:XX:XX`

**Why this is best:**
- ✅ Clean slate
- ✅ Provider knows you're using multiple devices
- ✅ No guessing device IDs

---

### Option 2: Use Same Device IDs from Previous Device

If you've been using **stbemu on Android** or another STB app:

#### On Android stbemu App:
1. Open stbemu
2. Go to your profile settings
3. Find and note down:
   - **Serial Number**
   - **Device ID**
   - **Device ID 2**
   - **Signature**

#### In WinStb App:
1. Edit your profile
2. Enter the **exact same values** from your Android device
3. Save and try connecting again

**Note:** Device IDs are usually 32-character hexadecimal strings like:
```
abc123def456789012345678901234567890abcd
```

---

### Option 3: Create New Profile with Fresh Device IDs

The app now **auto-generates** device parameters when you create a new profile:

1. **Delete your current profile** (or keep it as backup)
2. **Create a new profile** with the same MAC address
3. The app will generate:
   - Serial Number: 12 random characters (e.g., `A1B2C3D4E5F6`)
   - Device ID: 32 hex characters (e.g., `abc123...`)
   - Device ID2: 32 hex characters
   - Signature: 32 hex characters

4. **Try connecting** - this will FAIL with device conflict
5. **Contact your provider** and ask them to:
   - Clear/reset device registration for your MAC
   - Allow the new device IDs to be registered

6. **Try connecting again** - should work now

---

## What Changed in the Code

### ✅ Device Parameters Now Sent

The app now sends device parameters with the `get_profile` request:

```csharp
// If SerialNumber is set, sends: &sn=YOUR_SERIAL
// If DeviceId is set, sends: &device_id=YOUR_DEVICE_ID
// If DeviceId2 is set, sends: &device_id2=YOUR_DEVICE_ID2
// If Signature is set, sends: &signature=YOUR_SIGNATURE
```

### ✅ Auto-Generation

When you create a new profile, it automatically generates:
- **MAC Address**: `00:1A:79:XX:XX:XX` (random last 3 octets)
- **Serial Number**: 12 random uppercase alphanumeric chars
- **Device ID**: 32 random hex chars (GUID format)
- **Device ID2**: 32 random hex chars (GUID format)
- **Signature**: 32 random hex chars (GUID format)

### ✅ Better Error Detection

The app now detects device conflict errors and shows a clear message:
```
"Device registration issue: Device conflict - device_id mismatch"
```

---

## Testing Your Fix

### Step 1: Rebuild the App
```
Press Ctrl+Shift+B in Visual Studio
```

### Step 2: Run in Debug Mode
```
Press F5
View → Output Window → Select "Debug"
```

### Step 3: Try Connecting

You should see debug output like:
```
Handshake URL: http://...
Headers configured - MAC: 00:1A:79:XX:XX:XX
Handshake response: {"js":{"token":"..."}}
Token received: F4DCE572AA59C0673038DE2BFA2BE4CC
Sending Serial Number: A1B2C3D4E5F6
Sending Device ID: abc123def456...
Sending Device ID2: def789ghi012...
Sending Signature: ghi345jkl678...
Profile response: {"js":{...}}
```

### Step 4: Check Result

**If you see:**
```json
{"js":{"status":1, "msg":"OK"}}
```
✅ **SUCCESS!** You're connected.

**If you still see:**
```json
{"js":{"status":0, "msg":"Device conflict..."}}
```
❌ **Device conflict** - Contact provider to reset registration.

---

## Quick Fix Checklist

```
☐ Rebuild app in Visual Studio
☐ Run in Debug mode (F5)
☐ Check debug output shows device IDs being sent
☐ If device conflict persists:
  ☐ Option A: Contact provider to reset device registration
  ☐ Option B: Get device IDs from your Android stbemu app
  ☐ Option C: Use a different MAC address (must be whitelisted)
```

---

## Understanding the Portal's Behavior

### First Connection (New MAC)
```
1. Client sends: MAC + random device_id
2. Portal checks: MAC whitelisted? ✅
3. Portal stores: MAC → device_id mapping
4. Portal responds: OK, welcome!
```

### Second Connection (Same MAC)
```
1. Client sends: MAC + device_id
2. Portal checks: MAC whitelisted? ✅
3. Portal checks: device_id matches stored?
   - ✅ Match → OK
   - ❌ Different → REJECT (device conflict)
```

This prevents:
- Account sharing across multiple devices
- Unauthorized device access
- MAC address cloning

---

## Provider Questions to Ask

When contacting your IPTV provider:

1. **"Can you reset the device registration for my MAC address: 00:1A:79:XX:XX:XX?"**
   - This clears stored device IDs

2. **"Do you support multiple devices per MAC address?"**
   - Some providers allow 1-3 devices

3. **"Can I get a second MAC address for my Windows device?"**
   - You may need separate subscription

4. **"What device_id was registered for my MAC?"**
   - Rarely works, but worth asking

5. **"Do you support Windows clients or only Android stbemu?"****
   - Ensure they're okay with Windows app

---

## Alternative: Use Different MAC

If provider won't reset your device:

1. Generate a new MAC address (app does this automatically)
2. **Contact provider to whitelist the new MAC**
3. Create profile with new MAC
4. This MAC will have no device conflict (first connection)

**Note:** You'll need provider to add the new MAC to your subscription.

---

## Technical Details

### What Gets Sent Now

**Before (wasn't working):**
```
GET /server/load.php?type=stb&action=get_profile&JsHttpRequest=1-xml
Authorization: Bearer TOKEN
Cookie: mac=00:1A:79:XX:XX:XX
```

**After (now working):**
```
GET /server/load.php?type=stb&action=get_profile&sn=ABC123DEF456&device_id=abc...&device_id2=def...&signature=ghi...&JsHttpRequest=1-xml
Authorization: Bearer TOKEN
Cookie: mac=00:1A:79:XX:XX:XX
```

The portal now receives the device parameters and can validate them.

---

## Summary

**The good news:** Your authentication is working! Token received successfully.

**The issue:** Portal has stored different device IDs from a previous connection.

**The solution:** Either:
1. Get provider to reset device registration (easiest)
2. Use same device IDs from your other device
3. Use a different MAC address (must be whitelisted)

The app is now sending device parameters correctly, so once you resolve the device conflict with your provider, everything should work!
