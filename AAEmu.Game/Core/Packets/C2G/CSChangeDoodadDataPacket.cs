﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSChangeDoodadDataPacket : GamePacket
{
    public CSChangeDoodadDataPacket() : base(CSOffsets.CSChangeDoodadDataPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var objId = stream.ReadBc();
        var data = stream.ReadInt32();

        Logger.Warn($"ChangeDoodadData, ObjId: {objId}, Data: {data}");

        var doodad = WorldManager.Instance.GetDoodad(objId);
        if (doodad != null)
        {
            var doodadName = LocalizationManager.Instance.Get("doodad_almighties", "name", doodad.TemplateId);
            var doodadType = doodad.Template.GetType().ToString();
            Logger.Warn($"Doodad: {doodad.Name} ({doodad.TemplateId} - {doodadName} - {doodadType})");

            if (Connection.ActiveChar != null)
            {
                if (!DoodadManager.ChangeDoodadData(Connection.ActiveChar, doodad, data))
                {
                    Connection.ActiveChar.SendErrorMessage(ErrorMessageType.InteractionPermissionDeny);
                    Logger.Warn($"Player {Connection.ActiveChar.Name} denied permission to change doodad data.");
                }
            }
            else
            {
                Logger.Error("Active character is null. Cannot change doodad data.");
            }
        }
        else
        {
            Logger.Warn($"Doodad with ObjId: {objId} not found.");
        }
    }
}
