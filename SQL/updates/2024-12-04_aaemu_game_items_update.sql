-- Update items table with new values
ALTER TABLE items ADD COLUMN additional_details blob NULL AFTER details;