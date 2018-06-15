// -----------------------------------------------------------------------------
//  <copyright file="MultiLog.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.IO
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class MultiLog : ILog
	{
		private readonly ILog[] logs;

		public MultiLog(params ILog[] logs)
		{
			this.logs = logs;
		}

		public MultiLog(IEnumerable<ILog> logs)
		{
			this.logs = logs.ToArray();
		}

		#region Implementation of IDisposable

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			foreach (ILog item in logs)
			{
				item.Dispose();
			}
		}

		#endregion

		#region Implementation of ILog

		public LogThreshold Threshold
		{
			get { throw new NotSupportedException(); }
			set
			{
				foreach (ILog item in logs)
				{
					item.Threshold = value;
				}
			}
		}

		public bool PrefixLog
		{
			get { throw new NotSupportedException(); }
			set
			{
				foreach (ILog item in logs)
				{
					item.PrefixLog = value;
				}
			}
		}

		public string Prefix
		{
			get { throw new NotSupportedException(); }
			set
			{
				foreach (ILog item in logs)
				{
					item.Prefix = value;
				}
			}
		}

		public void Debug(string message)
		{
			foreach (ILog item in logs)
			{
				item.Debug(message);
			}
		}

		public void DebugFormat(string format, params object[] args)
		{
			foreach (ILog item in logs)
			{
				item.DebugFormat(format, args);
			}
		}

		public void Error(string message)
		{
			foreach (ILog item in logs)
			{
				item.Error(message);
			}
		}

		public void ErrorFormat(string format, params object[] args)
		{
			foreach (ILog item in logs)
			{
				item.ErrorFormat(format, args);
			}
		}

		public void Fatal(string message)
		{
			foreach (ILog item in logs)
			{
				item.Fatal(message);
			}
		}

		public void FatalFormat(string format, params object[] args)
		{
			foreach (ILog item in logs)
			{
				item.FatalFormat(format, args);
			}
		}

		public void Info(string message)
		{
			foreach (ILog item in logs)
			{
				item.Info(message);
			}
		}

		public void InfoFormat(string format, params object[] args)
		{
			foreach (ILog item in logs)
			{
				item.InfoFormat(format, args);
			}
		}

		public void Warn(string message)
		{
			foreach (ILog item in logs)
			{
				item.Warn(message);
			}
		}

		public void WarnFormat(string format, params object[] args)
		{
			foreach (ILog item in logs)
			{
				item.WarnFormat(format, args);
			}
		}

		#endregion
	}
}
