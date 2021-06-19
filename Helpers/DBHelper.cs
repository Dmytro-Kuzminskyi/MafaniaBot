using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace MafaniaBot.Helpers
{
    public static class DBHelper
    {
        public static async Task<string> GetSetUserLanguageCodeAsync(IConnectionMultiplexer redis, int userId, string langCode)
        {
            try
            {
                IDatabaseAsync db = redis.GetDatabase();
                var langCodeRedisValue = await db.StringGetAsync(new RedisKey($"Language:{userId}"));

                if (langCodeRedisValue.IsNullOrEmpty)
                {
                    await db.StringSetAsync(new RedisKey($"Language:{userId}"), new RedisValue(langCode));
                    return langCode;
                }
                else
                {
                    return langCodeRedisValue.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"DBHelper: Cannot get or set Language:{userId} value.", ex);
                return null;
            }
        }
    }
}
