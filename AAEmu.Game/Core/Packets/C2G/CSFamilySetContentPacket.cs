using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSFamilySetContentPacket : GamePacket
{
    public CSFamilySetContentPacket() : base(CSOffsets.CSFamilySetContentPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var content1 = stream.ReadString();
        var content2 = stream.ReadString();

        FamilyManager.Instance.SetContent(Connection.ActiveChar, content1, content2);

        Logger.Debug($"CSFamilySetContent, content1: {content1}, content2: {content2}");
    }
}
