using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCSocketingResultPacket : GamePacket
{
    private readonly byte _result;
    private readonly long _itemId;
    private readonly uint _itemType;
    private readonly byte _kind;
    private readonly bool _success;

    public SCSocketingResultPacket(byte result, long itemId, uint itemType, byte kind, bool success)
        : base(SCOffsets.SCSocketingResultPacket, 5)
    {
        _result = result;
        _itemId = itemId;
        _itemType = itemType;
        _kind = kind;
        _success = success;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_result);
        stream.Write(_itemId);
        stream.Write(_itemType);
        stream.Write(_kind);
        stream.Write(_success);
        return stream;
    }
}
