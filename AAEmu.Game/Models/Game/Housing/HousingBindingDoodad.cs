using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.World.Transform;

namespace AAEmu.Game.Models.Game.Housing;

public class HousingBindingDoodad
{
    public AttachPointKind AttachPointId { get; set; }
    public uint DoodadId { get; set; }
    // updated to version 5.0.7.0
    public bool ForceDbSave { get; set; }
    public int HousingId { get; set; }
    public WorldSpawnPosition Position { get; set; }
}
