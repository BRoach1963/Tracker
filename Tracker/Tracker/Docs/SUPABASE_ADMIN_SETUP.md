# Supabase Changes Required for Admin Login Feature

## Overview
To support the admin login feature, we need to add an `is_admin` column to the Supabase `profiles` table and update the C# model to sync this data.

## Required Changes

### 1. Database Schema Update (Supabase Dashboard)

Navigate to **SQL Editor** in your Supabase project and run:

```sql
-- Add is_admin column to profiles table
ALTER TABLE profiles 
ADD COLUMN is_admin BOOLEAN NOT NULL DEFAULT FALSE;

-- Create index for faster admin queries
CREATE INDEX idx_profiles_is_admin ON profiles(is_admin);

-- Add comment for documentation
COMMENT ON COLUMN profiles.is_admin IS 'Whether this user has administrator privileges for database management tools';
```

### 2. Update Row Level Security (RLS) Policies

If you have RLS enabled on the profiles table, update policies:

```sql
-- Allow users to read their own admin status
CREATE POLICY "Users can view their own admin status"
ON profiles
FOR SELECT
USING (auth.uid() = id);

-- Only admins can modify admin status (or use Supabase service role)
CREATE POLICY "Only service role can modify admin status"
ON profiles
FOR UPDATE
USING (false)  -- Regular users cannot update via client
WITH CHECK (false);
```

**Note**: Admin status should only be set via Supabase Dashboard or service role API calls, not through the client app.

### 3. Set Admin Users Manually

After adding the column, grant admin privileges to specific users:

```sql
-- Grant admin access to specific user by email
UPDATE profiles 
SET is_admin = TRUE 
WHERE email = 'admin@yourcompany.com';

-- Or by user ID
UPDATE profiles 
SET is_admin = TRUE 
WHERE id = 'user-uuid-here';

-- Verify admin users
SELECT email, display_name, is_admin 
FROM profiles 
WHERE is_admin = TRUE;
```

### 4. Update C# Model

Add the `is_admin` column mapping to [UserProfile.cs](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\Services\Backend\Models\UserProfile.cs):

```csharp
[Column("is_admin")]
public bool IsAdmin { get; set; } = false;
```

**Location**: Add after the `IsActive` property (around line 54).

### 5. Sync Admin Status on Login

Update the login flow to copy `is_admin` from Supabase profile to local User table. In `CreateLocalUserAsync()` or similar method:

```csharp
// When creating/updating local user from Supabase profile:
var localUser = await TrackerDbManager.Instance.GetOrCreateUserAsync(username);
if (localUser != null && SupabaseService.Instance.CurrentProfile != null)
{
    localUser.IsAdmin = SupabaseService.Instance.CurrentProfile.IsAdmin;
    await TrackerDbManager.Instance.UpdateUserAsync(localUser);
}
```

### 6. Validate Admin Access

Update `CanSelectAdmin` property in [LoginDialogViewModel.cs](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\ViewModels\DialogViewModels\LoginDialogViewModel.cs):

```csharp
public bool CanSelectAdmin
{
    get
    {
        // Check if user has admin privileges from Supabase
        var profile = SupabaseService.Instance.CurrentProfile;
        return profile?.IsAdmin ?? false;
    }
}
```

**Or** validate after login:

```csharp
// In IsAdminLogin setter:
set
{
    // Validate user has admin rights before allowing checkbox
    if (value && SupabaseService.Instance.CurrentProfile?.IsAdmin != true)
    {
        MessageBoxHelper.Show(
            "You do not have administrator privileges.",
            "Access Denied",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }
    _isAdminLogin = value;
    RaisePropertyChanged();
}
```

## Testing Checklist

### Step 1: Database Setup
- [ ] Run ALTER TABLE migration in Supabase SQL Editor
- [ ] Verify `is_admin` column exists in profiles table
- [ ] Set `is_admin = TRUE` for at least one test user
- [ ] Verify RLS policies allow reading is_admin

### Step 2: Code Updates
- [ ] Add `IsAdmin` property to UserProfile.cs with `[Column("is_admin")]` attribute
- [ ] Rebuild solution to ensure no compile errors
- [ ] Deploy updated application

### Step 3: Functional Testing
- [ ] Login as non-admin user → Admin checkbox disabled or hidden
- [ ] Login as admin user → Admin checkbox enabled
- [ ] Check admin checkbox → AdminWindow launches with red theme
- [ ] Uncheck admin checkbox → MainWindow launches normally
- [ ] Verify admin tools accessible only to admin users

### Step 4: Security Validation
- [ ] Attempt to check admin box as non-admin → Blocked
- [ ] Verify RLS prevents client-side modification of is_admin
- [ ] Confirm only service role or dashboard can set is_admin
- [ ] Test that admin status syncs from Supabase to local DB

## SQL Server Equivalent

If you're also using SQL Server, add the column there too:

```sql
-- For SQL Server installations
ALTER TABLE dbo.Users
ADD IsAdmin BIT NOT NULL DEFAULT 0;

-- Create index
CREATE NONCLUSTERED INDEX IX_Users_IsAdmin 
ON dbo.Users(IsAdmin)
WHERE IsAdmin = 1;  -- Filtered index for admins only

-- Grant admin to specific user
UPDATE dbo.Users
SET IsAdmin = 1
WHERE Email = 'admin@yourcompany.com';
```

Add to [01_CreateDatabase.sql](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\Database\SqlServer\01_CreateDatabase.sql) for future deployments (around line 40):

```sql
CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(200) NOT NULL,
    [Email] NVARCHAR(200) NULL,
    [DisplayName] NVARCHAR(200) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsAdmin] BIT NOT NULL DEFAULT 0,  -- ADD THIS LINE
    [LastLogin] DATETIME2 NULL,
    -- ... rest of columns
```

## Security Best Practices

### 1. Never Trust Client-Side Checks
The checkbox validation is for UX only. Always verify admin status server-side:

```csharp
// In AdminWindow.Loaded or AdminWindowViewModel constructor:
var profile = SupabaseService.Instance.CurrentProfile;
if (profile?.IsAdmin != true)
{
    MessageBoxHelper.Show("Access Denied", "You do not have admin privileges.");
    Close();
    return;
}
```

### 2. Audit Admin Actions
Consider adding audit logging for all admin actions:

```sql
-- Create admin_audit_log table
CREATE TABLE admin_audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES auth.users(id),
    action TEXT NOT NULL,
    target_table TEXT,
    target_id TEXT,
    changes JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### 3. Rate Limiting
Implement rate limiting on admin endpoints in Supabase Edge Functions or your API layer.

### 4. Multi-Factor Authentication
Require MFA for admin users (configure in Supabase Auth settings).

## Rollout Strategy

### Phase 1: Schema Update (Safe - Read-Only)
1. Add `is_admin` column with DEFAULT FALSE
2. Create indexes
3. No impact on existing users

### Phase 2: Grant Admin Access
1. Identify admin users
2. Run UPDATE statements to set is_admin = TRUE
3. Verify changes

### Phase 3: Deploy Code
1. Add IsAdmin property to UserProfile.cs
2. Update LoginDialogViewModel with validation
3. Deploy to production

### Phase 4: Validation
1. Test admin login flow
2. Test non-admin blocked from admin mode
3. Verify admin tools accessible

## Troubleshooting

### Issue: Admin checkbox not appearing
**Cause**: `CanSelectAdmin` returns false  
**Fix**: Verify user's `is_admin` is TRUE in Supabase profiles table

### Issue: Admin checkbox appears but access denied
**Cause**: Profile not synced to local User table  
**Fix**: Ensure `CreateLocalUserAsync()` copies IsAdmin from profile

### Issue: Non-admin can check admin box
**Cause**: Validation not implemented  
**Fix**: Add validation in `IsAdminLogin` setter or `CanSelectAdmin`

### Issue: RLS blocking profile reads
**Cause**: RLS policy too restrictive  
**Fix**: Ensure policy allows `auth.uid() = id` for SELECT

## Migration Script (Complete)

Save as `add_admin_support.sql` and run in Supabase SQL Editor:

```sql
-- ============================================
-- Admin Support Migration for Tracker App
-- Run Date: 2025-12-24
-- ============================================

-- Add is_admin column
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name='profiles' AND column_name='is_admin'
    ) THEN
        ALTER TABLE profiles 
        ADD COLUMN is_admin BOOLEAN NOT NULL DEFAULT FALSE;
    END IF;
END $$;

-- Create index
CREATE INDEX IF NOT EXISTS idx_profiles_is_admin 
ON profiles(is_admin) 
WHERE is_admin = TRUE;

-- Add column comment
COMMENT ON COLUMN profiles.is_admin IS 
'Whether this user has administrator privileges for database management tools';

-- Grant admin to initial admin user (REPLACE WITH YOUR EMAIL)
-- UPDATE profiles 
-- SET is_admin = TRUE 
-- WHERE email = 'your-admin-email@example.com';

-- Verify changes
SELECT 
    column_name, 
    data_type, 
    column_default, 
    is_nullable
FROM information_schema.columns
WHERE table_name = 'profiles' 
AND column_name = 'is_admin';

SELECT 
    COUNT(*) as total_admins,
    array_agg(email) as admin_emails
FROM profiles 
WHERE is_admin = TRUE;
```

## Next Steps

1. ✅ Run database migration in Supabase
2. ✅ Update UserProfile.cs model
3. ✅ Set at least one user to is_admin = TRUE
4. ✅ Test admin login flow
5. ⏳ Add server-side validation in AdminWindow
6. ⏳ Implement audit logging for admin actions
7. ⏳ Add MFA requirement for admin users (optional)

## Related Files

- [UserProfile.cs](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\Services\Backend\Models\UserProfile.cs) - Supabase model
- [User.cs](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\DataModels\User.cs) - Local SQLite model  
- [LoginDialogViewModel.cs](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\ViewModels\DialogViewModels\LoginDialogViewModel.cs) - Login validation
- [AdminWindow.xaml](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\Views\AdminWindow.xaml) - Admin interface
- [ADMIN_LOGIN_IMPLEMENTATION.md](c:\Users\vbpro\source\repos\Tracker\Tracker\Tracker\Docs\ADMIN_LOGIN_IMPLEMENTATION.md) - Implementation details
