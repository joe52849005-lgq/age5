using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSTakeAttachmentSequentiallyPacket : GamePacket
{
    public CSTakeAttachmentSequentiallyPacket() : base(CSOffsets.CSTakeAttachmentSequentiallyPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var takeMoney = false;
        var takeItems = false;
        var mailId = stream.ReadInt64();

        Logger.Debug("TakeAttachmentSequentially, mailId: {0}", mailId);
        var mail = MailManager.Instance.GetMailById(mailId);
        if (mail is null)
        {
            return;
        }
        if (mail.Header.ReceiverId != Connection.ActiveChar.Id) // just a check for hackers trying to steal mails
        {
            Connection.ActiveChar.SendErrorMessage(ErrorMessageType.MailInvalid);
        }
        else
        {
            if (mail.Body.CopperCoins > 0 || mail.Body.BillingAmount > 0 || mail.Body.MoneyAmount2 > 0)
                takeMoney = true;
            if (mail.Body.Attachments.Count > 0)
                takeItems = true;

            Connection.ActiveChar.Mails.GetAttached(mailId, takeMoney, takeItems, true);
        }
    }
}
