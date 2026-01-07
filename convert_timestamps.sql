-- Convert all timestamp with time zone columns to timestamp without time zone
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN (
        SELECT table_name, column_name 
        FROM information_schema.columns 
        WHERE data_type = 'timestamp with time zone' 
        AND table_schema = 'public'
    )
    LOOP
        EXECUTE format('ALTER TABLE %I ALTER COLUMN %I TYPE timestamp without time zone', r.table_name, r.column_name);
    END LOOP;
END $$;
