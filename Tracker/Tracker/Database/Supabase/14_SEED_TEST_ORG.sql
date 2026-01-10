-- ============================================================================
-- TRACKER DATABASE - SEED DATA: TEST ORGANIZATION
-- ============================================================================
-- Run this to create a test organization with sample data for development

-- ============================================================================
-- CREATE TEST ORGANIZATION
-- ============================================================================
INSERT INTO organizations (id, name, slug, settings)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'Prickly Cactus Software',
    'prickly-corp',
    '{
        "timezone": "America/New_York",
        "dateFormat": "MM/dd/yyyy",
        "fiscalYearStart": "01-01",
        "features": {
            "goalsEnabled": true,
            "metricsEnabled": true,
            "feedbackEnabled": true,
            "aiEnabled": true
        },
        "branding": {
            "primaryColor": "#3B82F6",
            "logoUrl": null
        }
    }'::jsonb
);

SELECT 'Test organization created successfully' AS status;
