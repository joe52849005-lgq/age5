using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCEnchantMagicalResultPacket : GamePacket
{
    private readonly bool _result;
    private readonly ulong _itemId;
    private readonly uint _itemType;

    public SCEnchantMagicalResultPacket(bool result, ulong itemId, uint itemType)
        : base(SCOffsets.SCEnchantMagicalResultPacket, 5)
    {
        _result = result;
        _itemId = itemId;
        _itemType = itemType;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_result);
        stream.Write(_itemId);
        stream.Write(_itemType);
        return stream;
    }
}
