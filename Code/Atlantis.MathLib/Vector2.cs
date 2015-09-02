// -----------------------------------------------------------------------------
//  <copyright file="Vector2.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis
{
    using System;

    /// <summary>
    /// Represents a vector in 2-D space.
    /// </summary>
    public class Vector2 : Vector, IEquatable<Vector2>
    {
        public Vector2() : base(rspace: 2)
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

        #region Implementation of IEquatable<Vector2>

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Vector2 other)
        {
            return false;
        }

        #endregion

        #region Factory Methods

        public static Vector2 FromPoints(Tuple<double, double> a, Tuple<double, double> b)
        {
            return Create<Vector2>(2, b.Item1 - a.Item1, b.Item2 - a.Item2);
        }

        #endregion

    }
}
