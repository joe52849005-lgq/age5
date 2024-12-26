using System;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCFamilyInfoSetPacket : GamePacket
{
    private readonly uint _familyId;
    private readonly int _level;
    private readonly int _exp;
    private readonly string _name;
    private readonly string _content1;
    private readonly string _content2;
    private readonly uint _ownerId;
    private readonly int _incMemberCount;
    private readonly DateTime _changeNameTime;

    public SCFamilyInfoSetPacket(uint familyId, int level, int exp, string name, string content1, string content2, uint ownerId, int incMemberCount, DateTime changeNameTime)
        : base(SCOffsets.SCFamilyInfoSetPacket, 5)
    {
        _familyId = familyId;
        _level = level;
        _exp = exp;
        _name = name;
        _content1 = content1;
        _content2 = content2;
        _ownerId = ownerId;
        _incMemberCount = incMemberCount;
        _changeNameTime = changeNameTime;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_familyId);
        stream.Write(_level);
        stream.Write(_exp);
        stream.Write(_name);
        stream.Write(_content1);
        stream.Write(_content2);
        stream.Write(_ownerId);
        stream.Write(_incMemberCount);
        stream.Write(_changeNameTime);
        return stream;
    }
}
