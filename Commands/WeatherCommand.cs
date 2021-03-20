using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MafaniaBot.Commands
{
    public class WeatherCommand : Command
    {
        public override string Pattern { get; }

        public override string Description { get; }

        private string city = null;
		private string units = "metric";
		private readonly string weatherAPIUrl = @"http://api.openweathermap.org/data/2.5/weather?q=";
		private readonly string weatherAPIKey = @"&appid=3427c491991e1ae8f4ec32a65206ccf1";
		private readonly string weatherUnits = @"&units=";

        public WeatherCommand()
        {
            Pattern = @"/weather";
            Description = "Прогноз погоды";
        }

		public override bool Contains(Message message)
		{
			return message.Text.StartsWith(Pattern) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
            try
            {
                long chatId = message.Chat.Id;
                int messageId = message.MessageId;
                string input = message.Text;

                if (input.Length < 10)
                {
                    Logger.Log.Debug($"/WEATHER SendTextMessage #chatId={chatId} #msg=Incorrect command format #replyToMessageId={messageId}");

                    await botClient.SendTextMessageAsync(chatId, "Введите команду в формате /weather <Город>", replyToMessageId: messageId);
                }
                else
                {
                    int startPos = input.IndexOf(' ');
                    int endPos = input.IndexOf(' ', startPos + 1);
                    city = endPos == -1 ?
                        input.Substring(startPos + 1) :
                        input.Substring(startPos + 1, endPos - startPos);
                    city = Regex.Replace(city, @"[^a-zA-Zа-яА-Я\-]+", "");

                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)
                            WebRequest.Create(weatherAPIUrl + city + weatherAPIKey + weatherUnits + units);

                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        using var receiveStream = response.GetResponseStream();
                        using var readStream = new StreamReader(receiveStream, Encoding.UTF8);
                        string jsonResponse = readStream.ReadToEnd();

                        JObject obj = JObject.Parse(jsonResponse);
                        string name = obj["name"].ToString();
                        string country = obj["sys"]["country"].ToString();
                        float temp = float.Parse(obj["main"]["temp"].ToString());
                        float feels_like = float.Parse(obj["main"]["feels_like"].ToString());
                        float temp_min = float.Parse(obj["main"]["temp_min"].ToString());
                        float temp_max = float.Parse(obj["main"]["temp_max"].ToString());
                        double pressure = float.Parse(obj["main"]["pressure"].ToString()) / 1.333;
                        int humidity = int.Parse(obj["main"]["humidity"].ToString());

                        string msg = "Текущая погода в " + name + ", " + country +
                            "\nТемпература: " + Math.Round(temp, 1) + " °С" +
                            "\nПо ощущениям: " + Math.Round(feels_like, 1) + " °С" +
                            "\nМинимальная: " + Math.Round(temp_min, 1) + " °С" +
                            "\nМаксимальная: " + Math.Round(temp_max, 1) + " °С" +
                            "\nДавление: " + Math.Round(pressure) + " мм pт. ст." +
                            "\nВлажность: " + humidity + " %";

                        Logger.Log.Debug($"/WEATHER SendTextMessage #chatId={chatId} #msg={msg} #replyToMessageId={messageId}");

                        await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
                    }
                    catch (WebException wex)
                    {
                        if (wex.Message.Contains("(404) Not Found"))
                        {
                            Logger.Log.Warn($"/WEATHER Not found #chatId={chatId} #replyToMessageId={messageId}", wex);

                            await botClient.SendTextMessageAsync(chatId, "Город не найден!", replyToMessageId: messageId);
                        }
                        else
                        {
                            Logger.Log.Error($"/WEATHER Error in api endpoint", wex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"/WEATHER ---", ex);
            }
		}
	}
}