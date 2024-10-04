using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCNLDrivers
{
    public static class AstroAndTimeUtils
    {
        #region Moon phase

        static string[] moonPhaseNames = new string[]
        {
            "New",
            "Waxing crescent",
            "First quarter",
            "Waxing gibbous",
            "Full",
            "Waning gibbous",
            "Last quarter",
            "Waning crescent"
        };

        static string[] moonPhaseIcons = new string[]
        {
            "🌑",
            "🌒",
            "🌓",
            "🌔",
            "🌕",
            "🌖",
            "🌗",
            "🌘"
        };

        public static readonly int MoonPhaseNumber = 8;

        public static string GetMoonPhaseByIdx(int idx)
        {
            if ((idx >= 0) && (idx < moonPhaseIcons.Length))
                return moonPhaseIcons[idx];
            else
                throw new ArgumentOutOfRangeException();
        }

        public static int GetJulianDate(int y, int m, int d)
        {
            y -= (12 - m) / 10;
            m += 9;

            if (m >= 12)
                m -= 12;

            var k1 = (int)(365.25 * (y + 4712));
            var k2 = (int)(30.6001 * m + 0.5);

            // 'j' for dates in Julian calendar:
            var julianDate = k1 + k2 + d + 59;

            //Gregorian calendar
            if (julianDate > 2299160)
            {
                var k3 = (int)((y / 100 + 49) * 0.75) - 38;
                julianDate -= k3; //at 12h UT (Universal Time)
            }

            return julianDate;
        }

        public static double GetMoonAge(int y, int m, int d)
        {
            double ip, age;

            int jD = GetJulianDate(y, m, d);

            ip = (jD + 4.867) / 29.53059;
            ip -= Math.Floor(ip);

            age = ip * 29.53059 + 29.53059 / 2;

            if (age > 29.53059)
                age -= 29.53059;

            return age;
        }

        public static int MoonPhase(DateTime date, int parts)
        {
            var mAge = GetMoonAge(date.Year, date.Month, date.Day);
            return Convert.ToInt32(mAge * parts / 29.53059) & 7;
        }

        public static string MoonPhaseIcon(DateTime date)
        {
            return moonPhaseIcons[MoonPhase(date, moonPhaseIcons.Length)];
        }

        public static string MoonPhaseName(DateTime date)
        {
            return moonPhaseNames[MoonPhase(date, moonPhaseNames.Length)];
        }

        public static string MoonPhaseDescription(DateTime date)
        {
            return string.Format("{0} Moon {1}",
                moonPhaseNames[MoonPhase(date, moonPhaseNames.Length)],
                moonPhaseIcons[MoonPhase(date, moonPhaseIcons.Length)]);
        }

        #endregion

        #region Clocks

        static string[] globeIcons = new string[] { "🌍", "🌏", "🌎" };

        static string[] clockIcons = new string[]
        {
            "🕛",
            "🕧",
            "🕐",
            "🕜",
            "🕑",
            "🕝",
            "🕒",
            "🕞",
            "🕓",
            "🕟",
            "🕔",
            "🕠",
            "🕕",
            "🕡",
            "🕖",
            "🕢",
            "🕗",
            "🕣",
            "🕘",
            "🕤",
            "🕙",
            "🕥",
            "🕚",
            "🕦"
        };

        public static string GetClockIcon(DateTime dt)
        {
            return GetClockIcon(dt.Hour, dt.Minute);
        }

        public static string GetClockIcon(int hour, int minute)
        {
            var idx = hour;
            if (idx >= 12)
                idx -= 12;

            idx *= 2;

            if (minute > 15)
            {
                idx++;
                if (minute > 45)
                    idx++;
            }

            return clockIcons[idx & 23];
        }

        #endregion
    }
}
