# Phase 3: Critical Security Fix Plan

**Issue**: SocialMediaLinkTextBox URL Validation Vulnerability  
**Priority**: 🔴 CRITICAL  
**Effort**: 30 minutes  
**Risk Level**: HIGH (Security)

---

## The Problem

**File**: `Tracker/Tracker/Controls/CustomControls/SocialMediaLinkTextBox.cs`

**Current Vulnerable Code** (lines 22-26, 157-170):
```csharp
private void ExecuteLaunchUrlCommand(object? obj)
{
    if (string.IsNullOrEmpty(this.Text)) return;
    Process.Start(FormatUrl(this.Text));  // ❌ NO VALIDATION
}

private ProcessStartInfo FormatUrl(string url)
{
    // Only adds https:// prefix, no validation
    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        url = "https://" + url;
    }

    return new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true  // ❌ DANGEROUS with untrusted input
    };
}
```

## Attack Scenarios

1. **File Access Attack**
   - User pastes: `file:///C:/sensitive/data.txt`
   - Result: Opens local file (data breach)

2. **Command Execution Attack**
   - User pastes: `cmd.exe /c del *.*`
   - Result: Executes system command

3. **SMB Attack**
   - User pastes: `\\attacker.com\share`
   - Result: Connects to attacker's SMB share

4. **JavaScript Injection**
   - User pastes: `javascript:alert('xss')`
   - Result: Becomes `https://javascript:alert('xss')` (still dangerous)

---

## The Fix

**Replace ExecuteLaunchUrlCommand and FormatUrl with:**

```csharp
private void ExecuteLaunchUrlCommand(object? obj)
{
    if (string.IsNullOrEmpty(this.Text)) return;

    try
    {
        var uri = new Uri(this.Text, UriKind.Absolute);
        
        // Only allow http/https
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            NotificationManager.Instance.ShowError(
                "Invalid URL", 
                "Only HTTP/HTTPS URLs are allowed");
            return;
        }
        
        Process.Start(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });
    }
    catch (UriFormatException)
    {
        NotificationManager.Instance.ShowError(
            "Invalid URL", 
            "Please enter a valid URL");
    }
    catch (Exception ex)
    {
        _logger.Error("Failed to launch URL: {0}", ex.Message);
        NotificationManager.Instance.ShowError(
            "Error", 
            "Failed to open URL");
    }
}

private ProcessStartInfo FormatUrl(string url)
{
    // This method is now only used for formatting, not validation
    // Validation happens in ExecuteLaunchUrlCommand
    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        url = "https://" + url;
    }

    return new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    };
}
```

---

## Implementation Steps

1. **Add Logger Field** (if not already present)
   ```csharp
   private readonly ILogger _logger = LoggingManager.GetComponentLogger(nameof(SocialMediaLinkTextBox));
   ```

2. **Replace ExecuteLaunchUrlCommand** with validated version above

3. **Add Try-Catch** around Uri parsing and Process.Start

4. **Add Error Handling** with user-friendly messages

5. **Test Cases**:
   - ✅ Valid URL: `https://linkedin.com` → Opens
   - ✅ URL without scheme: `linkedin.com` → Adds https:// and opens
   - ❌ File URL: `file:///C:/data.txt` → Shows error
   - ❌ Command: `cmd.exe /c del *.*` → Shows error
   - ❌ JavaScript: `javascript:alert('xss')` → Shows error
   - ❌ SMB: `\\attacker.com\share` → Shows error
   - ❌ Invalid URL: `not a url` → Shows error

---

## Files to Modify

- `Tracker/Tracker/Controls/CustomControls/SocialMediaLinkTextBox.cs`
  - Lines 22-26: ExecuteLaunchUrlCommand method
  - Lines 157-170: FormatUrl method

---

## Verification

After fix, verify:
1. ✅ Valid URLs open correctly
2. ✅ Invalid URLs show error message
3. ✅ Non-HTTP/HTTPS schemes rejected
4. ✅ Error messages are user-friendly
5. ✅ No exceptions thrown to user

---

## Deployment

- **Do NOT deploy** until this fix is applied
- **Test thoroughly** before release
- **Consider** adding unit tests for URL validation

---

## Related Issues

This fix addresses:
- **Phase 3 Finding 1**: Critical Security Vulnerability
- **OWASP**: Improper Input Validation
- **CWE-434**: Unrestricted Upload of File with Dangerous Type

---

**Status**: Ready for implementation  
**Estimated Time**: 30 minutes  
**Risk**: Low (improves security)
