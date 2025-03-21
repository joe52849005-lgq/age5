﻿using System;

using AAEmu.Commons.Network;
using AAEmu.Commons.Utils;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.World.Transform;

namespace AAEmu.Game.Models.Game.Friend;

public class Friend : PacketMarshaler
{
    public uint CharacterId { get; set; }
    public string Name { get; set; }
    public Race Race { get; set; }
    public byte Level { get; set; }
    public byte HeirLevel { get; set; }
    public int Health { get; set; }
    public AbilityType Ability1 { get; set; }
    public AbilityType Ability2 { get; set; }
    public AbilityType Ability3 { get; set; }
    public Transform Position { get; set; } = new Transform(null, null);
    public bool InParty { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastWorldLeaveTime { get; set; }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(CharacterId);
        stream.Write(Name);
        stream.Write((byte)Race);
        stream.Write(Level);
        stream.Write(HeirLevel);
        stream.Write(Health);
        stream.Write((byte)Ability1);
        stream.Write((byte)Ability2);
        stream.Write((byte)Ability3);
        stream.Write(Helpers.ConvertLongX(Position.World.Position.X));
        stream.Write(Helpers.ConvertLongY(Position.World.Position.Y));
        stream.Write(Position.World.Position.Z);
        stream.Write(Position.ZoneId);
        stream.Write((uint)0); // type(id)
        stream.Write(InParty);
        stream.Write(IsOnline);
        stream.Write(LastWorldLeaveTime);
        return stream;
    }
}
