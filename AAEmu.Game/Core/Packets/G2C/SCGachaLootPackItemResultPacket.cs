using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;

namespace AAEmu.Game.Core.Packets.G2C
{
    public class SCGachaLootPackItemResultPacket : GamePacket
    {
        private readonly ErrorMessageType _errorMessage;
        private readonly int _count;
        private readonly int _itemCount;
        private readonly bool _finish;
        private readonly Item[] _items;

        public SCGachaLootPackItemResultPacket(ErrorMessageType errorMessage,
            int count, int itemCount, bool finish, Item[] items)
            : base(SCOffsets.SCGachaLootPackItemResultPacket, 5)
        {
            _errorMessage = errorMessage;
            _count = count;
            _itemCount = itemCount;
            _finish = finish;
            _items = items;
        }

        public override PacketStream Write(PacketStream stream)
        {
            stream.Write((short)_errorMessage);
            if (_errorMessage == 0)
            {
                stream.Write(_count);
                stream.Write(_itemCount);
                stream.Write(_finish);
                foreach (var item in _items)
                {
                    stream.Write(item);
                }
            }

            return stream;
        }
    }
}
