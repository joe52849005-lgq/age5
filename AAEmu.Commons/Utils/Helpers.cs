﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace AAEmu.Commons.Utils;

public static class Helpers
{
    private static Assembly _assembly;
    private static string _exePath;
    private static string _baseDirectory;

    public static Assembly Assembly => _assembly ?? (_assembly = Assembly.GetEntryAssembly());
    public static string ExePath => _exePath ?? (_exePath = Assembly.Location);
    public static readonly bool Is64Bit = Environment.Is64BitOperatingSystem;

    public static string BaseDirectory
    {
        get
        {
            if (_baseDirectory == null)
            {
                try
                {
                    _baseDirectory = ExePath;
                    if (_baseDirectory.Length > 0)
                        _baseDirectory = Path.GetDirectoryName(_baseDirectory);
                }
                catch
                {
                    _baseDirectory = "";
                }
            }
            return _baseDirectory;
        }
    }

    public static IEnumerable<Type> GetTypesInNamespace(Assembly sourceAssembly, string nameSpace)
    {
        return sourceAssembly.GetTypes().Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
    }

    public static long UnixTime(DateTime time)
    {
        if (time <= DateTime.MinValue)
            return 0;
        if (time < DateTime.UnixEpoch)
            return 0;
        var timeSpan = (time - DateTime.UnixEpoch);
        return (long)timeSpan.TotalSeconds;
    }

    public static DateTime UnixTime(long time)
    {
        if (time > DateTime.MaxValue.Second)
            return DateTime.MaxValue;

        if (time < DateTime.MinValue.Second)
            return DateTime.MinValue;

        return DateTime.UnixEpoch.AddSeconds(time);
    }

    public static long UnixTimeNow()
    {
        var timeSpan = (DateTime.UtcNow - DateTime.UnixEpoch);
        return (long)timeSpan.TotalSeconds;
    }

    public static long UnixTimeNowInMilli()
    {
        var timeSpan = (DateTime.UtcNow - DateTime.UnixEpoch);
        return (long)timeSpan.TotalMilliseconds;
    }

    [Obsolete("This method is deprecated, it's better to use ConvertPosition", false)]
    public static float ConvertX(byte[] coords)
    {
        return (float)Math.Round(coords[0] * 0.002f + coords[1] * 0.5f + coords[2] * 128, 4, MidpointRounding.ToEven);
    }

    [Obsolete("This method is deprecated, it's better to use ConvertPosition", false)]
    public static byte[] ConvertX(float x)
    {
        var coords = new byte[3];
        var temp = x;
        coords[2] = (byte)(temp / 128f);
        temp -= coords[2] * 128;
        coords[1] = (byte)(temp / 0.5f);
        temp -= coords[1] * 0.5f;
        coords[0] = (byte)(temp * 512);
        return coords;
    }

    [Obsolete("This method is deprecated, it's better to use ConvertPosition", false)]
    public static float ConvertY(byte[] coords)
    {
        return (float)Math.Round(coords[0] * 0.002f + coords[1] * 0.5f + coords[2] * 128, 4, MidpointRounding.ToEven);
    }

    [Obsolete("This method is deprecated, it's better to use ConvertPosition", false)]
    public static byte[] ConvertY(float y)
    {
        var coords = new byte[3];
        var temp = y;
        coords[2] = (byte)(temp / 128);
        temp -= coords[2] * 128;
        coords[1] = (byte)(temp / 0.5f);
        temp -= coords[1] * 0.5f;
        coords[0] = (byte)(temp * 512);
        return coords;
    }

    [Obsolete("This method is deprecated, it's better to use ConvertPosition", false)]
    public static float ConvertZ(byte[] coords)
    {
        return (float)Math.Round(coords[0] * 0.001f + coords[1] * 0.2561f + coords[2] * 65.5625f - 100, 4,
            MidpointRounding.ToEven);
    }

    [Obsolete("This method is deprecated, it's better to use ConvertPosition", false)]
    public static byte[] ConvertZ(float z)
    {
        var coords = new byte[3];
        var temp = z + 100;
        coords[2] = (byte)(temp / 65.5625f);
        temp -= coords[2] * 65.5625f;
        coords[1] = (byte)(temp / 0.2561);
        temp -= coords[1] * 0.2561f;
        coords[0] = (byte)(temp / 0.001);
        return coords;
    }

    public static (float x, float y, float z) ConvertPosition(byte[] values)
    {
        var tempX = 8 * (values[0] + ((values[1] + (values[2] << 8)) << 8));
        var flagX = (int)(((-(values[8] & 0x80) >> 30) & 0xFFFFFFFE) + 1);
        var resX = ((long)tempX << 32) * flagX;

        var tempY = 8 * (values[3] + ((values[4] + (values[5] << 8)) << 8));
        var flagY = (((-(values[8] & 0x40) >> 30) & 0xFFFFFFFE) + 1);
        var resY = ((long)tempY << 32) * flagY;

        var tempZ = (ulong)(values[6] + ((values[7] + ((values[8] & 0x3f) << 8)) << 8));

        var resultX = ConvertLongX(resX);
        var resultY = ConvertLongY(resY);
        var resultZ = (float)Math.Round(tempZ * 0.00000023841858 * 4196 - 100, 4, MidpointRounding.ToEven);

        return (resultX, resultY, resultZ);
    }

    public static byte[] ConvertPosition(float x, float y, float z)
    {
        var longX = ConvertLongX(x);
        var longY = ConvertLongY(y);

        var preX = longX >> 31;
        var preY = longY >> 31;

        var resultX = (preX ^ (longX + preX + (0 > preX ? 1 : 0))) >> 3;
        var resultY = (preY ^ (longY + preY + (0 > preY ? 1 : 0))) >> 3;
        var resultZ = (long)Math.Floor((z + 100f) / 4196f * 4194304f + 0.5);

        var position = new byte[9];
        position[0] = (byte)(resultX >> 32);
        position[1] = (byte)(resultX >> 40);
        position[2] = (byte)(resultX >> 48);

        position[3] = (byte)(resultY >> 32);
        position[4] = (byte)(resultY >> 40);
        position[5] = (byte)(resultY >> 48);

        position[6] = (byte)resultZ;
        position[7] = (byte)(resultZ >> 8);
        position[8] = (byte)(((resultZ >> 16) & 0x3F) + (((y < 0 ? 1 : 0) + 2 * (x < 0 ? 1 : 0)) << 6));
        return position;
    }

    public static float ConvertLongX(long x)
    {
        return (x >> 32) / 4096f;
    }

    public static long ConvertLongX(float x)
    {
        return (long)(x * 4096) << 32;
    }

    public static float ConvertLongY(long y)
    {
        return (y >> 32) / 4096f;
    }

    public static long ConvertLongY(float y)
    {
        return (long)(y * 4096) << 32;
    }

    public static short ConvertRotation(sbyte rotation)
    {
        return (short)(rotation * 0.0078740157f / 0.000030518509f);
    }

    public static sbyte ConvertRotation(short rotation)
    {
        return (sbyte)(rotation * 0.000030518509f / 0.0078740157f);
    }

    public static byte[] StringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
    }

    public static byte[] ConvertIp(string ip)
    {
        var result = IPAddress.Parse(ip);
        return result.GetAddressBytes().Reverse().ToArray();
    }

    public static byte Crc8(byte[] data, int size)
    {
        var len = size;
        uint checksum = 0;
        for (var i = 0; i <= len - 1; i++)
        {
            checksum *= 0x13;
            checksum += data[i];
        }

        return (byte)(checksum);
    }

    public static byte Crc8(byte[] data)
    {
        var size = data.Length;
        return Crc8(data, size);
    }

    public static sbyte ConvertRadianToSbyteDirection(float radian)
    {
        var z = radian * 0.15915494309189533576888376337251; // values.X / (float)Math.PI * 2; // convert from radians to direction

        var dir = Convert.ToSByte(z * 127f);

        return dir;
    }

    public static string ByteArrayToString(byte[] data)
    {
        var lookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        int i = 0, p = 0, l = data.Length;
        var c = new char[l * 2 + 2];
        byte d;
        //int p = 2; c[0] = '0'; c[1] = 'x'; //если хотим 0x
        while (i < l)
        {
            d = data[i++];
            c[p++] = lookup[d / 0x10];
            c[p++] = lookup[d % 0x10];
        }
        return new string(c, 0, c.Length);
    }

    // Method for cloning an object using MemberwiseClone
    public static T Clone<T>(T obj)
    {
        var inst = obj.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        return (T)inst?.Invoke(obj, null);
    }

    public static T DeepCopy<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json);
    }

    // Метод для разделения списка на несколько списков с указанным размером
    // Method for splitting a list into several lists with a specified size
    public static List<List<T>> SplitList<T>(List<T> list, int size)
    {
        return list.Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / size)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }
    public static T[][] SplitArray<T>(T[] array, int size)
    {
        var arraysCount = (int)Math.Ceiling((double)array.Length / size);
        var arrays = new T[arraysCount][];

        for (var i = 0; i < arraysCount; i++)
        {
            var elementsCount = Math.Min(size, array.Length - i * size);
            arrays[i] = new T[elementsCount];
            Array.Copy(array, i * size, arrays[i], 0, elementsCount);
        }

        return arrays;
    }

}
