using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSExpandBlessUthstinPagePacket : GamePacket
{
    public CSExpandBlessUthstinPagePacket() : base(CSOffsets.CSExpandBlessUthstinPagePacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("CSExpandBlessUthstinPagePacket");

        // empty body
    
        var character = Connection.ActiveChar;
        var expandPageIndex = 0u;
        switch (character.Stats.PageCount)
        {
            case 1:
                expandPageIndex = 1;
                break;
            case 2:
                expandPageIndex = 2;
                break;
            case 3:
                return;
        }
        character.Stats.PageCount++;

        character.SendPacket(new SCBlessUthstinExpandPagePacket(
            character.ObjId,
            true,
            expandPageIndex // TODO уточнить что тут надо передавать
        ));
    }
}
