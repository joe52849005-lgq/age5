using System.Collections;

namespace AAEmu.Game.Models.Game.Quests;

public class CompletedQuest
{
    public uint Id { get; set; } // UInt16 in 1.2, UInt32 in 5+
    public BitArray Body { get; set; }

    public CompletedQuest()
    {
    }

    public CompletedQuest(ushort id)
    {
        Id = id;
        Body = new BitArray(64);
    }
}
