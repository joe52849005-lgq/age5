﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSSendUserMusicPacket : GamePacket
{
    public CSSendUserMusicPacket() : base(CSOffsets.CSSendUserMusicPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var songSize = stream.ReadUInt32(); // this is the size without the trailing null terminator 0x00
        var blockSize = stream.ReadUInt16();
        var data = stream.ReadBytes(blockSize);

        Logger.Debug("Caching MIDI data size: {0}/{1}", blockSize, songSize);
        MusicManager.Instance.CacheMidi(Connection.ActiveChar.Id, data);
    }
}
