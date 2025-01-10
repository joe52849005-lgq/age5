using System;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Housing;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Mails.Static;

namespace AAEmu.Game.Models.Game.Mails;

public class MailForRebuild : BaseMail
{
    /*
     * Working example for 5070
     * /testmail 30 .houseTax title "body(30, 'Фермерский дом', 'Уютный фермерский дом')" 0 0 0
     * 
     * Values for the extra flag look as following
     * Extra = ((long)zoneGroupId << 48) + ((long)extraUnknown << 32) + ((long)houseId);
     */

    private House _house;
    private Item _item;

    private static string RebuildSenderName = ".houseRebuild";

    public MailForRebuild(House house, Item item = null)
    {
        _house = house;
        MailType = MailType.HousingRebuild;
        Header.Status = MailStatus.Unpaid;
        Body.SendDate = DateTime.UtcNow;
        Body.RecvDate = DateTime.UtcNow;
        _item = item;
    }

    public static bool UpdateRebuildInfo(BaseMail mail, House house, string originalHouseName, string rebuildHouseName)
    {
        // Check if owner is still valid
        var ownerName = NameManager.Instance.GetCharacterName(house.OwnerId);
        if (ownerName == null)
        {
            return false;
        }

        mail.Header.ReceiverId = house.OwnerId;
        mail.ReceiverName = ownerName;

        // Grab the zone the house is in
        var zone = ZoneManager.Instance.GetZoneByKey(house.Transform.ZoneId);
        if (zone == null)
        {
            return false;
        }

        // Set mail title
        mail.Title = "title";

        //// Get Tax info
        //if (!HousingManager.Instance.CalculateBuildingTaxInfo(house.AccountId, house.Template, false, out var totalTaxAmountDue, out var heavyTaxHouseCount, out var normalTaxHouseCount, out var hostileTaxRate, out var oneWeekTaxCount))
        //{
        //    return false;
        //}

        //// Note: I'm sure this can be done better, but it works and displays correctly
        //var lateFees = 0;
        //var paymentDeadLine = house.TaxDueDate;
        //if (house.TaxDueDate <= DateTime.UtcNow)
        //{
        //    lateFees = 1;
        //    paymentDeadLine = house.ProtectionEndDate;
        //}

        // for 5.0.7.0
        //testmail 30 .houseTax title "body(30, 'Фермерский дом', 'Уютный фермерский дом')" 0 0 0
        mail.Body.Text = string.Format("body({0}, '{1}', '{2}')",
            (int)mail.MailType,  // тип письма = HousingRebuild
            originalHouseName.Replace("'",""),  // исходное имя дома
            rebuildHouseName.Replace("'","")   // имя дома после перепланировки
        );

        mail.Body.CopperCoins = 0;
        mail.Body.BillingAmount = 0;
        mail.Body.MoneyAmount2 = 0;

        // Extra tag
        ushort extraUnknown = 0;
        mail.Header.Extra = ((long)zone.GroupId << 48) + ((long)extraUnknown << 32) + house.Id;

        //Body.Text = string.Format("body('{0}', {1}, {2})", _itemName, _item.Count, _itemBuyoutPrice);
        //_item.OwnerId = _buyerId;
        //_item.SlotType = SlotType.MailAttachment;
        //Body.Attachments.Add(_item);

        return true;
    }

    /// <summary>
    /// Prepare mail
    /// </summary>
    /// <returns></returns>
    public bool FinalizeMail(string originalHouseName)
    {
        var rebuildHouseName = _house.Name;

        Header.SenderId = (uint)SystemMailSenderKind.None;
        Header.SenderName = RebuildSenderName; // .houseRebuild

        return UpdateRebuildInfo(this, _house, originalHouseName, rebuildHouseName);
    }
}
