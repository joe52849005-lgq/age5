namespace AAEmu.Game.Models.Game.Expeditions
{
    public class ExpeditionLevel
    {
        public int Id { get; set; }
        public int DailyExp { get; set; }
        public int MemberLimit { get; set; }
        public int NeedMoney { get; set; }
        public int SimilarBuffTagId { get; set; }
        public int SummonLimit { get; set; }
        public int TotalExp { get; set; }
    }
}
