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
        #region Constants

        /// <summary>
        /// Returns the position of the X component.
        /// </summary>
        public const int X = 0;

        /// <summary>
        /// Returns the position of the Y component.
        /// </summary>
        public const int Y = 1;

        /// <summary>
        /// Returns the position of the Z component.
        /// </summary>
        public const int Z = 2;

        #endregion
        
        private readonly int _rspace;
        private double[] _digits;

        protected Vector(int rspace)
        {
            _rspace = rspace;
            _digits = new double[rspace];
        }

        /// <summary>
        /// Gets a component of the vector. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public double this[int index]
        {
            get
            {
                if (index - 1 > _rspace)
                {
                    throw new ArgumentOutOfRangeException("index", "The specified index was beyond the space in which the vector resides.");
                }

                return _digits[index];
            }
        }

        #region Properties

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
                for (int i = 0; i < _rspace; ++i)
                {
                    sum = sum + Math.Pow(_digits[i], 2);
                }

                return Math.Sqrt(sum);
            }
        }

        #endregion
        
        #region Operators - Vector Operations

        /// <summary>
        /// Vector multiplied by a scalar.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Vector operator *(Vector vector, double scalar)
        {
            var result = new Vector(vector._rspace);
            var digits = new double[vector._digits.Length];

            for (int i = 0; i < vector._rspace; ++i)
            {
                digits[i] = vector._digits[i] * scalar;
            }

            return result;
        }

        /// <summary>
        /// Vector dot product Vector
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double operator *(Vector a, Vector b)
        {
            return Dot(a, b);
        }

        /// <summary>
        /// Vector x Vector
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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

            double result = 0.0;
            for (int i = 0; i < a._rspace; ++i)
            {
                result = result + (a._digits[i] * b._digits[i]);
            }

            return result;
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

            result._digits = digits;
            return (TVector)result;
        }

        #endregion
    }
}
