using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace UCNLDrivers
{

    public static class UnicodeDays
    {
        #region Properties

        static Dictionary<string, string> FixedHolydays = new Dictionary<string, string>()
        {
{ "0101", "🎅🎄🎁 | Happy New Year!" },
{ "0201", "👽 | International Science Fiction Day" },
{ "0301", "🥤 | Drinking Straw Day" },
{ "0401", "🍎 | Newton's Day" },
{ "0501", " 👦🧗‍🚣🏼‍🏕🌦🐾 | Boyscout Day" },
{ "0701", " 🎅🎄🎁 | Merry Orthodox Christmas!" },
{ "0901", " 🕺✨💃 | Choreographer's Day" },
{ "1001", "🎂 | Happy birthday!" },
{ "1101", "🙏 | International Thank You Day" },
{ "1401", "🎄 | Happy Old New year!" },
{ "1501", "🌯 | Shawarma Day" },
{ "1701", "🧑🏼‍🔬 | Children's Invention Day" },
{ "1801", "☃ | World Snowman Day" },
{ "1901", "❄️☃️🌨 | World Snow Day" },
{ "2001", "🧀😁 | Cheese Lovers Day" },
{ "2101", "🤗👐|International Hug Day" },
{ "2301", "✍️ | Handwriting Day" },
{ "2401", "🍨🍦🍧 | International Eskimo Pie Day" },
{ "2601", "🛄​🛃​🛂​ | International Customs Day" },
{ "2801​", "👨‍💻​🆔 | Data Privacy Day​" },
{ "2901", "☮️ | International Mobilization Day Against Nuclear War" },
{ "0102", "🧕 | World Hijab Day" },
{ "0302", "🤬 | World Day Against Profanity" },
{ "0602", "🍸🍻🍷​🍺​​🍹 | International Bartender's Day | 📵 | Mobile Phone Waiver Day" },
{ "0902", "👨‍⚕️🦷 | International Day of dentist | 🍕 | World Pizza Day" },
{ "1102", "🤒😷 | World Day of the Sick" },
{ "1202", "🐵🧬🦧 | Darwin Day! | 🎂 | Happy birthday!" },
{ "1402", "💓😘💋 | Happy Valentine's Day!" },
{ "1702", "🙌 | Random Acts of Kindness Day" },
{ "1802", "🔋 | Battery Day" },
{ "1902", "🦦🐋🐬🦈 | International Marine Mammal Protection Day" },
{ "2102", "🗺 | International Tourist Guide Day" },
{ "2702", "🐻‍❄️ | International Polar Bear Day | 🎂 | Happy birthday!" },
{ "0103", "💁🤗⭐ | World Compliment Day" },
{ "0203", "🥢 | World match Day" },
{ "0303", "✍📝📗 | World Day of the writer  | 👂🦻🙉 | International Day for Ear and Hearing" },
{ "0403", "🦾📐 | World Engineering Day" },
{ "0503", "📴 | Day of gadgets turned off" },
{ "0603", "👨‍⚕️🦷 | International Dentist Day | 👶📺 | International Children's Day of Broadcasting" },
{ "0803", "8️🌷👩 | International Women's Day" },
{ "0903", "🎧🎛🎶📀 | World DJ Day" },
{ "1203", "🌐🤐 | World Day Against Cyber Censorship" },
{ "1303", "🌠🔭 | International Day of Planetaria" },
{ "1403", "𝞹 | International Pi Day | 🌫🏞 | International Day for Rivers" },
{ "1503", "🛒🛍 | World Consumer Rights Day" },
{ "1703", "🍀☘️ | Saint Patrick's Day" },
{ "2003", "🌍🌏🌎 | Earth Day" },
{ "2103", "🎎 | International Day of Puppetry" },
{ "2203", "🚕🚖 | Taxi Driver Day" },
{ "2403", "😔 | International Depression Awarness Day" },
{ "2703", "🎭 | World Theatre Day" },
{ "3003", "🏞🚶🏼 | Take a Walk in the Park Day" },
{ "3103", "💾 | World Backup Day" },
{ "0104", "1️🤪🤣 | April Fool's Day" },
{ "0304", "🎉🥳✨🍸 | World Party Day" },
{ "0404", "🌐 | Internet Day" },
{ "0504", "🍲🥘 | Soup Day" },
{ "0904", "🎂 | Happy Birthday!" },
{ "1004", "👨‍👩‍👧‍👦 | Siblings Day" },
{ "1104", "🆓 | International Day of Fascist Concentration Camps Prisoners Liberation" },
{ "1204", "👨‍🚀 | Day of Aviation and Cosmonautics" },
{ "1304", "🎸🤟​🎶​​ | World Rock-n-roll Day" },
{ "1504", "🗿🎺📜​🏛​​🏄‍​ | International Day of Culture" },
{ "1604", "🎤🎶 | World Voice Day" },
{ "1704", "💰💸🤑 | World Money Day" },
{ "1804", "📻 | World Amateur Radio Day" },
{ "2304", "🏓 | World Table Tennis Day" },
{ "2504", "🧬 | DNA Day" },
{ "0105", "✊⚒️🧰 | Happy labour day!" },
{ "0305", "☀ | Sun Day" },
{ "0405", "🚒🧯 | International Firefighters Day" },
{ "0705", "📻 | World Radio Day" },
{ "0905", "🎆 | Victory Day" },
{ "1505", "☂ | World Climate Day" },
{ "1705", "⚕ | World Hypertension Day" },
{ "1805", "🖼 | International Museum Day" },
{ "2005", "📐 | World Metrology Day" },
{ "2305", "🐢 | World Turtle Day" },
{ "2405", "АЗЪЕСЪМ | Day of Slavic Writing and Culture" },
{ "3105", "👸 | Blonde Day" },
{ "0106", "🚸👶 | International Children's Day" },
{ "0906", "🙋 | World Friends Day" },
{ "1006", "🍦🍨 | World Ice Cream Day" },
{ "1206", "🇷🇺 | Day of Russia" },
{ "1306", "🐞 | Lady Bug Day" },
{ "1506", "🌬💨 | Global Wind Day" },
{ "2106", "🚤🗺 | World Hydrography Day" },
{ "2206", "🖤 | Day of Remembrance and Mourning" },
{ "2306", "🏅 | International Olympic Day" },
{ "2706", "🎣 | World Fisheries Day" },
{ "0207", "🐕🐕‍🦺🦮 | World Dogs Day" },
{ "0607", "💋😘 | World Kiss Day" },
{ "0707", "🌳 | Ivan Kupala" },
{ "0807", "🤧 | World Allergy Day | 🎂 | Happy birthday!" },
{ "1107", "🍫 | World Chocolate Day" },
{ "1407", "🌀 | Pandemonium Day" },
{ "1607", "🐍 | World Snakes Day" },
{ "1707", "🎂 | Happy birthday!" },
{ "2007", "♟ | International Chess Day" },
{ "2207", "🧠 | World Brain Day" },
{ "2307", "🐋🐬 | World Whale and Dolphin Day" },
{ "2807", "🎂 | Happy birthday!" },
{ "2907", "🐅 | International Tiger Day" },
{ "0308", "🍉 | Watermelon Day" },
{ "0508", "🚦 | International Traffic Light Day" },
{ "0808", "🏔 | International Mountaineering Day" },
{ "1308", "👋 | International Day of Lefties" },
{ "1408", "🦎 | World Lizard Day" },
{ "1508", "🏺⛏ | Archeologist Day" },
{ "1908", "📸📷 | World Photography Day" },
{ "2008", "🦟 | World Mosquito Day" },
{ "2708", "🥊 | International Boxing Day" },
{ "2808", "🌅 | Dream Day" },
{ "0109", "🏫📚 | International Knowledge Day" },
{ "0409", "🏹 | International Archery Day" },
{ "0609", "🦰 | Day of the Redheads" },
{ "0909", "💃🏻 | International Beauty Day" },
{ "1809", "🚰🚱 | World Water Monitoring Day" },
{ "1909", "🙂 | Birthay of smiley" },
{ "2209", "🚗⛔ | World Carfree Day" },
{ "2909", "♥🫀 | World Heart Day" },
{ "0110", "🎶🎼🎻 | International Music Day" },
{ "0310", "🍷⛔ | World Day of Sobriety and Fight against Alcoholism" },
{ "0410", "🐼🦤🐸🐋 | World Animal Day" },
{ "1010", "🍲 | World Porridge Day" },
{ "1510", "🧼 | World Hand Washing Day" },
{ "1610", "🍞🥖 | World Bread Day" },
{ "1810", "👔 | Necktie Day | 🍬 | World Candy Day" },
{ "2010", "👨‍🍳 | International Chefs Day" },
{ "2510", "🍝 | World Pasta Day" },
{ "2710", "🧸 | International Teddy Bear Day" },
{ "3110", "🔪🎃🕯😈| Happy Halloween Day!" },
{ "0811", "🛜⛔ | World Day Without Wifi" },
{ "1011", "📒 | International Accounting Day" },
{ "1211", "🫁 | World Pneumonia Day" },
{ "1911", "🚹 | International Men's Day" },
{ "2111", "👋 | World Hello Day" },
{ "2211", "👶 | International Sons Day" },
{ "0212", "👨‍🎨🎨 | 2D Artists' Day" },
{ "0512", "🥷 | Day of the Ninja" },
{ "0812", "👨‍🎨🎨 | Artists' Day" },
{ "1112", "💃🏼 | International Day of Tango" },
{ "1412", "🐒🙈🙉🙊 | Monkey Day" },
{ "1512", "☕ | Tea Day | 🎂 | Happy birthday!" },
{ "1612", "🌄 | Summit Day" },
{ "2212", "🔌 | Energy Industry Day" },
{ "2512", "🎅🎄🧦🎁 | Merry Christmas!" },
{ "2612", "🎁 | Boxing Day" },
{ "2812", "🎦 | Cinema Day | 🎂 | Happy birthday!" },
{ "2912", "🎻 | International Cello Day" },
{ "3112", "🎅🎄🎁 | Happy New Year!" }
        };


        #endregion

        #region Methods

        public static string GetDayDescriptionAndIcons()
        {

            var key = string.Format("{0:00}{1:00}", DateTime.Now.Day, DateTime.Now.Month);

            if (FixedHolydays.ContainsKey(key))
                return FixedHolydays[key];
            else
                return string.Empty;
        }

        public static Dictionary<string, string> GetAllDays()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var item in FixedHolydays)
                result.Add(item.Key, item.Value);

            return result;
        }

        #endregion
    }
}
