using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace MafaniaBot.Commands
{
	public class WeatherCommand : Command
	{
		public override string pattern => @"/weather";
		private string city = null;
		private string units = "metric";
		private readonly string weatherAPIUrl = @"http://api.openweathermap.org/data/2.5/weather?q=";
		private readonly string weatherAPIKey = @"&appid=3427c491991e1ae8f4ec32a65206ccf1";
		private readonly string weatherUnits = @"&units=";

		public override bool Contains(Message message)
		{
			if (message.Chat.Type == ChatType.Channel)
				return false;

			return message.Text.StartsWith(pattern) && !message.From.IsBot;
		}

		public override async Task Execute(Message message, ITelegramBotClient botClient)
		{
			var chatId = message.Chat.Id;
			var messageId = message.MessageId;
			var input = message.Text;

			if (input.Length < 10)
			{
				await botClient.SendTextMessageAsync(chatId, "Введите команду в формате /weather <Город>", 
					replyToMessageId: messageId);				
			}
			else
			{
				var startPos = input.IndexOf(' ');
				var endPos = input.IndexOf(' ', startPos + 1);
				city = endPos == -1 ?
					input.Substring(startPos + 1) :
					input.Substring(startPos + 1, endPos - startPos);
				city = Regex.Replace(city, @"[^a-zA-Zа-яА-Я\-]+", "");

				try
				{
					var request = (HttpWebRequest)
						WebRequest.Create(weatherAPIUrl + city + weatherAPIKey + weatherUnits + units);

					var response = (HttpWebResponse)request.GetResponse();

					using var receiveStream = response.GetResponseStream();
					using var readStream = new StreamReader(receiveStream, Encoding.UTF8);
					var jsonResponse = readStream.ReadToEnd();

					var obj = JObject.Parse(jsonResponse);
					var name = obj["name"].ToString();
					var country = obj["sys"]["country"].ToString();
					var temp = float.Parse(obj["main"]["temp"].ToString());
					var feels_like = float.Parse(obj["main"]["feels_like"].ToString());
					var temp_min = float.Parse(obj["main"]["temp_min"].ToString());
					var temp_max = float.Parse(obj["main"]["temp_max"].ToString());
					var pressure = float.Parse(obj["main"]["pressure"].ToString()) / 1.333;
					var humidity = int.Parse(obj["main"]["humidity"].ToString());

					var msg = "Текущая погода в " + name + ", " + country +
						"\nТемпература: " + Math.Round(temp, 1) + " °С" +
						"\nПо ощущениям: " + Math.Round(feels_like, 1) + " °С" +
						"\nМинимальная: " + Math.Round(temp_min, 1) + " °С" +
						"\nМаксимальная: " + Math.Round(temp_max, 1) + " °С" +
						"\nДавление: " + Math.Round(pressure) + " мм pт. ст." +
						"\nВлажность: " + humidity + " %";


					await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
				} 
				catch (WebException wex)
				{
					Console.WriteLine(wex.Message);
					if (wex.Message.Contains("(404) Not Found"))
					{
						await botClient.SendTextMessageAsync(chatId, "Город не найден!" , replyToMessageId: messageId);
					}
				}
			}
		}
	}
}