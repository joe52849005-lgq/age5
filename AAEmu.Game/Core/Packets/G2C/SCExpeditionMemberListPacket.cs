﻿using System.Collections.Generic;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Expeditions;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCExpeditionMemberListPacket : GamePacket
{
    private readonly uint _id;
    private readonly List<ExpeditionMember> _members;

    public SCExpeditionMemberListPacket(uint total, FactionsEnum id, List<ExpeditionMember> members) : base(SCOffsets.SCExpeditionMemberListPacket, 5)
    {
        _id = (uint)id;
        _members = members;
    }

    public SCExpeditionMemberListPacket(Expedition expedition) : base(SCOffsets.SCExpeditionMemberListPacket, 5)
    {
        _id = (uint)expedition.Id;
        _members = expedition.Members; // TODO max 20
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write((byte)_members.Count); // TODO max length 20
        stream.Write(_id); // expedition id
        foreach (var member in _members)
            member.Write(stream);
        return stream;
    }
}
