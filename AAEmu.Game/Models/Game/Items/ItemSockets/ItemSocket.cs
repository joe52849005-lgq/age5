namespace AAEmu.Game.Models.Game.Items.ItemSockets;

public class ItemSocket
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string BuffModifierTooltip { get; set; }
    public int EisetId { get; set; }
    public int EquipItemTagId { get; set; }
    public int EquipItemId { get; set; }
    public int EquipSlotGroupId { get; set; }
    public bool Extractable { get; set; }
    public bool IgnoreEquipItemTag { get; set; }
    public int ItemSocketChanceId { get; set; }
    public string SkillModifierTooltip { get; set; }
}
