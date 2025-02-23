-- Author: NLObP - 2025/02/23
-- Correction in order for `Stone Pack` (ID=17684) to be consume during the construction of the house, when dressed on the character `Dawnsdrop Jerkin` (ID=34937).
-- Исправление, чтобы тратился "Груз строительного камня" (ID=17684) при строительстве дома при одетом на персонаже "Кожаный жилет консорциума" (ID=34937).
-- Original consume_item_id was 0
UPDATE "skill_effects" SET "consume_item_id"='17684' WHERE "id"='33096' AND "consume_item_id"='0' AND "skill_id"='14575'
