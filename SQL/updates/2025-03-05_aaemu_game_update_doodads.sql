-- -------------------------------------------------
-- Adding fields to the `doodads` table
-- -------------------------------------------------
ALTER TABLE doodads
    ADD COLUMN freshness_time DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00'
	AFTER phase_time;

