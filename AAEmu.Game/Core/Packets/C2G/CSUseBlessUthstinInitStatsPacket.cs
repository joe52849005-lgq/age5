using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSUseBlessUthstinInitStatsPacket : GamePacket
{
    public CSUseBlessUthstinInitStatsPacket() : base(CSOffsets.CSUseBlessUthstinInitStatsPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("CSUseBlessUthstinInitStatsPacket");

        var uthstinPageIndex = stream.ReadUInt32();

        var character = Connection.ActiveChar;
        const uint itemTemplateId = (uint)ItemConstants.MigrationScale; // ID=8001059, Migration Scale

        character.SendPacket(new SCBlessUthstinInitStatsPacket(character.ObjId, true, uthstinPageIndex));
        
        character.Stats.ResetStatsByPageIndex(uthstinPageIndex);

        if (character.Inventory.Bag.ConsumeItem(ItemTaskType.BlessUthstinInitStats, itemTemplateId, 1, null) <= 0)
        {
            character.SendErrorMessage(ErrorMessageType.NotEnoughItem);
            Logger.Debug($"Not Enough Item {itemTemplateId}");
        }
    }
}
