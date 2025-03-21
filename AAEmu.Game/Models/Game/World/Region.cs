﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Gimmicks;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.World;

public class Region
{
    private readonly uint _worldId;
    private readonly object _objectsLock = new();
    private GameObject[] _objects;
    private int _objectsSize, _charactersSize;
    private Region[] _neighbors;
    private int _playerCount;

    public int X { get; }
    public int Y { get; }
    public int Id => Y + 1024 * X;
    public uint ZoneKey { get; set; }

    public Region(uint worldId, int x, int y, uint zoneKey)
    {
        _worldId = worldId;
        X = x;
        Y = y;
        ZoneKey = zoneKey;
    }

    public void AddObject(GameObject obj)
    {
        if (obj == null)
            return;
        lock (_objectsLock)
        {
            if (_objects == null)
            {
                _objects = new GameObject[50];
                _objectsSize = 0;
            }
            else if (_objectsSize >= _objects.Length)
            {
                var temp = new GameObject[_objects.Length * 2];
                Array.Copy(_objects, 0, temp, 0, _objectsSize);
                _objects = temp;
            }

            _objects[_objectsSize] = obj;
            _objectsSize++;
        }

        if (obj.Transform != null)
        {
            obj.Transform.WorldId = _worldId;
            var zoneId = WorldManager.Instance.GetZoneId(_worldId, obj.Transform.World.Position.X, obj.Transform.World.Position.Y);
            if (zoneId > 0)
                obj.Transform.ZoneId = zoneId;
        }

        if (obj is Character)
        {
            _charactersSize++;
            foreach (var region in GetNeighbors())
                if (region != null)
                    Interlocked.Increment(ref region._playerCount);
        }
        // Show debug info to subscribed players
        if (obj.Transform?._debugTrackers?.Count > 0)
            foreach (var chr in obj.Transform._debugTrackers)
                chr?.SendDebugMessage($"[{DateTime.UtcNow:HH:mm:ss}] {obj.ObjId} entered region ({X} {Y})){(obj is BaseUnit bu ? " - " + bu.Name : "")}");
    }

    public void RemoveObject(GameObject obj) // TODO Нужно доделать =_+
    {
        if (obj == null)
            return;
        lock (_objectsLock)
        {
            if (_objects == null || _objectsSize == 0)
                return;

            if (_objectsSize > 1)
            {
                var index = -1;
                for (var i = 0; i < _objects.Length; i++)
                    if (_objects[i] == obj)
                    {
                        index = i;
                        break;
                    }

                if (index > -1)
                {
                    _objects[index] = _objects[_objectsSize - 1];
                    _objects[_objectsSize - 1] = null;
                    _objectsSize--;
                }
            }
            else if (_objectsSize == 1 && _objects[0] == obj)
            {
                _objects[0] = null;
                _objects = null;
                _objectsSize = 0;
            }

            if (obj is Character)
            {
                _charactersSize--;
                foreach (var region in GetNeighbors())
                    if (region != null)
                        Interlocked.Decrement(ref region._playerCount);
            }
        }
        // Show debug info to subscribed players
        if (obj.Transform?._debugTrackers?.Count > 0)
            foreach (var chr in obj.Transform._debugTrackers)
                chr?.SendDebugMessage($"[{DateTime.UtcNow:HH:mm:ss}] {obj.ObjId} left the region ({X} {Y})){(obj is BaseUnit bu ? " - " + bu.Name : "")}");
    }

    public void AddToCharacters(GameObject obj)
    {
        if (_objects == null)
            return;

        // Show the player all the facilities in the region when he/she is added
        if (obj is Character objectAsCharacter)
        {
            var objectsInRegion = GetList(new List<GameObject>(), obj.ObjId);
            foreach (var go in objectsInRegion)
            {
                // Ignore doodads here, as we have a special packet for those
                if (go is Doodad)
                    continue;

                if (go is Gimmick)
                    continue;

                // turn on the motion of the visible NPC
                if (go is Npc npc && npc.Ai != null)
                    npc.Ai.ShouldTick = true;

                go.AddVisibleObject(objectAsCharacter);
            }

            // Handle Doodads separately with sets of SCDoodadsCreatedPacket
            var doodads = GetList(new List<Doodad>(), obj.ObjId).ToArray();
            for (var i = 0; i < doodads.Length; i += SCDoodadsCreatedPacket.MaxCountPerPacket)
            {
                var count = doodads.Length - i;
                var temp = new Doodad[count <= SCDoodadsCreatedPacket.MaxCountPerPacket
                    ? count
                    : SCDoodadsCreatedPacket.MaxCountPerPacket];
                Array.Copy(doodads, i, temp, 0, temp.Length);
                objectAsCharacter.SendPacket(new SCDoodadsCreatedPacket(temp));
            }

            // Handle Gimmicks separately with sets of SCGimmicksCreatedPacket
            var gimmicks = GetList(new List<Gimmick>(), obj.ObjId).ToArray();
            for (var i = 0; i < gimmicks.Length; i += SCGimmicksCreatedPacket.MaxCountPerPacket)
            {
                var count = gimmicks.Length - i;
                var temp = new Gimmick[count <= SCGimmicksCreatedPacket.MaxCountPerPacket
                    ? count
                    : SCGimmicksCreatedPacket.MaxCountPerPacket];
                Array.Copy(gimmicks, i, temp, 0, temp.Length);
                objectAsCharacter.SendPacket(new SCGimmicksCreatedPacket(temp));
            }
            // Not sure why or if this is needed, but it's always sent after the creation packets with no reference to any of them
            if (gimmicks.Length > 0)
                objectAsCharacter.SendPacket(new SCGimmickJointsBrokenPacket([]));
        }

        // show the object to all players in the region
        foreach (var characterInRegion in GetList(new List<Character>(), obj.ObjId))
            obj.AddVisibleObject(characterInRegion);
    }

    public void RemoveFromCharacters(GameObject obj)
    {
        if (_objects == null)
            return;

        // remove all visible objects in the region from the player
        if (obj is Character character1)
        {
            var unitIds = GetListId<Unit>(new List<uint>(), character1.ObjId).ToArray();
            var units = GetList(new List<Unit>(), character1.ObjId);
            foreach (var t in units)
            {
                if (t is Npc npc && npc.Ai != null)
                {
                    npc.Ai.ShouldTick = false;
                }
            }

            for (var offset = 0; offset < unitIds.Length; offset += SCUnitsRemovedPacket.MaxCountPerPacket)
            {
                var length = unitIds.Length - offset;
                var temp = new uint[length > SCUnitsRemovedPacket.MaxCountPerPacket
                    ? SCUnitsRemovedPacket.MaxCountPerPacket
                    : length];
                Array.Copy(unitIds, offset, temp, 0, temp.Length);
                character1.SendPacket(new SCUnitsRemovedPacket(temp));
            }

            var doodadIds = GetListId<Doodad>(new List<uint>(), character1.ObjId).ToArray();
            for (var offset = 0; offset < doodadIds.Length; offset += SCDoodadsRemovedPacket.MaxCountPerPacket)
            {
                var length = doodadIds.Length - offset;
                var last = length <= SCDoodadsRemovedPacket.MaxCountPerPacket;
                var temp = new uint[last ? length : SCDoodadsRemovedPacket.MaxCountPerPacket];
                Array.Copy(doodadIds, offset, temp, 0, temp.Length);
                character1.SendPacket(new SCDoodadsRemovedPacket(last, temp));
            }

            var gimmickIds = GetList<Gimmick>([], character1.ObjId).Select(g => g.ObjId).ToArray();
            for (var offset = 0; offset < gimmickIds.Length; offset += SCGimmicksRemovedPacket.MaxCountPerPacket)
            {
                var length = gimmickIds.Length - offset;
                var last = length <= SCGimmicksRemovedPacket.MaxCountPerPacket;
                var temp = new uint[last ? length : SCGimmicksRemovedPacket.MaxCountPerPacket];
                Array.Copy(gimmickIds, offset, temp, 0, temp.Length);
                character1.SendPacket(new SCGimmicksRemovedPacket(temp));
            }

            if (character1.CurrentTarget != null && unitIds.Contains(character1.CurrentTarget.ObjId))
            {
                character1.CurrentTarget = null;
                character1.SendPacket(new SCTargetChangedPacket(character1.ObjId, 0));
            }
            // TODO ... others types...
        }

        // remove the object from all players in the region
        foreach (var character in GetList(new List<Character>(), obj.ObjId))
            obj.RemoveVisibleObject(character);
    }

    public Region[] GetNeighbors()
    {
        //Will neighbor regions ever change?
        if (_neighbors == null)
        {
            _neighbors = WorldManager.Instance.GetNeighbors(_worldId, X, Y);
            return _neighbors;
        }
        else
        {
            return _neighbors;
        }
    }

    public bool AreNeighborsEmpty()
    {
        if (!IsEmpty())
            return false;
        foreach (var neighbor in GetNeighbors())
            if (!neighbor.IsEmpty())
                return false;
        return true;
    }

    public bool IsEmpty()
    {
        return _charactersSize <= 0;
    }

    public bool HasPlayerActivity()
    {
        return _playerCount > 0;
    }

    public List<uint> GetObjectIdsList(List<uint> result, uint exclude)
    {
        GameObject[] temp;
        lock (_objectsLock)
        {
            if (_objects == null || _objectsSize == 0)
                return result;
            temp = new GameObject[_objectsSize];
            Array.Copy(_objects, 0, temp, 0, _objectsSize);
        }

        foreach (var obj in temp)
            if (obj.ObjId != exclude)
                result.Add(obj.ObjId);
        return result;
    }

    public List<GameObject> GetObjectsList(List<GameObject> result, uint exclude)
    {
        GameObject[] temp;
        lock (_objectsLock)
        {
            if (_objects == null || _objectsSize == 0)
                return result;
            temp = new GameObject[_objectsSize];
            Array.Copy(_objects, 0, temp, 0, _objectsSize);
        }

        foreach (var obj in temp)
            if (obj != null && obj.ObjId != exclude)
                result.Add(obj);
        return result;
    }

    public List<uint> GetListId<T>(List<uint> result, uint exclude) where T : class
    {
        GameObject[] temp;
        lock (_objectsLock)
        {
            if (_objects == null || _objectsSize == 0)
                return result;
            temp = new GameObject[_objectsSize];
            Array.Copy(_objects, 0, temp, 0, _objectsSize);
        }

        foreach (var obj in temp)
            if (obj is T && obj.ObjId != exclude)
                result.Add(obj.ObjId);

        return result;
    }

    public List<T> GetList<T>(List<T> result, uint exclude) where T : class
    {
        GameObject[] temp;
        lock (_objectsLock)
        {
            if (_objects == null || _objectsSize == 0)
                return result;
            temp = new GameObject[_objectsSize];
            Array.Copy(_objects, 0, temp, 0, _objectsSize);
        }

        foreach (var obj in temp)
        {
            var item = obj as T;
            if (item != null && obj.ObjId != exclude)
                result.Add(item);
        }

        return result;
    }

    public List<T> GetList<T>(List<T> result, uint exclude, float x, float y, float sqrad, bool useModelSize = false) where T : class
    {
        GameObject[] temp;
        lock (_objectsLock)
        {
            if (_objects == null || _objectsSize == 0)
                return result;
            temp = new GameObject[_objectsSize];
            Array.Copy(_objects, 0, temp, 0, _objectsSize);
        }

        foreach (var obj in temp)
        {
            var item = obj as T;
            if (item == null || obj.ObjId == exclude)
                continue;

            var finalrad = sqrad;
            if (useModelSize)
                finalrad += obj.ModelSize * obj.ModelSize;

            var dx = obj.Transform.World.Position.X - x;
            dx *= dx;
            if (dx > finalrad)
                continue;
            var dy = obj.Transform.World.Position.Y - y;
            dy *= dy;
            if (dx + dy < finalrad)
                result.Add(item);
        }

        return result;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        if (obj.GetType() != typeof(Region))
            return false;
        var other = (Region)obj;
        return other._worldId == _worldId && other.X == X && other.Y == Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_worldId, X, Y);
    }

    public Region[] FindDifferenceBetweenRegions(Region other)
    {
        var oldNeighbors = this.GetNeighbors();
        var newNeighbors = other.GetNeighbors();

        var difference = oldNeighbors.Except(newNeighbors).ToArray();

        return difference;
    }
}
