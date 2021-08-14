using System;
using System.Collections.Generic;
using System.Linq;

namespace MafaniaBot.Extensions
{
    public static class IEnumerableExtensions
    {
        private static readonly Random rnd = new Random();

        public static T RandomElement<T>(this IEnumerable<T> sequence)
        {
            return sequence.ElementAtOrDefault(rnd.Next(sequence.Count()));
        }

        public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
        {
            float totalWeight = sequence.Sum(weightSelector);
            float itemWeightIndex = (float)(rnd.NextDouble() * totalWeight);
            float currentWeightIndex = 0;

            foreach (var item in from weightedItem in sequence
                                 select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;
                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;
            }

            return default(T);
        }
    }
}
