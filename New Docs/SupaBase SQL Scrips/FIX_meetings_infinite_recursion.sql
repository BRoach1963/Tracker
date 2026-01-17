-- ============================================================================
-- FIX: Meetings Table Infinite Recursion RLS Issue
-- ============================================================================
-- Problem: RLS policies on meetings and meeting_attendees referenced each other,
--          causing "infinite recursion detected in policy for relation meetings"
--
-- Solution: Use simple ownership check on meetings (no cross-table references)
--           meeting_attendees can reference meetings (one-way reference only)
--
-- Created: January 17, 2026
-- ============================================================================

-- Step 1: Drop all existing policies on meetings to clear the recursion
DROP POLICY IF EXISTS "Users can view their own meetings" ON meetings;
DROP POLICY IF EXISTS "Users can insert their own meetings" ON meetings;
DROP POLICY IF EXISTS "Users can update their own meetings" ON meetings;
DROP POLICY IF EXISTS "Users can delete their own meetings" ON meetings;
DROP POLICY IF EXISTS "Users can view meetings they created" ON meetings;
DROP POLICY IF EXISTS "Users can insert meetings" ON meetings;
DROP POLICY IF EXISTS "Users can update meetings they created" ON meetings;
DROP POLICY IF EXISTS "Users can delete meetings they created" ON meetings;

-- Step 2: Ensure RLS is enabled
ALTER TABLE meetings ENABLE ROW LEVEL SECURITY;

-- Step 3: Create simple, non-recursive policies
-- These only check created_by_user_id = auth.uid() with NO cross-table references

-- SELECT: Users can view meetings they created
CREATE POLICY "Users can view meetings they created"
ON meetings FOR SELECT
USING (created_by_user_id = auth.uid());

-- INSERT: Users can create meetings (WITH CHECK, not USING)
CREATE POLICY "Users can insert meetings"
ON meetings FOR INSERT
WITH CHECK (created_by_user_id = auth.uid());

-- UPDATE: Users can update meetings they created
CREATE POLICY "Users can update meetings they created"
ON meetings FOR UPDATE
USING (created_by_user_id = auth.uid());

-- DELETE: Users can delete meetings they created
CREATE POLICY "Users can delete meetings they created"
ON meetings FOR DELETE
USING (created_by_user_id = auth.uid());

-- Step 4: Verify the policies were created correctly
SELECT tablename, policyname, cmd, qual, with_check 
FROM pg_policies 
WHERE tablename = 'meetings';

-- Expected results:
-- SELECT: qual = (created_by_user_id = auth.uid()), with_check = NULL
-- INSERT: qual = NULL, with_check = (created_by_user_id = auth.uid())
-- UPDATE: qual = (created_by_user_id = auth.uid()), with_check = NULL
-- DELETE: qual = (created_by_user_id = auth.uid()), with_check = NULL
