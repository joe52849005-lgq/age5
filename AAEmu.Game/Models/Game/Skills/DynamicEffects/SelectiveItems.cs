using System.Collections.Generic;
using System.Text.Json.Serialization;
using AAEmu.Game.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AAEmu.Game.Models.Game.Skills
{
    public class ItemSelections
    {
        [Newtonsoft.Json.JsonConverter(typeof(JSonHexConverterULong))]
        [JsonProperty("item")]
        public ulong EncryptedItem { get; set; }

        // TODO: Add item decryption code
        [Newtonsoft.Json.JsonIgnore]
        public uint Item { get; set; }
        
        [JsonProperty("count")]
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
            Effect = obj.GetValue("effect")?.ToString() ?? string.Empty;
            Select = obj.GetValue("select")?.ToObject<int>() ?? 0;
            ConsumeItemCount = obj.GetValue("consume_item_count")?.ToObject<int>() ?? 0;
            ItemSelections = obj.GetValue("list")?.ToObject<List<ItemSelections>>() ?? [];
        }
    }
}
