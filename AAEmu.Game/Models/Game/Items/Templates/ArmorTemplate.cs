﻿using System;

namespace AAEmu.Game.Models.Game.Items.Templates;

public class ArmorTemplate : EquipItemTemplate
{
    public override Type ClassType => typeof(Armor);

    public Wearable WearableTemplate { get; set; }
    public WearableKind KindTemplate { get; set; }
    public WearableSlot SlotTemplate { get; set; }
    public bool BaseEnchantable { get; set; }
    public bool BaseEquipment { get; set; }
    public uint AssetId { get; set; }
    public uint Asset2Id { get; set; }
    public uint EnhancedItemMaterialId { get; set; }
    public bool EquipOnlyHasArmorVisual { get; set; }
    public bool InvisibleAsset { get; set; }
    public string NoVisualErrorMessage { get; set; }
    public uint RechargeRestrictItemId { get; set; }
    public uint SkinKindId { get; set; }
    public bool UseAsStat { get; set; }
    public bool OrUnitReqs { get; set; }
}
