using MafaniaBot.Constants;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MafaniaBot.Dictionaries
{
    public static class DataDictionary
    {
        public static IReadOnlyDictionary<string, string> WeatherIconMapper = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            { "informationIcon", "\ud83d\udce2" },
            { "pressureIcon", "\ud83c\udf21" },
            { "humidityIcon", "\ud83d\udca6" },
            { "cloudinessIcon", "\u26c5\ufe0f" },
            { "01d", "\u2600\ufe0f" }, { "01n", "\u2600\ufe0f" },
            { "02d", "\ud83c\udf24" }, { "02n", "\ud83c\udf24" },
            { "03d", "\u2601\ufe0f" }, { "03n", "\u2601\ufe0f" },
            { "04d", "\ud83c\udf25" }, { "04n", "\ud83c\udf25" },
            { "09d", "\ud83c\udf27" }, { "09n", "\ud83c\udf27" },
            { "10d", "\ud83c\udf26" }, { "10n", "\ud83c\udf26" },
            { "11d", "\u26c8" },       { "11n", "\u26c8" },
            { "13d", "\ud83c\udf28" }, { "13n", "\ud83c\udf28" },
            { "50d", "\ud83c\udf2b" }, { "50n", "\ud83c\udf2b" }
        });

        public static IReadOnlyDictionary<Direction, string> WindDirectionIconMapper = new ReadOnlyDictionary<Direction, string>(new Dictionary<Direction, string>
        {
            { Direction.N, "\u2b06\ufe0f" }, { Direction.NE, "\u2197\ufe0f" },
            { Direction.E, "\u27a1\ufe0f" }, { Direction.SE, "\u2198\ufe0f" },
            { Direction.S, "\u2b07\ufe0f" }, { Direction.SW, "\u2199\ufe0f" },
            { Direction.W, "\u2b05\ufe0f" }, { Direction.NW, "\u2196\ufe0f" }
        });
    }
}
