using System.Collections.Generic;

using AAEmu.Game.Models.Game.Taxations;

namespace AAEmu.Game.Models.Game.Housing;

public class HousingTemplate
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint CategoryId { get; set; }
    public uint MainModelId { get; set; }
    public uint DoorModelId { get; set; }
    public uint StairModelId { get; set; }
    public bool AutoZ { get; set; }
    public bool GateExists { get; set; }
    public int Hp { get; set; }
    public uint RepairCost { get; set; }
    public float GardenRadius { get; set; }
    public string Family { get; set; }
    public Taxation Taxation { get; set; }
    public uint GuardTowerSettingId { get; set; }
    public float CinemaRadius { get; set; }
    public float AutoZOffsetX { get; set; }
    public float AutoZOffsetY { get; set; }
    public float AutoZOffsetZ { get; set; }
    public float Alley { get; set; }
    public float ExtraHeightAbove { get; set; }
    public float ExtraHeightBelow { get; set; }
    public uint DecoLimit { get; set; }
    public uint AbsoluteDecoLimit { get; set; }
    public uint HousingDecoLimitId { get; set; }
    public bool IsSellable { get; set; }
    public bool HeavyTax { get; set; }
    public bool AlwaysPublic { get; set; }
    // updated to version 5.0.7.0
    public int CinemaId { get; set; }
    public bool DecoExpandability { get; set; }
    public int DemolishRefundItemId { get; set; }
    public int HousingRebuildingPackId { get; set; }
    public int HousingSizeId { get; set; }
    public int HousingUccPackId { get; set; }
    public int PackageDemolishSealCount { get; set; }
    public int RotateItemCount { get; set; }
    public int RotateItemId { get; set; }
    public int ServerTransferDemolishRefundItemId { get; set; }
    public int TaxationId { get; set; }
    public int UccKindFloor { get; set; }
    public int UccKindOutwall { get; set; }
    public int UccKindRoof { get; set; }
    public int UccKindTop { get; set; }
    public int UccKindWall { get; set; }
    public int UccScaleFloor { get; set; }
    public int UccScaleOutwall { get; set; }
    public int UccScaleRoof { get; set; }
    public int UccScaleTop { get; set; }
    public int UccScaleWall { get; set; }

    public Dictionary<int, HousingBuildStep> BuildSteps { get; set; }
    public HousingBindingDoodad[] HousingBindingDoodad { get; set; }

    public HousingTemplate()
    {
        BuildSteps = new Dictionary<int, HousingBuildStep>();
    }
}
