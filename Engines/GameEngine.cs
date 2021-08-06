using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MafaniaBot.Abstractions;
using MafaniaBot.Engines.GameSubscribers;
using MafaniaBot.Models;
using Telegram.Bot;

namespace MafaniaBot.Engines
{
    public sealed class GameEngine
    {
        private static readonly IReadOnlyDictionary<Type, Func<ISubscriber>> Strategies = new ReadOnlyDictionary<Type, Func<ISubscriber>>(new Dictionary<Type, Func<ISubscriber>>
        {
            { typeof(ClassicWordsGame), () => new ClassicWordsGameSubscriber() }
        });
        private static readonly object instanceLock = new object();
        private static readonly object gamesLock = new object();
        private static readonly object gameInvitesLock = new object();
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly HashSet<Game> games;
        private readonly HashSet<GameInvite> gameInvites;
        private static GameEngine instance = null;

        private GameEngine(ITelegramBotClient telegramBotClient)
        {
            _telegramBotClient = telegramBotClient;
            games = new HashSet<Game>();
            gameInvites = new HashSet<GameInvite>();
        }

        public static GameEngine Instance => instance;

        public event EventHandler<GenericEventArgs<GameInvite>> RemovedGameInvite;
        public event EventHandler<GenericEventArgs<GameInvite>> RegisteredGameInvite;

        public static GameEngine Initialize(ITelegramBotClient telegramBotClient)
        {
            lock (instanceLock) instance = instance ?? new GameEngine(telegramBotClient);
            return instance;
        }

        public Task RegisterGameInviteAsync(GameInvite gameInvite, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                lock (gameInvitesLock)
                {
                    if (gameInvites.Add(gameInvite))
                        RegisteredGameInvite?.Invoke(this, new GenericEventArgs<GameInvite>(gameInvite));
                }
            }, cancellationToken);
        }

        public Task RemoveGameInviteAsync(GameInvite gameInvite, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                lock (gameInvitesLock)
                {
                    if (gameInvites.Remove(gameInvite))
                    {
                        RemovedGameInvite?.Invoke(this, new GenericEventArgs<GameInvite>(gameInvite));
                    }
                }             
            }, cancellationToken);
        }

        public Task RemoveExpiredGameInvites()
        {
            return Task.Run(() =>
            {
                Parallel.ForEach(gameInvites, async gameInvite =>
                {
                    if (DateTime.Now - gameInvite.Date > gameInvite.TimeToLive)
                        await RemoveGameInviteAsync(gameInvite);
                });
            });
        }

        public void RegisterGame(Game game)
        {
            lock (gamesLock)
            {
                Parallel.ForEach(game.Players, player =>
                {
                    foreach (var g in games)
                    {
                        if (g.Players.Any(e => e.UserId == player.UserId))
                            return;
                    }
                });
                
                games.Add(game);
            }

            Parallel.ForEach(game.Players, player =>
            {
                var gameInvite = FindGameInviteFromUserByChatId(game.ChatId, player.UserId);

                if (gameInvite != null)
                    RemoveGameInviteAsync(gameInvite);
            });
            
            Subscribe(game);
            ((IPreparable)Convert.ChangeType(game, game.GameType)).Prepare();
        }

        public void RemoveGame(Game game)
        {           
            lock (gamesLock) games.Remove(game);
            Unsubscribe(game);
        }

        public GameInvite FindGameInviteFromUserByChatId(long chatId, long userId)
        {
            lock (gameInvitesLock)
            {
                return gameInvites.Where(e => e.ChatId == chatId && e.UserId == userId).FirstOrDefault();
            }
        }

        public Game FindGameByPlayerId(long playerId)
        {
            lock (gamesLock)
            {
                foreach (var game in games)
                {
                    if (game.Players.Any(e => e.UserId == playerId))
                        return game;
                }
            }

            return default;
        }

        public Game FindGameByPlayersId(long[] playersId)
        {
            int playersCount = playersId.Length;

            lock (gamesLock)
            {
                foreach (var game in games)
                {
                    if (game.Players.Length != playersCount)
                        continue;
                    else
                    {
                        for (int i = 0; i < playersCount; i++)
                        {
                            if (game.Players[i].UserId == playersId[i])
                                return game;
                        }
                    }
                }
            }

            return default;
        }

        private void Subscribe(Game game)
        {
            var strategy = Strategies[game.GameType]();
            strategy.Subscribe(game, _telegramBotClient);
        }

        private void Unsubscribe(Game game)
        {
            var strategy = Strategies[game.GameType]();
            strategy.Unsubscribe(game);
        }
    }
}
