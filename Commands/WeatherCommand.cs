using MafaniaBot.Abstractions;
using MafaniaBot.Dictionaries;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using Newtonsoft.Json;
using RestSharp;
using StackExchange.Redis;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Commands
{
    public class WeatherCommand : Command
    {
        public override string Pattern => @"/weather";
        public override string Description => "Прогноз погоды";

        private string city;
        private string units = "metric";
        private readonly string weatherAPIUrl = @"http://api.openweathermap.org/data/2.5/weather?q=";
        private readonly string weatherAPIKey = @"&appid=3427c491991e1ae8f4ec32a65206ccf1";
        private readonly string weatherUnits = @"&units=";
        private readonly string weatherLangCode = @"&lang=";

        public override bool Contains(Message message)
        {
            return message.Text.StartsWith(Pattern) && !message.From.IsBot;
        }

        public override async Task Execute(Message message, ITelegramBotClient botClient, IConnectionMultiplexer redis, IlocalizeService localizer)
        {
            try
            {
                var chatId = message.Chat.Id;
                var fromId = message.From.Id;
                var messageId = message.MessageId;
                var inputQuery = message.Text;
                string langCode = message.From.LanguageCode;
                string msg;

                Logger.Log.Info($"{GetType().Name}: #chatId={message.Chat.Id} #fromId={message.From}");

                localizer.Initialize(GetType().Name);
                langCode = await DBHelper.GetSetUserLanguageCodeAsync(redis, fromId, langCode);

                if (inputQuery.Length < 10)
                {
                    msg = localizer.GetResource("IncorrectWeatherFormat", langCode);

                    if (message.Chat.Type == ChatType.Private)
                    {
                        await botClient.SendTextMessageAsync(chatId, msg);

                        Logger.Log.Debug($"{GetType().Name}: #chatId={chatId} #msg={msg}");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);

                        Logger.Log.Debug($"{GetType().Name}: #chatId={chatId} #msg={msg} #replyToMessageId={messageId}");
                    }
                }
                else
                {
                    var startPos = inputQuery.IndexOf(' ');
                    var endPos = inputQuery.IndexOf(' ', startPos + 1);
                    city = endPos == -1 ?
                        inputQuery.Substring(startPos + 1) :
                        inputQuery.Substring(startPos + 1, endPos - startPos);
                    city = Regex.Replace(city, @"[^a-zA-Zа-яА-Я\-]+", "");

                    var client = new RestClient(weatherAPIUrl + city + weatherAPIKey + weatherUnits + units + weatherLangCode + langCode);
                    var request = new RestRequest(Method.GET);
                    var response = client.Execute(request);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            msg = localizer.GetResource("CityNotFound", langCode);

                            if (message.Chat.Type == ChatType.Private)
                            {
                                await botClient.SendTextMessageAsync(chatId, msg);

                                Logger.Log.Warn($"{GetType().Name}: Weather API exception #Code={response.StatusCode} #chatId={chatId} #msg={msg}");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);

                                Logger.Log.Warn($"{GetType().Name}: Weather API exception #Code={response.StatusCode} #chatId={chatId} #msg={msg} #replyToMessageId={messageId}");
                            }
                        }
                        else
                        {
                            msg = localizer.GetResource("Error", langCode);

                            if (message.Chat.Type == ChatType.Private)
                            {
                                await botClient.SendTextMessageAsync(chatId, msg);

                                Logger.Log.Error($"{GetType().Name}: Weather API exception #Code={response.StatusCode} #chatId={chatId} #msg={msg}");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);

                                Logger.Log.Error($"{GetType().Name}: Weather API exception #Code={response.StatusCode} #chatId={chatId} #msg={msg} #replyToMessageId={messageId}");
                            }
                        }
                        return;
                    }

                    var weatherData = JsonConvert.DeserializeObject<WeatherData>(response.Content);

                    msg = $"{DataDictionary.WeatherIconMapper["informationIcon"]} {localizer.GetResource("CurrentWeatherIn", langCode)} {weatherData.Name}, {weatherData.System.CountryCode}:\n\n" +
                        $"{DataDictionary.WeatherIconMapper[weatherData.Weather[0].Icon]} {weatherData.Main.Temperature,4:N1}°C\n" +
                        $"{char.ToUpper(weatherData.Weather[0].Description[0]) + weatherData.Weather[0].Description.Substring(1)}\n" +
                        $"{DataDictionary.WindDirectionIconMapper[WeatherHelper.ResolveWindDirection(weatherData.Wind.Deg)]} {weatherData.Wind.Speed,4:N1} {localizer.GetResource("WindUnitMetric", langCode)}   {DataDictionary.WeatherIconMapper["pressureIcon"]} {weatherData.Main.Pressure,4} {localizer.GetResource("PressureUnitMetric", langCode)}\n" +
                        $"{DataDictionary.WeatherIconMapper["humidityIcon"]} {weatherData.Main.Humidity,4}%         {DataDictionary.WeatherIconMapper["cloudinessIcon"]} {weatherData.Clouds.Cloudiness,4}%";

                    if (message.Chat.Type == ChatType.Private)
                    {
                        Logger.Log.Debug($"{GetType().Name}: #chatId={chatId} #msg={msg}");

                        await botClient.SendTextMessageAsync(chatId, msg);
                    }
                    else
                    {
                        Logger.Log.Debug($"{GetType().Name}: #chatId={chatId} #msg={msg} #replyToMessageId={messageId}");

                        await botClient.SendTextMessageAsync(chatId, msg, replyToMessageId: messageId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"{GetType().Name}: {ex.GetType().Name}", ex);
            }
        }
    }
}
