﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Expeditions;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCExpeditionListPacket : GamePacket
{
    private readonly Expedition[] _expeditions;

    public SCExpeditionListPacket() : base(SCOffsets.SCExpeditionListPacket, 5)
    {
        _expeditions = [];
    }

    public SCExpeditionListPacket(Expedition[] expeditions) : base(SCOffsets.SCExpeditionListPacket, 5)
    {
        _expeditions = expeditions; // TODO max 20
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write((byte)_expeditions.Length); // TODO max length 20
        foreach (var expedition in _expeditions)
            expedition.Write(stream);
        return stream;
    }
}
