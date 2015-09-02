// -----------------------------------------------------------------------------
//  <copyright file="VectorTestFixture.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.MathLib.Tests
{
    using System;
    using NUnit.Framework;

    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class VectorTestFixture
    {
        private Vector SUT;

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Create_ShouldThrowInvalidOperationException_When_SuppliedParametersDoNotMatchProvideidLength()
        {
            const int rspace = 3;
            double[] values = new double[5];

            SUT = Vector.Create(rspace, values);
        }

        [Test]
        public void Length_ShouldReturnTheLengthOfAVector()
        {
            Vector a = Vector.Create(2, new[] { 4.0, 3.0 });

            double actual = a.Length;
            const double expected = 5.0;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Dot_ShouldReturnTheDotProduct()
        {
            var a = Vector.Create(2, new[] { 4.0, 3.0 });
            var b = Vector.Create(2, new[] { 2.0, 5.0 });

            double result = a * b;
            const double expected = 23.0;

            Assert.That(result, Is.EqualTo(expected));
        }
    }

    // ReSharper enable InconsistentNaming
}
