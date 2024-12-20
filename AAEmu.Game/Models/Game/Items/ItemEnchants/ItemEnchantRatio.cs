﻿namespace AAEmu.Game.Models.Game.Items.ItemEnchants;

public class ItemEnchantRatio
{
    public int ItemEnchantRatioGroupId { get; set; }
    public int Grade { get; set; }
    public int GradeEnchantSuccessRatio { get; set; }
    public int GradeEnchantGreatSuccessRatio { get; set; }
    public int GradeEnchantBreakRatio { get; set; }
    public int GradeEnchantDowngradeRatio { get; set; }
    public int GradeEnchantDowngradeMin { get; set; }
    public int GradeEnchantDowngradeMax { get; set; }
    public int GradeEnchantCost { get; set; }
    public int CurrencyId { get; set; }
    public int GradeEnchantDisableRatio { get; set; }
}
