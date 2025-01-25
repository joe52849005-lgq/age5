using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World.Transform;

namespace AAEmu.Game.Models.Game.Skills.Plots;

public enum PlotObjectType : byte
{
    UNIT = 0x1,
    POSITION = 0x2
}

public class PlotObject : PacketMarshaler
{
    public PlotObjectType Type { get; set; }
    public uint UnitId { get; set; }
    public Transform Position { get; set; }

    public PlotObject(BaseUnit unit)
    {
        Type = PlotObjectType.UNIT;
        UnitId = unit.ObjId;
    }

    public PlotObject(uint unitId)
    {
        Type = PlotObjectType.UNIT;
        UnitId = unitId;
    }

    public PlotObject(Transform position)
    {
        Type = PlotObjectType.POSITION;
        Position = position.CloneDetached();
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write((byte)Type);

        switch (Type)
        {
            case PlotObjectType.UNIT:
                stream.WriteBc(UnitId);
                break;
            case PlotObjectType.POSITION:
                stream.WritePosition(Position.Local.Position);
                var ypr = Position.Local.ToRollPitchYawSBytes();
                stream.Write(ypr.Item1); // rotx
                stream.Write(ypr.Item2); // roty
                stream.Write(ypr.Item3); // rotz

                stream.WritePosition(Position.Local.Position); // added in 5.0.7.0
                var lineypr = Position.Local.ToRollPitchYawSBytes();
                stream.Write(lineypr.Item1); // rotx
                stream.Write(lineypr.Item2); // roty
                stream.Write(lineypr.Item3); // rotz

                stream.WriteBc(0); // unk1
                stream.WriteBc(0); // unk2
                stream.WriteBc(0); // unk3
                break;
        }

        return stream;
    }
}
