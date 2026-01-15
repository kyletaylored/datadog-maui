# IIS Troubleshooting Guide

Common issues and solutions when deploying the .NET Framework 4.8 API to IIS.

## Quick Fix

If you're experiencing issues with your IIS deployment, run the automated troubleshooting script:

```powershell
cd C:\path\to\datadog-maui
.\scripts\troubleshoot-iis.ps1
```

Or to automatically fix all detected issues:

```powershell
.\scripts\troubleshoot-iis.ps1 -FixAll
```

## Common Issues

### 1. HTTP Error 403.14 - Forbidden (Directory Listing)

**Symptoms:**
```
HTTP Error 403.14 - Forbidden
The Web server is configured to not list the contents of this directory.
Physical Path: C:\path\to\ApiFramework\bin
```

**Cause:** IIS site physical path is incorrectly pointing to the `bin` folder instead of the root application folder.

**Solution:**

**Option A: Use the troubleshooting script**
```powershell
.\scripts\troubleshoot-iis.ps1
```

**Option B: Fix manually in IIS Manager**
1. Open IIS Manager
2. Select your site (DatadogMauiApiFramework)
3. Click "Basic Settings" in the Actions pane
4. Update Physical path from `C:\path\to\ApiFramework\bin` to `C:\path\to\ApiFramework`
5. Click OK
6. Restart the site

**Option C: Fix with PowerShell**
```powershell
Import-Module WebAdministration
$siteName = "DatadogMauiApiFramework"
$correctPath = "C:\Users\datadog\Desktop\datadog-maui\ApiFramework"
Set-ItemProperty "IIS:\Sites\$siteName" -Name physicalPath -Value $correctPath
Restart-WebAppPool -Name "DatadogMauiApiFrameworkPool"
```

---

### 2. HTTP Error 401.3 - Access Denied (Permissions)

**Symptoms:**
```
Server Error in '/' Application.
Access is denied.
Error message 401.3: You do not have permission to view this directory or page
using the credentials you supplied (access denied due to Access Control Lists).
```

**Cause:** The IIS application pool identity doesn't have sufficient permissions to read the application files.

**Solution:**

**Option A: Use the troubleshooting script**
```powershell
.\scripts\troubleshoot-iis.ps1 -FixAll
```

**Option B: Fix manually with PowerShell**
```powershell
$appPath = "C:\Users\datadog\Desktop\datadog-maui\ApiFramework"
$appPool = "DatadogMauiApiFrameworkPool"

# Grant read/execute to IIS users
icacls "$appPath" /grant "IIS_IUSRS:(OI)(CI)RX" /T
icacls "$appPath" /grant "IUSR:(OI)(CI)RX" /T
icacls "$appPath" /grant "IIS APPPOOL\${appPool}:(OI)(CI)RX" /T

# Grant modify to bin folder
icacls "$appPath\bin" /grant "IIS APPPOOL\${appPool}:(OI)(CI)M" /T

# Restart IIS
iisreset
```

---

### 3. Configuration Error - Failed to Monitor Changes

**Symptoms:**
```
Configuration Error
Parser Error Message: An error occurred loading a configuration file:
Failed to start monitoring changes to 'C:\path\to\ApiFramework\bin'
because access is denied.
```

**Cause:** IIS needs permission to monitor the `bin` folder for changes, but the app pool identity lacks the necessary permissions.

**Solution:** Same as Issue #2 - grant proper permissions to the application pool identity.

```powershell
.\scripts\troubleshoot-iis.ps1 -FixAll
```

---

### 4. HTTP Error 500.19 - Duplicate MIME Type

**Symptoms:**
```
HTTP Error 500.19 - Internal Server Error
Error Code: 0x800700b7
Config Error: Cannot add duplicate collection entry of type 'mimeMap'
with unique key attribute 'fileExtension' set to '.json'
```

**Cause:** Web.config tries to add MIME types that are already defined in IIS's global configuration.

**Solution:** The Web.config has been updated to remove existing MIME types before adding them:

```xml
<staticContent>
  <remove fileExtension=".json" />
  <mimeMap fileExtension=".json" mimeType="application/json" />
  <remove fileExtension=".woff" />
  <mimeMap fileExtension=".woff" mimeType="application/font-woff" />
  <remove fileExtension=".woff2" />
  <mimeMap fileExtension=".woff2" mimeType="application/font-woff2" />
</staticContent>
```

If you still see this error, ensure your Web.config is up to date.

---

### 5. Missing index.html - Shows 403 Error

**Symptoms:**
- Accessing the root URL shows a 403 Forbidden error
- The error mentions no default document is configured
- Physical path is correct but file is missing

**Cause:** The `index.html` file is in the `wwwroot` subfolder but IIS is looking for it in the root.

**Solution:**

**Option A: Use the troubleshooting script**
```powershell
.\scripts\troubleshoot-iis.ps1
```

**Option B: Copy manually**
```powershell
$appPath = "C:\Users\datadog\Desktop\datadog-maui\ApiFramework"
Copy-Item "$appPath\wwwroot\index.html" "$appPath\index.html"
```

---

### 6. PowerShell Script Parsing Errors

**Symptoms:**
```
The string is missing the terminator: ".
ParserError: (:) [], ParseException
TerminatorExpectedAtEndOfString
```

**Cause:** Running scripts from RDP network share (`\\tsclient\...`) or scripts containing Unicode characters that don't parse correctly.

**Solution:**

**For RDP network share issues:**
1. Copy the entire project to a local drive:
```powershell
Copy-Item -Path "\\tsclient\datadog-maui" -Destination "C:\temp\datadog-maui" -Recurse
cd C:\temp\datadog-maui
```

2. Run scripts from the local copy

**For Unicode character issues:**
- Ensure scripts use only ASCII characters
- All provided scripts have been updated to use ASCII only

---

### 7. Application Pool Stops Automatically

**Symptoms:**
- Site works briefly then stops
- Application pool shows "Stopped" state
- Event Viewer shows application crashes

**Cause:** Application error causing the worker process to crash.

**Solution:**

1. Check Event Viewer for detailed error:
```powershell
Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 10
```

2. Enable detailed error messages in Web.config:
```xml
<system.web>
  <customErrors mode="Off" />
</system.web>
```

3. Check for missing dependencies:
   - Ensure .NET Framework 4.8 is installed
   - Verify all NuGet packages are restored
   - Check that Datadog.Trace DLLs are present in the `bin` folder

4. Restart the application pool:
```powershell
Restart-WebAppPool -Name "DatadogMauiApiFrameworkPool"
```

---

## Verification Checklist

After fixing issues, verify your deployment:

### 1. Check Site Configuration
```powershell
Import-Module WebAdministration
Get-Website -Name "DatadogMauiApiFramework" | Format-List Name, PhysicalPath, State
```

Expected output:
```
Name         : DatadogMauiApiFramework
PhysicalPath : C:\Users\datadog\Desktop\datadog-maui\ApiFramework
State        : Started
```

### 2. Check Application Pool
```powershell
Get-WebAppPoolState -Name "DatadogMauiApiFrameworkPool"
```

Expected: `Value: Started`

### 3. Test Endpoints

**Root (Web Dashboard):**
```powershell
Invoke-WebRequest http://localhost:5001/ -UseBasicParsing
```
Should return HTML content (StatusCode: 200)

**Health Check:**
```powershell
Invoke-RestMethod http://localhost:5001/health
```
Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-01-15T..."
}
```

**API Root Info:**
```powershell
Invoke-RestMethod http://localhost:5001/api
```

### 4. Check File Permissions
```powershell
$appPath = "C:\Users\datadog\Desktop\datadog-maui\ApiFramework"
(Get-Acl $appPath).Access | Where-Object { $_.IdentityReference -like "*IIS*" } | Format-Table IdentityReference, FileSystemRights
```

Should show entries for:
- `IIS_IUSRS`
- `IIS APPPOOL\DatadogMauiApiFrameworkPool`

---

## Clean Reinstall

If all else fails, perform a clean reinstall:

1. **Remove existing site:**
```powershell
Import-Module WebAdministration
Remove-Website -Name "DatadogMauiApiFramework"
Remove-WebAppPool -Name "DatadogMauiApiFrameworkPool"
```

2. **Clean up files:**
```powershell
Remove-Item "C:\Users\datadog\Desktop\datadog-maui\ApiFramework\bin" -Recurse -Force
Remove-Item "C:\Users\datadog\Desktop\datadog-maui\ApiFramework\obj" -Recurse -Force
```

3. **Redeploy:**
```powershell
cd C:\Users\datadog\Desktop\datadog-maui
.\scripts\deploy-iis-framework.ps1
```

4. **Run troubleshooting script:**
```powershell
.\scripts\troubleshoot-iis.ps1 -FixAll
```

---

## Getting Help

If you're still experiencing issues:

1. Run the troubleshooting script with verbose output:
```powershell
.\scripts\troubleshoot-iis.ps1 -Verbose
```

2. Check IIS logs:
```powershell
Get-Content "C:\inetpub\logs\LogFiles\W3SVC*\*.log" -Tail 50
```

3. Check Windows Event Log:
```powershell
Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 20 | Format-List
```

4. Verify .NET Framework installation:
```powershell
Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\' | Get-ItemProperty -Name Version
```

---

## Related Documentation

- [IIS Deployment Guide](IIS_DEPLOYMENT.md) - Full deployment instructions
- [Framework Quick Start](../FRAMEWORK_QUICKSTART.md) - Getting started guide
- [Deployment Scripts README](../scripts/README.md) - PowerShell script documentation
