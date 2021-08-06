using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Helpers;
using MafaniaBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Engines.GameSubscribers
{
    public class ClassicWordsGameSubscriber : ISubscriber
    {
        private ITelegramBotClient _botClient;
        
        public void Subscribe(Game game, ITelegramBotClient telegramBotClient)
        {
            var wordsGame = (ClassicWordsGame)game;
            _botClient = telegramBotClient;
            wordsGame.GamePrepared += GamePreparedEventRaised;
            wordsGame.GameStarted += GameStartedEventRaised;
            wordsGame.GameStopped += GameStoppedEventRaised;
            wordsGame.GameEnded += GameEndedEventRaised;
            wordsGame.WordFound += WordFoundEventRaised;
            wordsGame.WordExists += WordExistsEventRaised;
            wordsGame.WordNotExist += WordNotExistEventRaised;
        }

        public void Unsubscribe(Game game)
        {
            var wordsGame = (ClassicWordsGame)game;
            wordsGame.GamePrepared -= GamePreparedEventRaised;
            wordsGame.GameStarted -= GameStartedEventRaised;
            wordsGame.GameStopped -= GameStoppedEventRaised;
            wordsGame.GameEnded -= GameEndedEventRaised;
            wordsGame.WordFound -= WordFoundEventRaised;
            wordsGame.WordExists -= WordExistsEventRaised;
            wordsGame.WordNotExist -= WordNotExistEventRaised;
        }

        private void GamePreparedEventRaised(object sender, EventArgs e)
        {
            var gameSender = (ClassicWordsGame)sender;

            var firstPlayer = gameSender.Players[0];
            var secondPlayer = gameSender.Players[1];
            var firstPlayerMention = TextHelper.GenerateMention(firstPlayer.UserId, firstPlayer.FirstName, firstPlayer.LastName);
            var secondPlayerMention = TextHelper.GenerateMention(secondPlayer.UserId, secondPlayer.FirstName, secondPlayer.LastName);

            var linkBtn = InlineKeyboardButton.WithUrl("Перейти к игре", Startup.BOT_URL);
            var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { linkBtn } });

            var groupMsg = $"Игра между {firstPlayerMention} и {secondPlayerMention} начнется через 5 сек!";

            gameSender.GameResultsMessageId = _botClient.SendTextMessageAsync(gameSender.ChatId, groupMsg, parseMode: ParseMode.Html, replyMarkup: keyboard).GetAwaiter().GetResult().MessageId;
        }

        private void GameStartedEventRaised(object sender, EventArgs e)
        {
            var gameSender = (ClassicWordsGame)sender;

            var firstPlayer = gameSender.Players[0];
            var secondPlayer = gameSender.Players[1];
            
            var groupMsg = $"<b>Игра в классические слова</b>\n" +
                    $"Счет {TextHelper.ConvertTextToHtmlParseMode(firstPlayer.FirstName)}: 0\n" +
                    $"Счет {TextHelper.ConvertTextToHtmlParseMode(secondPlayer.FirstName)}: 0\n" +
                    gameSender.GetRemainingTimeString();

            var linkBtn = InlineKeyboardButton.WithUrl("Перейти к игре", Startup.BOT_URL);
            var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { linkBtn } });

            _botClient.EditMessageTextAsync(gameSender.ChatId, gameSender.GameResultsMessageId, groupMsg, parseMode: ParseMode.Html, replyMarkup: keyboard);

            Parallel.ForEach(gameSender.Players, async player =>
            {
                int pos = 0;
                gameSender.PlayersGameFieldDictionary.TryGetValue(player.UserId, out var playerGameField);
                var tempGameField = new List<string>(playerGameField);
                var privateMsg = "<b>Игра в классические слова началась!</b>\n" +
                            $"Твой счет: {player.Score}\n\n<pre>";

                for (int i = 0; i < gameSender.BoardHeight; i++)
                {
                    for (int j = 0; j < gameSender.BoardWidth; j++)
                        privateMsg += tempGameField[pos++] + " ";

                    privateMsg += "\n";
                }

                privateMsg += $"</pre>\n<b>{gameSender.GetRemainingTimeString()}</b>";

                await _botClient.SendTextMessageAsync(player.UserId, privateMsg, parseMode: ParseMode.Html);
            });
        }

        private void GameStoppedEventRaised(object sender, GenericEventArgs<long> e)
        {
            var gameSender = (ClassicWordsGame)sender;

            var initiator = gameSender.Players.Where(x => x.UserId == e.Value);
            var winner = gameSender.Players.Except(initiator).First();
            var commonMsg = "<b>Игра в классические слова завершена!</b>\n";
            var commonBlockMsg = $"{TextHelper.ConvertTextToHtmlParseMode(initiator.First().FirstName)} заблокировал(а) меня ☹️";
            var groupMsg = commonMsg +
                            $"🏆 Победитель {TextHelper.ConvertTextToHtmlParseMode(winner.FirstName)} 🏆\n" +
                            commonBlockMsg;

            var privateMsg = commonMsg + "🏆 Ты победил(а) 🏆\n" + commonBlockMsg;

            var groupChatDeleteTask = _botClient.DeleteMessageAsync(gameSender.ChatId, gameSender.GameResultsMessageId);
            var groupChatTask = _botClient.SendTextMessageAsync(gameSender.ChatId, groupMsg, parseMode: ParseMode.Html);
            var privateChatTask = _botClient.SendTextMessageAsync(winner.UserId, privateMsg, parseMode: ParseMode.Html);

            Task.WhenAll(new[] { groupChatDeleteTask, groupChatTask, privateChatTask });
        }

        private void GameEndedEventRaised(object sender, EventArgs e)
        {
            var gameSender = (ClassicWordsGame)sender;

            var firstPlayer = gameSender.Players[0];
            var secondPlayer = gameSender.Players[1];
            var commonMsg = "<b>Игра в классические слова завершена!</b>\n";

            var groupMsg = commonMsg;
            var privateMsgFirstPlayer = commonMsg;
            var privateMsgSecondPlayer = commonMsg;

            if (firstPlayer.Score > secondPlayer.Score)
            {
                groupMsg += $"🏆 Победитель {TextHelper.ConvertTextToHtmlParseMode(firstPlayer.FirstName)} 🏆\n";
                privateMsgFirstPlayer += "🏆 Ты победил(а) 🏆";
                privateMsgSecondPlayer += "Ты проиграл(а)";
            }

            if (firstPlayer.Score < secondPlayer.Score)
            {
                groupMsg += $"🏆 Победитель {TextHelper.ConvertTextToHtmlParseMode(secondPlayer.FirstName)} 🏆\n";
                privateMsgFirstPlayer += "Ты проиграл(а)";
                privateMsgSecondPlayer += "🏆 Ты победил(а) 🏆";
            }

            if (firstPlayer.Score == secondPlayer.Score)
            {
                var evenMsg = "🌚 Ничья 🌝";

                groupMsg += $"{evenMsg}\n";
                privateMsgFirstPlayer += evenMsg;
                privateMsgSecondPlayer += evenMsg;
            }

            groupMsg += $"Счет {TextHelper.ConvertTextToHtmlParseMode(firstPlayer.FirstName)}: {firstPlayer.Score}\n" +
                $"Счет {TextHelper.ConvertTextToHtmlParseMode(secondPlayer.FirstName)}: {secondPlayer.Score}";

            var groupChatDeleteTask = _botClient.DeleteMessageAsync(gameSender.ChatId, gameSender.GameResultsMessageId);
            var groupChatTask = _botClient.SendTextMessageAsync(gameSender.ChatId, groupMsg, parseMode: ParseMode.Html);
            var privateChatFirstPlayerTask = _botClient.SendTextMessageAsync(gameSender.Players[0].UserId, privateMsgFirstPlayer, parseMode: ParseMode.Html);
            var privateChatSecondPlayerTask = _botClient.SendTextMessageAsync(gameSender.Players[1].UserId, privateMsgSecondPlayer, parseMode: ParseMode.Html);

            Task.WhenAll(new[] { groupChatDeleteTask, groupChatTask, privateChatFirstPlayerTask, privateChatSecondPlayerTask });
        }

        private void WordFoundEventRaised(object sender, GenericEventArgs<Guess> e)
        {
            var gameSender = (ClassicWordsGame)sender;

            var msg = $"Слово <b>{e.Value.Text.ToLower()}</b> уже отгадали!\n" + gameSender.GenerateWordsGameBoardString(e.Value.UserId) + $"\n<b>{gameSender.GetRemainingTimeString()}</b>";

            _botClient.SendTextMessageAsync(e.Value.UserId, msg, parseMode: ParseMode.Html);
        }

        private void WordExistsEventRaised(object sender, GenericEventArgs<Guess> e)
        {
            var gameSender = (ClassicWordsGame)sender;

            var linkBtn = InlineKeyboardButton.WithUrl("Перейти к игре", Startup.BOT_URL);
            var keyboard = new InlineKeyboardMarkup(new[] { new InlineKeyboardButton[] { linkBtn } });

            var remainingTimeMsg = gameSender.GetRemainingTimeString();
            var privateMsg = $"Ты отгадал(а) слово <b>{e.Value.Text.ToLower()}</b>!\n" + gameSender.GenerateWordsGameBoardString(e.Value.UserId) + $"\n<b>{remainingTimeMsg}</b>";

            var firstPlayer = gameSender.Players[0];
            var secondPlayer = gameSender.Players[1];

            var groupMsg = $"<b>Игра в классические слова</b>\n" +
                            $"Счет {TextHelper.ConvertTextToHtmlParseMode(firstPlayer.FirstName)}: {firstPlayer.Score}\n" +
                            $"Счет {TextHelper.ConvertTextToHtmlParseMode(secondPlayer.FirstName)}: {secondPlayer.Score}\n" +
                            remainingTimeMsg;

            var privateChatTask = _botClient.SendTextMessageAsync(e.Value.UserId, privateMsg, parseMode: ParseMode.Html);
            var groupChatTask = _botClient.EditMessageTextAsync(gameSender.ChatId, gameSender.GameResultsMessageId, groupMsg, parseMode: ParseMode.Html, replyMarkup: keyboard);

            Task.WhenAll(new[] { privateChatTask, groupChatTask });
        }

        private void WordNotExistEventRaised(object sender, GenericEventArgs<Guess> e)
        {
            var gameSender = (ClassicWordsGame)sender;

            var msg = $"Слова <b>{e.Value.Text.ToLower()}</b> не существует!\n" + gameSender.GenerateWordsGameBoardString(e.Value.UserId) + $"\n<b>{gameSender.GetRemainingTimeString()}</b>";

            _botClient.SendTextMessageAsync(e.Value.UserId, msg, parseMode: ParseMode.Html);
        }
    }
}
