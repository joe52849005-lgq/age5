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
            // Проверка на null для "effect"
            if (obj.GetValue("effect") != null)
            {
                Effect = obj.GetValue("effect")?.ToString();
            }
            else
            {
                Effect = string.Empty; // или другое значение по умолчанию
            }

            // Проверка на null для "item_function"
            if (obj.GetValue("item_function") != null)
            {
                ItemFunction = obj.GetValue("item_function")?.ToString();
            }
            else
            {
                ItemFunction = string.Empty; // или другое значение по умолчанию
            }

            // Проверка на null для "rise"
            if (obj.GetValue("rise") != null)
            {
                Rise = obj.GetValue("rise")!.ToObject<int>();
            }
            else
            {
                Rise = 0; // или другое значение по умолчанию
            }

            // Проверка на null для "drop"
            if (obj.GetValue("drop") != null)
            {
                Drop = obj.GetValue("drop")!.ToObject<int>();
            }
            else
            {
                Drop = 0; // или другое значение по умолчанию
            }

            // Проверка на null для "riseweight"
            if (obj.TryGetValue("riseweight", out var riseweight))
            {
                RiseWeight = riseweight.ToObject<RiseWeight>();
            }
            else
            {
                RiseWeight = new RiseWeight(); // или другое значение по умолчанию
            }

            // Проверка на null для "dropweight"
            if (obj.TryGetValue("dropweight", out var dropweight))
            {
                DropWeight = dropweight.ToObject<DropWeight>();
            }
            else
            {
                DropWeight = new DropWeight(); // или другое значение по умолчанию
            }
        }
    }

    public class RiseWeight
    {
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Sta { get; set; }
        public int Int { get; set; }
        public int Spi { get; set; }
    }

    public class DropWeight
    {
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Sta { get; set; }
        public int Int { get; set; }
        public int Spi { get; set; }
    }
}
