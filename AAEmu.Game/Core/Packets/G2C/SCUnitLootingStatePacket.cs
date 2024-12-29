﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCUnitLootingStatePacket : GamePacket
{
    private readonly uint _unitObjId;
    private readonly byte _looting;
    private readonly bool _autoLoot;

    /// <summary>
    /// Sets the state of a unit if they are busy looting or not
    /// </summary>
    /// <param name="unitObjId"></param>
    /// <param name="looting">Looting state, 2 seems to be "done looting"</param>
    public SCUnitLootingStatePacket(uint unitObjId, byte looting, bool autoLoot)
        : base(SCOffsets.SCUnitLootingStatePacket, 5)
    {
        _unitObjId = unitObjId;
        _looting = looting;
        _autoLoot = autoLoot;
    }
    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_unitObjId);
        stream.Write(_looting);
        stream.Write(_autoLoot);
        return stream;
    }
}
