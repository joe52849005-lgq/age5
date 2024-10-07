using System;
using System.Collections.Generic;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Auction;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCAuctionSearchedPacket : GamePacket
{
    private List<AuctionItem> _auctionItems;
    private uint _page;
    private uint _count;
    private readonly List<AuctionLot> _lots;
    private readonly short _errorMsg;
    private readonly DateTime _serverTime;

    public SCAuctionSearchedPacket(List<AuctionItem> auctionItems, uint page) : base(SCOffsets.SCAuctionSearchedPacket, 5)
    {
        _auctionItems = auctionItems;
        _count = (uint)_auctionItems.Count;
        _page = page;
    }

    public SCAuctionSearchedPacket(uint page, uint count, List<AuctionLot> lots, short errorMsg, DateTime serverTime) :
        base(SCOffsets.SCAuctionSearchedPacket, 5)
    {
        _page = page;
        _count = count;
        _lots = lots;
        _errorMsg = errorMsg;
        _serverTime = serverTime;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_page);
        stream.Write(_count);

        foreach (var lot in _lots) // TODO не более 9
        {
            lot.Write(stream);
        }

        stream.Write(_errorMsg);
        stream.Write(_serverTime);

        return stream;
    }
}
