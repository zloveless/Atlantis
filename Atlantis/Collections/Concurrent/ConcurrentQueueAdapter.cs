// -----------------------------------------------------------------------------
//  <copyright file="ConcurrentQueueAdapter.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Collections.Concurrent
{
	using System.Collections.Concurrent;
	using System.Collections.Generic;

	/// <summary>
	///     Represents a thread-safe first in-first out (FIFO) collection. Encapsulates a ConcurrentQueue&lt;String&gt;
	/// </summary>
	public class ConcurrentQueueAdapter<T> : IQueue<T> where T : class
	{
		private readonly ConcurrentQueue<T> queue;

		public ConcurrentQueueAdapter()
		{
			queue = new ConcurrentQueue<T>();
		}

		public ConcurrentQueueAdapter(IEnumerable<T> items)
		{
			queue = new ConcurrentQueue<T>(items);
		}

		#region Implementation of IQueue<T>

		public int Count
		{
			get { return queue.Count; }
		}

		public T Peek()
		{
			T item;
			return queue.TryPeek(out item) ? item : null;
		}

		public T Pop()
		{
			T result;

			return queue.TryDequeue(out result) ? result : null;
		}

		public void Push(T item)
		{
			queue.Enqueue(item);
		}

		#endregion
	}
}
