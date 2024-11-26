using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSUseBlessUthstinExtendMaxStatsPacket : GamePacket
{
    public CSUseBlessUthstinExtendMaxStatsPacket() : base(CSOffsets.CSUseBlessUthstinExtendMaxStatsPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("CSUseBlessUthstinExtendMaxStatsPacket");

        // empty body

        var character = Connection.ActiveChar;
        const uint itemTemplateId = (uint)ItemConstants.MigrationScale; // ID=8001059, Migration Scale

        character.Stats.IncreaseLimit();

        character.SendPacket(new SCBlessUthstinExtendMaxStatsPacket(character.ObjId, true, character.Stats.ExtendMaxStats, character.Stats.ApplyExtendCount));
        
        if (character.Inventory.Bag.ConsumeItem(ItemTaskType.BlessUthstinExpandMaxStats, itemTemplateId, 1, null) <= 0)
        {
            character.SendErrorMessage(ErrorMessageType.NotEnoughItem);
            Logger.Debug($"Not Enough Item {itemTemplateId}");
        }
    }
}
