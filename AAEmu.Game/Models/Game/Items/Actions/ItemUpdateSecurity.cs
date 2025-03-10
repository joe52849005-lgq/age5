﻿using AAEmu.Commons.Network;

namespace AAEmu.Game.Models.Game.Items.Actions;

public class ItemUpdateSecurity : ItemTask
{
    private readonly Item _item;
    private readonly byte _bits;
    private readonly byte _prevBits;
    private readonly bool _isUnsecureExcess;
    private readonly bool _isUnsecureSet;
    private readonly bool _isUnpack;

    public ItemUpdateSecurity(Item item, byte bits, byte prevBits, bool isUnsecureExcess, bool isUnsecureSet, bool isUnpack)
    {
        _type = ItemAction.UpdateFlags; // 11
        _item = item;
        _bits = bits;
        _prevBits = prevBits;
        _isUnsecureExcess = isUnsecureExcess;
        _isUnsecureSet = isUnsecureSet;
        _isUnpack = isUnpack;
        _tLogt = SetTlogT(_type, SlotType.Bag); // установим tLogt по значению ItemAction
    }

    public override PacketStream Write(PacketStream stream)
    {
        base.Write(stream);
        stream.Write((byte)_item.SlotType); // type
        stream.Write((byte)_item.Slot);     // index
        stream.Write(_item.Id);             // id
        stream.Write(_bits);                // bits
        stream.Write(_prevBits);            // prevBits
        stream.Write(_isUnsecureExcess);    // isUnsecureExcess
        stream.Write(_isUnsecureSet);       // isUnSecureSet
        stream.Write(_isUnpack);            // isUnpack
        stream.Write(_item.UnsecureTime);   // unSecureDateTime
        stream.Write(_item.UnpackTime);     // unpackDateTime
        return stream;
    }
}
