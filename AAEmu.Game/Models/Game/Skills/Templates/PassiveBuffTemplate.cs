namespace AAEmu.Game.Models.Game.Skills.Templates;

public class PassiveBuffTemplate
{
    public uint Id { get; set; }
    public AbilityType AbilityId { get; set; }
    public byte Level { get; set; }
    public uint BuffId { get; set; }
    public int ReqPoints { get; set; }
    public bool Active { get; set; }
    // added in 5.0.7.0
    public int HighAbilityId { get; set; }
    public int SkillPoints { get; set; }
}
