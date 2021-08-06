using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MafaniaBot.Dictionaries
{
	public static class GameDictionary
	{
		public static readonly IReadOnlyDictionary<string, float> LetterWeights = new ReadOnlyDictionary<string, float>(new Dictionary<string, float>
		{
			{ "А", 0.0952f },
			{ "Б", 0.0161f },
			{ "В", 0.0371f },
			{ "Г", 0.0149f },
			{ "Д", 0.0239f },
			{ "Е", 0.0855f },
			{ "Ж", 0.0066f },
			{ "З", 0.0169f },
			{ "И", 0.0834f },
			{ "Й", 0.0057f },
			{ "К", 0.0496f },
			{ "Л", 0.0431f },
			{ "М", 0.0246f },
			{ "Н", 0.0706f },
			{ "О", 0.0966f },
			{ "П", 0.0316f },
			{ "Р", 0.0618f },
			{ "С", 0.05f },
			{ "Т", 0.061f },
			{ "У", 0.0206f },
			{ "Ф", 0.0068f },
			{ "Х", 0.0064f },
			{ "Ц", 0.0125f },
			{ "Ч", 0.0131f },
			{ "Ш", 0.008f },
			{ "Щ", 0.0055f },
			{ "Ы", 0.0114f },
			{ "Ь", 0.0209f },
			{ "Э", 0.0024f },
			{ "Ю", 0.0027f },
			{ "Я", 0.0141f }
		});

		public static readonly IReadOnlyDictionary<int, int> ScoreConversion = new ReadOnlyDictionary<int, int>(new Dictionary<int, int>
		{
			{ 2, 1 },
			{ 3, 2 },
			{ 4, 4 },
			{ 5, 5 },
			{ 6, 7 },
			{ 7, 8 },
			{ 8, 10 },
			{ 9, 11 },
			{ 10, 13 },
			{ 11, 14 },
			{ 12, 16 },
			{ 13, 17 },
			{ 14, 18 },
			{ 15, 20 }
		});
	}
}
