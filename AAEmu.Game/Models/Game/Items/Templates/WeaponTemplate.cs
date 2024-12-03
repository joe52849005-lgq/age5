using System;

namespace AAEmu.Game.Models.Game.Items.Templates;

public class WeaponTemplate : EquipItemTemplate
{
    public override Type ClassType => typeof(Weapon);

    public bool BaseEnchantable { get; set; }
    public Holdable HoldableTemplate { get; set; }
    public bool BaseEquipment { get; set; }
    public bool UseAsStat { get; set; }
    public uint AssetId { get; set; }
    public float DrawnScale { get; set; }
    public uint EnhancedItemMaterialId { get; set; }
    public uint FixedAttackedSoundId { get; set; }
    public uint FixedVisualEffectId { get; set; }
    public uint RechargeRestrictItemId { get; set; }
    public uint SkinKindId { get; set; }
    public float WornScale { get; set; }
    public bool OrUnitReqs { get; set; }
}
