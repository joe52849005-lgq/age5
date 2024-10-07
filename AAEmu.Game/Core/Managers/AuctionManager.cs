using System;
using System.Collections.Generic;
using System.Linq;

using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Auction;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Mails;

using MySql.Data.MySqlClient;

using NLog;

namespace AAEmu.Game.Core.Managers;

public class AuctionManager : Singleton<AuctionManager>
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    //public Dictionary<ulong, AuctionLot> AuctionLots;
    public List<AuctionLot> AuctionLots;
    public List<AuctionBid> AuctionBids;
    public List<AuctionSold> AuctionSolds;

    public List<AuctionItem> _auctionItems;
    public List<long> _deletedAuctionItemIds;

    private static int MaxListingFee = 1000000; // 100g, 100 copper coins = 1 silver, 100 silver = 1 gold.

    public void ListAuctionItem(Character player, ulong itemId, int startPrice, int buyoutPrice, byte duration, int minStack, int maxStack)
    {
        var newItem = player.Inventory.GetItemById(itemId);
        var newAuctionItem = CreateAuctionItem(player, newItem, startPrice, buyoutPrice, duration);

        if (newAuctionItem == null) // TODO
            return;

        if (newItem == null) // TODO
            return;

        var auctionFee = newAuctionItem.DirectMoney * .01 * (duration + 1);

        if (auctionFee > MaxListingFee)
            auctionFee = MaxListingFee;

        if (!player.ChangeMoney(SlotType.Bag, -(int)auctionFee))
        {
            player.SendErrorMessage(ErrorMessageType.CanNotPutupMoney);
            return;
        }
        player.Inventory.Bag.RemoveItem(ItemTaskType.Auction, newItem, true);
        AddAuctionItem(newAuctionItem);
        //player.SendPacket(new SCAuctionPostedPacket(newAuctionItem));
    }

    private void RemoveAuctionItemSold(AuctionItem itemToRemove, string buyer, int soldAmount)
    {
        if (_auctionItems.Contains(itemToRemove))
        {
            var itemTemplate = ItemManager.Instance.GetItemTemplateFromItemId(itemToRemove.ItemId);
            var newItem = ItemManager.Instance.Create(itemTemplate.Id, (int)itemToRemove.StackSize, itemToRemove.Grade);
            var itemList = new Item[10].ToList();
            itemList[0] = newItem;

            var moneyAfterFee = soldAmount * .9;
            var moneyToSend = new int[3];
            moneyToSend[0] = (int)moneyAfterFee;

            // TODO: Read this from saved data
            var recalculatedFee = itemToRemove.DirectMoney * .01 * (itemToRemove.Duration + 1);
            if (recalculatedFee > MaxListingFee) recalculatedFee = MaxListingFee;

            if (itemToRemove.ClientName != "")
            {
                var sellMail = new MailForAuction(newItem, itemToRemove.ClientId, soldAmount, (int)recalculatedFee);
                sellMail.FinalizeForSaleSeller((int)moneyAfterFee, (int)(soldAmount - moneyAfterFee));
                sellMail.Send();
            }

            var buyMail = new MailForAuction(newItem, itemToRemove.ClientId, soldAmount, (int)recalculatedFee);
            var buyerId = NameManager.Instance.GetCharacterId(buyer);
            buyMail.FinalizeForSaleBuyer(buyerId);
            buyMail.Send();

            RemoveAuctionItem(itemToRemove);
        }
    }

    private void RemoveAuctionItemFail(AuctionItem itemToRemove)
    {
        if (!_auctionItems.Contains(itemToRemove))
            return;

        if (itemToRemove.BidderName != "") //Player won the bid. 
        {
            RemoveAuctionItemSold(itemToRemove, itemToRemove.BidderName, itemToRemove.BidMoney);
            return;
        }
        else //Item did not sell by end of the timer. 
        {
            var itemTemplate = ItemManager.Instance.GetItemTemplateFromItemId(itemToRemove.ItemId);
            var newItem = ItemManager.Instance.Create(itemTemplate.Id, (int)itemToRemove.StackSize, itemToRemove.Grade);
            var itemList = new Item[10].ToList();
            itemList[0] = newItem;

            // TODO: Read this from saved data
            var recalculatedFee = itemToRemove.DirectMoney * .01 * (itemToRemove.Duration + 1);
            if (recalculatedFee > MaxListingFee) recalculatedFee = MaxListingFee;

            if (itemToRemove.ClientName != "")
            {
                var failMail = new MailForAuction(newItem, itemToRemove.ClientId, itemToRemove.DirectMoney, (int)recalculatedFee);
                failMail.FinalizeForFail();
                failMail.Send();
            }

            RemoveAuctionItem(itemToRemove);
        }
    }

    public void CancelAuctionItem(Character player, ulong auctionId)
    {
        var auctionItem = GetAuctionItemFromId(auctionId);

        if (auctionItem.BidderName != "") return;// Someone has already bid on the item and we do not want to remove it. 

        var moneyToSubtract = auctionItem.DirectMoney * .1f;
        var itemList = new Item[10].ToList();
        var newItem = ItemManager.Instance.Create(auctionItem.ItemId, (int)auctionItem.StackSize, auctionItem.Grade);
        itemList[0] = newItem;

        // TODO: Read this from saved data
        var recalculatedFee = auctionItem.DirectMoney * .01 * (auctionItem.Duration + 1);
        if (recalculatedFee > MaxListingFee) recalculatedFee = MaxListingFee;

        var cancelMail = new MailForAuction(newItem, auctionItem.ClientId, auctionItem.DirectMoney, (int)recalculatedFee);
        cancelMail.FinalizeForCancel();
        cancelMail.Send();
        // MailManager.Instance.SendMail(0, auctionItem.ClientName, "AuctionHouse", "Cancelled Listing", "See attached.", 1, new int[3], 0, itemList);

        RemoveAuctionItem(auctionItem);
        //player.SendPacket(new SCAuctionCanceledPacket(auctionItem));
    }

    private AuctionItem GetAuctionItemFromId(ulong auctionId)
    {
        var item = _auctionItems.Single(c => c.Id == auctionId);

        return item;
    }

    public void BidOnAuctionItem(Character player, ulong auctionId, int bidAmount)
    {
        var auctionItem = GetAuctionItemFromId(auctionId);
        if (auctionItem != null)
        {
            if (bidAmount >= auctionItem.DirectMoney && auctionItem.DirectMoney != 0) // Buy now
            {
                if (auctionItem.BidderId != 0) // send mail to person who bid if item was bought at full price. 
                {
                    var newMail = new MailForAuction(auctionItem.ItemId, auctionItem.ClientId, auctionItem.DirectMoney, 0);
                    newMail.FinalizeForBidFail(auctionItem.BidderId, auctionItem.BidMoney);
                    newMail.Send();
                }

                player.SubtractMoney(SlotType.Bag, auctionItem.DirectMoney);
                RemoveAuctionItemSold(auctionItem, player.Name, auctionItem.DirectMoney);
            }

            else if (bidAmount > auctionItem.BidMoney) // Bid
            {
                if (auctionItem.BidderName != "" && auctionItem.BidderId != 0) // Send mail to old bidder. 
                {
                    var moneyArray = new int[3];
                    moneyArray[0] = auctionItem.BidMoney;

                    // TODO: Read this from saved data
                    var recalculatedFee = auctionItem.DirectMoney * .01 * (auctionItem.Duration + 1);
                    if (recalculatedFee > MaxListingFee) recalculatedFee = MaxListingFee;

                    var cancelMail = new MailForAuction(auctionItem.ItemId, auctionItem.ClientId, auctionItem.DirectMoney, (int)recalculatedFee);
                    cancelMail.FinalizeForBidFail(auctionItem.BidderId, auctionItem.BidMoney);
                    cancelMail.Send();
                }

                // Set info to new bidders info
                auctionItem.BidderName = player.Name;
                auctionItem.BidderId = player.Id;
                auctionItem.BidWorldId = (byte)player.Transform.WorldId;
                auctionItem.BidMoney = bidAmount;

                player.SubtractMoney(SlotType.Bag, bidAmount, ItemTaskType.Auction);
                player.SendPacket(new SCAuctionBidPacket(auctionItem));
                auctionItem.IsDirty = true;
            }
        }
    }

    public List<AuctionItem> GetAuctionItems(AuctionSearch searchTemplate)
    {
        List<AuctionItem> auctionItemsFound = new List<AuctionItem>();
        //bool myListing = false;

        //if (searchTemplate.ItemName == "" && searchTemplate.CategoryA == 0 && searchTemplate.CategoryB == 0 && searchTemplate.CategoryC == 0)
        //{
        //    myListing = true;
        //    auctionItemsFound = _auctionItems.Where(c => c.ClientId == searchTemplate.PlayerId).ToList();
        //}

        //if (!myListing)
        //{
        //    var itemTemplateList = ItemManager.Instance.GetAllItems();
        //    var query = from item in itemTemplateList
        //                where ((searchTemplate.ItemName != "") ? item.searchString.Contains(searchTemplate.ItemName.ToLower()) : true)
        //                where ((searchTemplate.CategoryA != 0) ? searchTemplate.CategoryA == item.AuctionCategoryA : true)
        //                where ((searchTemplate.CategoryB != 0) ? searchTemplate.CategoryB == item.AuctionCategoryB : true)
        //                where ((searchTemplate.CategoryC != 0) ? searchTemplate.CategoryC == item.AuctionCategoryC : true)
        //                select item;

        //    var selectedItemList = query.ToList();

        //    auctionItemsFound = _auctionItems.Where(c => selectedItemList.Any(c2 => c2.Id == c.ItemId)).ToList();
        //}

        //if (searchTemplate.SortKind == 1) //Price
        //{
        //    var sortedList = auctionItemsFound.OrderByDescending(x => x.DirectMoney).ToList();
        //    auctionItemsFound = sortedList;
        //    if (searchTemplate.SortOrder == 1)
        //        auctionItemsFound.Reverse();
        //}

        ////TODO 2 Name of item
        ////TODO 3 Level of item

        //if (searchTemplate.SortKind == 4) //TimeLeft
        //{
        //    var sortedList = auctionItemsFound.OrderByDescending(x => x.TimeLeft).ToList();
        //    auctionItemsFound = sortedList;
        //    if (searchTemplate.SortOrder == 1)
        //        auctionItemsFound.Reverse();
        //}

        //if (searchTemplate.Page > 0)
        //{
        //    var startingItemNumber = (int)(searchTemplate.Page * 9);
        //    var endingitemNumber = (int)((searchTemplate.Page * 9) + 9);
        //    if (auctionItemsFound.Count > startingItemNumber)
        //    {
        //        var tempItemList = new List<AuctionItem>();
        //        for (var i = startingItemNumber; i < endingitemNumber; i++)
        //        {
        //            if (auctionItemsFound.ElementAtOrDefault(i) != null)
        //                tempItemList.Add(auctionItemsFound[i]);
        //        }
        //        auctionItemsFound = tempItemList;
        //    }
        //    else
        //        searchTemplate.Page = 0;
        //}

        //if (auctionItemsFound.Count > 9)
        //{
        //    var tempList = new List<AuctionItem>();

        //    for (int i = 0; i < 9; i++)
        //    {
        //        tempList.Add(auctionItemsFound[i]);
        //    }

        //    auctionItemsFound = tempList;
        //}
        return auctionItemsFound;
    }

    public AuctionItem GetCheapestAuctionItem(ulong itemId)
    {
        var tempList = new List<AuctionItem>();

        foreach (var item in _auctionItems)
        {
            if (item.ItemId == itemId)
                tempList.Add(item);
        }

        if (tempList.Count > 0)
        {
            tempList = tempList.OrderByDescending(x => x.DirectMoney).ToList();
            return tempList.First();
        }
        else
        {
            return null;
        }
    }

    public List<AuctionSold> GetSoldAuctionLots(uint templateId, byte itemGrade)
    {
        var idx = 0;
        if (AuctionSolds?.Count > 0)
        {
            var temp = AuctionSolds.OrderByDescending(x => x.Id).ToList();
            var auctionSold = temp.First();
            idx = auctionSold.Id;

        }
        var tempList = AuctionSolds.Where(lot => lot.ItemId == templateId).ToList();

        if (tempList.Count <= 0)
        {
            AuctionSolds = GenerateRandomAuctionSolds(templateId, itemGrade, ref idx);

            return AuctionSolds;
        }

        tempList = tempList.OrderBy(x => x.Day).ToList();

        return tempList;
    }

    private static List<AuctionSold> GenerateRandomAuctionSolds(uint templateId, byte itemGrade, ref int idx)
    {
        var tempList = new List<AuctionSold>();

        for (var i = 0; i < 14; i++)
        {
            tempList.Add(GenerateRandomAuctionSold(templateId, itemGrade, ref idx, i));
        }

        return tempList;
    }

    private static AuctionSold GenerateRandomAuctionSold(uint templateId, byte itemGrade, ref int idx, int day)
    {
        var Random = new Random();
        var MinCopper = Random.Next(1, 5999);
        var MaxCopper = Random.Next(6000, 9999);
        var AvgCopper = (MaxCopper + MinCopper) / 2;
        var Volume = Random.Next(1, 5999);

        var item = new AuctionSold
        {
            Id = ++idx,
            ItemId = templateId,
            Day = day,
            MinCopper = MinCopper,
            MaxCopper = MaxCopper,
            AvgCopper = AvgCopper,
            Volume = Volume,
            ItemGrade = itemGrade,
            WeeklyAvgCopper = 1400
        };

        return item;
    }

    public AuctionLot GetCheapestAuctionLot(uint templateId)
    {
        var tempList = AuctionLots.Where(lot => lot.Item.TemplateId == templateId).ToList();

        if (tempList.Count <= 0)
        {
            return null;
        }

        tempList = tempList.OrderBy(x => x.DirectMoney).ToList();

        return tempList.First();
    }

    public void CheapestAuctionLot(Character player, uint templateId, byte itemGrade = 0)
    {
        var DirectMoney = 0;
        var cheapestItem = GetCheapestAuctionLot(templateId);
        if (cheapestItem != null)
        {
            DirectMoney = cheapestItem.DirectMoney;
        }

        player.SendPacket(new SCAuctionLowestPricePacket(templateId, itemGrade, DirectMoney));
    }

    public static string GetLocalizedItemNameById(uint id)
    {
        return LocalizationManager.Instance.Get("items", "name", id, ItemManager.Instance.GetTemplate(id).Name ?? "");
    }

    public ulong GetNextId()
    {
        ulong nextId = 0;
        foreach (var item in AuctionLots)
        {
            if (nextId < item.Id)
                nextId = item.Id;
        }
        return nextId + 1;
    }

    public void RemoveAuctionItem(AuctionItem itemToRemove)
    {
        if (itemToRemove.ClientName == "") //Testing feature. Relists an item if the server listed it. 
        {
            itemToRemove.EndTime = DateTime.UtcNow.AddHours(48);
            return;
        }
        lock (_auctionItems)
        {
            lock (_deletedAuctionItemIds)
            {
                if (_auctionItems.Contains(itemToRemove))
                {
                    _deletedAuctionItemIds.Add((long)itemToRemove.Id);
                    _auctionItems.Remove(itemToRemove);
                }
            }
        }
    }

    public void AddAuctionItem(AuctionItem itemToAdd)
    {
        lock (_auctionItems)
        {
            _auctionItems.Add(itemToAdd);
        }
    }

    public void AddLot(AuctionLot lot)
    {
        AuctionLots.Add(lot);
    }

    public void UpdateAuctionHouse()
    {
        Logger.Trace("Updating Auction House!");
        lock (_auctionItems)
        {
            var itemsToRemove = _auctionItems.Where(c => DateTime.UtcNow > c.EndTime);

            foreach (var item in itemsToRemove)
            {
                if (item.BidderId != 0)
                    RemoveAuctionItemSold(item, item.BidderName, item.BidMoney);
                else
                    RemoveAuctionItemFail(item);
            }
        }
    }

    public AuctionItem CreateAuctionItem(Character player, Item itemToList, int startPrice, int buyoutPrice, byte duration)
    {
        var newItem = itemToList;

        ulong timeLeft;
        switch (duration)
        {
            case 0:
                timeLeft = 6; //6 hours
                break;
            case 1:
                timeLeft = 12; //12 hours
                break;
            case 2:
                timeLeft = 24; //24 hours
                break;
            case 3:
                timeLeft = 48; //48 hours
                break;
            default:
                timeLeft = 6; //default to 6 hours
                break;
        }

        var newAuctionItem = new AuctionItem
        {
            Id = GetNextId(),
            Duration = 5,
            ItemId = newItem.Template.Id,
            ObjectId = 0,
            Grade = newItem.Grade,
            Flags = newItem.ItemFlags,
            StackSize = (uint)newItem.Count,
            DetailType = 0,
            CreationTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(timeLeft),
            LifespanMins = 0,
            MadeUnitId = 0,
            WorldId = 0,
            UnpackDateTime = DateTime.UtcNow,
            UnsecureDateTime = DateTime.UtcNow,
            ChargeUseSkillTime = DateTime.UtcNow, // added in 5+
            WorldId2 = 0,
            ClientId = player.Id,
            ClientName = player.Name,
            StartMoney = startPrice,
            DirectMoney = buyoutPrice,
            ChargePercent = 100, // added in 5+
            BidWorldId = 0,
            BidderId = 0,
            BidderName = "",
            BidMoney = 0,
            Extra = 0,
            MinStack = 1, // added in 5+
            MaxStack = 100, // added in 5+
            IsDirty = true
        };
        return newAuctionItem;
    }

    public AuctionLot CreateAuctionLot(Character player, Item itemToList, int startPrice, int buyoutPrice, AuctionDuration duration, int minStack, int maxStack)
    {
        ulong timeLeft;
        switch (duration)
        {
            case AuctionDuration.AuctionDuration6Hours:
                timeLeft = 6; //6 hours
                break;
            case AuctionDuration.AuctionDuration12Hours:
                timeLeft = 12; //12 hours
                break;
            case AuctionDuration.AuctionDuration24Hours:
                timeLeft = 24; //24 hours
                break;
            case AuctionDuration.AuctionDuration48Hours:
                timeLeft = 48; //48 hours
                break;
            default:
                timeLeft = 6; //default to 6 hours
                break;
        }

        var newAuctionLot = new AuctionLot();
        newAuctionLot.Id = GetNextId();
        newAuctionLot.Duration = duration;

        newAuctionLot.Item = itemToList;

        newAuctionLot.EndTime = DateTime.UtcNow.AddHours(timeLeft);

        newAuctionLot.WorldId = 1;
        newAuctionLot.ClientId = player.Id;
        newAuctionLot.ClientName = player.Name;
        newAuctionLot.StartMoney = startPrice;
        newAuctionLot.DirectMoney = buyoutPrice;
        newAuctionLot.PostDate = DateTime.UtcNow;
        newAuctionLot.ChargePercent = 1000; // added in 5+
        newAuctionLot.BidWorldId = 255;
        newAuctionLot.BidderId = 0;
        newAuctionLot.BidderName = "";
        newAuctionLot.BidMoney = 0;
        newAuctionLot.Extra = 0;
        newAuctionLot.MinStack = minStack; // added in 5+
        newAuctionLot.MaxStack = maxStack; // added in 5+
        newAuctionLot.IsDirty = true;

        return newAuctionLot;
    }

    public void Load()
    {
        AuctionLots = new List<AuctionLot>();
        AuctionBids = new List<AuctionBid>();
        AuctionSolds = new List<AuctionSold>();

        _auctionItems = new List<AuctionItem>();
        _deletedAuctionItemIds = new List<long>();

        var auctionTask = new AuctionHouseTask();
        TaskManager.Instance.Schedule(auctionTask, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        // TODO отключим считывание сохранение в базу

        return;

        //using (var connection = MySQL.CreateConnection())
        //{
        //    using (var command = connection.CreateCommand())
        //    {
        //        command.CommandText = "SELECT * FROM auction_house";
        //        command.Prepare();
        //        using (var reader = command.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                var auctionItem = new AuctionItem();
        //                auctionItem.Id = reader.GetUInt64("id");
        //                auctionItem.Duration = reader.GetByte("duration"); // 8 is 6 hours, 9 is 12 hours, 10 is 24 hours, 11 is 48 hours
        //                auctionItem.ItemId = reader.GetUInt32("item_id");
        //                auctionItem.Id = reader.GetUInt32("object_id");
        //                auctionItem.Grade = reader.GetByte("grade");
        //                auctionItem.Flags = (ItemFlag)reader.GetByte("flags");
        //                auctionItem.StackSize = reader.GetUInt32("stack_size");
        //                auctionItem.DetailType = reader.GetByte("detail_type");
        //                auctionItem.CreationTime = reader.GetDateTime("creation_time");
        //                auctionItem.EndTime = reader.GetDateTime("end_time");
        //                auctionItem.LifespanMins = reader.GetUInt32("lifespan_mins");
        //                auctionItem.MadeUnitId = reader.GetUInt32("made_unit_id");
        //                auctionItem.WorldId = reader.GetByte("world_id");
        //                auctionItem.UnsecureDateTime = reader.GetDateTime("unsecure_date_time");
        //                auctionItem.UnpackDateTime = reader.GetDateTime("unpack_date_time");

        //                auctionItem.ChargeUseSkillTime = reader.GetDateTime("charge_use_skill_time"); // added in 5+

        //                auctionItem.WorldId2 = reader.GetByte("world_id_2");
        //                auctionItem.ClientId = reader.GetUInt32("client_id");
        //                auctionItem.ClientName = reader.GetString("client_name");
        //                auctionItem.StartMoney = reader.GetInt32("start_money");
        //                auctionItem.DirectMoney = reader.GetInt32("direct_money");

        //                auctionItem.ChargePercent = reader.GetInt32("charge_percent"); // added in 5+

        //                auctionItem.BidWorldId = reader.GetByte("bid_world_id");
        //                auctionItem.BidderId = reader.GetUInt32("bidder_id");
        //                auctionItem.BidderName = reader.GetString("bidder_name");
        //                auctionItem.BidMoney = reader.GetInt32("bid_money");
        //                auctionItem.Extra = reader.GetUInt32("extra");

        //                auctionItem.MinStack = reader.GetUInt32("min_stack"); // added in 5+
        //                auctionItem.MaxStack = reader.GetUInt32("max_stack"); // added in 5+

        //                AddAuctionItem(auctionItem);
        //            }
        //        }
        //    }
        //}
        //var auctionTask = new AuctionHouseTask();
        //TaskManager.Instance.Schedule(auctionTask, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public (int, int) Save(MySqlConnection connection, MySqlTransaction transaction)
    {
        var deletedCount = 0;
        var updatedCount = 0;

        // TODO отключим считывание сохранение в базу

        return (0, 0);

        //lock (_deletedAuctionItemIds)
        //{
        //    deletedCount = _deletedAuctionItemIds.Count;
        //    if (_deletedAuctionItemIds.Count > 0)
        //    {
        //        using (var command = connection.CreateCommand())
        //        {
        //            command.Connection = connection;
        //            command.Transaction = transaction;
        //            command.CommandText = "DELETE FROM auction_house WHERE `id` IN(" + string.Join(",", _deletedAuctionItemIds) + ")";
        //            command.Prepare();
        //            command.ExecuteNonQuery();
        //        }
        //        _deletedAuctionItemIds.Clear();
        //    }
        //}

        ////var dirtyItems = _auctionItems.Where(c => c.IsDirty == true);
        //var dirtyItems = AuctionLots.Where(c => c.IsDirty == true);
        //foreach (var mtbs in dirtyItems)
        //{
        //    if (mtbs.Item == null)
        //        continue;

        //    if (mtbs.Item.SlotType == SlotType.Invalid)
        //    {
        //        // Only give an error if it has no owner, otherwise it's likely a BuyBack item
        //        if (mtbs.Item.OwnerId <= 0)
        //            continue;

        //        // Try to re-attain the slot type by getting the owning container's type
        //        if (mtbs.Item._holdingContainer != null)
        //        {
        //            mtbs.Item.SlotType = ItemManager.Instance.GetContainerSlotTypeByContainerId(mtbs.Item._holdingContainer.ContainerId);
        //        }

        //        // If the slot type changed, give a warning, otherwise skip this save
        //        if (mtbs.Item.SlotType != SlotType.Invalid)
        //        {
        //            Logger.Warn($"Slot type for {mtbs.Item.Id} was None, changing to {mtbs.Item.SlotType}");
        //        }
        //        else
        //        {
        //            continue;
        //        }
        //    }
        //    if (!Enum.IsDefined(typeof(SlotType), mtbs.Item.SlotType))
        //    {
        //        Logger.Warn($"Found SlotType.{mtbs.Item.SlotType} in itemslist, skipping ID:{mtbs.Item.Id} - Template:{mtbs.Item.TemplateId}");
        //        continue;
        //    }

        //    var details = new Commons.Network.PacketStream();
        //    mtbs.Item.WriteDetails(details);

        //    using (var command = connection.CreateCommand())
        //    {
        //        command.Connection = connection;
        //        command.Transaction = transaction;

        //        command.CommandText = "REPLACE INTO auction_house(" +
        //            "`id`, `duration`, `item_id`, `object_id`, `grade`, `flags`, `stack_size`, `detail_type`," +
        //            " `creation_time`,`end_time`, `lifespan_mins`, `made_unit_id`, `world_id`, `unsecure_date_time`, `unpack_date_time`, `charge_use_skill_time`," +
        //            " `world_id_2`, `client_id`, `client_name`, `start_money`, `direct_money`, `charge_percent`, `bid_world_id`," +
        //            " `bidder_id`, `bidder_name`, `bid_money`, `extra`, `min_stack`, `max_stack`" +
        //            ") VALUES (" +
        //            "@id, @duration, @item_id, @object_id, @grade, @flags, @stack_size, @detail_type," +
        //            " @creation_time, @end_time, @lifespan_mins, @made_unit_id, @world_id, @unsecure_date_time, @unpack_date_time, @charge_use_skill_time," +
        //            " @world_id_2, @client_id, @client_name, @start_money, @direct_money, @charge_percent, @bid_world_id," +
        //            " @bidder_id, @bidder_name, @bid_money, @extra, @min_stack, @max_stack)";

        //        command.Parameters.AddWithValue("@id", mtbs.Id);
        //        command.Parameters.AddWithValue("@duration", (byte)mtbs.Duration);

        //        command.Parameters.AddWithValue("@item_id", mtbs.Item.TemplateId); // itemId
        //        command.Parameters.AddWithValue("@object_id", mtbs.Item.Id);       // id
        //        command.Parameters.AddWithValue("@grade", mtbs.Item.Grade);
        //        command.Parameters.AddWithValue("@flags", mtbs.Item.Flags);
        //        command.Parameters.AddWithValue("@stack_size", mtbs.Item.Count);
        //        command.Parameters.AddWithValue("@detail_type", mtbs.Item.DetailType);

        //        command.Parameters.AddWithValue("@details", details.GetBytes());

        //        command.Parameters.AddWithValue("@lifespan_mins", mtbs.Item.LifespanMins);
        //        command.Parameters.AddWithValue("@made_unit_id", mtbs.Item.MadeUnitId);
        //        command.Parameters.AddWithValue("@unsecure_date_time", mtbs.Item.UnsecureTime);
        //        command.Parameters.AddWithValue("@unpack_date_time", mtbs.Item.UnpackTime);
        //        command.Parameters.AddWithValue("@creation_time", mtbs.Item.CreateTime);

        //        command.Parameters.AddWithValue("@world_id", mtbs.Item.WorldId);

        //        command.Parameters.AddWithValue("@end_time", mtbs.EndTime);

        //        command.Parameters.AddWithValue("@charge_use_skill_time", mtbs.Item.ChargeUseSkillTime); // added in 5+
        //        command.Parameters.AddWithValue("@world_id_2", mtbs.WorldId);
        //        command.Parameters.AddWithValue("@client_id", mtbs.ClientId);
        //        command.Parameters.AddWithValue("@client_name", mtbs.ClientName);
        //        command.Parameters.AddWithValue("@start_money", mtbs.StartMoney);
        //        command.Parameters.AddWithValue("@direct_money", mtbs.DirectMoney);
        //        command.Parameters.AddWithValue("@time_left", mtbs.TimeLeft);
        //        command.Parameters.AddWithValue("@charge_percent", mtbs.ChargePercent); // added in 5+
        //        command.Parameters.AddWithValue("@bid_world_id", mtbs.BidWorldId);
        //        command.Parameters.AddWithValue("@bidder_id", mtbs.BidderId);
        //        command.Parameters.AddWithValue("@bidder_name", mtbs.BidderName);
        //        command.Parameters.AddWithValue("@bid_money", mtbs.BidMoney);
        //        command.Parameters.AddWithValue("@extra", mtbs.Extra);
        //        command.Parameters.AddWithValue("@min_stack", mtbs.MinStack); // added in 5+
        //        command.Parameters.AddWithValue("@max_stack", mtbs.MaxStack); // added in 5+

        //        command.Prepare();
        //        command.ExecuteNonQuery();
        //        updatedCount++;
        //        mtbs.IsDirty = false;
        //    }
        //}

        //return (updatedCount, deletedCount);
    }

    private List<AuctionLot> SortArticles(List<AuctionLot> articles, AuctionSearchSortKind kind, AuctionSearchSortOrder order)
    {
        if (kind == AuctionSearchSortKind.BidPrice)
        {
            if (order == AuctionSearchSortOrder.Asc)
            {
                return articles.OrderBy(o => o.BidMoney).ToList();
            }

            return articles.OrderByDescending(o => o.BidMoney).ToList();
        }

        if (kind == AuctionSearchSortKind.DirectPrice)
        {
            if (order == AuctionSearchSortOrder.Asc)
            {
                return articles.OrderBy(o => o.DirectMoney).ToList();
            }

            return articles.OrderByDescending(o => o.DirectMoney).ToList();
        }

        if (kind == AuctionSearchSortKind.ExpireDate) // TODO This will need to be fixed later due to varying durations
        {
            if (order == AuctionSearchSortOrder.Asc)
            {
                return articles.OrderBy(o => o.PostDate).ToList();
            }

            return articles.OrderByDescending(o => o.PostDate).ToList();
        }

        if (kind == AuctionSearchSortKind.ItemLevel)
        {
            if (order == AuctionSearchSortOrder.Asc)
            {
                return articles.OrderBy(o => o.Item.Template.Level).ToList();
            }

            return articles.OrderByDescending(o => o.Item.Template.Level).ToList();
        }

        return articles;
    }

    public void SearchAuctionLots(Character player, AuctionSearch search)
    {
        var searchedArticles = new List<AuctionLot>();
        foreach (var lot in AuctionLots)
        {
            var template = lot.Item.Template;
            var settings = template.AuctionSettings;
            if (settings.CategoryA != search.CategoryA && search.CategoryA != 0)
                continue;
            if (settings.CategoryB != search.CategoryB && search.CategoryB != 0)
                continue;
            if (settings.CategoryC != search.CategoryC && search.CategoryC != 0)
                continue;
            if (lot.Item.Grade != search.Grade && search.Grade != 0)
                continue;
            if (template.Level > search.MaxItemLevel && search.MaxItemLevel != 0)
                continue;
            if (template.Level < search.MinItemLevel && search.MinItemLevel != 0)
                continue;
            searchedArticles.Add(lot);
        }

        if (searchedArticles.Count == 0)
        {
            player.SendPacket(new SCAuctionSearchedPacket(0, 0, [], (short)ErrorMessageType.NoErrorMessage, DateTime.UtcNow));
            return;
        }
        var articles = SortArticles(searchedArticles, search.SortKind, search.SortOrder).ToArray();
        var dividedLists = Helpers.SplitArray(articles, 9); // Разделяем массив на массивы по 9 значений
        player.SendPacket(new SCAuctionSearchedPacket(search.Page, dividedLists[search.Page].Length, dividedLists[search.Page].ToList(), (short)ErrorMessageType.NoErrorMessage, DateTime.UtcNow));
    }

    public void PostLotOnAuction(Character player, uint npcId, uint npcId2, ulong itemId, int startPrice, int buyoutPrice, AuctionDuration duration, int minStack, int maxStack)
    {
        var item = ItemManager.Instance.GetItemByItemId(itemId);
        if (item == null)
        {
            return;
        }
        var lot = CreateAuctionLot(player, item, startPrice, buyoutPrice, duration, minStack, maxStack);
        if (lot == null)
        {
            return;
        }

        var auctionFee = lot.DirectMoney * 0.01 * ((byte)duration + 1);

        if (auctionFee > MaxListingFee)
        {
            auctionFee = MaxListingFee;
        }

        if (!player.ChangeMoney(SlotType.Bag, -(int)auctionFee))
        {
            player.SendErrorMessage(ErrorMessageType.CanNotPutupMoney);
            return;
        }

        //CheapestAuctionLot(player, item.TemplateId, item.Grade);

        player.Inventory.Bag.RemoveItem(ItemTaskType.Auction, item, true);
        AuctionLots.Add(lot);
        player.SendPacket(new SCAuctionPostedPacket(lot));
    }
}
