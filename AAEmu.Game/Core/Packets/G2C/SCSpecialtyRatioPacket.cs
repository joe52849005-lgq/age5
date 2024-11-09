using System.Collections.Generic;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Trading;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCSpecialtyRatioPacket : GamePacket
{
    private readonly ushort _bundleId;
    private readonly uint _npcId;
    private readonly int _count;
    private readonly int _eventCount;
    private readonly bool _isBegin;
    private readonly bool _isEnd;
    private readonly List<ProductInformation> _productInformations;

    
    private readonly int _ratio;


    public SCSpecialtyRatioPacket(int ratio) : base(SCOffsets.SCSpecialtyRatioPacket, 5)
    {
        _ratio = ratio;
    }
    public SCSpecialtyRatioPacket(ushort bundleId, uint npcId,  List<ProductInformation> productInformations, bool isBegin, bool isEnd)
        : base(SCOffsets.SCSpecialtyRatioPacket, 5)
    {
        _bundleId = bundleId;
        _npcId = npcId;
        _productInformations = productInformations;
        _count = productInformations.Count;
        _eventCount = 0; // TODO сделать позже
        _isBegin = isBegin;
        _isEnd = isEnd;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_bundleId);
        stream.Write(_npcId);
        stream.Write(_count);
        stream.Write(_eventCount);
        stream.Write(_isBegin);
        stream.Write(_isEnd);
        foreach (var productInformation in _productInformations)
        {
            stream.Write(productInformation);
        }
        // TODO
        //foreach (var event in _events)
        //{
        //    stream.Write(event);
        //}
        return stream;
    }
}
