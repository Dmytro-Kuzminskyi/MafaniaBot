using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Helpers
{
	public static class TextFormatter
	{
		public static string GenerateMention(long userId, string firstname, string lastname = null, ParseMode parseMode = ParseMode.Html)
		{
			string mention = null;

			if (parseMode == ParseMode.Html)
			{
				var userNameString = lastname != null ? $"{firstname} {lastname}" : $"{firstname}";

				mention = $"<a href=\"tg://user?id={userId}\">{ConvertTextToHtmlParseMode(userNameString)}</a>";
			}

			return mention;
		}

		public static string ConvertTextToHtmlParseMode(string text)
		{
			if (text != null)
			{
				text = text.Replace("<", "");
				text = text.Replace(">", "");
				text = text.Replace("&", "&amp;");
				return text;
			}

			return default;
		}
	}
}
