-- -------------------------------------------------
-- Adding fields to the `items` table
-- -------------------------------------------------
ALTER TABLE items
    ADD COLUMN freshness_time DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00'
	AFTER charge_count;
ALTER TABLE items
    ADD COLUMN charge_use_skill_time DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00'
	AFTER freshness_time;
ALTER TABLE items
    ADD COLUMN charge_start_time DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00'
	AFTER charge_use_skill_time;
ALTER TABLE items
    ADD COLUMN charge_proc_time DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00'
	AFTER charge_start_time;

