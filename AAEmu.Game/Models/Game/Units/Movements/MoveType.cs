﻿using System.Numerics;

using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.Game.Models.Game.Units.Movements;

public enum MoveTypeEnum
{
    Default = 0,
    Unit = 1,
    Vehicle = 2,
    Vehicle2 = 3,
    Ship = 4,
    ShipRequest = 5,
    Transfer = 6
}

public abstract class MoveType : PacketMarshaler
{
    public MoveTypeEnum Type { get; set; }
    public uint Time { get; set; }
    public WorldPos WorldPos { get; set; }
    public MoveTypeFlags Flags { get; set; }
    public uint ScType { get; set; }
    public byte Phase { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public Quaternion Rot { get; set; } // Z-axis rotation value must be in radians
    public Vector3 Velocity { get; set; }
    public short VelX { get; set; }
    public short VelY { get; set; }
    public short VelZ { get; set; }
    public sbyte RotationX { get; set; }
    public sbyte RotationY { get; set; }
    public sbyte RotationZ { get; set; }

    public override void Read(PacketStream stream)
    {
        Time = stream.ReadUInt32();
        Flags = (MoveTypeFlags)stream.ReadByte();
        if (Flags.HasFlag(MoveTypeFlags.HasScTypeAndPhase))
        {
            ScType = stream.ReadUInt32();
            Phase = stream.ReadByte();
            //Logger.Warn("ScType: {0} Phase: {1}", ScType, Phase);
        }
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(Time);
        stream.Write((byte)Flags);
        if (Flags.HasFlag(MoveTypeFlags.HasScTypeAndPhase))
        {
            stream.Write(ScType);
            stream.Write(Phase);
        }

        return stream;
    }

    public static MoveType GetType(MoveTypeEnum type)
    {
        MoveType mType = null;
        switch (type)
        {
            case MoveTypeEnum.Unit:
                mType = new UnitMoveType();
                break;
            case MoveTypeEnum.Vehicle:
                mType = new VehicleMoveType();
                break;
            case MoveTypeEnum.Vehicle2:
                mType = new VehicleMoveType();
                break;
            case MoveTypeEnum.Ship:
                mType = new ShipMoveType();
                break;
            case MoveTypeEnum.ShipRequest:
                mType = new ShipRequestMoveType();
                break;
            case MoveTypeEnum.Transfer:
                mType = new TransferData();
                break;
            default:
                mType = new DefaultMoveType();
                break;
        }

        mType.Type = type;
        return mType;
    }
}
