using MafaniaBot.Models;
using System.Collections.Generic;

namespace MafaniaBot.Helpers
{
    public static class GameHelper
    {
		public static string GenerateWordsGameBoard(WordsGame game)
		{
			List<string> p1Field = game.FirstPlayerField;
			List<string> p2Field = game.SecondPlayerField;
			int p1Score = game.FirstPlayerScore;
			int p2Score = game.SecondPlayerScore;
			int x = game.X;
			int y = game.Y;
			string output = null;
			int posP1 = 0;
			int posP2 = 0;
			int gap = 23;
			gap -= (p1Score.ToString().Length - 1) * 2;
			output += $"<b>Счет: {p1Score}";

			for (int i = 0; i < gap; i++)
			{
				output += " ";
			}

			output += $"Счет: {p2Score}</b>\n";
			output += "<pre>";

			for (int i = 0; i < y; i++)
			{
				for (int j = 0; j < x; j++)
				{
					output += p1Field[posP1++] + " ";
				}
				output += "    ";
				for (int j = 0; j < x; j++)
				{
					output += p2Field[posP2++] + " ";
				}
				output += "\n";
			}

			output += "</pre>\n";
			return output;
		}
	}
}
