// -----------------------------------------------------------------------------
//  <copyright file="ArrayExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Extensions
{
    using System;

    public static class ArrayExtensions
    {
        /// <summary>
        /// Converts two single-dimensional arrays into a two-dimensional matrix.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static T[,] To2DMatrix<T>(this T[] left, T[] right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            if (left.Length != right.Length) throw new ArgumentException("Input arrays are not of equal length.");

            var result = new T[2, left.Length];

            /*for (int a = 0; a < 2; ++a)
            {
                for (int b = 0; b < left.Length; ++b)
                {
                    //result[a, b] = 
                }
            }*/

            return result;
        }
    }
}
