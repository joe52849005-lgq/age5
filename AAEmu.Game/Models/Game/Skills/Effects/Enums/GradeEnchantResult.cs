namespace AAEmu.Game.Models.Game.Skills.Effects.Enums;

public enum GradeEnchantResult
{
    Break = 0,
    Downgrade = 1,
    Fail = 2, // неудача, предмет испорчен
    Fail2 = 3, // неудача, качество предмета осталось прежним
    Success = 4,
    GreatSuccess = 5
}
