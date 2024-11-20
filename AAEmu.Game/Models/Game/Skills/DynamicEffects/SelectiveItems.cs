using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace AAEmu.Game.Models.Game.Skills
{
    public class ItemSelections
    {
        public uint Item { get; set; }
        public int Count { get; set; }
    }

    public class SelectiveItems
    {
        public string Effect { get; set; }
        public int Select { get; set; }
        public int ConsumeItemCount { get; set; }
        public List<ItemSelections> ItemSelections { get; set; }

        public SelectiveItems(JObject obj)
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

            // Проверка на null для "select"
            if (obj.GetValue("select") != null)
            {
                Select = obj.GetValue("select")!.ToObject<int>();
            }
            else
            {
                Select = 0; // или другое значение по умолчанию
            }

            // Проверка на null для "consume_item_count"
            if (obj.GetValue("consume_item_count") != null)
            {
                ConsumeItemCount = obj.GetValue("consume_item_count")!.ToObject<int>();
            }
            else
            {
                ConsumeItemCount = 0; // или другое значение по умолчанию
            }

            // Проверка на null для "list"
            if (obj.GetValue("list") != null)
            {
                ItemSelections = obj.GetValue("list")?.ToObject<List<ItemSelections>>();
            }
            else
            {
                ItemSelections = []; // или другое значение по умолчанию
            }
        }
    }
}
