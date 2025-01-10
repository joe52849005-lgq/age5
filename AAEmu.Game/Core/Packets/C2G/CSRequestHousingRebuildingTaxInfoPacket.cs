using System;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSRequestHousingRebuildingTaxInfoPacket : GamePacket
{
    public CSRequestHousingRebuildingTaxInfoPacket() : base(CSOffsets.CSRequestHousingRebuildingTaxInfoPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("Entering in CSRequestHousingRebuildingTaxInfoPacket...");

        var tl = stream.ReadUInt16();

        Logger.Debug($"RequestHousingRebuildingTaxInfo, Tl: {tl}");

        Connection.ActiveChar.SendPacket(new SCRebuildHouseTaxInfoPacket(tl, 0, 0, 0, 0, DateTime.MinValue, false, 0, 0, false));

        //HousingManager.Instance.CancelForSale(tl, true);
    }
}
