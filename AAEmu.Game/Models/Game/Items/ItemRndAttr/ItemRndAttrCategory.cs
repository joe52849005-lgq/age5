namespace AAEmu.Game.Models.Game.Items.ItemRndAttr;

public class ItemRndAttrCategory
{
    public int Id { get; set; }
    public int CurrencyId { get; set; }
    public string Desc { get; set; }
    public int MaterialGradeLimit { get; set; }
    public int MaxEvolvingGrade { get; set; }
    public int MessageGrade { get; set; }
    public int ReRollItemId { get; set; }
}
