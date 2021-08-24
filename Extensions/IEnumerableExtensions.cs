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
    }
}
