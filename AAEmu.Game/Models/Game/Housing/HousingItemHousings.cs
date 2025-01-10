namespace AAEmu.Game.Models.Game.Housing;

public class HousingItemHousings
{
    public uint Id { get; set; }
    public uint ItemId { get; set; }
    public bool Completion { get; set; }
    public uint DesignId { get; set; }

    public HousingItemHousings()
    {
    }
}
