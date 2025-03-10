﻿using AAEmu.Commons.Network;
using AAEmu.Login.Core.Network.Login;

namespace AAEmu.Login.Core.Packets.L2C
{
    public class ACJoinResponsePacket : LoginPacket
    {
        private readonly ushort _reason; // in 3.0 byte, in 5.0 ushort
        private readonly byte _authId;
        private readonly uint _afs;
        private readonly short _unk2;
        private readonly byte _unk3;
        private readonly byte _slotCount;

        public ACJoinResponsePacket(byte authId, ushort reason, uint afs, byte slotCount) : base(LCOffsets.ACJoinResponsePacket)
        {
            _reason = reason;
            _afs = afs;
            _slotCount = slotCount;
            _authId = authId;
            _unk2 = 0;
            _unk3 = 0;

        }

        public override PacketStream Write(PacketStream stream)
        {
            stream.Write(_authId);
            stream.Write(_reason);
            stream.Write(_afs);
            stream.Write(_unk2);
            stream.Write(_unk3);
            stream.Write(_slotCount);

            // afs[0] -> макс кол-во персонажей на всех серверах
            // afs[1] -> дополнительно кол-во персонажей на сервер при использовании предмета увеличения слота
            // afs[2] -> 1 - режим предварительного создания персонажей

            return stream;
        }
    }
}
