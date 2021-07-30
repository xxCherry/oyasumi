using System;
using System.Collections.Generic;

namespace oyasumi.Extensions
{
    public static class ArrayExtensions
    {
        public static void RemoveAll<T>(ref T[] array, Predicate<T> filter) where T : IComparable
        {
            var newArray = new T[array.Length];
            var index = 0;

            foreach (var item in array)
            {
                if (!filter(item))
                {
                    newArray[index] = item;
                    index++;
                }
            }

            Array.Resize(ref newArray, index);
            array = newArray;
        }
    }
}