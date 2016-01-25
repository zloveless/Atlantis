// -----------------------------------------------------------------------------
//  <copyright file="EnumerableExtensions.cs" company="MultiPlayerForums.com">
//      Copyright (c) MultiPlayerForums.com All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Linq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtensions
    {
        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool None<TSource>(this IEnumerable<TSource> source)
        {
            return !source.Any();
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return !source.Any(predicate);
        }
    }
}
