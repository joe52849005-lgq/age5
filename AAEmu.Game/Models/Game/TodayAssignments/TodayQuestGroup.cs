namespace AAEmu.Game.Models.Game.TodayAssignments
{
    public class TodayQuestGroup
    {
        public int Id { get; set; }
        public bool AutomaticRestart { get; set; }
        public string Description { get; set; }
        public int ExpeditionLevelMax { get; set; }
        public int ExpeditionLevelMin { get; set; }
        public string Name { get; set; }
        public bool OrUnitReqs { get; set; }
        public int StepId { get; set; }
    }
}
