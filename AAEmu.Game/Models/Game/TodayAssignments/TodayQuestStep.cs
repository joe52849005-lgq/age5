namespace AAEmu.Game.Models.Game.TodayAssignments
{
    public class TodayQuestStep
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int ExpeditionLevelMax { get; set; }
        public int ExpeditionLevelMin { get; set; }
        public bool ExpeditionOnly { get; set; }
        public int FamilyLevelMax { get; set; }
        public int FamilyLevelMin { get; set; }
        public bool FamilyOnly { get; set; }
        public int IconId { get; set; }
        public int ItemNum { get; set; }
        public int ItemId { get; set; }
        public string Name { get; set; }
        public bool OrUnitReqs { get; set; }
        public int RealStep { get; set; }
    }
}
