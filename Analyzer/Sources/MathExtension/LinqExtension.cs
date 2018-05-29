using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace MathExtension
{
    public static class LinqExtension
    {
        public static (int max_index, T max_value) MaxWithIndex<T>(this IEnumerable<T> enumerable) where T: IComparable<T>
        { 
            if(!enumerable.Any()) throw new ArgumentException("the enumerable in argument should have at least one item");

            var aggregatedMax = enumerable.Skip(1).Aggregate((max_index: 0, actual_index: 0, max_value: enumerable.First()),
                (max, v) => v.CompareTo(max.max_value) > 0
                    ? (max.actual_index + 1, max.actual_index + 1, v)
                    : (max.max_index, max.actual_index + 1, max.max_value));

            return (aggregatedMax.max_index, aggregatedMax.max_value);
        }

        public static (int min_index, T min_value) MinWithIndex<T>(this IEnumerable<T> enumerable) where T : IComparable<T>
        {
            if (!enumerable.Any()) throw new ArgumentException("the enumerable in argument should have at least one item");

            var aggregatedMin = enumerable.Skip(1).Aggregate((min_index: 0, actual_index: 0, min_value: enumerable.First()),
                (min, v) => v.CompareTo(min.min_value) < 0
                    ? (min.actual_index + 1, min.actual_index + 1, v)
                    : (min.min_index, min.actual_index + 1, min.min_value));

            return (aggregatedMin.min_index, aggregatedMin.min_value);
        }
    }
}
