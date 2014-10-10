// -----------------------------------------------------------------------------
//  <copyright file="IQueue.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Collections
{
	public interface IQueue<T> where T : class
	{
		int Count { get; }

		T Peek();

		T Pop();

		void Push(T item);
	}
}
