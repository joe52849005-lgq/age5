using System;
using System.Linq;
using AAEmu.Game.Models.Game.Units;
using Newtonsoft.Json.Linq;

namespace AAEmu.Game.Models.Game.Skills
{
    public class BlessUthstin
    {
        public string Effect { get; set; }
        public string ItemFunction { get; set; }
        public int Rise { get; set; }
        public int Drop { get; set; }
        public RiseWeight RiseWeight { get; set; }
        public DropWeight DropWeight { get; set; }

        public BlessUthstin(JObject obj, string json)
        {
            Effect = obj.GetValue("effect")?.ToString() ?? string.Empty;
            ItemFunction = obj.GetValue("item_function")?.ToString() ?? string.Empty;
            Rise = obj.GetValue("rise")?.ToObject<int>() ?? 0;
            Drop = obj.GetValue("drop")?.ToObject<int>() ?? 0;

            RiseWeight = obj.TryGetValue("riseweight", out var riseweight)
                ? riseweight.ToObject<RiseWeight>()
                : new RiseWeight();

            DropWeight = obj.TryGetValue("dropweight", out var dropweight)
                ? dropweight.ToObject<DropWeight>()
                : new DropWeight();
        }
    }

    public class RiseWeight : AttributeWeight { }

    public class DropWeight : AttributeWeight { }
}

public class AttributeWeight
{
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Sta { get; set; }
    public int Int { get; set; }
    public int Spi { get; set; }

    public UnitAttribute CheckFields()
    {
        var fields = new[] { Str, Dex, Sta, Int, Spi };
        var countOfOnes = fields.Count(field => field == 1);

        return countOfOnes switch
        {
            1 => GetFieldSetToOne(),
            5 => GetRandomFieldSetToOne(),
            _ => UnitAttribute.Fai
        };
    }

    private UnitAttribute GetFieldSetToOne()
    {
        var fields = new (int Value, UnitAttribute Attribute)[]
        {
            (Str, UnitAttribute.Str),
            (Dex, UnitAttribute.Dex),
            (Sta, UnitAttribute.Sta),
            (Int, UnitAttribute.Int),
            (Spi, UnitAttribute.Spi)
        };

        var fieldSetToOne = fields.FirstOrDefault(f => f.Value == 1);
        return fieldSetToOne.Attribute != default ? fieldSetToOne.Attribute : UnitAttribute.Fai;
    }

    private UnitAttribute GetRandomFieldSetToOne()
    {
        var fields = new (int Value, UnitAttribute Attribute)[]
        {
            (Str, UnitAttribute.Str),
            (Dex, UnitAttribute.Dex),
            (Sta, UnitAttribute.Sta),
            (Int, UnitAttribute.Int),
            (Spi, UnitAttribute.Spi)
        };

        var fieldsSetToOne = fields.Where(f => f.Value == 1).ToList();

        if (fieldsSetToOne.Count == 0)
        {
            return UnitAttribute.Fai;
        }

        var random = new Random();
        var randomField = fieldsSetToOne[random.Next(fieldsSetToOne.Count)];
        return randomField.Attribute;
    }
}
