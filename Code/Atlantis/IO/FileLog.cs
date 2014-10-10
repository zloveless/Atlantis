// -----------------------------------------------------------------------------
//  <copyright file="FileLog.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.IO
{
	using System;
	using System.IO;
	using System.Text;

	public class FileLog : LogBaseClass
	{
		private readonly String fileName;
		private readonly Encoding encoding;

		public FileLog(String fileName) : this(fileName, new UTF8Encoding(false))
		{
		}

		public FileLog(String fileName, Encoding encoding)
		{
			this.fileName = fileName;
			this.encoding = encoding;
			stream        = new FileStream(fileName, FileMode.Append, FileAccess.Write);
		}
	}
}
