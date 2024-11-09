using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game.Trading;

public class ProductInformation : PacketMarshaler
{
    public uint ItemId { get; set; }
    public int Refund { get; set; }
    public int NoEventRefund { get; set; }
    public int Ratio { get; set; }
    public int Stock { get; set; }
    public bool CanProduce { get; set; }
    public sbyte Currency { get; set; }
    public byte Type { get; set; }


    public override void Read(PacketStream stream)
    {

        ItemId = stream.ReadUInt32();
        Refund = stream.ReadInt32();
        NoEventRefund = stream.ReadInt32();
        Ratio = stream.ReadInt32();
        Stock = stream.ReadInt32();
        CanProduce = stream.ReadBoolean();
        Currency = stream.ReadSByte();
        Type = stream.ReadByte();
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(ItemId);
        stream.Write(Refund);
        stream.Write(NoEventRefund);
        stream.Write(Ratio);
        stream.Write(Stock);
        stream.Write(CanProduce);
        stream.Write(Currency);
        stream.Write(Type);
        return stream;
    }

}
