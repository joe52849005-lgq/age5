-- -------------------------------------------------
-- Adding fields to the `characters` table
-- -------------------------------------------------
ALTER TABLE `characters`
    ADD COLUMN `heir_Level` tinyint NOT NULL AFTER `recoverable_exp`,
    ADD COLUMN `heir_exp` int UNSIGNED NOT NULL AFTER `heir_Level`;
