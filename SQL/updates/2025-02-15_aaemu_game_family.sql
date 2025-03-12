-- --------------------------------------------
-- Table structure for family
-- --------------------------------------------

CREATE TABLE `families` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(255) NOT NULL,
    `level` INT NOT NULL,
    `exp` INT NOT NULL,
    `content1` TEXT,
    `content2` TEXT,
    `inc_member_count` INT NOT NULL,
    `reset_time` DATETIME NOT NULL,
    `change_name_time` DATETIME NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;