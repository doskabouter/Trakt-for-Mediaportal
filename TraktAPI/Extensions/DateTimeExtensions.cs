﻿using System;
using System.Collections.Generic;
using System.Globalization;
using TraktAPI.Properties;

namespace TraktAPI.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Date Time extension method to return a unix epoch
        /// time as a long
        /// </summary>
        /// <returns> A long representing the Date Time as the number
        /// of seconds since 1/1/1970</returns>
        public static long ToEpoch(this DateTime dt)
        {
            return (long)(dt - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Long extension method to convert a Unix epoch
        /// time to a standard C# DateTime object.
        /// </summary>
        /// <returns>A DateTime object representing the unix
        /// time as seconds since 1/1/1970</returns>
        public static DateTime FromEpoch(this long unixTime)
        {
            return new DateTime(1970, 1, 1).AddSeconds(unixTime);
        }

        /// <summary>
        /// Converts string DateTime to ISO8601 format
        /// 2014-09-01T09:10:11.000Z
        /// </summary>
        /// <param name="dt">DateTime as string</param>
        /// <param name="hourShift">Number of hours to shift original time</param>
        /// <returns>ISO8601 Timestamp</returns>
        public static string ToISO8601(this string dt, double hourShift = 0, bool isLocal = false)
        {
            DateTime date;
            if (DateTime.TryParse(dt, out date))
            {
                if (isLocal)
                    return date.AddHours(hourShift).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                else
                    return date.AddHours(hourShift).ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        public static string ToISO8601(this DateTime dt, double hourShift = 0)
        {
            string retValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (dt == null)
                return retValue;

            return dt.AddHours(hourShift).ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public static DateTime FromISO8601(this string dt)
        {
            DateTime date;
            if (DateTime.TryParse(dt, out date))
            {
                return date;
            }

            return DateTime.UtcNow;
        }

        /// <summary>
        /// Returns the corresponding Olsen timezone e.g. 'Atlantic/Canary' into a Windows timezone e.g. 'GMT Standard Time'
        /// </summary>
        public static string OlsenToWindowsTimezone(this string olsenTimezone)
        {
            if (olsenTimezone == null)
                return null;

            if (_timezoneMappings == null)
            {
                try
                {
                    _timezoneMappings = Resources.OlsenToWindows.FromJSONDictionary<Dictionary<string, string>>();
                }
                catch
                {
                    return null;
                }
            }

            string windowsTimezone;
            _timezoneMappings.TryGetValue(olsenTimezone, out windowsTimezone);

            return windowsTimezone;
        }
        static Dictionary<string, string> _timezoneMappings = null;

        public static string ToLocalisedDayOfWeek(this DateTime date)
        {
            if (date == null) return null;

            return DateTimeFormatInfo.CurrentInfo?.GetDayName(date.DayOfWeek);
        }
    }
}
