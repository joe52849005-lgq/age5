using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCGachaLootPackItemLogPacket : GamePacket
{
    private readonly byte _lootLogCount;
    private readonly (uint id, byte type, int stack)[] _items;

    public SCGachaLootPackItemLogPacket(byte lootLogCount, (uint id, byte type, int stack)[] items)
        : base(SCOffsets.SCGachaLootPackItemLogPacket, 5)
    {
        _lootLogCount = lootLogCount;
        _items = items;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_lootLogCount);
        for (var i = 0; i < _lootLogCount; i++)
        {
            stream.Write(_items[i].id);
            stream.Write(_items[i].type);
            stream.Write(_items[i].stack);
        }
        return stream;
    }
}
