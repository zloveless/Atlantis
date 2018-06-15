// -----------------------------------------------------------------------------
//  <copyright file="ModeStringTestFixture.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace IrcClientDaemon.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using NUnit.Framework;

	// ReSharper disable InconsistentNaming
	// ReSharper disable PossibleNullReferenceException

	#region External type: ModeType

	public enum ModeType
	{
		/// <summary>
		/// Channel mode has many separate parameters which can be by requesting +[x] where [x] is the mode with no parameters.
		/// </summary>
		LIST,

		/// <summary>
		/// Channel mode that always takes a parameter, regardless whether it's set or unset.
		/// </summary>
		SETUNSET,

		/// <summary>
		/// Channel mode that requires a parameter only when being set, otherwise has no parameter.
		/// </summary>
		SET,

		/// <summary>
		/// Channel mode should never have a parameter associated with it.
		/// </summary>
		NOPARAM,

		/// <summary>
		/// Mode that grants a user access on a channel. The associated prefix should be stored with the user, not the channel.
		/// </summary>
		ACCESS,

		/// <summary>
		/// Generic mode representing a user mode that should be stored for the IrcClient (ourselves). Occurs when target and source are the same value.
		/// </summary>
		USER
	}

	#endregion

	#region External type: GenericMode

	public class GenericMode
	{
		public char Mode { get; set; }

		public String Parameter { get; set; }

		public bool IsSet { get; set; }

		public String Setter { get; set; }

		public String Target { get; set; }

		public ModeType Type { get; set; }
	}

	#endregion

	[TestFixture]
	public class ModeStringTestFixture
	{
		internal class ServerInfo
		{
			public readonly String ListModes = "beI";
			public readonly String ModesWithNoParameter = "ABCDGKMNOPQRSTcimnprstuz";
			public readonly String ModesWithParameter = "k";
			public readonly String ModesWithParameterWhenSet = "FHJLdfjl";
			public readonly String PrefixModes = "Yqaovh";
		}

		private readonly ServerInfo info = new ServerInfo();

		protected IEnumerable<GenericMode> ParseModes(String modestr, params String[] parameters)
		{
			bool set = false;
			for (int i = 0; i < modestr.Length; ++i)
			{
				if (modestr[i] == '+') set = true;
				else if (modestr[i] == '-') set = false;
				else if (info.ListModes.Contains(modestr[i]))
				{ // List modes always require a parameter.
					yield return new GenericMode {Mode = modestr[i], IsSet = set, Parameter = parameters[i - 1], Type = ModeType.LIST};
				}
				else if (info.ModesWithParameter.Contains(modestr[i]))
				{ // Modes that always take a parameter, regardless.
					yield return new GenericMode {Mode = modestr[i], IsSet = set, Type = ModeType.SETUNSET};
				}
				else if (info.ModesWithParameterWhenSet.Contains(modestr[i]))
				{ // Modes that only take a parameter when being set.
					yield return new GenericMode
					       {
						       Mode = modestr[i],
						       IsSet = set,
						       Parameter = set ? parameters[i - 1] : null,
						       Type = ModeType.SET
					       };
				}
				else if (info.ModesWithNoParameter.Contains(modestr[i]))
				{ // Modes that never take a parameter.
					yield return new GenericMode {Mode = modestr[i], IsSet = set, Type = ModeType.NOPARAM};
				}
				else if (info.PrefixModes.Contains(modestr[i]))
				{ // Modes that indicate access on a channel.
					yield return new GenericMode {Mode = modestr[i], IsSet = set, Parameter = parameters[i - 1], Type = ModeType.ACCESS};
				}
			}
		}

		[Test]
		public void Test1()
		{
			const String modestr = "+mk";
			String[] parameters = {"foo"};

			// Simulate MODE #target +mk foo (moderate channel with a password "foo")

			// Act
			var modes = ParseModes(modestr, parameters);

			// Assert
			var modearr = modes.ToArray();
			Assert.IsNotEmpty(modestr);
			Assert.That(modearr.Count() == 2);
			Assert.That(modearr.Count(x => x.IsSet) == 2); // The modestr is "+mk" meaning both "m" and "k" are being set.
			//Assert.That(modearr.Count(x => x.Type == ModeType.NOPARAM) == 1); // +m doesn't take any parameters
			//Assert.That(modearr.Count(x => x.Type == ModeType.SETUNSET) == 1); // +k requires a parameter.
		}
	}

	// ReSharper enable InconsistentNaming
	// ReSharper enable PossibleNullReferenceException
}
