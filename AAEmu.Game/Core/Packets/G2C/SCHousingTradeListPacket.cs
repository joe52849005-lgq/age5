using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCHousingTradeListPacket : GamePacket
{
    private readonly int _count;
    private readonly bool _first;
    private readonly bool _final;

    public SCHousingTradeListPacket(int count, bool first, bool final) : base(SCOffsets.SCHousingTradeListPacket, 5)
    {
        _count = count;
        _first = first;
        _final = final;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_count);
        stream.Write(_first);
        stream.Write(_final);

        return stream;
    }
}
