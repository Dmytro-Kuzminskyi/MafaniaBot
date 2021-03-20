using Newtonsoft.Json;

namespace MafaniaBot.Models
{
    public class BotCommand
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        public BotCommand(string command, string description)
        {
            Command = command;
            Description = description;
        }
    }
}
