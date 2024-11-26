DROP TABLE IF EXISTS `character_stats`;
CREATE TABLE character_stats (
    character_id INT PRIMARY KEY,
    PageIndex INT,
    value0 INT, value1 INT, value2 INT, value3 INT, value4 INT,
    value5 INT, value6 INT, value7 INT, value8 INT, value9 INT,
    value10 INT, value11 INT, value12 INT, value13 INT, value14 INT,
    ApplyNormalCount0 INT, ApplySpecialCount0 INT,
    ApplyNormalCount1 INT, ApplySpecialCount1 INT,
    ApplyNormalCount2 INT, ApplySpecialCount2 INT,
    PageCount INT,
    ApplyExtendCount INT,
    ExtendMaxStats INT
);