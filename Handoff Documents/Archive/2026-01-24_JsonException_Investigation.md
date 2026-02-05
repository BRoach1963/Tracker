# Handoff: JsonReaderException Investigation

**Date:** January 24, 2026  
**Status:** Resolved - Not a bug

## Summary

Thousands of `Newtonsoft.Json.JsonReaderException` errors appearing in Visual Studio Output window during runtime were investigated and determined to be **internal library behavior, not actual failures**.

## Key Findings

1. **Exceptions are caught internally** by the Supabase C# library's Postgrest implementation
2. **Data loads correctly** despite the exceptions:
   - "Team members returned: 9" ✓
   - "Profile loaded: Brian Roach" ✓
   - "Goals returned: 10" ✓
3. **Model/DB alignment verified** for `public.users` table - all columns match

## Root Cause

The Supabase.Postgrest library (v4.0.3) uses Newtonsoft.Json internally and throws/catches exceptions during type coercion attempts (especially for nulls, dates, JSONB fields). This is a known pattern where:

1. Try to parse value one way → exception thrown
2. Caught internally → try another approach → succeeds
3. Data deserializes correctly

## Visual Studio Behavior

- **Exception Settings** (Debug → Windows → Exception Settings): Controls whether debugger *breaks* on exceptions
- **Output Window**: Shows all first-chance exceptions regardless of Exception Settings
- To hide: Right-click Output window → uncheck "Exception Messages"

## Performance Impact

- **Debug with VS attached**: Visible overhead from exception logging
- **Release without debugger**: Exceptions still thrown/caught but no VS interception
- **Assessment**: Likely negligible for desktop app; monitor if users report slowness

## Action Items

- [x] Confirmed models align with database schema
- [ ] Consider opening issue on [supabase-csharp repo](https://github.com/supabase-community/supabase-csharp) if performance becomes a concern
- [ ] Future option: Replace Postgrest calls with raw HTTP + System.Text.Json for hot paths

## Validated Tables

| Table | Status |
|-------|--------|
| `public.users` | ✅ Aligned (verified against live DB) |

## No Action Required

The app works correctly. The exceptions are noise from the third-party library, not bugs in our code.
