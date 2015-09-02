// -----------------------------------------------------------------------------
//  <copyright file="Vector3.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis
{
    using System;

    /// <summary>
    ///     Represents a vector in 3-D space.
    /// </summary>
    public class Vector3 : Vector, IEquatable<Vector3>
    {
        public Vector3() : base(rspace: 3)
        {
        }

        public new double X
        {
            get { return this[Vector.X]; }
        }

        public new double Y
        {
            get { return this[Vector.Y]; }
        }

        public new double Z
        {
            get { return this[Vector.Z]; }
        }

        #region Operators

        public static double operator *(Vector3 a, Vector3 b)
        {
            return Dot(a, b);
        }

        public static Vector3 operator %(Vector3 a, Vector3 b)
        {
            throw new NotImplementedException();

            var result = Cross(a, b);
            return (Vector3)result;
        }

        #endregion

        #region Implementation of IEquatable<Vector3>

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Vector3 other)
        {
            //return X == other.X && Y == other.Y && Z == other.Z;
            return false;
        }

        #endregion

        #region Factory Methods

        public static Vector3 FromPoints(Tuple<double, double, double> a, Tuple<double, double, double> b)
        {
            return Create<Vector3>(3, b.Item1 - a.Item1, b.Item2 - a.Item2, b.Item3 - a.Item3);
        }

        #endregion
    }
}
