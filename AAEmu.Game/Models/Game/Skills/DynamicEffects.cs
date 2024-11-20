using System.Collections.Generic;

namespace AAEmu.Game.Models.Game.Skills
{
    class DynamicEffects
    {
        public Dictionary<uint,SelectiveItems> selectiveItems;
        public Dictionary<uint,BlessUthstin> blessUthstins;

        public DynamicEffects()
        {
            selectiveItems = new Dictionary<uint,SelectiveItems>();
            blessUthstins = new Dictionary<uint,BlessUthstin>();
        }
    }
}
