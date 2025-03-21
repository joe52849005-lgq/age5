﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCForceAttackSetPacket : GamePacket
{
    private readonly uint _objId;
    private readonly bool _on;

    public SCForceAttackSetPacket(uint objId, bool on) : base(SCOffsets.SCForceAttackSetPacket, 5)
    {
        _objId = objId;
        _on = on;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.WriteBc(_objId);
        stream.Write(_on);
        return stream;
    }
}
