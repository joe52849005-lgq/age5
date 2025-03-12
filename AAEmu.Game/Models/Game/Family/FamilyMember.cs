using System;
using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Models.Game.Family;

public class FamilyMember : PacketMarshaler
{
    public Character Character { get; set; }
    public uint Id { get; set; }
    public string Name { get; set; }
    public byte Role { get; set; }
    public bool Online => Character != null;
    public string Title { get; set; }
    public DateTime RoleUpdateTime { get; set; }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(Id);                  // member
        stream.Write(Name);                // memberName
        stream.Write(Character.Level);     // level
        stream.Write(Character.HeirLevel); // heirLevel
        stream.Write(Role);                // role
        stream.Write(Character.IsOnline);  // online
        stream.Write(Title);               // title
        stream.Write(RoleUpdateTime);      // roleUpdateTime
        return stream;
    }
}