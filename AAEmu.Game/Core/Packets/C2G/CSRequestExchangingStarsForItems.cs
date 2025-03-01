using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSRequestExchangingStarsForItems : GamePacket
{
    public CSRequestExchangingStarsForItems() : base(CSOffsets.CSRequestExchangingStarsForItems, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        // TODO придумать имя получше
        Logger.Debug("Entering in CSRequestExchangingStarsForItems...");

        var objId = stream.ReadBc();
        var itemId = stream.ReadUInt32();
        var useAAPoint = stream.ReadBoolean();

        Logger.Debug($"CSRequestExchangingStarsForItems, objId: {objId}, itemId: {itemId}, useAAPoint: {useAAPoint}");
    }
}
