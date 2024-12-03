namespace AAEmu.Game.Models.Game.Items.ItemRndAttr;

public class ItemRndAttrUnitModifierGroup
{
    public int Id { get; set; }
    public bool FixedAttr { get; set; }
    public int ItemRndAttrUnitModifierGroupSetId { get; set; }
    public int UnitAttributeId { get; set; }
    public int UnitModifierTypeId { get; set; }
    public int Weight { get; set; }
}
