using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Items.Actions;

namespace AAEmu.Game.Core.Packets.C2G;

public class CSBlessUthstinUseApplyStatsItemPacket : GamePacket
{
    public CSBlessUthstinUseApplyStatsItemPacket() : base(CSOffsets.CSBlessUthstinUseApplyStatsItemPacket, 5)
    {
    }

    public override void Read(PacketStream stream)
    {
        var itemId = stream.ReadUInt64();
        var pageIndex = stream.ReadUInt32();

        var character = Connection.ActiveChar;
        var item = ItemManager.Instance.GetItemByItemId(itemId);

        if (item == null)
        {
            Logger.Warn($"Item with ID {itemId} not found.");
            return;
        }

        var skillType = item.Template.UseSkillId;
        var blessUthstinItem = SkillManager.Instance.GetBlessUthstinItems(skillType);

        if (blessUthstinItem == null)
        {
            Logger.Warn($"BlessUthstinItem for skillType {skillType} not found.");
            return;
        }

        character.Stats.PageIndex = pageIndex;

        var itemCount = 0;
        switch (blessUthstinItem.ItemFunction)
        {
            case "normal":
                if (character.Stats.UseSphereNormal())
                {
                    Logger.Debug($"BlessUthstinUseApplyStatsItem: The sphere has been successfully utilized!" +
                                 $" character={character.Name}," +
                                 $" Item={item.TemplateId}," +
                                 $" skillType={skillType}"
                    );
                    itemCount = 1;
                }
                else
                {
                    Logger.Debug($"BlessUthstinUseApplyStatsItem: Sphere utilization limit is exhausted!" +
                                 $" character={character.Name}," +
                                 $" Item={item.TemplateId}," +
                                 $" skillType={skillType}"
                    );
                    return;
                }
                break;

            case "special":
                if (character.Stats.UseSphereSpecial())
                {
                    Logger.Debug($"BlessUthstinUseApplyStatsItem: The sphere has been successfully utilized!" +
                                 $" character={character.Name}," +
                                 $" Item={item.TemplateId}," +
                                 $" skillType={skillType}"
                    );
                }
                itemCount = GetSpecialItemCount(character.Stats.ApplySpecialCount);
                break;
        }

        var incStatsKind = blessUthstinItem.RiseWeight.CheckFields();
        var decStatsKind = blessUthstinItem.DropWeight.CheckFields();
        var incStatsPoint = blessUthstinItem.Rise;
        var decStatsPoint = blessUthstinItem.Drop;

        Logger.Debug($"BlessUthstinUseApplyStatsItem: " +
                     $"character={character.Name}," +
                     $" Item={item.TemplateId}," +
                     $" skillType={skillType}," +
                     $" incStatsKind={incStatsKind}," +
                     $" decStatsKind={decStatsKind}," +
                     $" incStatsPoint={incStatsPoint}," +
                     $" decStatsPoint={decStatsPoint}"
                     );

        character.SendPacket(new SCBlessUthstinConsumeApplyStatsPacket(
            character.ObjId,
            true,
            skillType,
            (int)incStatsKind,
            (int)decStatsKind,
            incStatsPoint,
            decStatsPoint
            ));

        if (character.Inventory.Bag.ConsumeItem(ItemTaskType.BlessUthstinChangeStats, item.TemplateId, itemCount, null) <= 0)
        {
            character.SendErrorMessage(ErrorMessageType.NotEnoughItem);
            Logger.Debug($"Not Enough Item {item.TemplateId}");
        }
    }

    private int GetSpecialItemCount(int applySpecialCount)
    {
        return applySpecialCount switch
        {
            0 => 1,
            1 => 2,
            2 => 5,
            3 => 10,
            4 => 17,
            5 => 26,
            6 => 37,
            7 => 50,
            8 => 65,
            9 => 82,
            10 => 101,
            11 => 122,
            12 => 145,
            13 => 170,
            14 => 197,
            15 => 226,
            _ => 0
        };
    }
}
