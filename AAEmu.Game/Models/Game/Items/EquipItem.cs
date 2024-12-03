using System;
using System.Linq;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Items.Templates;

namespace AAEmu.Game.Models.Game.Items;

public class EquipItem : Item
{
    public override ItemDetailType DetailType => ItemDetailType.Equipment;
    public override uint DetailBytesLength => 47; //56;

    public virtual int Str => 0;
    public virtual int Dex => 0;
    public virtual int Sta => 0;
    public virtual int Int => 0;
    public virtual int Spi => 0;
    public virtual byte MaxDurability => 0;

    public int RepairCost
    {
        get
        {
            var template = (EquipItemTemplate)Template;
            var grade = ItemManager.Instance.GetGradeTemplate(Grade);
            var cost = ItemManager.Instance.GetDurabilityRepairCostFactor() * 0.0099999998f * (1f - Durability * 1f / MaxDurability) * template.Price;
            cost = cost * grade.RefundMultiplier * 0.0099999998f;
            cost = (float)Math.Ceiling(cost);
            if (cost < 0 || cost < int.MinValue || cost > int.MaxValue)
                cost = 0;
            return (int)cost;
        }
    }

    public EquipItem()
    {
        GemIds = new uint[18]; // 18 in 5.0.7.0, 16 in 3.0.3.0, 7 in 1.2
    }

    public EquipItem(ulong id, ItemTemplate template, int count) : base(id, template, count)
    {
        GemIds = new uint[18]; // 18 in 5.0.7.0, 16 in 3.0.3.0, 7 in 1.2
    }

    public override void ReadDetails(PacketStream stream)
    {
        if (stream.LeftBytes < DetailBytesLength)
            return;
        Durability = stream.ReadByte();       // durability
        ChargeCount = stream.ReadInt16();     // chargeCount
        ChargeTime = stream.ReadDateTime();   // chargeTime
        TemperPhysical = stream.ReadUInt16(); // scaledA - Renovation level
        TemperMagical = stream.ReadUInt16();  // scaledB
        ChargeProcTime = stream.ReadDateTime(); // chargeProcTime
        MappingFailBonus = stream.ReadByte();   // mappingFailBonus - Compensation for awakening failure, нет в 4.5.2.6, есть в 4.5.1.0 и 5.7

        var mGems = stream.ReadPiscW(GemIds.Length);
        GemIds = mGems.Select(id => (uint)id).ToArray();
        AppearanceTemplateId = GemIds[0];  // appearanceTemplateId
        AdditionalDetails[3] = GemIds[1];  // Luna stone
        AdditionalDetails[3] = GemIds[2];  // Tempering эффект эфенских кубов
        AdditionalDetails[3] = GemIds[3];  // RemainingExperience
        //AdditionalDetails[4] = GemIds[4]; // crescent stone
        //AdditionalDetails[] = GemIds[5];
        //AdditionalDetails[] = GemIds[6];
        //AdditionalDetails[] = GemIds[7];
        //AdditionalDetails[] = GemIds[8];
        //AdditionalDetails[] = GemIds[9];
        //AdditionalDetails[] = GemIds[10];
        //AdditionalDetails[] = GemIds[11];
        //AdditionalDetails[] = GemIds[12];
        AdditionalDetails[4] = GemIds[13]; // 1 attribute
        AdditionalDetails[5] = GemIds[14]; // 2 attribute
        AdditionalDetails[6] = GemIds[15]; // 3 attribute
        AdditionalDetails[7] = GemIds[16]; // 4 attribute
        AdditionalDetails[8] = GemIds[17]; // 5 attribute
        //var mGems = stream.ReadPisc(4);
        //GemIds[0] = (uint)mGems[0];  // Can modify the appearance, TemplateId предмета для внешнего вида
        //GemIds[1] = (uint)mGems[1];  // Luna Stone
        //GemIds[2] = (uint)mGems[2];
        //GemIds[3] = (uint)mGems[3];  // Synthesis experience

        //mGems = stream.ReadPisc(4);
        //GemIds[4] = (uint)mGems[0];  // 1 crescent stone
        //GemIds[5] = (uint)mGems[1];  // 2 crescent stone
        //GemIds[6] = (uint)mGems[2];  // 3 crescent stone
        //GemIds[7] = (uint)mGems[3];  // 4 crescent stone

        //mGems = stream.ReadPisc(4);
        //GemIds[8] = (uint)mGems[0];  // 5 crescent stone
        //GemIds[9] = (uint)mGems[1];  // 6 crescent stone
        //GemIds[10] = (uint)mGems[2]; // 7 crescent stone
        //GemIds[11] = (uint)mGems[3]; // 8 crescent stone

        //mGems = stream.ReadPisc(4);
        //GemIds[12] = (uint)mGems[0]; // 9 crescent stone
        //GemIds[13] = (uint)mGems[1]; // 0 attribute Str
        //GemIds[14] = (uint)mGems[2]; // 1 attribute Dex
        //GemIds[15] = (uint)mGems[3]; // 2 attribute Sta
        //mGems = stream.ReadPisc(2);
        //GemIds[16] = (uint)mGems[0]; // 3 attribute Int
        //GemIds[17] = (uint)mGems[1]; // 4 attribute Spi
    }

    public override void WriteDetails(PacketStream stream)
    {
        stream.Write(Durability);     // durability
        stream.Write(ChargeCount);    // chargeCount
        stream.Write(ChargeTime);     // chargeTime
        stream.Write(TemperPhysical); // scaledA
        stream.Write(TemperMagical);  // scaledB
        stream.Write(ChargeProcTime);   // chargeProcTime
        stream.Write(MappingFailBonus); // mappingFailBonus - Compensation for awakening failure, нет в 4.5.2.6, есть в 4.5.1.0 и 5.7

        GemIds[0] = AppearanceTemplateId;

        var gemIds = GemIds.Select(id => (long)id).ToArray();
        stream.WritePiscW(gemIds.Length, gemIds);
        //stream.WritePisc(GemIds[0], GemIds[1], GemIds[2], GemIds[3]);
        //stream.WritePisc(GemIds[4], GemIds[5], GemIds[6], GemIds[7]);
        //stream.WritePisc(GemIds[8], GemIds[9], GemIds[10], GemIds[11]);
        //stream.WritePisc(GemIds[12], GemIds[13], GemIds[14], GemIds[15]); // в 3+ длина данных 36 (когда нет информации), в 1.2 было 56
        //stream.WritePisc(GemIds[16], GemIds[17]);
    }

    public override void WriteDetails(PacketStream stream, bool additionalEffect)
    {
        stream.Write(Durability);     // durability
        stream.Write(ChargeCount);    // chargeCount
        stream.Write(ChargeTime);     // chargeTime
        stream.Write(TemperPhysical); // scaledA
        stream.Write(TemperMagical);  // scaledB
        stream.Write(ChargeProcTime);   // chargeProcTime
        stream.Write(MappingFailBonus); // mappingFailBonus - Compensation for awakening failure, нет в 4.5.2.6, есть в 4.5.1.0 и 5.7

        if (additionalEffect)
        {
            GemIds[0] = AppearanceTemplateId; // AdditionalDetails[0];
            GemIds[1] = AdditionalDetails[1]; // Luna stone
            GemIds[2] = AdditionalDetails[2]; // Tempering эффект эфенских кубов
            GemIds[3] = AdditionalDetails[3]; // RemainingExperience
            //GemIds[4] = AdditionalDetails[]; // crescent stone
            //GemIds[5] = AdditionalDetails[];
            //GemIds[6] = AdditionalDetails[];
            //GemIds[7] = AdditionalDetails[];
            //GemIds[8] = AdditionalDetails[];
            //GemIds[9] = AdditionalDetails[];
            //GemIds[10] = AdditionalDetails[];
            //GemIds[11] = AdditionalDetails[];
            //GemIds[12] = AdditionalDetails[];
            GemIds[13] = AdditionalDetails[4]; // 1 attribute
            GemIds[14] = AdditionalDetails[5]; // 2 attribute
            GemIds[15] = AdditionalDetails[6]; // 3 attribute
            GemIds[16] = AdditionalDetails[7]; // 4 attribute
            GemIds[17] = AdditionalDetails[8]; // 5 attribute

            var gemIds = GemIds.Select(id => (long)id).ToArray();
            stream.WritePiscW(gemIds.Length, gemIds);
            //stream.WritePisc(GemIds[0], GemIds[1], GemIds[2], GemIds[3]);
            //stream.WritePisc(GemIds[4], GemIds[5], GemIds[6], GemIds[7]);
            //stream.WritePisc(GemIds[8], GemIds[9], GemIds[10], GemIds[11]);
            //stream.WritePisc(GemIds[12], GemIds[13], GemIds[14], GemIds[15]); // в 3+ длина данных 36 (когда нет информации), в 1.2 было 56
            //stream.WritePisc(GemIds[16], GemIds[17]);
        }
    }
}
