# Account Settings

Manage your Tracker account profile, preferences, and security settings.

## Accessing Account Settings

1. Click your **profile icon** in the top-right corner
2. Select **Account** from the menu

Or use keyboard shortcut: `Ctrl+Shift+A`

## Profile Information

Your profile is displayed in **view mode** by default. Click the **pencil icon** (✏️) to switch to **edit mode**.

### Profile Fields

| Field | Description |
|-------|-------------|
| **First Name** | Your first/given name |
| **Last Name** | Your last/family name |
| **Job Title** | Your role or position |
| **Company** | Your organization name |
| **Phone** | Contact phone number (formatted automatically) |

**To Edit**:
1. Go to **Account** → **Profile**
2. Click the **pencil icon** to enter edit mode
3. Modify any fields (hint text shows expected format)
4. Click **Save** to save changes, or **Cancel** to discard
5. Profile returns to view mode

### Display Name
Your display name (shown in the top-right profile button) is typically set during account creation:
- Appears in the app header
- Used in report headers
- Shown in exported documents

### Email Address
Your account email is used for:
- Signing in
- Password reset
- Notifications
- Receipts and invoices

**Changing Email**:
1. Go to **Account** → **Profile**
2. Click **Change Email**
3. Enter new email address
4. Verify via email link
5. Old email receives notification

### Profile Photo
Add a personal touch:
1. Go to **Account** → **Profile**
2. Click your avatar or the camera icon
3. Choose an image file (JPG, PNG, GIF)
4. Image uploads and displays immediately
5. Avatar appears in the title bar and account dialog

**Supported formats**: JPG, PNG, GIF, WebP (max 2MB)

### Initials Display
If no photo is set, your initials are displayed:
- Automatically generated from display name
- "John Smith" → "JS"
- Theme-colored background

## Security Settings

### Change Password
1. Go to **Account** → **Security**
2. Click **Change Password**
3. Enter current password
4. Enter new password (twice)
5. Click **Update Password**

**Password Requirements**:
- Minimum 8 characters
- Mix of uppercase and lowercase
- At least one number
- At least one special character

### Active Sessions
See where you're logged in:
1. Go to **Account** → **Security**
2. View **Active Sessions**
3. See device, location, last activity

**Sign Out Other Sessions**:
1. Click **Sign Out All Others**
2. Only current session remains active
3. Use if you suspect unauthorized access

### Delete Account
**Warning**: This is permanent!

1. Go to **Account** → **Security**
2. Scroll to **Danger Zone**
3. Click **Delete Account**
4. Enter password to confirm
5. Type "DELETE" to verify
6. Account and all data permanently removed

**Before deleting**:
- Export your data first
- Cancel any active subscription
- This cannot be undone

## Subscription Management

### View Current Plan
1. Go to **Account** → **Subscription**
2. See your current plan and features
3. View next billing date
4. See payment method on file

### Upgrade Plan
1. Go to **Account** → **Subscription**
2. Click **Upgrade**
3. Choose new plan
4. Confirm payment
5. Features activate immediately

### Downgrade Plan
1. Go to **Account** → **Subscription**
2. Click **Change Plan**
3. Select lower tier
4. Changes at end of billing period

### Cancel Subscription
1. Go to **Account** → **Subscription**
2. Click **Cancel Subscription**
3. Confirm cancellation
4. Access continues until period ends
5. Reverts to Free tier

## Notification Preferences

### Email Notifications
Control what emails you receive:

| Notification | Description | Default |
|--------------|-------------|---------|
| **Security Alerts** | Password changes, new logins | Always on |
| **Billing** | Receipts, payment issues | Always on |
| **Product Updates** | New features, improvements | On |
| **Tips & Tutorials** | Usage tips, best practices | On |
| **Marketing** | Promotions, offers | Off |

To change:
1. Go to **Account** → **Notifications**
2. Toggle each notification type
3. Changes save automatically

### In-App Notifications
See [Settings](../features/settings.md) for in-app notification preferences.

## Data & Privacy

### Export Your Data
Download everything:
1. Go to **Account** → **Data & Privacy**
2. Click **Export All Data**
3. Choose format (JSON or CSV)
4. Download starts automatically

**Includes**:
- Team members and profiles
- 1:1 meeting history and notes
- Tasks, projects, OKRs, KPIs
- Goals, feedback, quick notes
- Settings and preferences

### Data Retention
- **Active accounts**: Data retained indefinitely
- **Free accounts inactive >12 months**: Warning email sent
- **Deleted accounts**: Data removed within 30 days
- **Backups**: Purged after 90 days

### Privacy Settings
1. Go to **Account** → **Data & Privacy**
2. **Usage Analytics**: Help improve Tracker (anonymized)
3. **Error Reporting**: Send crash reports automatically

## Connected Services

### Calendar Integration
Manage calendar connections:
1. Go to **Account** → **Connected Services**
2. See connected calendars
3. Add or remove connections
4. Configure sync settings

**Supported Calendars**:
- Google Calendar
- Microsoft Outlook

### Disconnect a Service
1. Go to **Account** → **Connected Services**
2. Click **Disconnect** next to the service
3. Confirm disconnection
4. Service access revoked immediately

## Help & Support

### Contact Support
From Account Settings:
1. Click **Help & Support**
2. Choose contact method:
   - **Email**: Opens email to support
   - **Documentation**: Opens help center
   - **AI Assistant**: Opens Help Bot (if available)

### Support Levels by Plan
| Plan | Support | Response Time |
|------|---------|--------------|
| Free | Community Forums | Best effort |
| Standard | Email Support | 24 hours |
| Pro | Priority Support | 4 hours |
| Enterprise | Dedicated Support | 1 hour |

### Report a Bug
1. Go to **Account** → **Help & Support**
2. Click **Report a Bug**
3. Describe the issue
4. Logs attached automatically (optional)
5. Submit report

## Signing Out

### Sign Out Current Device
1. Click profile icon
2. Select **Sign Out**
3. You're returned to sign-in screen
4. Your settings and database preferences are preserved for next sign-in

### Sign Out All Devices
1. Go to **Account** → **Security**
2. Click **Sign Out All Sessions**
3. All devices signed out
4. You remain signed in on current device

## Account Isolation

Each Tracker account has completely isolated settings and data:

- **Settings**: Stored per-account in `%LocalAppData%\Tracker\Users\{accountId}\`
- **Database preferences**: Each account can have different database locations
- **Multiple accounts**: You can use multiple accounts on the same computer without conflicts

When you switch accounts:
1. Sign out from current account
2. Sign in with different credentials
3. Tracker loads that account's settings and database preferences automatically

## Related Topics
- [Signing In](login.md)
- [Subscription Plans](subscriptions.md)
- [Billing & Payments](billing.md)
- [Settings](../features/settings.md)

