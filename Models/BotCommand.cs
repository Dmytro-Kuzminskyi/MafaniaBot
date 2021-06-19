using Newtonsoft.Json;

namespace MafaniaBot.Models
{
    public class BotCommand
    {
        public BotCommand(string command, string description)
        {
            Command = command;
            Description = description;
        }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }   
    }
}
