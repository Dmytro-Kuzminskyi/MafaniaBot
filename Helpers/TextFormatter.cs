using Telegram.Bot.Types.Enums;

namespace MafaniaBot.Helpers
{
	public static class TextFormatter
	{
		public static string GenerateMention(long userId, string firstname, string lastname, ParseMode parseMode = ParseMode.Html)
		{
			string mention = null;

			if (parseMode == ParseMode.Html)
			{
				mention = $"<a href=\"tg://user?id={userId}\">{ConvertTextToHtmlParseMode(firstname)} " +
							$"{ConvertTextToHtmlParseMode(lastname)}</a>";
			}

			return mention;
		}

		public static string ConvertTextToHtmlParseMode(string text)
		{
			text = text.Replace("<", "");
			text = text.Replace(">", "");
			text = text.Replace("&", "&amp;");

			return text;
		}
	}
}
