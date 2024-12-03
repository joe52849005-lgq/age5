using System.Collections.Generic;

using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Items;
namespace AAEmu.Game.Core.Packets.G2C;

public class SCItemEvolvingPacket : GamePacket
{
    private readonly bool _isEvolving; //
    private readonly ulong _itemId;
    private readonly bool _changeAttr;
    private readonly byte _afterItemGrade; // The grade of the item after it is upgraded.
    private readonly ItemEvolvingAttribute _beforeAttribute;
    private readonly ItemEvolvingAttribute _afterAttribute;
    private readonly List<ItemEvolvingAttribute> _addAttributes;
    private readonly byte _addAttrCount;
    private readonly int _addExp;
    private readonly int _bonusExp;
    private readonly byte _beforeItemGrade;

    public SCItemEvolvingPacket(
        bool isEvolving,
        ulong itemId,
        bool changeAttr,
        byte afterItemGrade,
        ItemEvolvingAttribute beforeAttribute,
        ItemEvolvingAttribute afterAttribute,
        List<ItemEvolvingAttribute> addAttributes,
        byte addAttrCount,
        int addExp,
        int bonusExp,
        byte beforeItemGrade)
        : base(SCOffsets.SCItemEvolvingPacket, 5)
    {
        _isEvolving = isEvolving;
        _itemId = itemId;
        _changeAttr = changeAttr;
        _afterItemGrade = afterItemGrade;
        _beforeAttribute = beforeAttribute;
        _afterAttribute = afterAttribute;
        _addAttributes = addAttributes;
        _addAttrCount = addAttrCount;
        _addExp = addExp;
        _bonusExp = bonusExp;
        _beforeItemGrade = beforeItemGrade;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_isEvolving); //Whether the experience is improved
        stream.Write(_itemId);
        // When _changeAttr is equal to false, only the amount of experience points added will be displayed.
        stream.Write(_changeAttr);
        stream.Write(_afterItemGrade); // The level of the item after it is upgraded.
        // sub_39A1E4501(1)
        stream.Write(_beforeAttribute.Attribute);      // _beforeAttribute previous properties
        stream.Write(_beforeAttribute.AttributeType);  // type previous properties type - Его функция пока не известна
        stream.Write(_beforeAttribute.AttributeValue); // value The value of the previous attribute
        Logger.Debug($"SCItemEvolvingPacket: -- before - Attribute={_beforeAttribute.Attribute}, AttributeType={_beforeAttribute.AttributeType}, AttributeValue={_beforeAttribute.AttributeValue}");
        // sub_39A1E4501(2)
        stream.Write(_afterAttribute.Attribute);      // The following attributes (appear in the replacement column)
        stream.Write(_afterAttribute.AttributeType);  // The Type of the subsequent attributes (appears in the replacement column) whose function is currently unknown
        stream.Write(_afterAttribute.AttributeValue); // The Type value of the subsequent attribute (appears in the replacement column)
        Logger.Debug($"SCItemEvolvingPacket: -- after - Attribute={_afterAttribute.Attribute}, AttributeType={_afterAttribute.AttributeType}, AttributeValue={_afterAttribute.AttributeValue}");
        stream.Write(_beforeItemGrade); // _beforeItemGrade Previous item level
        stream.Write(_addAttrCount);    // _addAttrCount The number of added attributes
        stream.Write(_addExp); // addExp
        stream.Write(_bonusExp); // bonusExp
        // To be improved, here should be a table
        // i = addAttrCount It should be the value of the increased number of attributes.
        Logger.Debug($"SCItemEvolvingPacket: -- examine addAttributes.Count:{_addAttributes.Count}");
        for (var i = 0; i < _addAttributes.Count; i++)
        {
            // sub_39A1E4501(3)
            Logger.Debug($"SCItemEvolvingPacket: - addAttributes[i].Attribute={_addAttributes[i].Attribute}, addAttributes[i].AttributeType={_addAttributes[i].AttributeType}, addAttributes[i].AttributeValue={_addAttributes[i].AttributeValue}");
            stream.Write(_addAttributes[i].Attribute);      // Added attributes (appears in the Add column)
            stream.Write(_addAttributes[i].AttributeType);  // The function of the added attribute type (appears in the add column) is currently unknown.
            stream.Write(_addAttributes[i].AttributeValue); // Add the value of the attribute (appears in the Add column)
        }

        return stream;
    }
}
