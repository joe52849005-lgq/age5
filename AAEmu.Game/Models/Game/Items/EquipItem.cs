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
        //GemIds = new uint[22]; // 18 in 5.0.7.0, 16 in 3.0.3.0, 7 in 1.2
    }

    public EquipItem(ulong id, ItemTemplate template, int count) : base(id, template, count)
    {
        //GemIds = new uint[22]; // 18 in 5.0.7.0, 16 in 3.0.3.0, 7 in 1.2
    }

    public override void ReadDetails(PacketStream stream)
    {
        if (stream.LeftBytes < DetailBytesLength)
            return;
        Durability = stream.ReadByte();       // durability
        ChargeCount = stream.ReadInt16();     // chargeCount
        ChargeTime = stream.ReadDateTime();   // chargeTime

        //ChargeTime = DateTime.UtcNow; // сбросим дату

        TemperPhysical = stream.ReadUInt16(); // scaledA - Renovation level
        TemperMagical = stream.ReadUInt16();  // scaledB
        ChargeProcTime = stream.ReadDateTime(); // chargeProcTime
        MappingFailBonus = stream.ReadByte();   // mappingFailBonus - Compensation for awakening failure, нет в 4.5.2.6, есть в 4.5.1.0 и 5.7

        //var mGems = new long[22]; // 18 ячеек + 4 дополнительных
        //var mGems = stream.ReadPiscW(18);
        //GemIds = mGems.Select(id => (uint)id).ToArray();

        var mGems = stream.ReadPisc(4);
        GemIds[0] = (uint)mGems[0];  // Can modify the appearance, TemplateId предмета для внешнего вида
        GemIds[1] = (uint)mGems[1];  // Luna Stone
        GemIds[2] = (uint)mGems[2];
        GemIds[3] = (uint)mGems[3];  // Synthesis experience

        mGems = stream.ReadPisc(4);
        GemIds[4] = (uint)mGems[0];  // 1 crescent stone
        GemIds[5] = (uint)mGems[1];  // 2 crescent stone
        GemIds[6] = (uint)mGems[2];  // 3 crescent stone
        GemIds[7] = (uint)mGems[3];  // 4 crescent stone

        mGems = stream.ReadPisc(4);
        GemIds[8] = (uint)mGems[0];  // 5 crescent stone
        GemIds[9] = (uint)mGems[1];  // 6 crescent stone
        GemIds[10] = (uint)mGems[2]; // 7 crescent stone
        GemIds[11] = (uint)mGems[3]; // 8 crescent stone

        mGems = stream.ReadPisc(4);
        GemIds[12] = (uint)mGems[0]; // 9 crescent stone
        GemIds[13] = (uint)mGems[1]; // 0 attribute Str
        GemIds[14] = (uint)mGems[2]; // 1 attribute Dex
        GemIds[15] = (uint)mGems[3]; // 2 attribute Sta
        mGems = stream.ReadPisc(2);
        GemIds[16] = (uint)mGems[0]; // 3 attribute Int
        GemIds[17] = (uint)mGems[1]; // 4 attribute Spi

        GemIds[21] = TemperPhysical;
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

        var gemIds = GemIds.Select(id => (long)id).ToArray();
        stream.WritePiscW(18, gemIds); // 18 ячеек + 4 дополнительных
        //stream.WritePisc(GemIds[0], GemIds[1], GemIds[2], GemIds[3]);
        //stream.WritePisc(GemIds[4], GemIds[5], GemIds[6], GemIds[7]);
        //stream.WritePisc(GemIds[8], GemIds[9], GemIds[10], GemIds[11]);
        //stream.WritePisc(GemIds[12], GemIds[13], GemIds[14], GemIds[15]); // в 3+ длина данных 36 (когда нет информации), в 1.2 было 56
        //stream.WritePisc(GemIds[16], GemIds[17]);
    }

    public override void ReadAdditionalDetails(PacketStream stream)
    {
        GemIds[0] = stream.ReadUInt32();  // added for normal operation of repairing objects and for transformation
        ImageItemTemplateId = GemIds[0];

        Durability = stream.ReadByte();   // durability
        GemIds[18] = stream.ReadUInt16(); // unk

        GemIds[1] = stream.ReadUInt32();  // Luna Gem, TemplateId EnchantingGem - Позволяет зачаровать предмет снаряжения.
        RuneId = (ushort)GemIds[1];

        GemIds[2] = stream.ReadUInt32();  // unk
        GemIds[2] = stream.ReadUInt32();  // unk

        // Чтение времени
        //ChargeStartTime = stream.ReadDateTime();
        //GemIds[19] = stream.ReadUInt32(); // unk
        //GemIds[20] = stream.ReadUInt32(); // unk

        GemIds[4] = stream.ReadUInt32();  // 1 crescent stone, TemplateId Socket - Позволяет придать предмету снаряжения дополнительные свойства.
        GemIds[5] = stream.ReadUInt32();  // 2 crescent stone
        GemIds[6] = stream.ReadUInt32();  // 3 crescent stone
        GemIds[7] = stream.ReadUInt32();  // 4 crescent stone
        GemIds[8] = stream.ReadUInt32();  // 5 crescent stone
        GemIds[9] = stream.ReadUInt32();  // 6 crescent stone
        GemIds[10] = stream.ReadUInt32(); // 7 crescent stone
        GemIds[11] = stream.ReadUInt32(); // 8 crescent stone
        GemIds[12] = stream.ReadUInt32(); // 9 crescent stone

        TemperPhysical = stream.ReadUInt16(); // TemperPhysical
        TemperMagical = stream.ReadUInt16(); // TemperMagical
        GemIds[21] = (uint)((TemperMagical << 16) | TemperPhysical);

        GemIds[3] = stream.ReadUInt32();  // RemainingExperience

        GemIds[13] = stream.ReadUInt32(); // 5 Additional Effects
        GemIds[14] = stream.ReadUInt32(); //
        GemIds[15] = stream.ReadUInt32(); //
        GemIds[16] = stream.ReadUInt32(); //
        GemIds[17] = stream.ReadUInt32(); //
    }

    public override void WriteAdditionalDetails(PacketStream stream)
    {
        stream.Write(GemIds[0]);  // added for normal operation of repairing objects and for transformation

        stream.Write(Durability); // durability
        stream.Write((short)GemIds[18]); // unk

        stream.Write(GemIds[1]);  // Luna Gem, TemplateId EnchantingGem - Позволяет зачаровать предмет снаряжения.
        RuneId = (ushort)GemIds[1];

        stream.Write(ChargeTime);  //  ChargeStartTime

        stream.Write(GemIds[2]);  // 

        //// Преобразуем DateTime в long (Ticks)
        //var ticks = ChargeTime.Ticks;
        //// Разделяем на младшие и старшие 32 бита
        //var low = (uint)(ticks & 0xFFFFFFFF);       // Младшие 32 бита
        //var hi = (uint)(ticks >> 32);               // Старшие 32 бита
        ////// Записываем в массив GemIds
        ////GemIds[19] = low;
        ////GemIds[20] = hi;
        //// Записываем в поток
        //stream.Write(low);  // ChargeStartTime, low
        //stream.Write(hi);   // ChargeStartTime, hi

        stream.Write(GemIds[4]);  // 1 crescent stone, TemplateId Socket - Позволяет придать предмету снаряжения дополнительные свойства.
        stream.Write(GemIds[5]);  // 2 crescent stone
        stream.Write(GemIds[6]);  // 3 crescent stone
        stream.Write(GemIds[7]);  // 4 crescent stone
        stream.Write(GemIds[8]);  // 5 crescent stone
        stream.Write(GemIds[9]);  // 6 crescent stone
        stream.Write(GemIds[10]); // 7 crescent stone
        stream.Write(GemIds[11]); // 8 crescent stone
        stream.Write(GemIds[12]); // 9 crescent stone

        TemperPhysical = (ushort)(GemIds[21] & 0xFFFF);  // Младшие 16 бит
        TemperMagical = (ushort)(GemIds[21] >> 16);      // Старшие 16 бит
        stream.Write(TemperPhysical);  // Записываем TemperPhysical
        stream.Write(TemperMagical);   // Записываем TemperMagical

        stream.Write(GemIds[3]);  // RemainingExperience

        stream.Write(GemIds[13]); // 5 Additional Effects
        stream.Write(GemIds[14]); //
        stream.Write(GemIds[15]); //
        stream.Write(GemIds[16]); //
        stream.Write(GemIds[17]); //
    }
}
