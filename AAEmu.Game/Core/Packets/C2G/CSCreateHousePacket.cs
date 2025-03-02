﻿using AAEmu.Commons.Network;
using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSCreateHousePacket : GamePacket
{
    public CSCreateHousePacket() : base(CSOffsets.CSCreateHousePacket, 5)
    {
        //
    }

    public override void Read(PacketStream stream)
    {
        Logger.Debug("Entering in CSCreateHousePacket...");

        var designId = stream.ReadUInt32();
        var x = Helpers.ConvertLongX(stream.ReadInt64());
        var y = Helpers.ConvertLongY(stream.ReadInt64());
        var z = stream.ReadSingle();
        var zRot = stream.ReadSingle();
        var itemId = stream.ReadUInt64();
        var moneyAmount = stream.ReadInt32();
        var ht = stream.ReadInt32();
        var autoUseAaPoint = stream.ReadBoolean();

        Logger.Debug($"CreateHouse, Id: {designId}, X: {x}, Y: {y}, Z: {z}, ZRot: {zRot}");

        HousingManager.Instance.Build(Connection, designId, x, y, z, zRot, itemId, moneyAmount, ht, autoUseAaPoint);
    }
}
