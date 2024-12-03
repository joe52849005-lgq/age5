namespace AAEmu.Game.Models.Game.Items;

public struct ItemEvolvingAttribute
{
    public ItemEvolvingAttribute()
    {
    }

    public ushort Attribute { get; set; } = 221;
    public byte AttributeType { get; set; } = 0;
    public int AttributeValue { get; set; } = 0;
}
