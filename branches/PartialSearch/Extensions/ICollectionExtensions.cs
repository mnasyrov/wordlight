using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Extensions
{
    public static class ICollectionExtensions
    {
        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static bool IsNotNullAndEmpty<T>(this ICollection<T> collection)
        {
            return collection != null && collection.Count > 0;
        }
    }
}
