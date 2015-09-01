// -----------------------------------------------------------------------------
//  <copyright file="Vector.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis
{
    using System;

    // ReSharper disable LoopCanBeConvertedToQuery
    // ReSharper disable ForCanBeConvertedToForeach

    /// <summary>
    /// Represents an n-dimensional Vector for R(n) space.
    /// </summary>
    public class Vector
    {
        private readonly int _rspace;
        private readonly double[] _digits;

        protected Vector(int rspace)
        {
            _rspace = rspace;
            _digits = new double[rspace];
        }

        /// <summary>
        /// Returns the dimension of the current vector.
        /// </summary>
        public int Dimension
        {
            get { return _rspace; }
        }

        /// <summary>
        /// Returns the length (magnitude) of the current vector beign represented.
        /// </summary>
        public double Length
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < _digits.Length; ++i)
                {
                    sum = sum + Math.Pow(_digits[i], 2);
                }

                return Math.Sqrt(sum);
            }
        }

        #region Operators - Vector Operations

        /// <summary>
        /// Vector multiplied by a scalar.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector operator *(Vector vector, double scalar)
        {
            throw new NotImplementedException();
        }

        public static double operator *(Vector a, Vector b)
        {
            return Dot(a, b);
        }

        public static Vector operator %(Vector a, Vector b)
        {
            return Cross(a, b);
        }

        #endregion
        
        #region Vector Operations

        public static double Dot(Vector a, Vector b)
        {
            if (a._rspace != b._rspace)
            {
                throw new ArgumentException("The specified vectors are not in the same space.");
            }

            throw new NotImplementedException();
        }

        public static Vector Cross(Vector a, Vector b)
        {
            if (a._rspace != b._rspace)
            {
                throw new ArgumentException("The specified vectors are not in the same space.");
            }

            throw new NotImplementedException();
        }

        #endregion

        #region Factory methods

        public static Vector Create(int rspace, params double[] digits)
        {
            return Create<Vector>(rspace, digits);
        }

        public static TVector Create<TVector>(int rspace, params double[] digits) where TVector : Vector
        {
            var result = new Vector(rspace);

            if (digits == null
                || digits.Length != rspace)
            {
                throw new InvalidOperationException("Unequal amount of digits when creating a vector.");
            }

            for (int i = 0; i < rspace; ++i)
            {
                result._digits[i] = digits[i];
            }

            return (TVector)result;
        }

        // TODO: Add "FromPoints" factory method.

        #endregion
    }
}
