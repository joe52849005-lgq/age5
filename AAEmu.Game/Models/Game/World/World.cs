﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.World.Transform;
using AAEmu.Game.Models.Game.World.Xml;
using AAEmu.Game.Models.Game.World.Zones;
using NLog;

namespace AAEmu.Game.Models.Game.World;

public class World
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public uint Id { get; set; } // iid - InstanceId
    public virtual string Name { get; set; }
    public float MaxHeight { get; set; }
    public virtual double HeightMaxCoefficient { get; set; }
    public float OceanLevel { get; set; }
    public int CellX { get; set; }
    public int CellY { get; set; }
    public uint TemplateId { get; set; } // worldId
    public WorldSpawnPosition SpawnPosition { get; set; } = new();
    public Region[,] Regions { get; set; } // TODO ... world - okay, instance - ....
    public virtual ushort[,] HeightMaps { get; set; }
    public List<uint> ZoneKeys { get; set; } = new();
    public ConcurrentDictionary<uint, XmlWorldZone> XmlWorldZones;
    public BoatPhysicsManager Physics { get; set; }
    public WaterBodies Water { get; set; }
    public WorldEvents Events { get; set; } = new();
    public Dictionary<uint, List<Area>> SubZones { get; set; } // uint is zoneid 
    public Dictionary<uint, List<Area>> HousingZones { get; set; } // uint is zoneid 

    public World()
    {
        Events = new WorldEvents();
        SubZones = new Dictionary<uint, List<Area>>();
        HousingZones = new Dictionary<uint, List<Area>>();
    }

    ~World()
    {
        Logger.Info($"World {Id} removed");
    }

    public virtual bool IsWater(Vector3 point)
    {
        if (Water != null)
            return Water.IsWater(point);

        if (point.Z <= OceanLevel)
            return true;

        // TODO: Check shapes
        return false;
    }

    public float GetRawHeightMapHeight(int x, int y)
    {
        // This is the old GetHeight()
        var sx = x / 2;
        var sy = y / 2;
        return (float)(HeightMaps[sx, sy] / HeightMaxCoefficient);
    }

    public static float Lerp(float s, float e, float t)
    {
        return s + (e - s) * t;
    }

    private static float Blerp(float cX0Y0, float cX1Y0, float cX0Y1, float cX1Y1, float tx, float ty)
    {
        return Lerp(Lerp(cX0Y0, cX1Y0, tx), Lerp(cX0Y1, cX1Y1, tx), ty);
    }

    private static System.Drawing.Rectangle FindNearestSignificantPoints(int x, int y)
    {
        return new System.Drawing.Rectangle(x - (x % 2), y - (y % 2), 2, 2);
    }

    public float GetHeight(float x, float y)
    {
        // return GetRawHeightMapHeight((int)x, (int)y); // <-- the old way we used to do things

        // Get bordering points
        var border = FindNearestSignificantPoints((int)Math.Floor(x), (int)Math.Floor(y));

        // Get heights for these points
        var heightTL = GetRawHeightMapHeight(border.Left, border.Top);
        var heightTR = GetRawHeightMapHeight(border.Right, border.Top);
        var heightBL = GetRawHeightMapHeight(border.Left, border.Bottom);
        var heightBR = GetRawHeightMapHeight(border.Right, border.Bottom);
        var offX = (x - border.Left) / 2;
        var offY = (y - border.Top) / 2;
        var height = Blerp(heightTL, heightTR, heightBL, heightBR, offX, offY); // bilinear interpolation

        return height;
    }

    /// <summary>
    /// Get Sector at specific offset
    /// </summary>
    /// <param name="x">X offset of the Sector</param>
    /// <param name="y">Y offset of the Sector</param>
    /// <returns></returns>
    public Region GetRegion(int x, int y)
    {
        if (ValidRegion(x, y))
            if (Regions[x, y] == null)
                return Regions[x, y] = new Region(Id, x, y, 0);
            else
                return Regions[x, y];

        return null;
    }

    public bool ValidRegion(int x, int y)
    {
        return x >= 0 && x < CellX * WorldManager.SECTORS_PER_CELL && y >= 0 && y < CellY * WorldManager.SECTORS_PER_CELL;
    }
}
