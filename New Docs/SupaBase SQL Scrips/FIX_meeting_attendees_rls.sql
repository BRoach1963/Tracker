-- ============================================================================
-- FIX: meeting_attendees RLS Policies
-- ============================================================================
-- Problem: "permission denied for table meeting_attendees" error
--          No RLS policies existed for meeting_attendees table
--
-- Solution: Create policies that reference the parent meetings table.
--           Users can access attendees for meetings they created.
--
-- IMPORTANT: This uses one-way reference (meeting_attendees → meetings).
--            The meetings table MUST have simple policies (no reverse reference)
--            to avoid infinite recursion.
--
-- Created: January 16, 2026
-- Updated: January 17, 2026
-- ============================================================================

-- Enable RLS on the table if not already enabled
ALTER TABLE meeting_attendees ENABLE ROW LEVEL SECURITY;

-- Drop existing policies if any (to avoid conflicts)
DROP POLICY IF EXISTS "Users can view meeting_attendees for their meetings" ON meeting_attendees;
DROP POLICY IF EXISTS "Users can insert meeting_attendees for their meetings" ON meeting_attendees;
DROP POLICY IF EXISTS "Users can update meeting_attendees for their meetings" ON meeting_attendees;
DROP POLICY IF EXISTS "Users can delete meeting_attendees for their meetings" ON meeting_attendees;

-- Create SELECT policy: Users can view attendees for meetings they created
CREATE POLICY "Users can view meeting_attendees for their meetings"
ON meeting_attendees
FOR SELECT
USING (
    EXISTS (
        SELECT 1 FROM meetings m 
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);

-- Create INSERT policy: Users can add attendees to their own meetings
-- Note: INSERT uses WITH CHECK, not USING
CREATE POLICY "Users can insert meeting_attendees for their meetings"
ON meeting_attendees
FOR INSERT
WITH CHECK (
    EXISTS (
        SELECT 1 FROM meetings m 
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);

-- Create UPDATE policy: Users can update attendees for their own meetings  
CREATE POLICY "Users can update meeting_attendees for their meetings"
ON meeting_attendees
FOR UPDATE
USING (
    EXISTS (
        SELECT 1 FROM meetings m 
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);

-- Create DELETE policy: Users can remove attendees from their own meetings
CREATE POLICY "Users can delete meeting_attendees for their meetings"
ON meeting_attendees
FOR DELETE
USING (
    EXISTS (
        SELECT 1 FROM meetings m 
        WHERE m.id = meeting_attendees.meeting_id 
        AND m.created_by_user_id = auth.uid()
    )
);

-- Verify the policies
SELECT tablename, policyname, cmd, qual, with_check 
FROM pg_policies 
WHERE tablename = 'meeting_attendees';

-- Expected results:
-- SELECT: qual = EXISTS(...), with_check = NULL
-- INSERT: qual = NULL, with_check = EXISTS(...)  <-- NULL qual is normal for INSERT!
-- UPDATE: qual = EXISTS(...), with_check = NULL
-- DELETE: qual = EXISTS(...), with_check = NULL

-- Verify the policies were created
SELECT tablename, policyname, cmd, qual 
FROM pg_policies 
WHERE tablename = 'meeting_attendees';
