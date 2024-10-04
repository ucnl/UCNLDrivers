using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCNLDrivers
{
    public static class MDates
    {
        public static string GetReferenceNote()
        {
            Dictionary<string, string> mstr = new Dictionary<string, string>()
            {
                { "0101", "👪🌃🥂🎇🎄🎅" },
                { "1001", "🎂" },
                { "1202", "🎂" },
                { "1402", "💝" },
                { "2702", "🎂" },
                { "0103", "😸" },
                { "0803", "💐🍾" },
                { "0903", "🎂" },
                { "1403", "𝛑" },
                { "0104", "📅1️🤪🤣" },
                { "1204", "🚀☭" },
                { "0105", "☭✊⚒️🧰" },
                { "0106", "💐👶" },
                { "0606", "​​🇷🇺Ъ" },
                { "0806", "🌊" },
                { "1206", "​​🇷🇺​🍾🐻‍❄️" },
                { "2106", "🧘" },
                { "2506", "⛵" },
                { "2706", "🎣" },
                { "0807", "🎂" },
                { "2007", "♞" },
                { "1707", "🎂" },
                { "2807", "🎂" },
                { "0508", "🚂🛲🚆🚅🚄🍾" },
                { "1608", "🎂" },
                { "0109", "🎂" },
                { "1309", "💻" },
                { "3110", "🔪🎃🕯" },
                { "0711", "☭✊" },
                { "1011", "⚛🧬📡🔬⚗" },
                { "1911", "🧔♂🔧" },
                { "1512", "🎂" },
                { "2112", "🐲🇨🇳📅🎆" },
                { "2512", "2️5️👼🎄🌟" },
                { "2812", "🎂" },
                { "3112", "👪🌃🥂🎇🎄🎅" },
            };

            var key = string.Format("{0:00}{1:00}", DateTime.Now.Day, DateTime.Now.Month);

            if (mstr.ContainsKey(key))
                return mstr[key];
            else
                return string.Empty;
        }        
    }
}
