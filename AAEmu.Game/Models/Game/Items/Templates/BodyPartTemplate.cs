using System;

namespace AAEmu.Game.Models.Game.Items.Templates;

public class BodyPartTemplate : ItemTemplate
{
    public override Type ClassType => typeof(BodyPart);

    public uint ModelId { get; set; }
    public bool NpcOnly { get; set; }
    //public bool BeautyShopOnly { get; set; }
    public uint ItemId { get; set; }
    public uint SlotTypeId { get; set; }
    // added in 5.0.7.0
    public int Asset1Id { get; set; }
    public int Asset2Id { get; set; }
    public int Asset3Id { get; set; }
    public int Asset4Id { get; set; }
    public int AssetId { get; set; }
    public int CustomTextureId { get; set; }
    public int CustomTexture1Id { get; set; }
    public int CustomTexture2Id { get; set; }
    public int CustomTexture3Id { get; set; }
    public int CustomTexture4Id { get; set; }
    public string FaceMask { get; set; }
    public string HairBase { get; set; }
    public int LeftEyeHeight { get; set; }
    public int LeftEyeWidth { get; set; }
    public int LeftEyeX { get; set; }
    public int LeftEyeY { get; set; }
    public bool OddEye { get; set; }
    public int RightEyeHeight { get; set; }
    public int RightEyeWidth { get; set; }
    public int RightEyeX { get; set; }
    public int RightEyeY { get; set; }
}
