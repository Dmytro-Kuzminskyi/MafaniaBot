using System;
using System.Reflection;
using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace MafaniaBot
{
	public static class Helpers
	{
		public static InlineKeyboardMarkup GetInlineKeyboard(List<KeyValuePair<string, string>> keyboardData, int rowSize, string property)
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
	}
}
