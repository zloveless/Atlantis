// -----------------------------------------------------------------------------
//  <copyright file="LogBaseClass.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.IO
{
	using System;
	using System.IO;
	using System.Text;

	public abstract class LogBaseClass : ILog
	{
		protected Stream stream;
	    protected Encoding _encoding = Encoding.UTF8;

		#region Methods

		protected virtual StringBuilder BuildLogMessage(LogThreshold threshold, string format, params object[] args)
		{
			var builder = new StringBuilder();

			if (PrefixLog)
			{
				builder.Append(threshold.ToString().ToUpper());

				if (!string.IsNullOrEmpty(Prefix))
				{
					builder.Append(" ");
					builder.Append(Prefix);
				}
				else
				{
					builder.Append(" ");
					builder.Append(DateTime.Now.ToString("g"));
				}

				builder.Append(" ");
			}

			builder.AppendFormat(format, args);
			builder.Append('\n');

			return builder;
		}

		protected virtual void Write(LogThreshold threshold, string format, params object[] args)
		{
			if (Threshold.HasFlag(threshold))
			{
				var message = BuildLogMessage(threshold, format, args);

				var buf = _encoding.GetBytes(message.ToString());
				stream.Write(buf, 0, buf.Length);
				stream.Flush();
			}
		}

		#endregion

		#region Implementation of IDisposable

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing) return;

			if (stream != null) stream.Dispose();
		}

		#endregion
		
		#region Implementation of ILog

		public LogThreshold Threshold { get; set; }

		public bool PrefixLog { get; set; }

		public string Prefix { get; set; }
		
		public virtual void Debug(string message)
		{
			Write(LogThreshold.Debug, message);
		}

		public virtual void DebugFormat(string format, params object[] args)
		{
			Write(LogThreshold.Debug, format, args);
		}

		public virtual void Error(string message)
		{
			Write(LogThreshold.Error, message);
		}

		public virtual void ErrorFormat(string format, params object[] args)
		{
			Write(LogThreshold.Error, format, args);
		}

		public virtual void Fatal(string message)
		{
			Write(LogThreshold.Fatal, message);
		}

		public void FatalFormat(string format, params object[] args)
		{
			Write(LogThreshold.Fatal, format, args);
		}

		public virtual void Info(string message)
		{
			Write(LogThreshold.Info, message);
		}

		public virtual void InfoFormat(string format, params object[] args)
		{
			Write(LogThreshold.Info, format, args);
		}

		public virtual void Warn(string message)
		{
			Write(LogThreshold.Warning, message);
		}

		public virtual void WarnFormat(string format, params object[] args)
		{
			Write(LogThreshold.Warning, format, args);
		}

		#endregion
	}
}
