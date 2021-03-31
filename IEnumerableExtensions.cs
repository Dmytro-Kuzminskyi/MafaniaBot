using System;
using System.Linq;
using System.Collections.Generic;

public static class IEnumerableExtensions
{

    public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
    {
        float totalWeight = sequence.Sum(weightSelector);
        float itemWeightIndex = (float)(new Random().NextDouble() * totalWeight);
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