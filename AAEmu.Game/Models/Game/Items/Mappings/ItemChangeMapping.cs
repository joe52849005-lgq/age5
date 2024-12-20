namespace AAEmu.Game.Models.Game.Items.Mappings;

public class ItemChangeMapping
{
    public int Id { get; set; }
    public int MappingGroupId { get; set; }
    public int SourceGradeId { get; set; }
    public int SourceItemId { get; set; }
    public int TargetItemId { get; set; }
}
