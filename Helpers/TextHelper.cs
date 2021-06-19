using System;
using System.Reflection;
using System.Collections.Generic;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot.Helpers
{
	public static class TextHelper
	{
		public static InlineKeyboardMarkup CreateInlineKeyboard(List<KeyValuePair<string, string>> keyboardData, int rowSize, string property)
		{
			int i = 0;
			int k = 0;
			double div = (double)keyboardData.Count / (double)rowSize;
			int rows = int.Parse(Math.Ceiling(div).ToString());
			var keyboardInline = new InlineKeyboardButton[rows][];

			while (i < keyboardData.Count)
			{
				var keyboardButtons = new InlineKeyboardButton[rowSize];

				for (int j = 0; j < rowSize; j++)
				{
					if (i == keyboardData.Count)
					{
						int l = 0;
						for (int m = rowSize - 1; m > 0; m--)
						{
							if (keyboardButtons[m] == null)
								l++;
						}
						InlineKeyboardButton[] keyboardButtonsTemp = keyboardButtons;
						keyboardButtons = new InlineKeyboardButton[rowSize - l];
						for (int m = 0; m < rowSize - l; m++)
							keyboardButtons[m] = keyboardButtonsTemp[m];
						break;
					}
					keyboardButtons[j] = new InlineKeyboardButton
					{
						Text = keyboardData[i].Key
					};
					PropertyInfo pi = keyboardButtons[j].GetType().GetProperty(property);
					pi.SetValue(keyboardButtons[j], keyboardData[i].Value);
					i++;
				}
				keyboardInline[k++] = keyboardButtons;
			}
			return new InlineKeyboardMarkup(keyboardInline);
		}

		public static string GenerateMention(int userId, string firstname, string lastname, ParseMode parseMode = ParseMode.Html)
		{
			string mention = null;

			if (parseMode == ParseMode.Html)
			{
				mention = lastname != null ?
						$"<a href=\"tg://user?id={userId}\">{ConvertTextToHtmlParseMode(firstname)} " +
						$"{ConvertTextToHtmlParseMode(lastname)}</a>" :
						$"<a href=\"tg://user?id={userId}\">{ConvertTextToHtmlParseMode(firstname)}</a>";
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
			return null;
		}
	}
}
