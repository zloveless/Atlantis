// -----------------------------------------------------------------------------
//  <copyright file="QueueAdapter.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Collections
{
	using System.Collections.Generic;

	public class QueueAdapter<T> : IQueue<T> where T : class
	{
		public Queue<T> queue;

		public QueueAdapter()
		{
			queue = new Queue<T>();
		}

		public QueueAdapter(IEnumerable<T> items)
		{
			queue = new Queue<T>(items);
		}

		#region Implementation of IQueue<T>

		public int Count
		{
			get { return queue.Count; }
		}

		public T Peek()
		{
			return queue.Peek();
		}

		public T Pop()
		{
			return queue.Dequeue();
		}

		public void Push(T item)
		{
			queue.Enqueue(item);
		}

		#endregion
	}
}
