﻿using AAEmu.Game.Utils;

namespace AAEmu.Game.Core.Managers.Id;

public class ObjectIdManager : IdManager
{
    private static ObjectIdManager _instance;
    private const uint FirstId = 0x00000100;
    private const uint LastId = 0x00FFFFFE;
    private static readonly uint[] Exclude = System.Array.Empty<uint>();
    private static readonly string[,] ObjTables = { { } };

    public static ObjectIdManager Instance => _instance ?? (_instance = new ObjectIdManager());

    public ObjectIdManager() : base("ObjectIdManager", FirstId, LastId, ObjTables, Exclude)
    {
    }

    //public override void ReleaseId(uint usedObjId)
    //{
    //    return;
    //}
}
