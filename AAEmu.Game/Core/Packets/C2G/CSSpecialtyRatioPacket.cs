using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSSpecialtyRatioPacket : GamePacket
{
    public CSSpecialtyRatioPacket() : base(CSOffsets.CSSpecialtyRatioPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var bundleId = stream.ReadUInt16();
        var npcId = stream.ReadUInt32();

        Logger.Debug("CSSpecialtyRatioPacket");

        SpecialtyManager.Instance.SendProductInformation(Connection.ActiveChar, bundleId, npcId);
    }
}
