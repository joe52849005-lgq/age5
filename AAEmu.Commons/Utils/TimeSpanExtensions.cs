using System;

namespace AAEmu.Commons.Utils;

public static class TimeSpanExtensions
{
    public static bool IsBetween(this TimeSpan time, TimeSpan startTime, TimeSpan endTime)
    {
        if (endTime == startTime)
        {
            return true;
        }

        if (endTime < startTime)
        {
            return time <= endTime || time >= startTime;
        }

        return time >= startTime && time <= endTime;
    }

    public static bool IsTimeBetween(this TimeSpan currentTime, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            // Обычный случай: диапазон не переходит через полночь
            return currentTime >= startTime && currentTime <= endTime;
        }
        else
        {
            // Диапазон переходит через полночь
            return currentTime >= startTime || currentTime <= endTime;
        }
    }
}
