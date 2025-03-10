﻿using System;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Mails.Static;

namespace AAEmu.Game.Models.Game.Mails;

public class MailForAuction : BaseMail
{
    private uint _buyerId;
    private uint _sellerId;
    private Item _item;
    private string _itemName;
    private int _itemBuyoutPrice;
    private int _sellerShare;
    private int _listingFee;
    private int _tradeTaxFee;

    private static string AuctionName = "Auctioneer";
    // TODO: verify title names
    private static string TitleSold = "Successful Auction Notice";
    private static string TitleNotSold = "Failed Auction Notice";
    private static string TitleBidWin = "Succesfull Purchase";
    private static string TitleBidLost = "Failed Bid Notice";
    private static string TitleCancel = "Cancelled Auction Notice";

    // Mail examples for 1.2

    public MailForAuction(Item itemToSell, uint sellerId, int buyoutPrice, int listingFee) : base()
    {
        _buyerId = 0;
        _sellerId = sellerId;
        _item = itemToSell;
        _itemBuyoutPrice = buyoutPrice;
        _sellerShare = 0;
        _listingFee = listingFee;
        _tradeTaxFee = 0;
        _itemName = LocalizationManager.Instance.Get("items", "name", _item.TemplateId, "Item:" + itemToSell.TemplateId.ToString());

        // Correct types and name will be set in finalize functions
        MailType = MailType.Invalid;
        Header.SenderId = (uint)SystemMailSenderKind.None;
        Header.SenderName = AuctionName; // Name changes depending on type of mail

        Body.SendDate = DateTime.UtcNow;
        Body.RecvDate = DateTime.UtcNow; // These mails should always be instant
    }

    /// <summary>
    /// Please refactor AH to not use this function, only finalize with FinalizeForBidFail() or face null exceptions
    /// </summary>
    /// <param name="itemTemplateIdToSell"></param>
    /// <param name="sellerId"></param>
    /// <param name="buyoutPrice"></param>
    /// <param name="listingFee"></param>
    public MailForAuction(uint itemTemplateIdToSell, uint sellerId, int buyoutPrice, int listingFee) : base()
    {
        _buyerId = 0;
        _sellerId = sellerId;
        _item = null;
        _itemBuyoutPrice = buyoutPrice;
        _sellerShare = 0;
        _listingFee = listingFee;
        _tradeTaxFee = 0;
        _itemName = LocalizationManager.Instance.Get("items", "name", itemTemplateIdToSell, "Item:" + itemTemplateIdToSell.ToString());

        // Correct types and name will be set in finalize functions
        MailType = MailType.Invalid;
        Header.SenderId = (uint)SystemMailSenderKind.None;
        Header.SenderName = AuctionName; // Name changes depending on type of mail

        Body.SendDate = DateTime.UtcNow;
        Body.RecvDate = DateTime.UtcNow; // These mails should always be instant
    }

    /// <summary>
    /// Prepare mail for the person who is buying the item
    /// </summary>
    /// <returns></returns>
    public bool FinalizeForSaleBuyer(uint buyerId)
    {
        // /testmail 16 .auctionBidWin AHBidWin "body('My Sold Item', 7, 400000)"
        _buyerId = buyerId;

        var nameBuyer = NameManager.Instance.GetCharacterName(_buyerId);
        if (nameBuyer == null)
            return false;

        Header.SenderName = ".auctionBidWin";
        ReceiverName = nameBuyer;

        MailType = MailType.AuctionBidWin;
        Title = TitleBidWin;
        Header.ReceiverId = _buyerId;

        Body.Text = string.Format("body('{0}', {1}, {2})", _itemName.Replace("'", ""), _item.Count, _itemBuyoutPrice);
        _item.OwnerId = _buyerId;
        _item.SlotType = SlotType.MailAttachment;
        Body.Attachments.Add(_item);

        return true;
    }

    /// <summary>
    /// Prepare mail for the person selling the item
    /// </summary>
    /// <returns></returns>
    public bool FinalizeForSaleSeller(int sellerShare, int tradeTaxFee)
    {
        // /testmail 14 .auctionOffSuccess AHBuy "body('My Sold Item',7, 364000, 400000, 40000, 4000)"
        _sellerShare = sellerShare;
        _tradeTaxFee = tradeTaxFee;

        var nameSeller = NameManager.Instance.GetCharacterName(_sellerId);
        if (nameSeller == null)
            return false;

        Header.SenderName = ".auctionOffSuccess";
        Header.ReceiverId = _sellerId;
        ReceiverName = nameSeller;

        MailType = MailType.AuctionOffSuccess;
        Title = TitleSold;

        Body.Text = string.Format("body('{0}', {1}, {2}, {3}, {4}, {5})",
            _itemName.Replace("'", ""), _item.Count, _sellerShare, _itemBuyoutPrice, _tradeTaxFee, _listingFee);

        AttachMoney(sellerShare);

        return true;
    }

    /// <summary>
    /// Prepare mail for returning item to owner because of cancel
    /// </summary>
    /// <returns></returns>
    public bool FinalizeForCancel()
    {
        // /testmail 13 .auctionOffCancel AHCancel "body('My Sold Item',7)"

        var nameSeller = NameManager.Instance.GetCharacterName(_sellerId);
        if (nameSeller == null)
            return false;

        Header.SenderName = ".auctionOffCancel";
        Header.ReceiverId = _sellerId;
        ReceiverName = nameSeller;

        MailType = MailType.AuctionOffCancel;
        Title = TitleCancel;

        Body.Text = string.Format("body('{0}', {1})", _itemName.Replace("'", ""), _item.Count);
        _item.OwnerId = _sellerId;
        _item.SlotType = SlotType.MailAttachment;
        Body.Attachments.Add(_item);

        return true;
    }

    /// <summary>
    /// Prepare mail for returning item to owner because of expired
    /// </summary>
    /// <returns></returns>
    public bool FinalizeForFail()
    {
        // /testmail 15 .auctionOffFail AHFail "body('My Sold Item',7)"

        var nameSeller = NameManager.Instance.GetCharacterName(_sellerId);
        if (nameSeller == null)
            return false;

        Header.SenderName = ".auctionOffFail";
        Header.ReceiverId = _sellerId;
        ReceiverName = nameSeller;

        MailType = MailType.AuctionOffFail;
        Title = TitleNotSold;

        Body.Text = string.Format("body('{0}', {1})", _itemName.Replace("'", ""), _item.Count);
        _item.OwnerId = _sellerId;
        _item.SlotType = SlotType.MailAttachment;
        Body.Attachments.Add(_item);

        return true;
    }

    /// <summary>
    /// Prepare mail for the person who was outbid
    /// </summary>
    /// <returns></returns>
    public bool FinalizeForBidFail(uint previousBuyerId, int previousBid)
    {
        // /testmail 17 .auctionBidFail AHBidFail "body('My Sold Item')"
        _buyerId = previousBuyerId;

        var nameBuyer = NameManager.Instance.GetCharacterName(_buyerId);
        if (nameBuyer == null)
            return false;

        Header.SenderName = ".auctionBidFail";
        Header.ReceiverId = _buyerId;
        ReceiverName = nameBuyer;

        MailType = MailType.AuctionOffFail;
        Title = TitleBidLost;

        Body.Text = string.Format("body('{0}')", _itemName.Replace("'", ""));

        AttachMoney(previousBid);

        return true;
    }
}
