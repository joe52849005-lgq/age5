using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.Skills;

public class DefaultSkill
{
    public SkillTemplate Template { get; set; }
    public byte Slot { get; set; }
    public bool AddToSlot { get; set; }
    // added in 5.0.7.0
    public int SkillActiveTypeId { get; set; }
    public int SkillBookCategoryId { get; set; }
    public int SkillId { get; set; }
    public int SlotIndex { get; set; }
}
