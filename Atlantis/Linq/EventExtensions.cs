// -----------------------------------------------------------------------------
//  <copyright file="EventExtensions.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------
namespace Atlantis.Linq
{
	using System;

	public static partial class Extensions
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void Raise(this EventHandler source, object sender, EventArgs args)
		{
			if (source != null) source(sender, args);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TEventArgs"></typeparam>
		/// <param name="source"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void Raise<TEventArgs>(this EventHandler<TEventArgs> source, object sender, TEventArgs args) where TEventArgs : EventArgs
		{
			if (source != null) source(sender, args);
		}
	}
}
