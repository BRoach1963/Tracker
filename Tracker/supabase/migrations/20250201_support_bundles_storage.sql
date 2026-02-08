-- Create storage bucket for support bundles
-- Run this in Supabase SQL Editor

-- Create the bucket (private by default)
INSERT INTO storage.buckets (id, name, public)
VALUES ('support-bundles', 'support-bundles', false)
ON CONFLICT (id) DO NOTHING;

-- Allow authenticated users to upload to their own folder
CREATE POLICY "Users can upload support bundles"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK (
  bucket_id = 'support-bundles' AND
  (storage.foldername(name))[1] = 'bundles'
);

-- Allow authenticated users to get signed URLs for their uploads
CREATE POLICY "Users can read their support bundles"
ON storage.objects FOR SELECT
TO authenticated
USING (bucket_id = 'support-bundles');

-- Service role can read all (for Edge Function if needed)
CREATE POLICY "Service role can read all support bundles"
ON storage.objects FOR SELECT
TO service_role
USING (bucket_id = 'support-bundles');
