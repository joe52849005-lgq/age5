﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCAccountWarnedCodePacket : GamePacket
{
    private readonly byte _source;
    private readonly byte _messageType;
    private readonly string _msg;

    public SCAccountWarnedCodePacket(byte source, byte messageType, string msg) : base(SCOffsets.SCAccountWarnedCodePacket, 5)
    {
        _source = source;
        _messageType = messageType;
        _msg = msg;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_source);
        stream.Write(_messageType);
        stream.Write(_msg);
        return stream;
    }
}
