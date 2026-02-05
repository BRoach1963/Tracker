# Google Calendar Integration Setup

This guide explains how to configure Google Calendar OAuth integration in ProCohere.

## Overview

ProCohere uses OAuth 2.0 to integrate with Google Calendar. You need to:
1. Create OAuth credentials in Google Cloud Console
2. Add credentials to `appsettings.json`

---

## Part 1: Create OAuth Credentials

### Step 1: Go to Google Cloud Console
1. Navigate to https://console.cloud.google.com/
2. Sign in with your Google account

### Step 2: Create or Select a Project
1. Click the project dropdown (top left, next to "Google Cloud")
2. Click "New Project"
3. Name it "ProCohere" (or any name)
4. Click "Create"

### Step 3: Enable Google Calendar API
1. In the left sidebar, go to "APIs & Services" > "Library"
2. Search for "Google Calendar API"
3. Click on it and click "Enable"

### Step 4: Configure OAuth Consent Screen
1. Go to "APIs & Services" > "OAuth consent screen"
2. Select "External" (unless you have a Google Workspace)
3. Click "Create"
4. Fill in:
   - App name: `ProCohere`
   - User support email: Your email
   - Developer contact email: Your email
5. Click "Save and Continue"
6. On the Scopes screen, click "Add or Remove Scopes"
7. Search for and select: `https://www.googleapis.com/auth/calendar` and `https://www.googleapis.com/auth/calendar.events`
8. Click "Update" then "Save and Continue"
9. Click "Save and Continue" on Test Users screen
10. Click "Back to Dashboard"

### Step 5: Create OAuth Client ID
1. Go to "APIs & Services" > "Credentials"
2. Click "Create Credentials" > "OAuth client ID"
3. Application type: **Desktop app**
4. Name: `ProCohere Desktop`
5. Click "Create"
6. **IMPORTANT**: Copy the Client ID and Client Secret that appear
   - Client ID looks like: `123456789-abc123.apps.googleusercontent.com`
   - Client Secret looks like: `GOCSPX-abc123xyz789`

---

## Part 2: Configure appsettings.json

### Locate appsettings.json
The file is at: `Tracker\ProCohere.Avalonia\appsettings.json`

### Add Your Credentials
1. Open `appsettings.json` in a text editor
2. Replace placeholders with your actual credentials:

```json
{
  "GoogleCalendar": {
    "ClientId": "123456789-abc123.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-abc123xyz789"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE"
  }
}
```

3. Save the file

### Development vs. Production
- **Development**: You can also use environment variables (they take precedence over appsettings.json):
  ```powershell
  $env:GOOGLE_CALENDAR_CLIENT_ID = "your-client-id"
  $env:GOOGLE_CALENDAR_CLIENT_SECRET = "your-client-secret"
  ```
- **Production**: The installer will include `appsettings.json` with embedded credentials

---

## Part 3: Verify Setup

### Test the OAuth Flow
1. Run ProCohere
2. Go to Settings > Calendar Integration
3. Click "Connect Google Calendar"
4. A browser window should open asking you to sign in and authorize ProCohere
5. After authorization, you should see "Connected" status

### Troubleshooting
- **"OAuth credentials not configured"**: Check that appsettings.json exists and has correct JSON syntax
- **Browser doesn't open**: Check firewall/antivirus settings
- **"Access denied"**: Make sure you authorized the app in the OAuth consent screen
- **"Invalid client"**: Double-check Client ID and Secret are copied correctly (no extra spaces)

---

## Security Notes

### What to Commit to Git
- ✅ `appsettings.example.json` - Template with placeholders
- ❌ `appsettings.json` - Contains real credentials (in .gitignore)

### How Credentials Are Used
- **Client ID/Secret**: Identifies ProCohere to Google (public, embedded in app)
- **User Tokens**: Stored encrypted in Supabase `calendar_integrations` table
- OAuth tokens are per-user and can be revoked at https://myaccount.google.com/permissions

### Desktop App Security Model
Desktop apps are "public clients" in OAuth terminology - they cannot keep secrets from determined users. The Client ID and Secret identify the ProCohere application to Google, but the real security comes from:
1. User authorization (user must explicitly grant permission)
2. Encrypted token storage (access/refresh tokens stored securely)
3. Token expiration and refresh flows

This is industry-standard for desktop OAuth applications (Slack, Discord, VS Code all use this model).

---

## Additional Resources
- Google OAuth 2.0 Documentation: https://developers.google.com/identity/protocols/oauth2
- Google Calendar API Reference: https://developers.google.com/calendar/api/v3/reference
