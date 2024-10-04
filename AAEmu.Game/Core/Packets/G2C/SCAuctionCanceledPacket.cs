using System;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Auction;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCAuctionCanceledPacket : GamePacket
{
    private readonly AuctionItem item;
    public SCAuctionCanceledPacket(AuctionItem auctionItem) : base(SCOffsets.SCAuctionCanceledPacket, 5)
    {
        item = auctionItem;
    }

    public override PacketStream Write(PacketStream stream)
    {
        item.Write(stream);
        return stream;
    }
}
