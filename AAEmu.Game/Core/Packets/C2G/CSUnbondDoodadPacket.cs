﻿//using AAEmu.Commons.Network;
//using AAEmu.Game.Core.Network.Game;
//using AAEmu.Game.Core.Packets.G2C;

//namespace AAEmu.Game.Core.Packets.C2G;

//public class CSUnbondDoodadPacket : GamePacket
//{
//    public CSUnbondDoodadPacket() : base(CSOffsets.CSUnbondDoodadPacket, 5)
//    {
//    }

//    public override void Read(PacketStream stream)
//    {
//        var characterObjId = stream.ReadBc();
//        var doodadObjId = stream.ReadBc();

//        if (Connection.ActiveChar.ObjId != characterObjId || Connection.ActiveChar.Bonding == null || Connection.ActiveChar.Bonding.ObjId != doodadObjId)
//            return;

//        var doodad = Connection.ActiveChar.Bonding.GetOwner();
//        doodad.Seat.UnLoadPassenger(Connection.ActiveChar, doodad.ObjId); // we free up the place where we were sitting

//        Connection.ActiveChar.Bonding.SetOwner(null);
//        Connection.ActiveChar.Bonding = null;
//        Connection.ActiveChar.Transform.Parent = null;

//        Connection.ActiveChar.BroadcastPacket(new SCUnbondDoodadPacket(Connection.ActiveChar.ObjId, Connection.ActiveChar.Id, doodadObjId), true);
//    }
//}
