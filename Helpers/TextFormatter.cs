using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Helpers
{
	public static class TextFormatter
	{
		public static string GetTextWithoutCommand(string text, string command)
        {
			var isShortCommand = !text.Contains($"{command}@{Startup.BOT_USERNAME}");

			return isShortCommand ? text.Replace(command, string.Empty).Trim()
									: text.Replace($"{command}@{Startup.BOT_USERNAME}", string.Empty).Trim();
		}

		public static string GenerateMention(long userId, string firstname, string lastname = null, ParseMode parseMode = ParseMode.Html)
		{
			string mention = null;

			if (parseMode == ParseMode.Html)
			{
				var userNameString = lastname != null ? $"{firstname} {lastname}" : $"{firstname}";

				mention = $"<a href=\"tg://user?id={userId}\">{ConvertTextToParseMode(userNameString)}</a>";
			}

			return mention;
		}

		public static string ConvertTextToParseMode(string text, ParseMode parseMode = ParseMode.Html)
		{
			if (text != null)
			{
				if (parseMode == ParseMode.Html)
				{
					text = text.Replace("<", "");
					text = text.Replace(">", "");
					text = text.Replace("&", "&amp;");

					return text;
				}
			}

			return default;
		}
	}
}
