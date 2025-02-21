using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSCofferInteractionPacket : GamePacket
{
    public CSCofferInteractionPacket() : base(CSOffsets.CSCofferInteractionPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var cofferObjId = stream.ReadBc();
        var opening = stream.ReadBoolean();

        Logger.Warn($"CofferInteraction, cofferObjId: {cofferObjId}, opening: {opening}");

        var activeChar = Connection.ActiveChar;
        if (activeChar == null)
        {
            Logger.Warn("Active character is null. Cannot process coffer interaction.");
            return;
        }

        if (opening)
        {
            if (!DoodadManager.OpenCofferDoodad(activeChar, cofferObjId))
            {
                Logger.Warn($"{activeChar.Name} failed to open coffer objId {cofferObjId}");
                // Если не удалось открыть, coffer, вероятно, используется другим игроком
                activeChar.SendErrorMessage(ErrorMessageType.CofferInUse);
            }
        }
        else
        {
            if (!DoodadManager.CloseCofferDoodad(activeChar, cofferObjId))
            {
                Logger.Warn($"{activeChar.Name} failed to close coffer objId {cofferObjId}");
            }
        }
    }
}
