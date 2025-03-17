-- -------------------------------------------------
-- Set default values (DEFAULT 0) for the heir_level and heir_exp fields in the characters table.
-- -------------------------------------------------
ALTER TABLE `characters`
MODIFY COLUMN `heir_level` tinyint NOT NULL DEFAULT 0,
MODIFY COLUMN `heir_exp` int UNSIGNED NOT NULL DEFAULT 0;