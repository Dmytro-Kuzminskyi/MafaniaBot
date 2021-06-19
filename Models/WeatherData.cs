using Newtonsoft.Json;

namespace MafaniaBot.Models
{
    public class WeatherData
    {
        [JsonProperty("main")]
        public Main Main { get; set; }
        [JsonProperty("weather")]
        public Weather[] Weather { get; set; }
        [JsonProperty("clouds")]
        public Clouds Clouds { get; set; }
        [JsonProperty("wind")]
        public Wind Wind { get; set; }
        [JsonProperty("sys")]
        public Sys System { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Main
    {
        [JsonProperty("temp")]
        public float Temperature { get; set; }
        [JsonProperty("pressure")]
        public int Pressure { get; set; }
        [JsonProperty("humidity")]
        public int Humidity { get; set; }
    }

    public class Weather
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class Clouds
    {
        [JsonProperty("all")]
        public int Cloudiness { get; set; }
    }

    public class Wind
    {
        [JsonProperty("speed")]
        public float Speed { get; set; }
        [JsonProperty("deg")]
        public float Deg { get; set; }
    }

    public class Sys
    {
        [JsonProperty("country")]
        public string CountryCode { get; set; }
    }
}
