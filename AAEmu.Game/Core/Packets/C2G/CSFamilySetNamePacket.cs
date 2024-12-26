using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSFamilySetNamePacket : GamePacket
{
    public CSFamilySetNamePacket() : base(CSOffsets.CSFamilySetNamePacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var name = stream.ReadString();

        FamilyManager.Instance.SetName(Connection.ActiveChar, name);

        Logger.Debug($"CSFamilySetName, name: {name}");
    }
}
