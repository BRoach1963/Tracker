# Supabase Email Configuration for Tracker

For password reset and email change to work, Supabase needs proper configuration.

## Required Setup in Supabase Dashboard

### 1. Site URL Configuration
Go to: **Authentication** → **URL Configuration**

- **Site URL**: Set to `https://www.pricklycactussoftware.com` 
- This is where password reset links will redirect

### 2. Redirect URLs  
Add these to the allowed redirect URLs:
- `https://www.pricklycactussoftware.com/auth/callback`
- `tracker://auth/callback` (for desktop app deep linking)

### 3. Email Templates
Go to: **Authentication** → **Email Templates**

Customize:
- **Confirm signup** - Email verification
- **Reset password** - Password reset link
- **Change email** - Email change confirmation

### 4. SMTP Settings (Optional but Recommended)
Go to: **Project Settings** → **Auth** → **SMTP Settings**

By default, Supabase uses their own email service with rate limits.
For production, configure your own SMTP:
- SendGrid
- Mailgun  
- Amazon SES
- etc.

---

## Desktop App Considerations

### The Challenge
Password reset emails contain a link like:
```
https://your-site.com/auth/reset?token=abc123
```

For a desktop app, we have options:

### Option A: Web Landing Page (Recommended)
1. Create a web page at `pricklycactussoftware.com/tracker/auth/reset`
2. Page shows: "Open this link in Tracker" with a `tracker://` link
3. Register `tracker://` protocol handler in Windows

### Option B: Manual Token Entry
1. User gets email with a token/code
2. User enters code in the app
3. App validates and allows password change

### Option C: In-App Browser
1. Open reset link in embedded browser
2. Capture the redirect with token
3. Complete reset in-app

---

## Testing Password Reset

1. Check Supabase Dashboard → Logs → Auth for any errors
2. Check spam folder - Supabase default emails often go there
3. Verify the email exists in Auth → Users
4. Try with a fresh email first

---

## Current Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Password Reset | ⚠️ API Works | Needs email template + redirect handling |
| Email Change | ⚠️ API Works | Needs confirmation email + redirect |
| Avatar Upload | ✅ Working | Uses Supabase Storage |


